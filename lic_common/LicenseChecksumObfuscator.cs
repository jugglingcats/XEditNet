using System;

namespace XEditNet.Licensing
{
	/// <summary>
	/// ByteObfuscator
	/// </summary>
	internal abstract class LicenseChecksumObfuscator
	{
		#region Constants
		private const int numberOfBitsInData = 5;
		private static readonly byte[] obfuscationArray;
		#endregion

		#region Static (type) constructor
		static LicenseChecksumObfuscator()
		{
			// 25 random 0-31 numbers
			obfuscationArray = new byte[]{ 12,22,7,31,4,17,23,0,28,14,
											4,30,29,3,13,20,19,8,5,16,
											11,29,9,1,21};
		}
		#endregion

		#region Obfuscate / De-obfuscate members
		internal static byte[] Obfuscate(byte[] ByteArray)
		{
			ValidateParameters(ByteArray);

			byte[] returnArray = (byte[])ByteArray.Clone();

			ApplyArray(ref returnArray);
			ObfuscateBits(ref returnArray);

			return returnArray;
		}

		internal static byte[] DeObfuscate(byte[] ByteArray)
		{
			ValidateParameters(ByteArray);

			byte[] returnArray = (byte[])ByteArray.Clone();

			DeobfuscateBits(ref returnArray);
			ApplyArray(ref returnArray);

			return returnArray;
		}
		#endregion

		#region Data validation
		private static void ValidateParameters(byte[] ByteArray)
		{
			if (ByteArray.Length != obfuscationArray.Length)
				throw new Exception("Illegal License operation: LicenseChecksumObfuscator object threw an array size exception performing ValidateParameters");
		}
		#endregion

		#region Bit obfuscation code
		private static void ObfuscateBits(ref byte[] ByteArray)
		{
//			return;
//
			for (int pass = 1; pass <= 16; pass <<= 1)		// Exponential for next loop
			{
				for (int byteIndex = 0; byteIndex < ByteArray.Length; byteIndex++)
				{
					byte mask = 1;
					int byteToMask = byteIndex;
					for (int bitIndex = 0; bitIndex < numberOfBitsInData; bitIndex++)
					{
						byteToMask += pass;
						if (byteToMask >= ByteArray.Length)
							byteToMask -= ByteArray.Length;

						SwapBits(ref ByteArray, byteIndex, byteToMask, mask);
						mask <<= 1;
					}
				}
			}
		}

		private static void DeobfuscateBits(ref byte[] ByteArray)
		{
//			return;
//
			for (int pass = 16; pass >= 1; pass >>= 1)		// Exponential for next loop
			{
				for (int byteIndex = ByteArray.Length - 1; byteIndex >= 0; byteIndex--)
				{
					byte mask = 1 << (numberOfBitsInData - 1);
					int byteToMask = (byteIndex + ((numberOfBitsInData + 1) * pass)) % ByteArray.Length;
					for (int bitIndex = numberOfBitsInData - 1; bitIndex >= 0; bitIndex--)
					{
						byteToMask -= pass;
						if (byteToMask < 0)
							byteToMask += ByteArray.Length;

						SwapBits(ref ByteArray, byteIndex, byteToMask, mask);
						mask >>= 1;
					}
				}
			}
		}

		private static void SwapBits(ref byte[] ByteArray, int byte1, int byte2, byte mask)
		{
			byte work = (byte)(ByteArray[byte1] & mask);
			ByteArray[byte1] &= (byte)(~mask);
			ByteArray[byte1] |= (byte)(ByteArray[byte2] & mask);
			ByteArray[byte2] &= (byte)(~mask);
			ByteArray[byte2] |= work;
		}

		private static void ApplyArray(ref byte[] ByteArray)
		{
			for (int index = 0; index < ByteArray.Length; index++)
			{
				ByteArray[index] ^= obfuscationArray[index];
			}
		}
		#endregion
	}
}
