using System;
using System.Text;

namespace XEditNet.Licensing
{
	/// <summary>
	/// Converts license keys to a byte array and vice versa
	/// </summary>
	internal abstract class LicenseKeyConvertor
	{
		#region Constants
		private const int keyLength = 25;			// Length of key
		private const int seperationFrequency = 5;	// frequency of seperators(in hypens per character)
		private const char seperator = '-';			// Seperator
		#endregion

		#region Static read-onlies (set by type constructor)
		private static readonly int keyLengthSeperated;
		#endregion

		#region Static (type) constructor
		static LicenseKeyConvertor()
		{
			keyLengthSeperated = keyLength + (int)Math.Ceiling((double)keyLength / seperationFrequency) - 1;
		}
		#endregion

		#region Byte Array to / from Key conversion members
		internal static string KeyFromByteArray(byte []ByteArray)
		{
			if (ByteArray.Length > keyLength)
				throw new InvalidLicenseException("License key is invalid");

			int seperationCounter = 0;
			StringBuilder sb = new StringBuilder(keyLengthSeperated, keyLengthSeperated);
			foreach(byte myByte in ByteArray)
			{
				if (seperationFrequency == seperationCounter++)
				{
					sb.Append(seperator);
					seperationCounter = 1;
				}
				sb.Append(LicenseCharacterConvertor.CharacterValue(myByte));
			}
			return sb.ToString();
		}

		internal static byte[] ByteArrayFromKey(string Key)
		{
			if (Key.Length != keyLengthSeperated)
				throw new InvalidLicenseException("License key is invalid");

			byte []returnArray = new byte[keyLength];
			char []charArray = Key.ToCharArray();
			int seperationCounter = 0;
			int returnArrayIndex = 0;
			foreach(char keyCharacter in charArray)
			{
				if (seperationFrequency != seperationCounter++)
					returnArray[returnArrayIndex++] = LicenseCharacterConvertor.ValueOf(keyCharacter);
				else
					seperationCounter = 0;
			}

			return returnArray;
		}

		#endregion
	}

	internal class InvalidLicenseException : Exception
	{
		public InvalidLicenseException(string s) : base(s)
		{
		}
	}
}
