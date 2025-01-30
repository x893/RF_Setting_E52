using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace loramesh
{
	public class RF_Setting : Form
	{
		private delegate void READDATE(byte[] readdate);
		private delegate void DownloadFinish(bool finish);

		private readonly List<CMD.CMD_ATT> att_list = new List<CMD.CMD_ATT>();
		private readonly CMD cmd = new CMD();
		private readonly int[] baudsend = new int[]
		{
			500,
			260,
			140,
			80,
			40,
			30,
			20,
			20,
			20,
			20
		};

		private readonly int[] baudrec = new int[]
		{
			250,
			130,
			70,
			40,
			20,
			20,
			20,
			20,
			20,
			20
		};

		private int baudnow = 10;
		private readonly string[,] group_list = new string[8, 2];
		private bool uart_busy = false;
		private bool send_enable = true;
		private bool _loadUpdateFileStatus = false;
		private string _updatePath = "";

		private Thread _fileDownloadThread;
		private Ymodem ymodem = new Ymodem();
		private YModemType _yModemType = YModemType.YModem;

		public RF_Setting()
		{
			InitializeComponent();
		}

		private void Uart_Com_Click(object sender, EventArgs e)
		{
			string[] portNames = SerialPort.GetPortNames();
			Uart_Com.Items.Clear();
			for (int i = 0; i < portNames.Length; i++)
				Uart_Com.Items.Add(portNames[i]);
		}

		private void Uart_Bt_Click(object sender, EventArgs e)
		{
			try
			{
				if (Uart_Bt.Text == "Close Uart")
				{
					serialPort1.Close();
					Uart_Bt.Text = "Open Uart";
				}
				else
				{
					serialPort1.PortName = Uart_Com.Text;
					serialPort1.BaudRate = Convert.ToInt32(Uart_Stop_CB.Text);
					serialPort1.Parity = (Parity)Uart_Para_CB.SelectedIndex;
					serialPort1.Open();
					Uart_Bt.Text = "Close Uart";
					baudnow = Uart_Stop_CB.SelectedIndex;
					uart_busy = false;
				}
			}
			catch (Exception ex2)
			{
				Uart_Bt.Text = "Open Uart";
				serialPort1.Close();
				MessageBox.Show("Serial Open Failed！！！" + "\n" + ex2.Message);
			}
		}

		private void Combo_init()
		{
			foreach (CMD.CMD_ATT cmd_ATT in att_list)
			{
				if (cmd_ATT.cmd_type == 0)
				{
					foreach (CMD.CMD_CONTROL cmd_CONTROL in cmd_ATT.cmd_control)
						if (cmd_CONTROL.CMD_PARA_LIST != null)
						{
							cmd_CONTROL.COMBOBOX.Items.Clear();
							for (int i = 0; i < cmd_CONTROL.CMD_PARA_LIST.Length / 2; i++)
							{
								string[] array = cmd_CONTROL.CMD_PARA_LIST[i].Split(new char[] { '&' });
								cmd_CONTROL.COMBOBOX.Items.Add(array[0]);
							}
						}
				}
				else if (cmd_ATT.cmd_type == 1)
				{
					Tip.AutoPopDelay = 5000;
					Tip.InitialDelay = 50;
					Tip.ReshowDelay = 50;
					foreach (CMD.CMD_CONTROL cmd_CONTROL2 in cmd_ATT.cmd_control)
						if (cmd_CONTROL2.CMD_RANGE_LIST != null)
						{
							string caption = cmd_CONTROL2.CMD_RANGE_LIST.Replace("&", " --- ");
							Tip.SetToolTip(cmd_CONTROL2.CONTROL, caption);
						}
				}
			}
		}

		private void RF_Setting_Load(object sender, EventArgs e)
		{
			Cmd_init();
			Combo_init();
		}

		private void Cmd_init()
		{
			CMD.CMD_ATT cmd_ATT = new CMD.CMD_ATT
			{
				CMD = cmd.AT_POWER,
				cmd_type = 1,
				p = CMD.P.RW
			};

			CMD.CMD_CONTROL cmd_CONTROL = new CMD.CMD_CONTROL
			{
				CMD_RANGE_LIST = cmd.POWER_RANGE,
				CONTROL = power_txt,
				CMD_PARA_LIST = null,
				COMBOBOX = null
			};
			cmd_ATT.cmd_control.Add(cmd_CONTROL);

			CMD.CMD_CONTROL cmd_CONTROL2 = new CMD.CMD_CONTROL
			{
				CMD_RANGE_LIST = cmd.SAVE_CMD_RANGE,
				CONTROL = save_range
			};
			cmd_CONTROL.CMD_PARA_LIST = null;
			cmd_CONTROL.COMBOBOX = null;
			cmd_ATT.cmd_control.Add(cmd_CONTROL2);
			att_list.Add(cmd_ATT);

			CMD.CMD_ATT cmd_ATT2 = new CMD.CMD_ATT
			{
				CMD = cmd.AT_CHANNEL,
				cmd_type = 1,
				p = CMD.P.RW
			};
			CMD.CMD_CONTROL cmd_CONTROL3 = new CMD.CMD_CONTROL
			{
				CMD_RANGE_LIST = cmd.CHANNEL_RANGE,
				CONTROL = chan_txt,
				CMD_PARA_LIST = null,
				COMBOBOX = null
			};
			cmd_ATT2.cmd_control.Add(cmd_CONTROL3);
			cmd_ATT2.cmd_control.Add(cmd_CONTROL2);
			att_list.Add(cmd_ATT2);

			CMD.CMD_ATT cmd_ATT3 = new CMD.CMD_ATT
			{
				CMD = cmd.AT_RATE,
				cmd_type = 0,
				p = CMD.P.RW
			};

			CMD.CMD_CONTROL cmd_CONTROL4 = new CMD.CMD_CONTROL
			{
				CMD_RANGE_LIST = null,
				CONTROL = null,
				CMD_PARA_LIST = cmd.RATE_CMD_PARA,
				COMBOBOX = rate_cb
			};
			cmd_ATT3.cmd_control.Add(cmd_CONTROL4);
			att_list.Add(cmd_ATT3);

			CMD.CMD_ATT cmd_ATT4 = new CMD.CMD_ATT
			{
				CMD = cmd.AT_OPTION,
				cmd_type = 0,
				p = CMD.P.RW
			};

			CMD.CMD_CONTROL cmd_CONTROL5 = new CMD.CMD_CONTROL
			{
				CMD_RANGE_LIST = null,
				CONTROL = null,
				CMD_PARA_LIST = cmd.OPTION_CMD_PARA,
				COMBOBOX = option_cb
			};
			cmd_ATT4.cmd_control.Add(cmd_CONTROL5);
			cmd_ATT4.cmd_control.Add(cmd_CONTROL2);
			att_list.Add(cmd_ATT4);

			CMD.CMD_ATT cmd_ATT5 = new CMD.CMD_ATT
			{
				CMD = cmd.AT_PANID,
				cmd_type = 1,
				p = CMD.P.RW
			};

			CMD.CMD_CONTROL cmd_CONTROL6 = new CMD.CMD_CONTROL
			{
				CMD_RANGE_LIST = cmd.PANID_RANGE,
				CONTROL = panid_txt,
				CMD_PARA_LIST = null,
				COMBOBOX = null
			};
			cmd_ATT5.cmd_control.Add(cmd_CONTROL6);
			cmd_ATT5.cmd_control.Add(cmd_CONTROL2);
			att_list.Add(cmd_ATT5);

			CMD.CMD_ATT cmd_ATT6 = new CMD.CMD_ATT
			{
				CMD = cmd.AT_SRC_ADDR,
				cmd_type = 1,
				p = CMD.P.RW
			};

			CMD.CMD_CONTROL cmd_CONTROL7 = new CMD.CMD_CONTROL
			{
				CMD_RANGE_LIST = cmd.ADDR_RANGE,
				CONTROL = src_addr_txt,
				CMD_PARA_LIST = null,
				COMBOBOX = null
			};
			cmd_ATT6.cmd_control.Add(cmd_CONTROL7);
			cmd_ATT6.cmd_control.Add(cmd_CONTROL2);
			att_list.Add(cmd_ATT6);

			CMD.CMD_ATT cmd_ATT7 = new CMD.CMD_ATT
			{
				CMD = cmd.AT_DST_ADDR,
				cmd_type = 1,
				p = CMD.P.RW
			};

			CMD.CMD_CONTROL cmd_CONTROL8 = new CMD.CMD_CONTROL
			{
				CMD_RANGE_LIST = cmd.ADDR_RANGE,
				CONTROL = dst_addr_txt,
				CMD_PARA_LIST = null,
				COMBOBOX = null
			};
			cmd_ATT7.cmd_control.Add(cmd_CONTROL8);
			cmd_ATT7.cmd_control.Add(cmd_CONTROL2);
			att_list.Add(cmd_ATT7);

			CMD.CMD_ATT cmd_ATT8 = new CMD.CMD_ATT
			{
				CMD = cmd.AT_TYPE,
				cmd_type = 0,
				p = CMD.P.RW
			};

			CMD.CMD_CONTROL cmd_CONTROL9 = new CMD.CMD_CONTROL
			{
				CMD_RANGE_LIST = null,
				CONTROL = null,
				CMD_PARA_LIST = cmd.TYPE_CMD_PARA,
				COMBOBOX = type_cb
			};
			cmd_ATT8.cmd_control.Add(cmd_CONTROL9);
			att_list.Add(cmd_ATT8);

			CMD.CMD_ATT cmd_ATT9 = new CMD.CMD_ATT
			{
				CMD = cmd.AT_SRC_PORT,
				cmd_type = 0,
				p = CMD.P.RW
			};

			CMD.CMD_CONTROL cmd_CONTROL10 = new CMD.CMD_CONTROL
			{
				CMD_RANGE_LIST = null,
				CONTROL = null,
				CMD_PARA_LIST = cmd.PORT_PARA,
				COMBOBOX = src_port_cb
			};
			cmd_ATT9.cmd_control.Add(cmd_CONTROL10);
			cmd_ATT9.cmd_control.Add(cmd_CONTROL2);
			att_list.Add(cmd_ATT9);

			CMD.CMD_ATT cmd_ATT10 = new CMD.CMD_ATT
			{
				CMD = cmd.AT_DST_PORT,
				cmd_type = 0,
				p = CMD.P.RW
			};
			CMD.CMD_CONTROL cmd_CONTROL11 = new CMD.CMD_CONTROL
			{
				CMD_RANGE_LIST = null,
				CONTROL = null,
				CMD_PARA_LIST = cmd.PORT_PARA,
				COMBOBOX = dst_port_cb
			};
			cmd_ATT10.cmd_control.Add(cmd_CONTROL11);
			cmd_ATT10.cmd_control.Add(cmd_CONTROL2);
			att_list.Add(cmd_ATT10);

			CMD.CMD_ATT cmd_ATT11 = new CMD.CMD_ATT
			{
				CMD = cmd.AT_HEAD,
				cmd_type = 0,
				p = CMD.P.RW
			};

			CMD.CMD_CONTROL cmd_CONTROL12 = new CMD.CMD_CONTROL
			{
				CMD_RANGE_LIST = null,
				CONTROL = null,
				CMD_PARA_LIST = cmd.ENABLE_PARA,
				COMBOBOX = head_cb
			};
			cmd_ATT11.cmd_control.Add(cmd_CONTROL12);
			att_list.Add(cmd_ATT11);

			CMD.CMD_ATT cmd_ATT12 = new CMD.CMD_ATT
			{
				CMD = cmd.AT_SECURITY,
				cmd_type = 0,
				p = CMD.P.RW
			};

			CMD.CMD_CONTROL cmd_CONTROL13 = new CMD.CMD_CONTROL
			{
				CMD_RANGE_LIST = null,
				CONTROL = null,
				CMD_PARA_LIST = cmd.ENABLE_PARA,
				COMBOBOX = security_cb
			};
			cmd_ATT12.cmd_control.Add(cmd_CONTROL13);
			att_list.Add(cmd_ATT12);

			CMD.CMD_ATT cmd_ATT13 = new CMD.CMD_ATT
			{
				CMD = cmd.AT_MAC,
				cmd_type = 1,
				p = CMD.P.R
			};

			CMD.CMD_CONTROL cmd_CONTROL14 = new CMD.CMD_CONTROL
			{
				CMD_RANGE_LIST = cmd.MAC_RANGE,
				CONTROL = mac_txt,
				CMD_PARA_LIST = null,
				COMBOBOX = null
			};
			cmd_ATT13.cmd_control.Add(cmd_CONTROL14);
			att_list.Add(cmd_ATT13);

			CMD.CMD_ATT cmd_ATT14 = new CMD.CMD_ATT
			{
				CMD = cmd.AT_KEY,
				cmd_type = 1,
				p = CMD.P.W
			};

			CMD.CMD_CONTROL cmd_CONTROL15 = new CMD.CMD_CONTROL
			{
				CMD_RANGE_LIST = cmd.KEY_RANGE,
				CONTROL = key_txt,
				CMD_PARA_LIST = null,
				COMBOBOX = null
			};
			cmd_ATT14.cmd_control.Add(cmd_CONTROL15);
			att_list.Add(cmd_ATT14);

			CMD.CMD_ATT cmd_ATT15 = new CMD.CMD_ATT
			{
				CMD = cmd.AT_UART,
				cmd_type = 0,
				p = CMD.P.RW
			};

			CMD.CMD_CONTROL cmd_CONTROL16 = new CMD.CMD_CONTROL
			{
				CMD_RANGE_LIST = null,
				CONTROL = null,
				CMD_PARA_LIST = cmd.BAUD_CMD_PARA,
				COMBOBOX = baud_cb
			};
			cmd_ATT15.cmd_control.Add(cmd_CONTROL16);

			CMD.CMD_CONTROL cmd_CONTROL17 = new CMD.CMD_CONTROL
			{
				CMD_RANGE_LIST = null,
				CONTROL = null,
				CMD_PARA_LIST = cmd.PARITY_CMD_PARA,
				COMBOBOX = parity_cb
			};
			cmd_ATT15.cmd_control.Add(cmd_CONTROL17);
			att_list.Add(cmd_ATT15);
		}

		private void Delay(double delayTime)
		{
			DateTime now = DateTime.Now;
			double num;
			do
			{
				num = (DateTime.Now - now).Milliseconds;
				Thread.Sleep(10);
				Application.DoEvents();
			}
			while (num < delayTime);
		}

		private void Read_cmd()
		{
			foreach (CMD.CMD_ATT cmd_ATT in att_list)
				if (cmd_ATT.p == CMD.P.R || cmd_ATT.p == CMD.P.RW)
				{
					string str = cmd_ATT.CMD + "=?";
					SER_WRITE(str);
					Delay(baudsend[baudnow]);
				}
			group_view.Items.Clear();
			router_view.Items.Clear();
		}

		private void Write_cmd()
		{
			try
			{
				foreach (CMD.CMD_ATT cmd_ATT in att_list)
				{
					if (cmd_ATT.p == CMD.P.W || cmd_ATT.p == CMD.P.RW)
					{
						string cmd_name = cmd_ATT.CMD + "=";
						foreach (CMD.CMD_CONTROL cmd_CONTROL in cmd_ATT.cmd_control)
						{
							int cmd_type = cmd_ATT.cmd_type;
							if (cmd_CONTROL.CMD_PARA_LIST == null)
								cmd_type = 1;

							if (cmd_type == 1)
							{
								if (cmd_CONTROL.CONTROL.Text == "")
									send_enable = false;
								else
								{
									float value = float.Parse(cmd_CONTROL.CONTROL.Text);
									string[] ranges = cmd_CONTROL.CMD_RANGE_LIST.Split(new char[] { '&' });
									if (value < float.Parse(ranges[0]) || value > float.Parse(ranges[1]))
									{
										MSG.AppendText(string.Format("CMD value error {0} ranges:{1}\r\n", cmd_CONTROL.CONTROL.Text, cmd_CONTROL.CMD_RANGE_LIST));
										return;
									}
									cmd_name = cmd_name + cmd_CONTROL.CONTROL.Text + ",";
									if (cmd_CONTROL.CONTROL.Name != save_range.Name)
										cmd_CONTROL.CONTROL.Text = "";
								}
								continue;
							}
							if (cmd_type == 0)
							{
								if (cmd_CONTROL.COMBOBOX.Name == baud_cb.Name)
								{
									cmd_name = cmd_name + cmd_CONTROL.COMBOBOX.Text + ",";
									cmd_CONTROL.COMBOBOX.SelectedIndex = -1;
								}
								else
								{
									for (int i = 0; i < cmd_CONTROL.CMD_PARA_LIST.Length / 2; i++)
									{
										string[] array2 = cmd_CONTROL.CMD_PARA_LIST[i].Split(new char[] { '&' });
										if (cmd_CONTROL.COMBOBOX.Text == array2[0])
										{
											cmd_CONTROL.COMBOBOX.SelectedIndex = i;
											cmd_name = cmd_name + array2[1] + ",";
											cmd_CONTROL.COMBOBOX.SelectedIndex = -1;
											break;
										}
									}
								}
								continue;
							}
						}

						if (!send_enable)
							send_enable = true;
						else
						{
							cmd_name = cmd_name.Substring(0, cmd_name.Length - 1);
							SER_WRITE(cmd_name);
							Delay((baudsend[baudnow] + 50));
						}
					}
				}
			}
			catch (Exception ex)
			{
				MSG.AppendText(string.Format("UART or CMD error\r\n{0}", ex.Message));
			}
		}

		private void Read_Cmd_Click(object sender, EventArgs e)
		{
			uart_busy = true;
			string str = "AT+DEVTYPE=?";

			serialPort1.DiscardInBuffer();
			SER_WRITE(str);
			Delay(baudsend[baudnow]);

			int bytesToRead = serialPort1.BytesToRead;
			if (bytesToRead > 0)
			{
				byte[] bytes = new byte[bytesToRead];
				serialPort1.Read(bytes, 0, bytesToRead);
				string response = Encoding.Default.GetString(bytes);
				string[] array2 = response.Split(new char[] { '=', '-', 'N' });
				if (array2[1] == "E52" && array2[2] == "400")
					Tip.SetToolTip(chan_txt, "0---99");
				else if (array2[1] == "E52" && array2[2] == "900")
					Tip.SetToolTip(chan_txt, "0---79");
			}
			uart_busy = false;
			Read_cmd();
		}

		private void SER_WRITE(string str)
		{
			serialPort1.Write(str);
			MSG.AppendText("Send:" + str + "\r\n");
		}

		private void SER_WRITE(byte[] temp)
		{
			serialPort1.Write(temp, 0, temp.Length);
		}

		private void SerialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			if (!uart_busy)
			{
				int bytesToRead = serialPort1.BytesToRead;
				if (bytesToRead > 0)
				{
					Thread.Sleep(baudrec[baudnow]);
					bytesToRead = serialPort1.BytesToRead;
					byte[] bytes = new byte[bytesToRead];
					serialPort1.Read(bytes, 0, bytesToRead);
					Invoke(new READDATE(Readbuffer), new object[] { bytes });
				}
			}
		}

		private void AT_FUN(string serialPort1rebuffer)
		{
			try
			{
				string[] array = serialPort1rebuffer.Split(new char[] { '=', ',', '\r', '\n' });

				if (array.Length < 3 || array[1] == "OK")
				{
					if (array[0] == "AT+GROUP_ADD")
						Group_ReadCmd(array);
					else if (array[1] == "OK")
						MSG.AppendText(array[0] + "Configuration successful\r\n");
				}
				else
				{
					foreach (CMD.CMD_ATT cmd_ATT in att_list)
					{
						if (array[0] == cmd_ATT.CMD)
						{
							foreach (CMD.CMD_CONTROL cmd_CONTROL in cmd_ATT.cmd_control)
							{
								int cmd_type = cmd_ATT.cmd_type;
								int num = cmd_type;
								if (num != 0)
								{
									if (num == 1 && cmd_CONTROL.CONTROL.Name != save_range.Name)
										cmd_CONTROL.CONTROL.Text = array[2];
								}
								else if (cmd_CONTROL.COMBOBOX != null)
								{
									if (cmd_CONTROL.COMBOBOX.Name != save_range.Name)
									{
										if (cmd_CONTROL.COMBOBOX.Name == baud_cb.Name)
										{
											baud_cb.Text = array[1];
											parity_cb.SelectedIndex = Convert.ToInt32(array[2]);
											return;
										}
										for (int i = 0; i < cmd_CONTROL.CMD_PARA_LIST.Length / 2; i++)
										{
											string[] array2 = cmd_CONTROL.CMD_PARA_LIST[i].Split(new char[] { '&' });
											if (array2[1] == array[2])
												cmd_CONTROL.COMBOBOX.SelectedIndex = i;
										}
									}
								}
							}
						}
					}
				}
			}
			catch { }
		}

		private void Readbuffer(byte[] readdata)
		{
			string text = Encoding.Default.GetString(readdata);
			string value = "AT+";
			for (; ; )
			{
				int num = text.IndexOf(value);
				int num2 = text.IndexOf("\r\n");
				if (!(num != -1 && num2 != -1))
					break;

				AT_FUN(text);
				if (!(text.Length != num2 + 2))
					break;
				text = text.Substring(num2 + 2);
			}

			MSG.AppendText("Recieve+" + text + "\r\n");
		}

		private void Write_Cmd_Click(object sender, EventArgs e)
		{
			Write_cmd();
		}

		private void FR_Click(object sender, EventArgs e)
		{
			SER_WRITE("AT+RESET");
		}

		private void Group_ReadCmd(string[] redate)
		{
			string[] array = (from s in redate
							  where !string.IsNullOrEmpty(s)
							  select s).ToArray<string>();
			if (array.Length < 26)
				MSG.AppendText("Read Error or null\r\n");
			else
			{
				for (int i = 0; i < 8; i++)
				{
					group_list[i, 0] = array[2 + 3 * i + 1];
					group_list[i, 1] = array[2 + 3 * i + 2];
				}
				Group_List_Add(group_list);
			}
		}

		private void Group_Read_Click(object sender, EventArgs e)
		{
			group_list.Initialize();
			serialPort1.DiscardInBuffer();
			SER_WRITE("AT+GROUP_ADD=?");
			MSG.AppendText("Please wait...\r\n");
		}

		public void Group_List_Add(string[,] group_list)
		{
			group_view.View = View.Details;
			group_view.Items.Clear();
			group_view.BeginUpdate();
			for (int i = 0; i < 8; i++)
			{
				ListViewItem listViewItem = new ListViewItem
				{
					Text = "No." + Convert.ToString(i)
				};
				listViewItem.SubItems.Add(group_list[i, 0]);
				listViewItem.SubItems.Add(group_list[i, 1]);
				group_view.Items.Add(listViewItem);
			}
			group_view.EndUpdate();
		}

		private void Group_View_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (group_view.FocusedItem != null && group_view.SelectedItems != null)
			{
				int index = group_view.SelectedItems[0].Index;
				group_txt.Text = group_list[index, 1];
			}
		}

		private void Group_Clr_Click(object sender, EventArgs e)
		{
			SER_WRITE("AT+GROUP_CLR=1");
			Delay(500.0);
			group_read.PerformClick();
		}

		private void Group_Del_Click(object sender, EventArgs e)
		{
			SER_WRITE("AT+GROUP_DEL=" + group_txt.Text);
			Delay(500.0);
			group_read.PerformClick();
		}

		private void Group_Add_Click(object sender, EventArgs e)
		{
			SER_WRITE("AT+GROUP_ADD=" + group_txt.Text);
			Delay(500.0);
			group_read.PerformClick();
		}

		private void Router_Read_Click(object sender, EventArgs e)
		{
			try
			{
				uart_busy = true;
				serialPort1.DiscardInBuffer();
				SER_WRITE("AT+ROUTER_CLR=?");

				MSG.AppendText("Please wait...\r\n");

				Application.DoEvents();
				Thread.Sleep(4300);
				int bytesToRead = serialPort1.BytesToRead;
				if (bytesToRead > 0)
				{
					byte[] bytes = new byte[bytesToRead];
					serialPort1.Read(bytes, 0, bytesToRead);
					string response = Encoding.Default.GetString(bytes);
					response = response.Replace("\r\n", "&");

					string[] tokens = response.Split(new char[] { '&' });
					string value = "NO.  DST    HOP    SC  RSSI";
					int token_number = -1;
					for (int i = 0; i < tokens.Length; i++)
						if (tokens[i] == "AT+ROUTER_CLR=OK" && tokens[i + 1].Contains(value))
						{
							token_number = i;
							break;
						}

					if (tokens.Length < 4 || token_number < 0)
					{
						uart_busy = false;
						MSG.AppendText("Read Error or null\r\n");
						return;
					}
					string[,] array3 = new string[256, 5];
					tokens = (from s in tokens
							  where !string.IsNullOrEmpty(s)
							  select s).ToArray();
					for (int j = token_number; j < tokens.Length - 2; j++)
					{
						string[] array4 = tokens[j + 2].Split(new char[] { ' ' });
						array4 = (from s in array4
								  where !string.IsNullOrEmpty(s)
								  select s).ToArray();

						if (!(int.TryParse(array4[0], out int num2)))
							break;

						if (num2 != j - token_number + 1)
							break;
						for (int k = 0; k < 5; k++)
							array3[j - token_number, k] = array4[k];
					}
					Router_List_Add(array3);
				}
			}
			catch { }
			uart_busy = false;
		}

		public void Router_List_Add(string[,] temp_str)
		{
			router_view.View = View.Details;
			router_view.Items.Clear();
			router_view.BeginUpdate();
			for (int i = 0; i < temp_str.Length / 5; i++)
			{
				if (temp_str[i, 0] != null)
				{
					ListViewItem listViewItem = new ListViewItem { Text = temp_str[i, 0] };
					listViewItem.SubItems.Add(temp_str[i, 1]);
					listViewItem.SubItems.Add(temp_str[i, 2]);
					listViewItem.SubItems.Add(temp_str[i, 3]);
					listViewItem.SubItems.Add(temp_str[i, 4]);
					router_view.Items.Add(listViewItem);
				}
			}
			router_view.EndUpdate();
		}

		private void Router_Del_Click(object sender, EventArgs e)
		{
			SER_WRITE("AT+ROUTER_CLR=1");
			Delay(500.0);
			router_read.PerformClick();
		}

		private void Router_Save_Click(object sender, EventArgs e)
		{
			SER_WRITE("AT+ROUTER_SAVE=1");
			Delay(500.0);
			router_read.PerformClick();
		}

		private void Router_Clr_Click(object sender, EventArgs e)
		{
			SER_WRITE("AT+ROUTER_CLR=0");
			Delay(500.0);
			router_read.PerformClick();
		}

		private void Router_Load_Click(object sender, EventArgs e)
		{
			SER_WRITE("AT+ROUTER_READ=1");
			Delay(500.0);
			router_read.PerformClick();
		}

		private void RE_Click(object sender, EventArgs e)
		{
			SER_WRITE("AT+DEFAULT");
		}

		private void Clear_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			MSG.Clear();
		}

		private void Ymodem_Open_file_Click(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "|*.bin" };
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				_loadUpdateFileStatus = true;
				filepath.Text = openFileDialog.FileName;
				_updatePath = openFileDialog.FileName;
			}
			else
				_loadUpdateFileStatus = false;
		}

		private void Ymodem_Down_Click(object sender, EventArgs e)
		{
			if (!_loadUpdateFileStatus)
				MessageBox.Show("Please load the upgrade file first !");
			else
			{
				uart_busy = true;
				FileDownloadThreadProc();
			}
		}

		private void FileDownloadThreadProc()
		{
			if (_fileDownloadThread != null && _fileDownloadThread.IsAlive)
				ymodem.YmodemUploadFileStop();
			else
			{
				_fileDownloadThread = new Thread(delegate () { FileDownloadProc(); }) { IsBackground = true };
				_fileDownloadThread.Start();
			}
		}

		private byte[] cmd_download = new byte[]
		{
				65,
				84,
				43,
				73,
				65,
				80
		};
		private bool FileDownloadProc()
		{
			SER_WRITE(cmd_download);

			MessageBox.Show("Please Wait");

			Thread.Sleep(1000);
			serialPort1.DiscardInBuffer();
			Invoke(new Action(delegate ()
			{
				progressBar_Upload.Value = 0;
				label_UploadProgress.Text = "0 %";
			}));
			serialPort1.Close();

			_yModemType = YModemType.YModem_1K;
			ymodem = new Ymodem
			{
				Path = _updatePath,
				PortName = Uart_Com.Text,
				BaudRate = 115200
			};
			baudnow = 9;
			ymodem.NowDownloadProgressEvent += NowDownloadProgressEvent;
			ymodem.DownloadResultEvent += DownloadFinishEvent;
			ymodem.YmodemUploadFile(_yModemType, 0);
			return true;
		}

		private void NowDownloadProgressEvent(object sender, EventArgs e)
		{
			int value = Convert.ToInt32(sender);
			Invoke(new Action(delegate ()
			{
				progressBar_Upload.Value = value;
				label_UploadProgress.Text = value.ToString() + " %";
			}));
		}

		private void DownloadFinishEvent(object sender, EventArgs e)
		{
			DownloadFinish method = new DownloadFinish(UploadFileResult);
			Invoke(method, new object[] { (bool)sender });
		}

		private void UploadFileResult(bool result)
		{
			MessageBox.Show(result ? "Download Success !" : "Download failed !");
		}

		#region InitializeComponent
		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RF_Setting));
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.uart_panel = new System.Windows.Forms.Panel();
			this.Uart_Para = new System.Windows.Forms.Label();
			this.Uart_Para_CB = new System.Windows.Forms.ComboBox();
			this.Uart_Baud = new System.Windows.Forms.Label();
			this.Uart_Stop_CB = new System.Windows.Forms.ComboBox();
			this.Uart_Com_Lb = new System.Windows.Forms.Label();
			this.RE = new System.Windows.Forms.Button();
			this.FR = new System.Windows.Forms.Button();
			this.Write_Cmd = new System.Windows.Forms.Button();
			this.Read_Cmd = new System.Windows.Forms.Button();
			this.Uart_Bt = new System.Windows.Forms.Button();
			this.Uart_Com = new System.Windows.Forms.ComboBox();
			this.serialPort1 = new System.IO.Ports.SerialPort(this.components);
			this.cmd_panel = new System.Windows.Forms.Panel();
			this.key_txt = new System.Windows.Forms.TextBox();
			this.dst_port_cb = new System.Windows.Forms.ComboBox();
			this.dst_port_lb = new System.Windows.Forms.Label();
			this.key_lb = new System.Windows.Forms.Label();
			this.src_port_cb = new System.Windows.Forms.ComboBox();
			this.src_port_lb = new System.Windows.Forms.Label();
			this.mac_txt = new System.Windows.Forms.TextBox();
			this.dst_addr_txt = new System.Windows.Forms.TextBox();
			this.security_cb = new System.Windows.Forms.ComboBox();
			this.dst_addr_lb = new System.Windows.Forms.Label();
			this.mac_lb = new System.Windows.Forms.Label();
			this.security_lb = new System.Windows.Forms.Label();
			this.src_addr_txt = new System.Windows.Forms.TextBox();
			this.src_addr_lb = new System.Windows.Forms.Label();
			this.head_cb = new System.Windows.Forms.ComboBox();
			this.type_cb = new System.Windows.Forms.ComboBox();
			this.type_lb = new System.Windows.Forms.Label();
			this.head_lb = new System.Windows.Forms.Label();
			this.panid_txt = new System.Windows.Forms.TextBox();
			this.panid_lb = new System.Windows.Forms.Label();
			this.option_cb = new System.Windows.Forms.ComboBox();
			this.option_lb = new System.Windows.Forms.Label();
			this.rate_cb = new System.Windows.Forms.ComboBox();
			this.rate_lb = new System.Windows.Forms.Label();
			this.parity_cb = new System.Windows.Forms.ComboBox();
			this.parity_lb = new System.Windows.Forms.Label();
			this.baud_cb = new System.Windows.Forms.ComboBox();
			this.baud_lb = new System.Windows.Forms.Label();
			this.chan_txt = new System.Windows.Forms.TextBox();
			this.chan_lb = new System.Windows.Forms.Label();
			this.power_txt = new System.Windows.Forms.TextBox();
			this.power_lb = new System.Windows.Forms.Label();
			this.reset_time_txt = new System.Windows.Forms.TextBox();
			this.reset_time_lb = new System.Windows.Forms.Label();
			this.reset_aux_cb = new System.Windows.Forms.ComboBox();
			this.reset_aux_lb = new System.Windows.Forms.Label();
			this.save_range = new System.Windows.Forms.Label();
			this.MSG = new System.Windows.Forms.TextBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.Clear = new System.Windows.Forms.LinkLabel();
			this.panel2 = new System.Windows.Forms.Panel();
			this.cmd_tb = new System.Windows.Forms.TabControl();
			this.cmd_seting = new System.Windows.Forms.TabPage();
			this.group_set = new System.Windows.Forms.TabPage();
			this.panel3 = new System.Windows.Forms.Panel();
			this.group_view = new System.Windows.Forms.ListView();
			this.G1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.G2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.G3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.group_lb = new System.Windows.Forms.Label();
			this.group_txt = new System.Windows.Forms.TextBox();
			this.group_add = new System.Windows.Forms.Button();
			this.group_clr = new System.Windows.Forms.Button();
			this.group_del = new System.Windows.Forms.Button();
			this.group_read = new System.Windows.Forms.Button();
			this.router_set = new System.Windows.Forms.TabPage();
			this.panel4 = new System.Windows.Forms.Panel();
			this.router_read = new System.Windows.Forms.Button();
			this.router_load = new System.Windows.Forms.Button();
			this.router_save = new System.Windows.Forms.Button();
			this.router_view = new System.Windows.Forms.ListView();
			this.router_no = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.router_dst_addr = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.router_next_addr = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.router_score = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.router_rssi = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.router_clr = new System.Windows.Forms.Button();
			this.router_del = new System.Windows.Forms.Button();
			this.UP = new System.Windows.Forms.TabPage();
			this.Ymodem_Down = new System.Windows.Forms.Button();
			this.Ymodem_Open_file = new System.Windows.Forms.Button();
			this.label_UploadProgress = new System.Windows.Forms.Label();
			this.progressBar_Upload = new System.Windows.Forms.ProgressBar();
			this.filepath = new System.Windows.Forms.TextBox();
			this.Path_label = new System.Windows.Forms.Label();
			this.Tip = new System.Windows.Forms.ToolTip(this.components);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.uart_panel.SuspendLayout();
			this.cmd_panel.SuspendLayout();
			this.panel1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.cmd_tb.SuspendLayout();
			this.cmd_seting.SuspendLayout();
			this.group_set.SuspendLayout();
			this.panel3.SuspendLayout();
			this.router_set.SuspendLayout();
			this.panel4.SuspendLayout();
			this.UP.SuspendLayout();
			this.SuspendLayout();
			// 
			// pictureBox1
			// 
			this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
			this.pictureBox1.Location = new System.Drawing.Point(9, 10);
			this.pictureBox1.Margin = new System.Windows.Forms.Padding(0);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(178, 93);
			this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.pictureBox1.TabIndex = 16;
			this.pictureBox1.TabStop = false;
			// 
			// uart_panel
			// 
			this.uart_panel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.uart_panel.Controls.Add(this.Uart_Para);
			this.uart_panel.Controls.Add(this.Uart_Para_CB);
			this.uart_panel.Controls.Add(this.Uart_Baud);
			this.uart_panel.Controls.Add(this.Uart_Stop_CB);
			this.uart_panel.Controls.Add(this.Uart_Com_Lb);
			this.uart_panel.Controls.Add(this.RE);
			this.uart_panel.Controls.Add(this.FR);
			this.uart_panel.Controls.Add(this.Write_Cmd);
			this.uart_panel.Controls.Add(this.Read_Cmd);
			this.uart_panel.Controls.Add(this.Uart_Bt);
			this.uart_panel.Controls.Add(this.Uart_Com);
			this.uart_panel.Location = new System.Drawing.Point(9, 107);
			this.uart_panel.Name = "uart_panel";
			this.uart_panel.Size = new System.Drawing.Size(594, 73);
			this.uart_panel.TabIndex = 17;
			// 
			// Uart_Para
			// 
			this.Uart_Para.AutoSize = true;
			this.Uart_Para.Location = new System.Drawing.Point(3, 45);
			this.Uart_Para.Name = "Uart_Para";
			this.Uart_Para.Size = new System.Drawing.Size(29, 13);
			this.Uart_Para.TabIndex = 27;
			this.Uart_Para.Text = "Para";
			// 
			// Uart_Para_CB
			// 
			this.Uart_Para_CB.FormattingEnabled = true;
			this.Uart_Para_CB.Items.AddRange(new object[] {
            "None",
            "Odd",
            "Even"});
			this.Uart_Para_CB.Location = new System.Drawing.Point(64, 42);
			this.Uart_Para_CB.Margin = new System.Windows.Forms.Padding(2);
			this.Uart_Para_CB.Name = "Uart_Para_CB";
			this.Uart_Para_CB.Size = new System.Drawing.Size(70, 21);
			this.Uart_Para_CB.TabIndex = 26;
			this.Uart_Para_CB.Text = "None";
			// 
			// Uart_Baud
			// 
			this.Uart_Baud.AutoSize = true;
			this.Uart_Baud.Location = new System.Drawing.Point(3, 10);
			this.Uart_Baud.Name = "Uart_Baud";
			this.Uart_Baud.Size = new System.Drawing.Size(32, 13);
			this.Uart_Baud.TabIndex = 25;
			this.Uart_Baud.Text = "Baud";
			// 
			// Uart_Stop_CB
			// 
			this.Uart_Stop_CB.FormattingEnabled = true;
			this.Uart_Stop_CB.Items.AddRange(new object[] {
            "1200",
            "2400",
            "4800",
            "9600",
            "19200",
            "38400",
            "57600",
            "115200",
            "230400",
            "460800"});
			this.Uart_Stop_CB.Location = new System.Drawing.Point(64, 8);
			this.Uart_Stop_CB.Margin = new System.Windows.Forms.Padding(2);
			this.Uart_Stop_CB.Name = "Uart_Stop_CB";
			this.Uart_Stop_CB.Size = new System.Drawing.Size(70, 21);
			this.Uart_Stop_CB.TabIndex = 24;
			this.Uart_Stop_CB.Text = "115200";
			// 
			// Uart_Com_Lb
			// 
			this.Uart_Com_Lb.AutoSize = true;
			this.Uart_Com_Lb.Location = new System.Drawing.Point(168, 11);
			this.Uart_Com_Lb.Name = "Uart_Com_Lb";
			this.Uart_Com_Lb.Size = new System.Drawing.Size(27, 13);
			this.Uart_Com_Lb.TabIndex = 23;
			this.Uart_Com_Lb.Text = "Uart";
			// 
			// RE
			// 
			this.RE.Location = new System.Drawing.Point(445, 39);
			this.RE.Name = "RE";
			this.RE.Size = new System.Drawing.Size(75, 25);
			this.RE.TabIndex = 4;
			this.RE.Text = "Restore";
			this.RE.UseVisualStyleBackColor = true;
			this.RE.Click += new System.EventHandler(this.RE_Click);
			// 
			// FR
			// 
			this.FR.Location = new System.Drawing.Point(445, 5);
			this.FR.Name = "FR";
			this.FR.Size = new System.Drawing.Size(75, 25);
			this.FR.TabIndex = 3;
			this.FR.Text = "Reset";
			this.FR.UseVisualStyleBackColor = true;
			this.FR.Click += new System.EventHandler(this.FR_Click);
			// 
			// Write_Cmd
			// 
			this.Write_Cmd.Location = new System.Drawing.Point(323, 39);
			this.Write_Cmd.Name = "Write_Cmd";
			this.Write_Cmd.Size = new System.Drawing.Size(75, 25);
			this.Write_Cmd.TabIndex = 2;
			this.Write_Cmd.Text = "Write";
			this.Write_Cmd.UseVisualStyleBackColor = true;
			this.Write_Cmd.Click += new System.EventHandler(this.Write_Cmd_Click);
			// 
			// Read_Cmd
			// 
			this.Read_Cmd.Location = new System.Drawing.Point(323, 5);
			this.Read_Cmd.Name = "Read_Cmd";
			this.Read_Cmd.Size = new System.Drawing.Size(75, 25);
			this.Read_Cmd.TabIndex = 1;
			this.Read_Cmd.Text = "Read";
			this.Read_Cmd.UseVisualStyleBackColor = true;
			this.Read_Cmd.Click += new System.EventHandler(this.Read_Cmd_Click);
			// 
			// Uart_Bt
			// 
			this.Uart_Bt.Location = new System.Drawing.Point(172, 37);
			this.Uart_Bt.Margin = new System.Windows.Forms.Padding(2);
			this.Uart_Bt.Name = "Uart_Bt";
			this.Uart_Bt.Size = new System.Drawing.Size(107, 26);
			this.Uart_Bt.TabIndex = 0;
			this.Uart_Bt.Text = "Open Uart";
			this.Uart_Bt.UseVisualStyleBackColor = true;
			this.Uart_Bt.Click += new System.EventHandler(this.Uart_Bt_Click);
			// 
			// Uart_Com
			// 
			this.Uart_Com.FormattingEnabled = true;
			this.Uart_Com.Location = new System.Drawing.Point(201, 9);
			this.Uart_Com.Margin = new System.Windows.Forms.Padding(2);
			this.Uart_Com.Name = "Uart_Com";
			this.Uart_Com.Size = new System.Drawing.Size(78, 21);
			this.Uart_Com.TabIndex = 12;
			this.Uart_Com.Click += new System.EventHandler(this.Uart_Com_Click);
			// 
			// serialPort1
			// 
			this.serialPort1.ReadBufferSize = 15000;
			this.serialPort1.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(this.SerialPort1_DataReceived);
			// 
			// cmd_panel
			// 
			this.cmd_panel.AutoScroll = true;
			this.cmd_panel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.cmd_panel.Controls.Add(this.key_txt);
			this.cmd_panel.Controls.Add(this.dst_port_cb);
			this.cmd_panel.Controls.Add(this.dst_port_lb);
			this.cmd_panel.Controls.Add(this.key_lb);
			this.cmd_panel.Controls.Add(this.src_port_cb);
			this.cmd_panel.Controls.Add(this.src_port_lb);
			this.cmd_panel.Controls.Add(this.mac_txt);
			this.cmd_panel.Controls.Add(this.dst_addr_txt);
			this.cmd_panel.Controls.Add(this.security_cb);
			this.cmd_panel.Controls.Add(this.dst_addr_lb);
			this.cmd_panel.Controls.Add(this.mac_lb);
			this.cmd_panel.Controls.Add(this.security_lb);
			this.cmd_panel.Controls.Add(this.src_addr_txt);
			this.cmd_panel.Controls.Add(this.src_addr_lb);
			this.cmd_panel.Controls.Add(this.head_cb);
			this.cmd_panel.Controls.Add(this.type_cb);
			this.cmd_panel.Controls.Add(this.type_lb);
			this.cmd_panel.Controls.Add(this.head_lb);
			this.cmd_panel.Controls.Add(this.panid_txt);
			this.cmd_panel.Controls.Add(this.panid_lb);
			this.cmd_panel.Controls.Add(this.option_cb);
			this.cmd_panel.Controls.Add(this.option_lb);
			this.cmd_panel.Controls.Add(this.rate_cb);
			this.cmd_panel.Controls.Add(this.rate_lb);
			this.cmd_panel.Controls.Add(this.parity_cb);
			this.cmd_panel.Controls.Add(this.parity_lb);
			this.cmd_panel.Controls.Add(this.baud_cb);
			this.cmd_panel.Controls.Add(this.baud_lb);
			this.cmd_panel.Controls.Add(this.chan_txt);
			this.cmd_panel.Controls.Add(this.chan_lb);
			this.cmd_panel.Controls.Add(this.power_txt);
			this.cmd_panel.Controls.Add(this.power_lb);
			this.cmd_panel.Location = new System.Drawing.Point(4, 5);
			this.cmd_panel.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.cmd_panel.Name = "cmd_panel";
			this.cmd_panel.Size = new System.Drawing.Size(334, 274);
			this.cmd_panel.TabIndex = 18;
			// 
			// key_txt
			// 
			this.key_txt.Location = new System.Drawing.Point(235, 218);
			this.key_txt.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.key_txt.Name = "key_txt";
			this.key_txt.Size = new System.Drawing.Size(77, 20);
			this.key_txt.TabIndex = 35;
			// 
			// dst_port_cb
			// 
			this.dst_port_cb.FormattingEnabled = true;
			this.dst_port_cb.Location = new System.Drawing.Point(235, 158);
			this.dst_port_cb.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.dst_port_cb.Name = "dst_port_cb";
			this.dst_port_cb.Size = new System.Drawing.Size(77, 21);
			this.dst_port_cb.TabIndex = 23;
			// 
			// dst_port_lb
			// 
			this.dst_port_lb.AutoSize = true;
			this.dst_port_lb.Location = new System.Drawing.Point(173, 160);
			this.dst_port_lb.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.dst_port_lb.Name = "dst_port_lb";
			this.dst_port_lb.Size = new System.Drawing.Size(45, 13);
			this.dst_port_lb.TabIndex = 22;
			this.dst_port_lb.Text = "Dst Port";
			// 
			// key_lb
			// 
			this.key_lb.AutoSize = true;
			this.key_lb.Location = new System.Drawing.Point(173, 221);
			this.key_lb.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.key_lb.Name = "key_lb";
			this.key_lb.Size = new System.Drawing.Size(25, 13);
			this.key_lb.TabIndex = 34;
			this.key_lb.Text = "Key";
			// 
			// src_port_cb
			// 
			this.src_port_cb.FormattingEnabled = true;
			this.src_port_cb.Location = new System.Drawing.Point(80, 158);
			this.src_port_cb.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.src_port_cb.Name = "src_port_cb";
			this.src_port_cb.Size = new System.Drawing.Size(77, 21);
			this.src_port_cb.TabIndex = 21;
			// 
			// src_port_lb
			// 
			this.src_port_lb.AutoSize = true;
			this.src_port_lb.Location = new System.Drawing.Point(9, 160);
			this.src_port_lb.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.src_port_lb.Name = "src_port_lb";
			this.src_port_lb.Size = new System.Drawing.Size(45, 13);
			this.src_port_lb.TabIndex = 20;
			this.src_port_lb.Text = "Src Port";
			// 
			// mac_txt
			// 
			this.mac_txt.Location = new System.Drawing.Point(80, 218);
			this.mac_txt.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.mac_txt.Name = "mac_txt";
			this.mac_txt.Size = new System.Drawing.Size(77, 20);
			this.mac_txt.TabIndex = 33;
			// 
			// dst_addr_txt
			// 
			this.dst_addr_txt.Location = new System.Drawing.Point(235, 127);
			this.dst_addr_txt.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.dst_addr_txt.Name = "dst_addr_txt";
			this.dst_addr_txt.Size = new System.Drawing.Size(77, 20);
			this.dst_addr_txt.TabIndex = 19;
			// 
			// security_cb
			// 
			this.security_cb.FormattingEnabled = true;
			this.security_cb.Location = new System.Drawing.Point(235, 188);
			this.security_cb.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.security_cb.Name = "security_cb";
			this.security_cb.Size = new System.Drawing.Size(77, 21);
			this.security_cb.TabIndex = 27;
			// 
			// dst_addr_lb
			// 
			this.dst_addr_lb.AutoSize = true;
			this.dst_addr_lb.Location = new System.Drawing.Point(173, 130);
			this.dst_addr_lb.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.dst_addr_lb.Name = "dst_addr_lb";
			this.dst_addr_lb.Size = new System.Drawing.Size(48, 13);
			this.dst_addr_lb.TabIndex = 18;
			this.dst_addr_lb.Text = "Dst Addr";
			// 
			// mac_lb
			// 
			this.mac_lb.AutoSize = true;
			this.mac_lb.Location = new System.Drawing.Point(9, 221);
			this.mac_lb.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.mac_lb.Name = "mac_lb";
			this.mac_lb.Size = new System.Drawing.Size(30, 13);
			this.mac_lb.TabIndex = 32;
			this.mac_lb.Text = "MAC";
			// 
			// security_lb
			// 
			this.security_lb.AutoSize = true;
			this.security_lb.Location = new System.Drawing.Point(173, 191);
			this.security_lb.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.security_lb.Name = "security_lb";
			this.security_lb.Size = new System.Drawing.Size(45, 13);
			this.security_lb.TabIndex = 26;
			this.security_lb.Text = "Security";
			// 
			// src_addr_txt
			// 
			this.src_addr_txt.Location = new System.Drawing.Point(80, 127);
			this.src_addr_txt.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.src_addr_txt.Name = "src_addr_txt";
			this.src_addr_txt.Size = new System.Drawing.Size(77, 20);
			this.src_addr_txt.TabIndex = 17;
			// 
			// src_addr_lb
			// 
			this.src_addr_lb.AutoSize = true;
			this.src_addr_lb.Location = new System.Drawing.Point(9, 130);
			this.src_addr_lb.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.src_addr_lb.Name = "src_addr_lb";
			this.src_addr_lb.Size = new System.Drawing.Size(48, 13);
			this.src_addr_lb.TabIndex = 16;
			this.src_addr_lb.Text = "Src Addr";
			// 
			// head_cb
			// 
			this.head_cb.FormattingEnabled = true;
			this.head_cb.Location = new System.Drawing.Point(80, 188);
			this.head_cb.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.head_cb.Name = "head_cb";
			this.head_cb.Size = new System.Drawing.Size(77, 21);
			this.head_cb.TabIndex = 25;
			// 
			// type_cb
			// 
			this.type_cb.FormattingEnabled = true;
			this.type_cb.Location = new System.Drawing.Point(235, 97);
			this.type_cb.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.type_cb.Name = "type_cb";
			this.type_cb.Size = new System.Drawing.Size(77, 21);
			this.type_cb.TabIndex = 15;
			// 
			// type_lb
			// 
			this.type_lb.AutoSize = true;
			this.type_lb.Location = new System.Drawing.Point(173, 100);
			this.type_lb.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.type_lb.Name = "type_lb";
			this.type_lb.Size = new System.Drawing.Size(31, 13);
			this.type_lb.TabIndex = 14;
			this.type_lb.Text = "Type";
			// 
			// head_lb
			// 
			this.head_lb.AutoSize = true;
			this.head_lb.Location = new System.Drawing.Point(9, 191);
			this.head_lb.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.head_lb.Name = "head_lb";
			this.head_lb.Size = new System.Drawing.Size(53, 13);
			this.head_lb.TabIndex = 24;
			this.head_lb.Text = "Out Head";
			// 
			// panid_txt
			// 
			this.panid_txt.Location = new System.Drawing.Point(80, 67);
			this.panid_txt.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.panid_txt.Name = "panid_txt";
			this.panid_txt.Size = new System.Drawing.Size(77, 20);
			this.panid_txt.TabIndex = 13;
			// 
			// panid_lb
			// 
			this.panid_lb.AutoSize = true;
			this.panid_lb.Location = new System.Drawing.Point(9, 69);
			this.panid_lb.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.panid_lb.Name = "panid_lb";
			this.panid_lb.Size = new System.Drawing.Size(40, 13);
			this.panid_lb.TabIndex = 12;
			this.panid_lb.Text = "PANID";
			// 
			// option_cb
			// 
			this.option_cb.FormattingEnabled = true;
			this.option_cb.Location = new System.Drawing.Point(80, 97);
			this.option_cb.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.option_cb.Name = "option_cb";
			this.option_cb.Size = new System.Drawing.Size(77, 21);
			this.option_cb.TabIndex = 11;
			// 
			// option_lb
			// 
			this.option_lb.AutoSize = true;
			this.option_lb.Location = new System.Drawing.Point(9, 100);
			this.option_lb.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.option_lb.Name = "option_lb";
			this.option_lb.Size = new System.Drawing.Size(38, 13);
			this.option_lb.TabIndex = 10;
			this.option_lb.Text = "Option";
			// 
			// rate_cb
			// 
			this.rate_cb.FormattingEnabled = true;
			this.rate_cb.Location = new System.Drawing.Point(235, 67);
			this.rate_cb.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.rate_cb.Name = "rate_cb";
			this.rate_cb.Size = new System.Drawing.Size(77, 21);
			this.rate_cb.TabIndex = 9;
			// 
			// rate_lb
			// 
			this.rate_lb.AutoSize = true;
			this.rate_lb.Location = new System.Drawing.Point(173, 69);
			this.rate_lb.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.rate_lb.Name = "rate_lb";
			this.rate_lb.Size = new System.Drawing.Size(30, 13);
			this.rate_lb.TabIndex = 8;
			this.rate_lb.Text = "Rate";
			// 
			// parity_cb
			// 
			this.parity_cb.FormattingEnabled = true;
			this.parity_cb.Location = new System.Drawing.Point(235, 36);
			this.parity_cb.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.parity_cb.Name = "parity_cb";
			this.parity_cb.Size = new System.Drawing.Size(77, 21);
			this.parity_cb.TabIndex = 7;
			// 
			// parity_lb
			// 
			this.parity_lb.AutoSize = true;
			this.parity_lb.Location = new System.Drawing.Point(173, 42);
			this.parity_lb.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.parity_lb.Name = "parity_lb";
			this.parity_lb.Size = new System.Drawing.Size(33, 13);
			this.parity_lb.TabIndex = 6;
			this.parity_lb.Text = "Parity";
			// 
			// baud_cb
			// 
			this.baud_cb.FormattingEnabled = true;
			this.baud_cb.Location = new System.Drawing.Point(80, 36);
			this.baud_cb.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.baud_cb.Name = "baud_cb";
			this.baud_cb.Size = new System.Drawing.Size(77, 21);
			this.baud_cb.TabIndex = 5;
			// 
			// baud_lb
			// 
			this.baud_lb.AutoSize = true;
			this.baud_lb.Location = new System.Drawing.Point(9, 39);
			this.baud_lb.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.baud_lb.Name = "baud_lb";
			this.baud_lb.Size = new System.Drawing.Size(32, 13);
			this.baud_lb.TabIndex = 4;
			this.baud_lb.Text = "Baud";
			// 
			// chan_txt
			// 
			this.chan_txt.Location = new System.Drawing.Point(235, 6);
			this.chan_txt.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.chan_txt.Name = "chan_txt";
			this.chan_txt.Size = new System.Drawing.Size(77, 20);
			this.chan_txt.TabIndex = 3;
			// 
			// chan_lb
			// 
			this.chan_lb.AutoSize = true;
			this.chan_lb.Location = new System.Drawing.Point(173, 9);
			this.chan_lb.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.chan_lb.Name = "chan_lb";
			this.chan_lb.Size = new System.Drawing.Size(32, 13);
			this.chan_lb.TabIndex = 2;
			this.chan_lb.Text = "Chan";
			// 
			// power_txt
			// 
			this.power_txt.Location = new System.Drawing.Point(80, 6);
			this.power_txt.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.power_txt.Name = "power_txt";
			this.power_txt.Size = new System.Drawing.Size(77, 20);
			this.power_txt.TabIndex = 1;
			// 
			// power_lb
			// 
			this.power_lb.AutoSize = true;
			this.power_lb.Location = new System.Drawing.Point(9, 9);
			this.power_lb.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.power_lb.Name = "power_lb";
			this.power_lb.Size = new System.Drawing.Size(37, 13);
			this.power_lb.TabIndex = 0;
			this.power_lb.Text = "Power";
			// 
			// reset_time_txt
			// 
			this.reset_time_txt.Location = new System.Drawing.Point(751, 49);
			this.reset_time_txt.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.reset_time_txt.Name = "reset_time_txt";
			this.reset_time_txt.Size = new System.Drawing.Size(77, 20);
			this.reset_time_txt.TabIndex = 31;
			this.reset_time_txt.Visible = false;
			// 
			// reset_time_lb
			// 
			this.reset_time_lb.AutoSize = true;
			this.reset_time_lb.Location = new System.Drawing.Point(678, 52);
			this.reset_time_lb.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.reset_time_lb.Name = "reset_time_lb";
			this.reset_time_lb.Size = new System.Drawing.Size(40, 13);
			this.reset_time_lb.TabIndex = 30;
			this.reset_time_lb.Text = "R_time";
			this.reset_time_lb.Visible = false;
			// 
			// reset_aux_cb
			// 
			this.reset_aux_cb.FormattingEnabled = true;
			this.reset_aux_cb.Location = new System.Drawing.Point(751, 17);
			this.reset_aux_cb.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.reset_aux_cb.Name = "reset_aux_cb";
			this.reset_aux_cb.Size = new System.Drawing.Size(77, 21);
			this.reset_aux_cb.TabIndex = 29;
			this.reset_aux_cb.Visible = false;
			// 
			// reset_aux_lb
			// 
			this.reset_aux_lb.AutoSize = true;
			this.reset_aux_lb.Location = new System.Drawing.Point(678, 20);
			this.reset_aux_lb.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.reset_aux_lb.Name = "reset_aux_lb";
			this.reset_aux_lb.Size = new System.Drawing.Size(38, 13);
			this.reset_aux_lb.TabIndex = 28;
			this.reset_aux_lb.Text = "R_aux";
			this.reset_aux_lb.Visible = false;
			// 
			// save_range
			// 
			this.save_range.AutoSize = true;
			this.save_range.Location = new System.Drawing.Point(630, 10);
			this.save_range.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.save_range.Name = "save_range";
			this.save_range.Size = new System.Drawing.Size(13, 13);
			this.save_range.TabIndex = 19;
			this.save_range.Text = "0";
			this.save_range.Visible = false;
			// 
			// MSG
			// 
			this.MSG.Location = new System.Drawing.Point(2, 3);
			this.MSG.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.MSG.Multiline = true;
			this.MSG.Name = "MSG";
			this.MSG.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.MSG.Size = new System.Drawing.Size(228, 305);
			this.MSG.TabIndex = 20;
			// 
			// panel1
			// 
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel1.Controls.Add(this.Clear);
			this.panel1.Controls.Add(this.MSG);
			this.panel1.Location = new System.Drawing.Point(366, 186);
			this.panel1.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(238, 332);
			this.panel1.TabIndex = 20;
			// 
			// Clear
			// 
			this.Clear.AutoSize = true;
			this.Clear.Location = new System.Drawing.Point(200, 309);
			this.Clear.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.Clear.Name = "Clear";
			this.Clear.Size = new System.Drawing.Size(31, 13);
			this.Clear.TabIndex = 28;
			this.Clear.TabStop = true;
			this.Clear.Text = "Clear";
			this.Clear.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.Clear_LinkClicked);
			// 
			// panel2
			// 
			this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel2.Controls.Add(this.cmd_tb);
			this.panel2.Location = new System.Drawing.Point(9, 186);
			this.panel2.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(355, 332);
			this.panel2.TabIndex = 21;
			// 
			// cmd_tb
			// 
			this.cmd_tb.Controls.Add(this.cmd_seting);
			this.cmd_tb.Controls.Add(this.group_set);
			this.cmd_tb.Controls.Add(this.router_set);
			this.cmd_tb.Controls.Add(this.UP);
			this.cmd_tb.Location = new System.Drawing.Point(2, 6);
			this.cmd_tb.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.cmd_tb.Name = "cmd_tb";
			this.cmd_tb.SelectedIndex = 0;
			this.cmd_tb.Size = new System.Drawing.Size(348, 322);
			this.cmd_tb.TabIndex = 0;
			// 
			// cmd_seting
			// 
			this.cmd_seting.BackColor = System.Drawing.SystemColors.Control;
			this.cmd_seting.Controls.Add(this.cmd_panel);
			this.cmd_seting.Location = new System.Drawing.Point(4, 22);
			this.cmd_seting.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.cmd_seting.Name = "cmd_seting";
			this.cmd_seting.Padding = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.cmd_seting.Size = new System.Drawing.Size(340, 296);
			this.cmd_seting.TabIndex = 0;
			this.cmd_seting.Text = "Cmd Info";
			// 
			// group_set
			// 
			this.group_set.BackColor = System.Drawing.SystemColors.Control;
			this.group_set.Controls.Add(this.panel3);
			this.group_set.Location = new System.Drawing.Point(4, 22);
			this.group_set.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.group_set.Name = "group_set";
			this.group_set.Padding = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.group_set.Size = new System.Drawing.Size(340, 296);
			this.group_set.TabIndex = 1;
			this.group_set.Text = "Group Cfg";
			// 
			// panel3
			// 
			this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel3.Controls.Add(this.group_view);
			this.panel3.Controls.Add(this.group_lb);
			this.panel3.Controls.Add(this.group_txt);
			this.panel3.Controls.Add(this.group_add);
			this.panel3.Controls.Add(this.group_clr);
			this.panel3.Controls.Add(this.group_del);
			this.panel3.Controls.Add(this.group_read);
			this.panel3.Location = new System.Drawing.Point(4, 5);
			this.panel3.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(334, 290);
			this.panel3.TabIndex = 22;
			// 
			// group_view
			// 
			this.group_view.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.G1,
            this.G2,
            this.G3});
			this.group_view.FullRowSelect = true;
			this.group_view.HideSelection = false;
			this.group_view.Location = new System.Drawing.Point(2, 40);
			this.group_view.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.group_view.Name = "group_view";
			this.group_view.Size = new System.Drawing.Size(328, 234);
			this.group_view.TabIndex = 6;
			this.group_view.UseCompatibleStateImageBehavior = false;
			this.group_view.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.Group_View_MouseDoubleClick);
			// 
			// G1
			// 
			this.G1.Text = "NO";
			this.G1.Width = 100;
			// 
			// G2
			// 
			this.G2.Text = "Group";
			this.G2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.G2.Width = 100;
			// 
			// G3
			// 
			this.G3.Text = "Group";
			this.G3.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.G3.Width = 100;
			// 
			// group_lb
			// 
			this.group_lb.AutoSize = true;
			this.group_lb.Location = new System.Drawing.Point(2, 16);
			this.group_lb.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.group_lb.Name = "group_lb";
			this.group_lb.Size = new System.Drawing.Size(36, 13);
			this.group_lb.TabIndex = 4;
			this.group_lb.Text = "Group";
			// 
			// group_txt
			// 
			this.group_txt.Location = new System.Drawing.Point(42, 10);
			this.group_txt.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.group_txt.Name = "group_txt";
			this.group_txt.Size = new System.Drawing.Size(74, 20);
			this.group_txt.TabIndex = 5;
			// 
			// group_add
			// 
			this.group_add.Location = new System.Drawing.Point(119, 10);
			this.group_add.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.group_add.Name = "group_add";
			this.group_add.Size = new System.Drawing.Size(27, 25);
			this.group_add.TabIndex = 8;
			this.group_add.Text = "+";
			this.group_add.UseVisualStyleBackColor = true;
			this.group_add.Click += new System.EventHandler(this.Group_Add_Click);
			// 
			// group_clr
			// 
			this.group_clr.Location = new System.Drawing.Point(248, 10);
			this.group_clr.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.group_clr.Name = "group_clr";
			this.group_clr.Size = new System.Drawing.Size(81, 25);
			this.group_clr.TabIndex = 11;
			this.group_clr.Text = "Clr";
			this.group_clr.UseVisualStyleBackColor = true;
			this.group_clr.Click += new System.EventHandler(this.Group_Clr_Click);
			// 
			// group_del
			// 
			this.group_del.Location = new System.Drawing.Point(151, 10);
			this.group_del.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.group_del.Name = "group_del";
			this.group_del.Size = new System.Drawing.Size(23, 25);
			this.group_del.TabIndex = 9;
			this.group_del.Text = "-";
			this.group_del.UseVisualStyleBackColor = true;
			this.group_del.Click += new System.EventHandler(this.Group_Del_Click);
			// 
			// group_read
			// 
			this.group_read.Location = new System.Drawing.Point(177, 10);
			this.group_read.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.group_read.Name = "group_read";
			this.group_read.Size = new System.Drawing.Size(66, 25);
			this.group_read.TabIndex = 10;
			this.group_read.Text = "Read";
			this.group_read.UseVisualStyleBackColor = true;
			this.group_read.Click += new System.EventHandler(this.Group_Read_Click);
			// 
			// router_set
			// 
			this.router_set.BackColor = System.Drawing.SystemColors.Control;
			this.router_set.Controls.Add(this.panel4);
			this.router_set.Location = new System.Drawing.Point(4, 22);
			this.router_set.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.router_set.Name = "router_set";
			this.router_set.Size = new System.Drawing.Size(340, 296);
			this.router_set.TabIndex = 2;
			this.router_set.Text = "Router Info";
			// 
			// panel4
			// 
			this.panel4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel4.Controls.Add(this.router_read);
			this.panel4.Controls.Add(this.router_load);
			this.panel4.Controls.Add(this.router_save);
			this.panel4.Controls.Add(this.router_view);
			this.panel4.Controls.Add(this.router_clr);
			this.panel4.Controls.Add(this.router_del);
			this.panel4.Location = new System.Drawing.Point(2, 3);
			this.panel4.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.panel4.Name = "panel4";
			this.panel4.Size = new System.Drawing.Size(338, 292);
			this.panel4.TabIndex = 25;
			// 
			// router_read
			// 
			this.router_read.Location = new System.Drawing.Point(2, 10);
			this.router_read.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.router_read.Name = "router_read";
			this.router_read.Size = new System.Drawing.Size(56, 25);
			this.router_read.TabIndex = 16;
			this.router_read.Text = "Read";
			this.router_read.UseVisualStyleBackColor = true;
			this.router_read.Click += new System.EventHandler(this.Router_Read_Click);
			// 
			// router_load
			// 
			this.router_load.Location = new System.Drawing.Point(199, 10);
			this.router_load.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.router_load.Name = "router_load";
			this.router_load.Size = new System.Drawing.Size(62, 25);
			this.router_load.TabIndex = 12;
			this.router_load.Text = "Load";
			this.router_load.UseVisualStyleBackColor = true;
			this.router_load.Click += new System.EventHandler(this.Router_Load_Click);
			// 
			// router_save
			// 
			this.router_save.Location = new System.Drawing.Point(129, 10);
			this.router_save.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.router_save.Name = "router_save";
			this.router_save.Size = new System.Drawing.Size(63, 25);
			this.router_save.TabIndex = 15;
			this.router_save.Text = "Save";
			this.router_save.UseVisualStyleBackColor = true;
			this.router_save.Click += new System.EventHandler(this.Router_Save_Click);
			// 
			// router_view
			// 
			this.router_view.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.router_no,
            this.router_dst_addr,
            this.router_next_addr,
            this.router_score,
            this.router_rssi});
			this.router_view.HideSelection = false;
			this.router_view.Location = new System.Drawing.Point(2, 41);
			this.router_view.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.router_view.Name = "router_view";
			this.router_view.Size = new System.Drawing.Size(332, 235);
			this.router_view.TabIndex = 7;
			this.router_view.UseCompatibleStateImageBehavior = false;
			// 
			// router_no
			// 
			this.router_no.Text = "No.";
			this.router_no.Width = 40;
			// 
			// router_dst_addr
			// 
			this.router_dst_addr.Text = "DST_ADDR";
			// 
			// router_next_addr
			// 
			this.router_next_addr.Text = "NEXT_ADDR";
			this.router_next_addr.Width = 70;
			// 
			// router_score
			// 
			this.router_score.Text = "SCORE";
			this.router_score.Width = 50;
			// 
			// router_rssi
			// 
			this.router_rssi.Text = "RSSI";
			this.router_rssi.Width = 40;
			// 
			// router_clr
			// 
			this.router_clr.Location = new System.Drawing.Point(267, 10);
			this.router_clr.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.router_clr.Name = "router_clr";
			this.router_clr.Size = new System.Drawing.Size(67, 25);
			this.router_clr.TabIndex = 13;
			this.router_clr.Text = "Clr";
			this.router_clr.UseVisualStyleBackColor = true;
			this.router_clr.Click += new System.EventHandler(this.Router_Clr_Click);
			// 
			// router_del
			// 
			this.router_del.Location = new System.Drawing.Point(64, 10);
			this.router_del.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.router_del.Name = "router_del";
			this.router_del.Size = new System.Drawing.Size(58, 25);
			this.router_del.TabIndex = 14;
			this.router_del.Text = "Del";
			this.router_del.UseVisualStyleBackColor = true;
			this.router_del.Click += new System.EventHandler(this.Router_Del_Click);
			// 
			// UP
			// 
			this.UP.BackColor = System.Drawing.SystemColors.Control;
			this.UP.Controls.Add(this.Ymodem_Down);
			this.UP.Controls.Add(this.Ymodem_Open_file);
			this.UP.Controls.Add(this.label_UploadProgress);
			this.UP.Controls.Add(this.progressBar_Upload);
			this.UP.Controls.Add(this.filepath);
			this.UP.Controls.Add(this.Path_label);
			this.UP.Location = new System.Drawing.Point(4, 22);
			this.UP.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.UP.Name = "UP";
			this.UP.Size = new System.Drawing.Size(340, 296);
			this.UP.TabIndex = 3;
			this.UP.Text = "Update";
			// 
			// Ymodem_Down
			// 
			this.Ymodem_Down.Location = new System.Drawing.Point(182, 88);
			this.Ymodem_Down.Margin = new System.Windows.Forms.Padding(2);
			this.Ymodem_Down.Name = "Ymodem_Down";
			this.Ymodem_Down.Size = new System.Drawing.Size(99, 29);
			this.Ymodem_Down.TabIndex = 80;
			this.Ymodem_Down.Text = "Start";
			this.Ymodem_Down.UseVisualStyleBackColor = true;
			this.Ymodem_Down.Click += new System.EventHandler(this.Ymodem_Down_Click);
			// 
			// Ymodem_Open_file
			// 
			this.Ymodem_Open_file.Location = new System.Drawing.Point(27, 88);
			this.Ymodem_Open_file.Margin = new System.Windows.Forms.Padding(2);
			this.Ymodem_Open_file.Name = "Ymodem_Open_file";
			this.Ymodem_Open_file.Size = new System.Drawing.Size(100, 29);
			this.Ymodem_Open_file.TabIndex = 79;
			this.Ymodem_Open_file.Text = "Load File";
			this.Ymodem_Open_file.UseVisualStyleBackColor = true;
			this.Ymodem_Open_file.Click += new System.EventHandler(this.Ymodem_Open_file_Click);
			// 
			// label_UploadProgress
			// 
			this.label_UploadProgress.AutoSize = true;
			this.label_UploadProgress.BackColor = System.Drawing.Color.Transparent;
			this.label_UploadProgress.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.label_UploadProgress.Font = new System.Drawing.Font("Microsoft YaHei", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
			this.label_UploadProgress.Location = new System.Drawing.Point(142, 197);
			this.label_UploadProgress.Name = "label_UploadProgress";
			this.label_UploadProgress.Size = new System.Drawing.Size(35, 19);
			this.label_UploadProgress.TabIndex = 81;
			this.label_UploadProgress.Text = "0 %";
			// 
			// progressBar_Upload
			// 
			this.progressBar_Upload.Location = new System.Drawing.Point(27, 180);
			this.progressBar_Upload.Margin = new System.Windows.Forms.Padding(2);
			this.progressBar_Upload.Name = "progressBar_Upload";
			this.progressBar_Upload.Size = new System.Drawing.Size(254, 50);
			this.progressBar_Upload.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
			this.progressBar_Upload.TabIndex = 76;
			// 
			// filepath
			// 
			this.filepath.Font = new System.Drawing.Font("SimSun", 13.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
			this.filepath.Location = new System.Drawing.Point(67, 131);
			this.filepath.Margin = new System.Windows.Forms.Padding(2);
			this.filepath.Name = "filepath";
			this.filepath.Size = new System.Drawing.Size(215, 28);
			this.filepath.TabIndex = 77;
			// 
			// Path_label
			// 
			this.Path_label.AutoSize = true;
			this.Path_label.Location = new System.Drawing.Point(24, 136);
			this.Path_label.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.Path_label.Name = "Path_label";
			this.Path_label.Size = new System.Drawing.Size(35, 13);
			this.Path_label.TabIndex = 78;
			this.Path_label.Text = "Path：";
			// 
			// RF_Setting
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(614, 523);
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.save_range);
			this.Controls.Add(this.uart_panel);
			this.Controls.Add(this.reset_time_txt);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.reset_time_lb);
			this.Controls.Add(this.reset_aux_cb);
			this.Controls.Add(this.reset_aux_lb);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
			this.Name = "RF_Setting";
			this.Text = "RF Setting E52 V1.0";
			this.Load += new System.EventHandler(this.RF_Setting_Load);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.uart_panel.ResumeLayout(false);
			this.uart_panel.PerformLayout();
			this.cmd_panel.ResumeLayout(false);
			this.cmd_panel.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.cmd_tb.ResumeLayout(false);
			this.cmd_seting.ResumeLayout(false);
			this.group_set.ResumeLayout(false);
			this.panel3.ResumeLayout(false);
			this.panel3.PerformLayout();
			this.router_set.ResumeLayout(false);
			this.panel4.ResumeLayout(false);
			this.UP.ResumeLayout(false);
			this.UP.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		private IContainer components = null;
		private PictureBox pictureBox1;
		private Panel uart_panel;
		private Label Uart_Para;
		private ComboBox Uart_Para_CB;
		private Label Uart_Baud;
		private ComboBox Uart_Stop_CB;
		private Label Uart_Com_Lb;
		private Button RE;
		private Button FR;
		private Button Write_Cmd;
		private Button Read_Cmd;
		private Button Uart_Bt;
		private ComboBox Uart_Com;
		private SerialPort serialPort1;
		private Panel cmd_panel;
		private Label power_lb;
		private Label save_range;
		private TextBox power_txt;
		private TextBox chan_txt;
		private Label chan_lb;
		private ComboBox baud_cb;
		private Label baud_lb;
		private ComboBox parity_cb;
		private Label parity_lb;
		private ComboBox rate_cb;
		private Label rate_lb;
		private TextBox MSG;
		private Panel panel1;
		private ComboBox option_cb;
		private Label option_lb;
		private TextBox panid_txt;
		private Label panid_lb;
		private ComboBox type_cb;
		private Label type_lb;
		private TextBox src_addr_txt;
		private Label src_addr_lb;
		private TextBox dst_addr_txt;
		private Label dst_addr_lb;
		private Label src_port_lb;
		private ComboBox src_port_cb;
		private ComboBox dst_port_cb;
		private Label dst_port_lb;
		private ComboBox head_cb;
		private Label head_lb;
		private ComboBox security_cb;
		private Label security_lb;
		private ComboBox reset_aux_cb;
		private Label reset_aux_lb;
		private TextBox reset_time_txt;
		private Label reset_time_lb;
		private TextBox key_txt;
		private Label key_lb;
		private TextBox mac_txt;
		private Label mac_lb;
		private Panel panel2;
		private TabControl cmd_tb;
		private TabPage cmd_seting;
		private TabPage group_set;
		private TextBox group_txt;
		private Label group_lb;
		private Button group_del;
		private Button group_add;
		private ListView router_view;
		private ListView group_view;
		private Button group_read;
		private Button group_clr;
		private Button router_clr;
		private Button router_load;
		private Button router_save;
		private Button router_del;
		private Panel panel3;
		private Panel panel4;
		private TabPage router_set;
		private ColumnHeader G1;
		private ColumnHeader G2;
		private ColumnHeader G3;
		private ColumnHeader router_no;
		private ColumnHeader router_dst_addr;
		private ColumnHeader router_next_addr;
		private ColumnHeader router_score;
		private ColumnHeader router_rssi;
		private Button router_read;
		private LinkLabel Clear;
		private TabPage UP;
		private Button Ymodem_Down;
		private Button Ymodem_Open_file;
		private Label label_UploadProgress;
		private ProgressBar progressBar_Upload;
		private TextBox filepath;
		private Label Path_label;
		private ToolTip Tip;
		#endregion
	}
}
