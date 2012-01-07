using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace XEditNet.Licensing
{
	internal class XEditNetLicenseContext: LicenseContext
	{
		#region private members
		private readonly string strHiveLocation="SOFTWARE\\XEditNet";
		#endregion

		#region Construction
		public XEditNetLicenseContext()
		{
			//Nothing to construct in this Version
		}
		#endregion

		#region required overrides
		public override string GetSavedLicenseKey(Type type,Assembly resourceAssembly)
		{
            Microsoft.Win32.RegistryKey key1 = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(strHiveLocation);
			if ( key1 == null )
				return string.Empty;

			object o=key1.GetValue("key");
			if ( o == null )
				return string.Empty;

			return o.ToString().ToUpper();
		}

		public override void SetSavedLicenseKey(Type type,string key)
		{
			try
			{
                Microsoft.Win32.RegistryKey key1 = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(strHiveLocation, true);
				key1.SetValue("key",key.ToUpper());
			}
			catch
			{
				Microsoft.Win32.RegistryKey key1 = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(strHiveLocation);
                key1 = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(strHiveLocation, true);
				key1.SetValue("key",key.ToUpper());
			}
			
		}
		public override object GetService(Type type)
		{
			// No Service In this Version
			return null;
		}

		public override LicenseUsageMode UsageMode 
		{
			get
			{						
				return LicenseUsageMode.Designtime;
			}
		}
		#endregion
	}
}
