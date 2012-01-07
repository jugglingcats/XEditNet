using System;
using System.Reflection;
using System.IO;
using System.Xml;

using NUnit.Framework;
using XEditNet.Location;
using XEditNet.Styles;

namespace XEditNet.TestSuite
{
	/// Namespace tests.
	[TestFixture]
	public class NamespaceTests
	{
//		private static readonly string NS1="http://xeditnet.com/ns1#";
//		private static readonly string NS2="http://xeditnet.com/ns2#";

		private SelectionManager selectionManager=new SelectionManager(new Stylesheet());
		/// Internal XEditNet test.
		[Test]
		public void BasicUris()
		{
			XmlDocument doc1=new XmlDocument();
			XmlDocument doc2=new XmlDocument();

			doc1.LoadXml("<maindoc xmlns='ns'/>");
			doc2.LoadXml("<subdoc xmlns='ns'><elem/></subdoc>");

			XmlNode i=doc1.ImportNode(doc2.DocumentElement.FirstChild, true);

			doc1.DocumentElement.AppendChild(i);

			Assert.AreEqual(0, i.Attributes.Count, "Expected no attributes on imported node");
		}

		/// Internal XEditNet test.
		[Test]
		public void ReusedPrefixes()
		{
			XmlDocument doc1=new XmlDocument();

			doc1.LoadXml("<x:doc xmlns:x='ns1'><x:sect xmlns:x='ns2'/></x:doc>");
			XmlAttribute a=doc1.CreateAttribute("x:t", "ns3");
			doc1.DocumentElement.Attributes.Append(a);

			Console.WriteLine(a.Name);

			Console.WriteLine(doc1.OuterXml);
		}

		/// Internal XEditNet test.
		[Test]
		public void CutAndPaste()
		{
			XmlDocument doc1=new XmlDocument();

			doc1.LoadXml("<maindoc xmlns='ns'><elem><subelem/></elem></maindoc>");

			SelectionPoint start=new ElementSelectionPoint(doc1.DocumentElement.FirstChild, TagType.StartTag);
			SelectionPoint end=new ElementSelectionPoint(doc1.DocumentElement, TagType.EndTag);
			Selection sel=new Selection(start, end);

			sel=selectionManager.Cut(sel);
			selectionManager.Paste(sel);

			Console.WriteLine(doc1.OuterXml);

			Assert.AreEqual(0, doc1.DocumentElement.FirstChild.Attributes.Count, "Expected pasted node to have no attributes");
		}
	}
}
