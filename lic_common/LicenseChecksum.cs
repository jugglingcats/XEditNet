using System;

namespace XEditNet.Licensing
{
	/// <summary>
	/// License checksum application and validation
	/// </summary>
	internal abstract class LicenseChecksum
	{
		#region Constant fields
		private const int checksumFrequency = 5;
		#endregion

		#region Append Checksum members
		internal static byte[] AppendChecksum(byte[] ByteArray)
		{
			int checksumInsertFrequency = (checksumFrequency - 1);
			if ((ByteArray.Length % checksumInsertFrequency) != 0)
			{
				throw new Exception(String.Format("Illegal License operation: LicenseChecksum object threw an array size granularity exception performing AppendChecksum against array of length {0} - must be a multiple of {1} long",
					ByteArray.Length, checksumInsertFrequency));
			}
			byte[] returnArray = new byte[ByteArray.Length + (ByteArray.Length / checksumInsertFrequency)];

			int byteIndex = 0;
			int checksumBlockCount = 0;
			byte checksum = 0;
			foreach(byte myByte in ByteArray)
			{
				returnArray[byteIndex++] = myByte;
				checksum ^= myByte;
				if (++checksumBlockCount >= checksumInsertFrequency)
				{
					returnArray[byteIndex++] = checksum;
					checksum = 0;
					checksumBlockCount = 0;
				}
			}

			return returnArray;
		}
		#endregion

		#region Checksum validation and stripping members
		internal static byte[] ValidateAndStripChecksum(byte[] ByteArray)
		{
			if ((ByteArray.Length % checksumFrequency) != 0)
			{
				throw new Exception(String.Format("Illegal License operation: LicenseChecksum object threw an array size granularity exception performing ValidateChecksum against array of length {0} - must be a multiple of {1} long",
					ByteArray.Length, checksumFrequency));
			}
			byte[] returnArray = new byte[ByteArray.Length - (ByteArray.Length / checksumFrequency)];

			int byteIndex = 0;
			int checksumBlockCount = 0;
			byte checksum = 0;
			foreach(byte myByte in ByteArray)
			{
				if (++checksumBlockCount < checksumFrequency)
				{
					returnArray[byteIndex++] = myByte;

					checksum ^= myByte;
				}
				else
				{
					if (myByte != checksum)
						throw new Exception("Illegal License operation: LicenseChecksum object threw an Invalid Checksum exception performing ValidateAndStripChecksum");

					checksum = 0;
					checksumBlockCount = 0;
				}
			}

			return returnArray;
		}
		#endregion
	}
}
