using System;

namespace XEditNet.Licensing
{
	/// <summary>
	/// The type of license
	/// </summary>
	internal class LicenseType : ILicenseItem
	{
		#region Enumerations
		internal enum LicenseTypes : int
		{
			None = 0,
			Trial = 1,
			Full = 2,
			Invalid = 3
		}
		#endregion

		#region Private state members
		private LicenseTypes licenseType;
		#endregion

		#region Constructors
		internal LicenseType(LicenseTypes License)
		{
			Type = License;
		}

		internal LicenseType(int LicenseValue)
		{
			Value = LicenseValue;
		}
		#endregion

		#region Properties
		internal LicenseTypes Type
		{
			get { return licenseType; }
			set
			{
				if (!Enum.IsDefined(typeof(LicenseTypes), value))
					throw new Exception(String.Format("Illegal License operation: License object threw an Out Of Bounds exception attempting to set a unrecognized License type of {0}",
						value));

				licenseType = value;
			}
		}
		#endregion

		#region LicenseItem interface properties
		public int Value
		{
			get { return (int)Type; }
			set { Type = (LicenseTypes)value; }
		}

		public int DataSize
		{
			get { return 3; }
		}
		#endregion
	}
}
