using System;

namespace XEditNet.Licensing
{
	/// <summary>
	/// LicenseBaseByteSerializer.
	/// </summary>
	internal class LicenseBaseByteSerializer
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
		internal LicenseBaseByteSerializer()
		{
			byteArray = new byte[arraySize];
			byteIndex = 0;
			bitIndex = 0;
			bitMask = 1;
		}
		#endregion

		#region Properties
		internal byte[] ByteArray
		{
			get { return byteArray; }
		}
		#endregion

		#region Data append members
		internal void AppendData(ILicenseItem Data)
		{
			int dataValue = Data.Value;
			int dataSize = Data.DataSize;
			int dataMask = 1;

			for(int bit = 0; bit < dataSize; bit++, dataMask <<= 1)
			{
				if ((dataValue & dataMask) != 0)
				{
					byteArray[byteIndex] |= bitMask;
				}

				bitIndex++;
				bitMask <<= 1;

				if (bitIndex >= numberOfBitsInData)
				{
					bitIndex = 0;
					bitMask = 1;

					if (++byteIndex > arraySize)
						throw new Exception("Illegal License operation: LicenseBaseByteSerializer object threw a Buffer Overflow exception in AppendData");
				}
			}
		}
		#endregion
	}
}
