using System;
using System.ComponentModel;

namespace XEditNet.Licensing
{
	/// This object is referenced by the standard Microsoft License Provider and is not called directly
	internal class XEditNetLicenseProvider : System.ComponentModel.LicenseProvider
	{
		public XEditNetLicenseProvider()
		{
		}

		public override System.ComponentModel.License GetLicense(	LicenseContext context,
									Type type,
									object instance,
									bool allowExceptions) 
		{
			/* the context passed in can be used to determine runtime or designtime usage, however it does not handle 
			 * saved license keys and therfore we have our own to handle that.
			 * There is a resource drain if the requesting controls do not release the License generated and returned
			 * to them.
			 */
			XEditNetLicenseContext lcTemp = new XEditNetLicenseContext();
			return new XEditNetLicence(lcTemp.GetSavedLicenseKey(typeof(XEditNetLicenseProvider),System.Reflection.Assembly.GetExecutingAssembly()));
		}
	}
}
