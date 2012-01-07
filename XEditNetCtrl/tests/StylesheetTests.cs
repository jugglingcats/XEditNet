using System;
using System.Reflection;
using System.IO;
using System.Xml;
using System.Xml.XPath;

using NUnit.Framework;

namespace XEditNet.TestSuite
{
	/// Stylesheet tests.
	[TestFixture]
	public class StylesheetTests
	{
		/// Internal XEditNet test.
		[Test]
		public void RelativeXPaths()
		{
			XmlDocument doc1=new XmlDocument();

			doc1.LoadXml("<doc xmlns='ns'><elem testatt='test'><elem/></elem></doc>");

			// get ref to 'elem' node
			XmlNode i=doc1.DocumentElement.FirstChild;
			XPathNavigator nav=i.CreateNavigator();

			XPathExpression xp=doc1.DocumentElement.CreateNavigator().Compile("x:elem");
			XmlNamespaceManager xnm=new XmlNamespaceManager(doc1.NameTable);
			xnm.AddNamespace("x", "ns");
			xp.SetContext(xnm);
			Assert.IsTrue(nav.Matches(xp), "Failed compiled xpath test");
		}

	}
}
