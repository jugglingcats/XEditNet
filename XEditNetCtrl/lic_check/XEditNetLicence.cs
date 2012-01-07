using System;
using System.ComponentModel;

namespace XEditNet.Licensing
{
	internal sealed class XEditNetLicence : License
	{
		#region private members
		private string licenseKey;
		private LicenseState validity;
		private DateTime expiryDate;
		#endregion

		#region ConSruction/Destruction
		public XEditNetLicence(string strSavedLicense)
		{
			/*
			 * Constructor, creates and destroys the underlying real BaseLicense, 
			 * providing access to the 3 public Members, LicenseKey, Validity and ExpiryDate
			 * */
			try
			{
				LicenseBase licReal= new LicenseBase(strSavedLicense.ToUpper());
				licenseKey=licReal.LicenseKey;
				validity=XEditNetLicenseValidator.ValidateLicense(LicenseKey);
				if ( licReal.LicenseObject.Type == LicenseType.LicenseTypes.Trial )
					expiryDate=licReal.ExpiryDate.LicenseDateTime;

				licReal=null;
			}
			catch (InvalidLicenseException)
			{
				validity=LicenseState.Invalid;
				expiryDate=DateTime.MinValue;
			}
		}
		public override void Dispose()
		{
				}
		#endregion

		#region Public readonly access
		public override string LicenseKey {get{return licenseKey;}}
		public LicenseState Validity{get{return validity;}}
		public DateTime ExpiryDate{get{return expiryDate;}}
		#endregion
	}
}
