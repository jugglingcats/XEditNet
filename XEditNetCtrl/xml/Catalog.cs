using System;
using System.Collections;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using XEditNet.Xml.Serialization;

namespace XEditNet.Xml
{
	/// <summary>
	/// Summary description for XmlCatalog.
	/// </summary>
	public class XmlCatalog
	{
		[XmlElement("Entry", typeof(CatalogEntry))]
		public ArrayList Entries=new ArrayList();

		public static XmlCatalog Load()
		{
			Assembly asm=typeof(XmlCatalog).Assembly;
			Uri baseUri=new Uri(asm.CodeBase);
			Uri catalogUri=new Uri(baseUri, "catalog.xml");

			XmlTextReader xtr=new XmlTextReader(catalogUri.AbsoluteUri);
			SimpleXmlSerializer serializer = 
				new SimpleXmlSerializer(typeof(XmlCatalog));

			try 
			{
				XmlCatalog xc=(XmlCatalog) serializer.Deserialize(xtr);
				return xc;
			}
			finally 
			{
				xtr.Close();
			}
		}

		public string GetSystemId(string publicId)
		{
			foreach ( CatalogEntry e in Entries )
			{
				if ( e.PublicId.Equals(publicId) )
					return e.SystemId;
			}
			return null;
		}
	}

	public class CatalogEntry
	{
		[XmlAttribute]
		public string PublicId;

		[XmlAttribute]
		public string SystemId;
	}
}

/*
<Catalog>
<Entry PublicId="-//DTD//Docbook//EN" SystemId="c:/tmp/abc.dtd"/>
</Catalog>

<RegisteredTypes>
<Type Group="Docbook" Name="Docbook Book" PublicId="-//DTD//Docbook//EN" Profile="Customisations.dll!Docbook" Root="book"/>
<Type Group="Docbook" Name="Docbook Chapter" Profile="Customisations.dll!Docbook" Root="chapter"/>
</RegisteredTypes>
*/

