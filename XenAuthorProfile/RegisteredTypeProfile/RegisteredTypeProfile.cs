using System;
using System.Windows.Forms;
using System.Xml;
using Gui.Wizard;
using XEditNet.Profile;
using XEditNet.Profile.Registered;

namespace XEditNet.Profile.Registered
{
	/// <summary>
	/// This is a wrapper class around a real profile.
	/// </summary>
	public class RegisteredTypeProfile : IXEditNetProfile
	{
		private ProfileInfo profileInfo;
		private IXEditNetProfile profile;

		public RegisteredTypeProfile(ProfileInfo pi)
		{
			profileInfo=pi;

			if ( pi.Profile != null )
				profile=ProfileProvider.GetProfile(pi.Profile, null);
		}

		public UserControl GetEditorRegion(XmlDocument doc)
		{
			if ( profile == null )
				return null;

			return profile.GetEditorRegion(doc);
		}

		public ICreateWizardPlugin GetCreateWizardPlugin()
		{
			if ( profile == null )
				return new RegisteredTypeDefaultWizard(profileInfo);

			ICreateWizardPlugin cwp=profile.GetCreateWizardPlugin();
			if ( cwp == null )
				return new RegisteredTypeDefaultWizard(profileInfo);

			return cwp;
		}

		public ProfileInfo Info
		{
			get { return profileInfo; }
			set { return; }
		}
	}
}
