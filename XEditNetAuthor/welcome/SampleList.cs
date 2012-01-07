using System;
using System.Collections;
using System.IO;
using System.Xml.Serialization;

namespace XEditNetAuthor.Welcome
{
	/// <summary>
	/// Summary description for SampleList.
	/// </summary>
	public class SampleList
	{
		[XmlElement("Sample", typeof(Sample))]
		public ArrayList Samples=new ArrayList();

		public static SampleList FromXml(string filename)
		{
			XmlSerializer xs=new XmlSerializer(typeof(SampleList));
			FileStream fs=new FileStream(filename, FileMode.Open);
			try
			{
				SampleList sl=(SampleList) xs.Deserialize(fs);
				return sl;
			}
			finally
			{
				fs.Close();
			}
		}
	}

	public class Sample
	{
		[XmlAttribute]
		public string Name;
		[XmlAttribute]
		public string Description;
		[XmlAttribute]
		public string File;
	}
}
