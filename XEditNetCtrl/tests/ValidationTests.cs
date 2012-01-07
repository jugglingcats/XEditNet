using System;
using System.Reflection;
using System.IO;
using System.Xml;

using NUnit.Framework;
using XEditNet.Styles;
using XEditNet.Validation;
using XEditNet.Location;
using XEditNet.Dtd;

namespace XEditNet.TestSuite
{
	/// Validation tests.
	[TestFixture]
	public class ValidationTests
	{
		private XmlDocument doc;
		private ValidationManager v;

		/// Internal XEditNet test.
		[SetUp]
		public void ValidationSetup()
		{
			Console.WriteLine("Init setup for tests");

			Assembly a = typeof(ValidationTests).Assembly;
			string name = a.FullName.Split(',')[0]+".validationSample.xml";
			Stream stm = a.GetManifestResourceStream(name);
			if ( stm == null )
				throw new FileNotFoundException("Failed to load sample XML for validation from manifest");
			XmlTextReader xtr=new XmlTextReader(stm);
			XmlValidatingReader xvr=new XmlValidatingReader(xtr);

			doc=new XmlDocument();
			doc.Load(xvr);

			v=new ValidationManager();
			v.Attach(doc, null);

			stm.Close();
		}

		/// Internal XEditNet test.
		[Test]
		public void AddSectionNoId()
		{
			XmlElement elem=doc.CreateElement("sect");
			doc.DocumentElement.AppendChild(elem);
			Assert.AreEqual(1, v.InvalidNodes.Count);
		}

		/// Internal XEditNet test.
		[Test]
		public void RequiredAttribute()
		{
			XmlElement img=doc.CreateElement("img");

			doc.DocumentElement.AppendChild(img);

			foreach ( ValidationError ve in v.InvalidNodes.AllErrors )
				Console.WriteLine(ve.Message);

			// required attribute missing
			Assert.AreEqual(1, v.InvalidNodes.AllErrors.Length);

			img.SetAttribute("src", "testval");

			// should have removed the 1 missing attribute errors
			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length);

			img.SetAttribute("src", "testval2");
			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length);

