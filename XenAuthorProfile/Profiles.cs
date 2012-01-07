using System;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Reflection;
using System.Xml.Serialization;
using Gui.Wizard;
using XEditNet.Util;

// TODO: H: needs rework for latest form
//			need a way to only override the actual control area, but still bind to
//			element insert, etc.
//			may be simpler just to provide new toolbar/menu merge

namespace XEditNet.Profile
{
	/// <summary>
	/// Summary description for Profiles.
	/// </summary>
	public interface IXEditNetProfile
	{
		UserControl GetEditorRegion(XmlDocument doc);
		ICreateWizardPlugin GetCreateWizardPlugin();
		ProfileInfo Info
		{
			get;
			set;
		}
	}

	public class ProfileInfo
	{
		public ProfileInfo()
		{
		}

		public ProfileInfo(string name)
		{
			Name=name;
		}

		public ProfileInfo(string group, string name)
		{
			Group=group;
			Name=name;
		}

		[XmlAttribute]
		public string Group;

		[XmlAttribute]
		public string Name;

		[XmlAttribute]
		public string PublicId;

		[XmlAttribute]
		public string SystemId;

		[XmlAttribute]
		public string Profile;

		[XmlAttribute]
		public string Root;

		[XmlAttribute]
		public string Stylesheet;
	}

	public interface ICreateWizardPlugin
	{
		WizardPage[] Pages
		{
			get;
		}

		XmlDocument CreateDocument();
	}

	public interface IXEditNetEditorRegion
	{
		XEditNetCtrl Editor
		{
			get;
		}
	}

	public class ProfileProvider
	{
		public static IXEditNetProfile GetProfile(XmlDocument doc)
		{
			if ( doc == null )
				return null;

			XmlProcessingInstruction stylePi=doc.SelectSingleNode(
				"processing-instruction('xeditnet-profile')") as XmlProcessingInstruction;

			if ( stylePi == null )
				return GetNamespaceProfile(doc);

			string spec=stylePi.Data;
			Uri baseUri=GetSafeUri(doc.BaseURI);

			return GetProfile(spec, baseUri);
		}

		private static Uri GetSafeUri(string uri)
		{
			if ( uri == null )
				return null;

			return uri.Length > 0 ? new Uri(uri) : null;
		}

		public static IXEditNetProfile GetProfile(string codebase, Uri baseUri)
		{
			string[] parts=codebase.Split('!');
			if ( parts.Length == 0 )
				throw new ArgumentException("xeditnet-profile must refer to a class");
	
			if ( parts.Length > 2 )
				throw new ArgumentException("xeditnet-profile must be of the form [assembly]!classname");
	
			// TODO: M: exception handling
	
			Assembly asm=typeof(ProfileProvider).Assembly;
			string className=parts[0];
	
			if ( parts.Length > 1 )
			{
				Uri uri=baseUri == null ? new Uri(parts[0]) : new Uri(baseUri, parts[0]);

				if ( !File.Exists(uri.LocalPath) )
					uri=new Uri(new Uri(asm.CodeBase), parts[0]);

				asm=Assembly.LoadFrom(uri.LocalPath);
				if ( asm == null )
					throw new ArgumentException("Failed to load assembly from "+uri.LocalPath);

				className=parts[1];
			}
	
			object o=asm.CreateInstance(className);
			if ( o == null )
				throw new ArgumentException("Error loading class for specified profile");

			IXEditNetProfile ret=o as IXEditNetProfile;

			if ( ret == null )
				throw new ArgumentException("Specified profile must refer to a class implementing IXEditNetProfile");

			return ret;
		}

		private static IXEditNetProfile GetNamespaceProfile(XmlDocument doc)
		{
			if ( doc == null )
				return null;

			if ( doc.DocumentElement == null )
				return null;

			string nsUri=doc.DocumentElement.NamespaceURI;
			if ( nsUri.Length == 0 )
				return null;

			FileInfo fi=FileUtils.FindFile("namespaceProfiles.xml");
			if ( fi == null )
				return null;

			XmlDocument mappings=new XmlDocument();
			mappings.Load(fi.FullName);

			string xpath=string.Format("//Mapping[@Namespace='{0}']", nsUri);
			XmlElement n=(XmlElement) mappings.SelectSingleNode(xpath);
			if ( n == null )
				return null;

			string codebase=n.GetAttribute("Codebase");
			if ( codebase.Length > 0 )
			{
				Uri baseUri=GetSafeUri(doc.BaseURI);
				return GetProfile(codebase, baseUri);
			}
			string stylesheet=n.GetAttribute("Stylesheet");
			return new SimpleStyledProfile(stylesheet);
		}
	}

	internal class SimpleStyledProfile : IXEditNetProfile
	{
		private string stylesheet;

		public SimpleStyledProfile(string stylesheet)
		{
			this.stylesheet=stylesheet;
		}

		public UserControl GetEditorRegion(XmlDocument doc)
		{
			return null;
		}

		public ICreateWizardPlugin GetCreateWizardPlugin()
		{
			return null;
		}

		public ProfileInfo Info
		{
			get 
			{
				ProfileInfo pi=new ProfileInfo();
				pi.Stylesheet=this.stylesheet;
				return pi;
			}
			set { return; }
		}
	}
}
