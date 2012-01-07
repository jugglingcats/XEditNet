using System;

namespace XEditNet.Licensing
{
	/// <summary>
	/// Converts characters in licenses to their byte value
	/// </summary>
	internal abstract class LicenseCharacterConvertor
	{
		#region Static read-onlies (set by type constructor)
		private static readonly char[] lookUp;
		#endregion

		#region Static (type) constructor
		static LicenseCharacterConvertor()
		{
			// 32 characters (row switched for obfuscation)
			lookUp = new char[]{
				'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 
				'2', '3', '4', '5', '6', '7', '8', '9',
				'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
				'J', 'K', 'L', 'M', 'N', 'P', 'Q', 'R'
				};
		}
		#endregion

		#region Character to / from byte value conversion members
		internal static byte ValueOf(char Character)
		{
			for (byte Index = 0; Index <= lookUp.Length; Index++)
			{
				if (lookUp[Index] == Character)
					return Index;
			}
			throw new Exception(String.Format("Illegal License operation: LicenseCharacterConvertor object threw a Character Not Found exception attempting to determine ValueOf character '{0}'",
				Character));
		}

		internal static char CharacterValue(byte Byte)
		{
			if (Byte >= lookUp.Length)
			{
				throw new Exception(String.Format("Illegal License operation: LicenseCharacterConvertor object threw an Out Of Bounds exception attempting to determine a CharacterValue from the value of {0}",
					Byte));
			}
			return lookUp[Byte];
		}
		#endregion
	}
}
