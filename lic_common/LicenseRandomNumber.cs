using System;

namespace XEditNet.Licensing
{
	internal class LicenseRandomNumber : ILicenseItem
	{
		#region Private state members
		private byte licenseRandomNumber;
		#endregion

		#region Constructors
		internal LicenseRandomNumber(System.Random randomGenerator)
		{
			GenerateNewNumber(randomGenerator);
		}
		#endregion

		#region Number Generation
		internal void GenerateNewNumber(System.Random randomGenerator)
		{
			byte[] buffer = new byte[1];
			randomGenerator.NextBytes(buffer);
			licenseRandomNumber = buffer[0];
		}
		#endregion

		#region LicenseItem interface properties
		public int Value
		{
			get { return licenseRandomNumber; }
			set
			{
				if ((value < 0) || (value > 255))
					throw new Exception(String.Format("Illegal License operation: LicenseNumber object threw an Out Of Bounds exception attempting to set a Value number of {0} - which is out of byte bounds",
						value));
				licenseRandomNumber = (byte)value;
			}
		}

		public int DataSize
		{
			get { return 8; }
		}
		#endregion

	}
}
