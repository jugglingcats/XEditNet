using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml;
using Gui.Wizard;
using XEditNet.Profile;
using XEditNet.Util;

namespace XEditNet.Profile.Registered
{
	/// <summary>
	/// Summary description for GenDtdWizard.
	/// </summary>
	public class RegisteredTypeDefaultWizard : ICreateWizardPlugin
	{
		ProfileInfo profileInfo;

		public RegisteredTypeDefaultWizard(ProfileInfo prof)
		{
			profileInfo=prof;	
		}

		public WizardPage[] Pages
		{
			get
			{
				WizardPage wp=new WizardPage();
				return new WizardPage[] {wp};
			}
		}

		public XmlDocument CreateDocument()
		{
			// <?xeditnet-profile ../xenwebprofile/bin/debug/xenwebprofile.dll!XenWebProfile.ProfileImpl?>

			XmlDocument doc=new XmlDocument();
			XmlDocumentType doctype=doc.CreateDocumentType(profileInfo.Root, profileInfo.PublicId, profileInfo.SystemId, null);
			XmlElement root=doc.CreateElement(profileInfo.Root);
			XmlProcessingInstruction pi=doc.CreateProcessingInstruction("xeditnet-profile", profileInfo.Profile);
			
			doc.AppendChild(doctype);
			doc.AppendChild(pi);
			doc.AppendChild(root);
			return doc;
		}
	}
}
