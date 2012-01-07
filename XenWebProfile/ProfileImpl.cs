using System;
using System.Windows.Forms;
using System.Xml;
using Gui.Wizard;
using XEditNet.Profile;

namespace XenWebProfile
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class ProfileImpl : IXEditNetProfile
	{
		private ProfileInfo info=new ProfileInfo("Website", "Default Website");

		public UserControl GetEditorRegion(XmlDocument doc)
		{
			return new WebProfileEditorRegion();
		}

		public ICreateWizardPlugin GetCreateWizardPlugin()
		{
			return null;
		}

		public ProfileInfo Info
		{
			get { return info; }
			set { info=value; }
		}
	}
}