			img.RemoveAttribute("src");
			// require attribute error should be back
			Assert.AreEqual(1, v.InvalidNodes.AllErrors.Length);
		}

		/// Internal XEditNet test.
		[Test]
		public void UndefinedAttribute()
		{
			XmlElement para=doc.CreateElement("para");

			doc.DocumentElement.AppendChild(para);

			// no errors at this point
			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length);

			para.SetAttribute("undefined", "testval");
			// should have 1 error
			Assert.AreEqual(1, v.InvalidNodes.AllErrors.Length, "Expected error for undefined attribute");

			para.SetAttribute("undefined", "testval2");
			// should still have 1 error
			Assert.AreEqual(1, v.InvalidNodes.AllErrors.Length, "Expected error to remain after changing attribute");

			para.RemoveAttribute("undefined");
			// should be gone now
			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length, "Expected error to be removed with undefined attribute");
		}

		/// Internal XEditNet test.
		[Test]
		public void DuplicateIdNested()
		{
			XmlElement a1=doc.CreateElement("anchor");
			a1.SetAttribute("id", "id1");

			XmlElement sect=doc.CreateElement("sect");
			XmlElement title=doc.CreateElement("title");
			sect.AppendChild(title);

			XmlElement a2=doc.CreateElement("anchor");
			a2.SetAttribute("id", "id1");

			sect.AppendChild(a2);

			// no errors at this point (not added to doc yet)
			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length);

			doc.DocumentElement.AppendChild(a1);
			doc.DocumentElement.AppendChild(sect);

			// one error
			Assert.AreEqual(2, v.InvalidNodes.AllErrors.Length, "Expected duplicate ids");

			doc.DocumentElement.RemoveChild(sect);
			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length, "Duplicate id error was not removed (nested)");
		}

		/// Internal XEditNet test.
		[Test]
		public void SimpleDuplicateId()
		{
			XmlElement a1=doc.CreateElement("anchor");
			a1.SetAttribute("id", "id1");

			XmlElement a2=doc.CreateElement("anchor");
			a2.SetAttribute("id", "id1");

			// no errors at this point (not added to doc yet)
			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length);

			doc.DocumentElement.AppendChild(a1);
			doc.DocumentElement.AppendChild(a2);

			Assert.AreEqual(2, v.InvalidNodes.AllErrors.Length, "Expected duplicate ids");

			doc.DocumentElement.RemoveChild(a2);

			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length, "Errors remain after removing duplicate");
		}

		/// Internal XEditNet test.
		[Test]
		public void DuplicateIdInEntity()
		{
			XmlElement a1=doc.CreateElement("anchor");
			a1.SetAttribute("id", "id1");

			XmlEntityReference xer=doc.CreateEntityReference("test");

			// no errors at this point (not added to doc yet)
			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length);

			doc.DocumentElement.AppendChild(a1);
			doc.DocumentElement.AppendChild(xer);

			Assert.AreEqual(2, v.InvalidNodes.AllErrors.Length, "Expected duplicate ids");

			doc.DocumentElement.RemoveChild(a1);

			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length, "Errors remain after removing duplicate");
		}

		/// Internal XEditNet test.
		[Test]
		public void DuplicateIdInEntityAttached()
		{
			v.Detach();

			XmlElement a1=doc.CreateElement("anchor");
			a1.SetAttribute("id", "id1");

			XmlEntityReference xer=doc.CreateEntityReference("test");

			// no errors at this point (not added to doc yet)
			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length);

			doc.DocumentElement.AppendChild(a1);
			doc.DocumentElement.AppendChild(xer);

			v=new ValidationManager();
			v.Attach(doc, null);

			Assert.AreEqual(2, v.InvalidNodes.AllErrors.Length, "Expected duplicate ids");

			doc.DocumentElement.RemoveChild(a1);

			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length, "Errors remain after removing duplicate");
		}

		/// Internal XEditNet test.
		[Test]
		public void UnknownElementInEntityAttached()
		{
			v.Detach();

			XmlEntityReference xer=doc.CreateEntityReference("unk");

			// no errors at this point (not added to doc yet)
			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length);

			doc.DocumentElement.AppendChild(xer);

			v=new ValidationManager();
			v.Attach(doc, null);
			v.ValidateAll();

			Assert.AreEqual(2, v.InvalidNodes.AllErrors.Length, "Expected some errors");

			doc.DocumentElement.RemoveChild(xer);

			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length, "Errors remain after removing duplicate");
		}

		/// Internal XEditNet test.
		[Test]
		public void DuplicateIdMultiple()
		{
			XmlElement a1=doc.CreateElement("anchor");
			a1.SetAttribute("id", "id1");

			XmlElement a2=doc.CreateElement("anchor");
			a2.SetAttribute("id", "id1");

			XmlElement a3=doc.CreateElement("anchor");
			a3.SetAttribute("id", "id1");

			// no errors at this point (not added to doc yet)
			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length);

			doc.DocumentElement.AppendChild(a1);
			doc.DocumentElement.AppendChild(a2);
			doc.DocumentElement.AppendChild(a3);

			Assert.AreEqual(3, v.InvalidNodes.AllErrors.Length, "Expected duplicate ids");

			doc.DocumentElement.RemoveChild(a1);
			Assert.AreEqual(2, v.InvalidNodes.AllErrors.Length, "Wrong number of errors after removal");

			a3.GetAttributeNode("id").Value="id2";
			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length, "Wrong number of errors after attribute value change");

			a2.SetAttribute("id", "id2");
			Assert.AreEqual(2, v.InvalidNodes.AllErrors.Length, "Wrong number of errors after SetAttribute change");
		}

		/// Internal XEditNet test.
		[Test]
		public void IdRefWhenIdRemoved()
		{
			XmlElement a1=doc.CreateElement("anchor");
			a1.SetAttribute("id", "id1");

			XmlElement a2=doc.CreateElement("xref");
			a2.SetAttribute("idref", "id1");

			doc.DocumentElement.AppendChild(a1);
			doc.DocumentElement.AppendChild(a2);

			// no errors at this point (id and idref match)
			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length);

			doc.DocumentElement.RemoveChild(a1);
			Assert.AreEqual(1, v.InvalidNodes.AllErrors.Length, "Wrong number of errors after removal");

			doc.DocumentElement.AppendChild(a1);
			// no errors at this point (id and idref match)
			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length, "Expected no errors after adding id back in");
		}

		/// Internal XEditNet test.
		[Test]
		public void DocumentElementRemove()
		{
			doc.RemoveAll();
		}

		/// Internal XEditNet test.
		[Test]
		public void NonSchemaElementRemove()
		{
			XmlElement a1=doc.CreateElement("unknown");
			doc.DocumentElement.AppendChild(a1);
			Assert.AreEqual(1, v.InvalidNodes.AllErrors.Length, "Expected errors for unknown element");
			doc.DocumentElement.RemoveChild(a1);
			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length, "Expected no errors after removing unknown elem");
		}

		/// Internal XEditNet test.
		[Test]
		public void DefaultedAttribute()
		{
			XmlElement def=doc.CreateElement("defaulted");

			// no errors at this point (not added to doc yet)
			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length);

			doc.DocumentElement.AppendChild(def);

			XmlName xn=new XmlName(def.GetAttributeNode("att"));
			Console.WriteLine("Attribute name {0}", xn);

			// should still be no errors
			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length, "Insert of defaulted attribute should not introduce errors");
		}

		/// Internal XEditNet test.
		[Test]
		public void PrebuildWithErrors()
		{
			XmlElement sect=doc.CreateElement("sect");
			XmlElement subsect=doc.CreateElement("sect");

			XmlElement para=doc.CreateElement("para");

			subsect.AppendChild(para);
			subsect.SetAttribute("undefined", "testval");

			sect.AppendChild(subsect);
			// should be no errors at this point (nothing added to doc yet)
			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length, "Errors before nodes added to doc");

			doc.DocumentElement.AppendChild(sect);
			// should be errors (two node not allowed, and one undefined attribute)
			Assert.AreEqual(3, v.InvalidNodes.AllErrors.Length, "Wrong number of errors after final insert");
		}

		/// Internal XEditNet test.
		[Test]
		public void ChangeChild()
		{
			XmlElement sect=doc.CreateElement("sect");
			XmlElement title=doc.CreateElement("title");
			sect.AppendChild(title);
			sect.AppendChild(doc.CreateElement("para"));

			doc.DocumentElement.AppendChild(sect);
			doc.DocumentElement.AppendChild(sect.CloneNode(true));

			XmlElement n=doc.CreateElement("title");

			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length, "Unexpected errors after setup");
			SelectionManager.Change(Selection.Empty, sect, n);
			// title in wrong place plus two invalid child nodes
			Assert.AreEqual(3, v.InvalidNodes.AllErrors.Length, "Wrong number of errors after change");

		}

		/// Internal XEditNet test.
		[Test]
		public void ValidMixedContent()
		{
			XmlElement p=doc.CreateElement("para");
			XmlElement a=doc.CreateElement("anchor");
			p.AppendChild(a);
			doc.DocumentElement.AppendChild(p);

			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length, "Unexpected errors after setup");

		}

		/// Internal XEditNet test.
		[Test]
		public void ValidInsert()
		{
			XmlElement sect=doc.CreateElement("sect");
			XmlElement p=doc.CreateElement("other");
			sect.AppendChild(p);
			doc.DocumentElement.AppendChild(sect);

			Assert.AreEqual(2, v.InvalidNodes.AllErrors.Length, "Expected error after setup");
			ElementListItem[] items=v.GetValidElements(sect, p, false);
			Console.WriteLine("Possible inserts: {0}", items.Length);
			Assert.AreEqual(1, items.Length);
		}

		/// Internal XEditNet test.
		[Test]
		public void BugElementNotAllowedHere()
		{
			// too many were being returned when two required titles in sect
			XmlElement sect=doc.CreateElement("sect");
			XmlElement t1=doc.CreateElement("title");
			XmlElement t2=doc.CreateElement("title");
			sect.AppendChild(t1);
			sect.AppendChild(t2);
			doc.DocumentElement.AppendChild(sect);

			Assert.AreEqual(1, v.InvalidNodes.AllErrors.Length, "Expected a single error");
		}


		/// Internal XEditNet test.
		[Test]
		public void BackspaceOverPara()
		{
			XmlElement para=doc.CreateElement("para");

			doc.DocumentElement.AppendChild(para);

			XmlText text=doc.CreateTextNode("testing...");
			para.AppendChild(text);

			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length, "Errors before backspace");

			TextSelectionPoint tsp=new TextSelectionPoint(text, 0);
			Selection sel=new Selection(tsp);

			SelectionManager selManager=new SelectionManager(new Stylesheet());
			selManager.Backspace(sel);

			Assert.AreEqual(1, v.InvalidNodes.AllErrors.Length, "Expected error after backspace");

			para=doc.CreateElement("para");
			doc.DocumentElement.AppendChild(para);

			para.AppendChild(text);

			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length, "Errors still exist after undo");
		}

		/// Internal XEditNet test.
		[Test]
		public void NewDocumentWithIdRefErrors()
		{
			v.Detach();
			v=new ValidationManager();

			XmlElement a1=doc.CreateElement("anchor");
			a1.SetAttribute("id", "id1");

			XmlElement a2=doc.CreateElement("anchor");
			a2.SetAttribute("id", "id1");

			XmlElement l1=doc.CreateElement("xref");
			l1.SetAttribute("idref", "undefined");

			doc.DocumentElement.AppendChild(a1);
			doc.DocumentElement.AppendChild(a2);
			doc.DocumentElement.AppendChild(l1);

			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length, "Errors before attaching");

			v.Attach(doc, null);

			Assert.AreEqual(3, v.InvalidNodes.AllErrors.Length, "Expected errors after attaching");
		}

		/// Internal XEditNet test.
		[Test]
		public void NewDocumentWithValidationErrors()
		{
			v.Detach();
			v=new ValidationManager();

			XmlElement a1=doc.CreateElement("unknown");
			doc.DocumentElement.AppendChild(a1);

			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length, "Errors before attaching");

			v.Attach(doc, null);
			v.ValidateAll();
			Assert.AreEqual(1, v.InvalidNodes.AllErrors.Length, "Expected errors after attaching");
		}

		/// Internal XEditNet test.
		// disabled at present [Test]
		public void XmlNamespace()
		{
			XmlElement x=doc.CreateElement("nstest");

			doc.DocumentElement.AppendChild(x);
			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length, "Didn't expect error yet");

			x.SetAttribute("test", "dummyns", "testing...");

			foreach ( XmlAttribute a in x.Attributes )
				Console.WriteLine("{0}, prefix={1}, namespace={2}", a.Name, a.Prefix, a.NamespaceURI);

			Console.WriteLine(x.OuterXml);

			// this was throwing errors
			Assert.AreEqual(0, v.InvalidNodes.AllErrors.Length);
		}
	}
}
