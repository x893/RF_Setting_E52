namespace loramesh
{
	public enum InitialCrcValue
	{
		Zeros = 0,
		NonZero2 = 7439,  // 0x00001D0F
		NonZero1 = 65535, // 0x0000FFFF
	}

	internal class Crc16Ccitt
	{
		private const ushort poly = 4129;
		private readonly ushort[] table = new ushort[256];
		private readonly ushort initialValue = 0;

		public ushort ComputeChecksum(byte[] bytes, int length)
		{
			ushort checksum = initialValue;
			for (int index = 0; index < length; ++index)
				checksum = (ushort)((uint)checksum << 8 ^ table[checksum >> 8 ^ byte.MaxValue & bytes[index]]);
			return checksum;
		}

		public Crc16Ccitt(InitialCrcValue initial)
		{
			initialValue = (ushort)initial;
			for (int index = 0; index < table.Length; ++index)
			{
				ushort crc = 0;
				ushort mask = (ushort)(index << 8);
				for (int index8 = 0; index8 < 8; ++index8)
				{
					if (((crc ^ mask) & 0x8000) != 0)
						crc = (ushort)(crc << 1 ^ poly);
					else
						crc <<= 1;
					mask <<= 1;
				}
				table[index] = crc;
			}
		}
	}
}
