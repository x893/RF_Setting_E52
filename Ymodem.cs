using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace loramesh
{
	public enum YModemType
	{
		YModem,
		YModem_1K,
	}

	public class Ymodem
	{
		private readonly SerialPort serialPort = new SerialPort();
		private string _path;
		private string _portName;
		private int _baudRate;
		private bool _fileDownloadStop = false;

		public event EventHandler NowDownloadProgressEvent;

		public event EventHandler DownloadResultEvent;

		public string Path
		{
			get => _path;
			set => _path = value;
		}

		public string PortName
		{
			get => _portName;
			set => _portName = value;
		}

		public int BaudRate
		{
			get => _baudRate;
			set => _baudRate = value;
		}

		public void YmodemUploadFileStop() => _fileDownloadStop = true;

		public void YmodemUploadFile(YModemType yModemType, int waitFirstPacketAckMs)
		{
			int packet_length = 0;
			int packet_number = 0;
			byte head = 0;
			bool complete = false;
			bool check_response = true;

			switch (yModemType)
			{
				case YModemType.YModem:
					packet_length = 128;
					break;
				case YModemType.YModem_1K:
					packet_length = 1024;
					break;
			}

			byte[] packet_data = new byte[packet_length];
			FileStream fileStream = new FileStream(_path, FileMode.Open, FileAccess.Read);
			serialPort.PortName = _portName;
			serialPort.BaudRate = _baudRate;
			serialPort.ReadTimeout = 3000;
			serialPort.Open();

			try
			{
				if (SerialPortWaitReadEx(67))
				{
					SendYmodemInitialPacket(1, packet_number, packet_data, 128, _path, fileStream);
					if (waitFirstPacketAckMs > 0)
						Thread.Sleep(waitFirstPacketAckMs);
					if (SerialPortWaitRead(6))
					{
						if (SerialPortWaitRead(67))
						{
							int bytesToSend;
							do
							{
								bytesToSend = fileStream.Read(packet_data, 0, packet_length);
								XOR_Out(ref packet_data);
								if (bytesToSend != 0)
								{
									switch (yModemType)
									{
										case YModemType.YModem:
											if (bytesToSend < 128)
											{
												for (int index = bytesToSend; index < 128; ++index)
													packet_data[index] = (byte)26;
											}
											head = 1;
											break;
										case YModemType.YModem_1K:
											if (bytesToSend < 128)
											{
												for (int index = bytesToSend; index < 128; ++index)
													packet_data[index] = 26;
												packet_length = 128;
												head = 1;
											}
											else if (bytesToSend < 1024)
											{
												for (int index = bytesToSend; index < packet_length; ++index)
													packet_data[index] = 26;
												packet_length = 1024;
												head = 2;
											}
											else
												head = 2;
											break;
									}
									++packet_number;
									SendYmodemPacket(head, packet_number, packet_data, packet_length);
									if (SerialPortWaitRead(6))
									{
										if (!_fileDownloadStop)
										{
											int sender = (int)(packet_length * packet_number / fileStream.Length * 100.0);
											if (sender > 100)
												sender = 100;
											NowDownloadProgressEvent(sender, new EventArgs());
										}
										else
										{
											check_response = false;
											break;
										}
									}
									else
									{
										check_response = false;
										break;
									}
								}
								else
									break;
							} while (packet_length == bytesToSend);

							if (check_response)
							{
								serialPort.Write(new byte[] { 4 }, 0, 1);
								if (!SerialPortWaitRead(6))
								{
									serialPort.Write(new byte[] { 4 }, 0, 1);
									if (!SerialPortWaitRead(6))
										check_response = false;
								}

								if (check_response)
								{
									packet_data = new byte[128];
									Thread.Sleep(5);
									serialPort.DiscardInBuffer();
									SendYmodemPacket(1, 0, packet_data, packet_data.Length);
									if (SerialPortWaitRead1(6))
										complete = true;
								}
							}
						}
					}
				}
			}
			catch { }

			if (_fileDownloadStop)
			{
				byte[] buffer = new byte[]
				{
					24,
					24,
					24,
					24,
					24
				};
				serialPort.Write(buffer, 0, buffer.Length);
				DownloadResultEvent(false, new EventArgs());
			}
			else if (complete)
				DownloadResultEvent(true, new EventArgs());
			else
				DownloadResultEvent(false, new EventArgs());

			try
			{
				fileStream.Close();
				serialPort.Close();
			}
			catch { }
		}

		private void XOR_Out(ref byte[] data)
		{
			byte[] bytes = Encoding.Unicode.GetBytes("ebyteebyteebyteebyteebyteebyteebyteebyteebyteebyteebyteebyteebyteebyteebyteebyte");
			for (int index = 0; index < data.Length; ++index)
				data[index] = (byte)(data[index] ^ bytes[index % 128]);
		}

		private void SendYmodemInitialPacket(
			byte head,
			int packetNumber,
			byte[] data,
			int dataSize,
			string path,
			FileStream fileStream
			)
		{
			string fileName = System.IO.Path.GetFileName(path);
			string file_length = fileStream.Length.ToString();

			int index1;
			for (index1 = 0; index1 < fileName.Length && fileName.ToCharArray()[index1] > char.MinValue; ++index1)
				data[index1] = (byte)fileName.ToCharArray()[index1];
			data[index1] = 0;

			int index2;
			for (index2 = 0; index2 < file_length.Length && file_length.ToCharArray()[index2] > char.MinValue; ++index2)
				data[index1 + 1 + index2] = (byte)file_length.ToCharArray()[index2];
			data[index1 + 1 + index2] = 0;

			for (int index3 = index1 + 1 + index2 + 1; index3 < dataSize; ++index3)
				data[index3] = 0;

			SendYmodemPacket(head, packetNumber, data, dataSize);
		}

		private void SendYmodemPacket(byte head, int packetNumber, byte[] data, int dataSize)
		{
			byte[] numArray = new byte[3 + dataSize + 2];
			numArray[0] = head;
			numArray[1] = Convert.ToByte(packetNumber & byte.MaxValue);
			numArray[2] = Convert.ToByte(~numArray[1] & byte.MaxValue);
			Array.Copy(data, 0, numArray, 3, dataSize);
			ushort checksum = new Crc16Ccitt(InitialCrcValue.Zeros).ComputeChecksum(data, dataSize);
			numArray[3 + dataSize] = (byte)(checksum / 256U);
			numArray[3 + dataSize + 1] = (byte)(checksum % 256U);
			serialPort.Write(numArray, 0, 3 + dataSize + 2);
		}

		private bool SerialPortWaitRead(byte data)
		{
			try
			{
				if (serialPort.ReadByte() == data)
					return true;
			}
			catch { }
			return false;
		}

		private bool SerialPortWaitRead1(byte data)
		{
			try
			{
				byte m = (byte)serialPort.ReadByte();
				if (m == data)
					return true;
			}
			catch { }
			return false;
		}

		public static long GetMsTimeStamp()
		{
			return Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds);
		}

		private bool SerialPortWaitReadEx(byte data)
		{
			long num = GetMsTimeStamp() + (long)serialPort.ReadTimeout;
			for (long msTimeStamp = GetMsTimeStamp(); msTimeStamp <= num; msTimeStamp = GetMsTimeStamp())
			{
				try
				{
					if (serialPort.ReadByte() == (int)data)
						return true;
				}
				catch { }
			}
			return false;
		}
	}
}
