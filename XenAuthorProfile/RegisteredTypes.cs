using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace XEditNet.Profile
{
	/// <summary>
	/// Summary description for RegisteredTypes.
	/// </summary>
	public class RegisteredTypes
	{
		[XmlElement("Type", typeof(ProfileInfo))]
		public ArrayList Types=new ArrayList();

		public RegisteredTypes()
		{
		}

		public static RegisteredTypes Load()
		{
			Assembly asm=typeof(RegisteredTypes).Assembly;
			Uri baseUri=new Uri(asm.CodeBase);
			Uri typesUri=new Uri(baseUri, "types.xml");

			XmlTextReader xtr=new XmlTextReader(typesUri.AbsoluteUri);
			XmlSerializer serializer = 
				new XmlSerializer(typeof(RegisteredTypes));

			try 
			{
				RegisteredTypes rt=(RegisteredTypes) serializer.Deserialize(xtr);
				return rt;
			}
			catch ( FileNotFoundException )
			{
				return new RegisteredTypes();
			}
			catch ( InvalidOperationException )
			{
				return new RegisteredTypes();
			}
			finally 
			{
				xtr.Close();
			}
		}
	}
}
