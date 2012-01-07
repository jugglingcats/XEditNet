using System;

namespace XEditNet.Licensing
{
	/// <summary>
	/// LicenseBaseByteDeserializer
	/// </summary>
	internal class LicenseBaseByteDeserializer
	{
		#region Constants
		private const int arraySize = 20;
		private const int numberOfBitsInData = 5;
		#endregion

		#region State members
		private byte[] byteArray;
		private int byteIndex;
		private int bitIndex;
		private byte bitMask;
		#endregion

		#region Constructors
		internal LicenseBaseByteDeserializer(byte[] ByteArray)
		{
			byteArray = (byte[])ByteArray.Clone();
			byteIndex = 0;
			bitIndex = 0;
			bitMask = 1;
		}
		#endregion

		#region Data append members
		internal void ReadData(ILicenseItem Data)
		{
			int dataSize = Data.DataSize;
			int dataValue = 0;
			int dataMask = 1;

			for (int bit = 0; bit < dataSize; bit++, dataMask <<= 1)
			{
				if ((byteArray[byteIndex] & bitMask) != 0)
				{
					dataValue |= dataMask;
				}

				bitIndex++;
				bitMask <<= 1;

				if (bitIndex >= numberOfBitsInData)
				{
					bitIndex = 0;
					bitMask = 1;

					if (++byteIndex > arraySize)
						throw new Exception("Illegal License operation: LicenseBaseByteDeserializer object threw a Buffer Overflow exception in ReadData");
				}
			}

			Data.Value = dataValue;
		}
		#endregion
	}
}
