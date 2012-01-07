using System;
using System.Windows.Forms;
using System.Xml;
using Gui.Wizard;
using XEditNet.Profile;

namespace XEditNet.Profile.SimpleDtd
{
	/// <summary>
	/// Summary description for GenDtdProfile.
	/// </summary>
	public class BlankDocumentProfile : IXEditNetProfile
	{
		public BlankDocumentProfile()
		{
		}

		public UserControl GetEditorRegion(XmlDocument doc)
		{
			// this profile doesn't provide a special form
			return null;
		}

		public ICreateWizardPlugin GetCreateWizardPlugin()
		{
			return null;
		}

		public ProfileInfo Info
		{
			get { return new ProfileInfo("Blank Document"); }
			set { return; }
		}
	}
}
