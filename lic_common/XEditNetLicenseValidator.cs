using System;

namespace XEditNet.Licensing
{
	/// <summary>
	/// The type of license current in force.
	/// </summary>
	public enum LicenseState
	{
		/// Full license.
		Full,
		/// Invalid license.
		Invalid,
		/// No license.
		None,
		/// Trial license that is still active.
		Trial_Active,
		/// Trial license that has expired.
		Trial_Expired
	}

	/// <summary>
	/// Represents an object that can validate an XEditNet licence.
	/// </summary>
	public abstract class XEditNetLicenseValidator
	{
		/// <summary>
		/// Validate a license key.
		/// </summary>
		/// <param name="Key">The key to validate.</param>
		/// <returns>A LicenceState enumeration.</returns>
		public static LicenseState ValidateLicense(string Key)
		{
			try
			{
				LicenseBase license = new LicenseBase(Key);

				if (license.ProductObject.ProductCode != Product.Products.XEditNetCtrl)
					return LicenseState.Invalid;

				if (license.ReleaseObject.Value != 0)
					return LicenseState.Invalid;

				switch (license.LicenseObject.Type)
				{
					case LicenseType.LicenseTypes.Full:
						return LicenseState.Full;

					case LicenseType.LicenseTypes.Invalid:
						return LicenseState.Invalid;

					case LicenseType.LicenseTypes.None:
						return LicenseState.None;

					case LicenseType.LicenseTypes.Trial:
						if (DateTime.Now <= license.ExpiryDate.LicenseDateTime)
							return LicenseState.Trial_Active;
						return LicenseState.Trial_Expired;
				}
			}
			catch
			{
				return LicenseState.Invalid;
			}
			return LicenseState.Invalid;
		}
	}
}
