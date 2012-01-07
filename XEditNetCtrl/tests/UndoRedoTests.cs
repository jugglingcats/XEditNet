using System;
using System.Xml;
using NUnit.Framework;
using XEditNet.Location;
using XEditNet.Undo;

namespace XEditNet.TestSuite
{
	/// Undo/redo tests.
	[TestFixture]
	public class UndoRedoTests : IUndoContextProvider
	{
		private XmlDocument doc;
		private UndoManager u;

		/// Internal XEditNet test.
		[SetUp]
		public void ValidationSetup()
		{
			Console.WriteLine("Init setup for tests");

			doc=new XmlDocument();
			doc.AppendChild(doc.CreateElement("doc"));
		}

		/// Internal XEditNet test.
		[Test]
		public void BugRedoNodeChanged()
		{
			XmlElement x=doc.CreateElement("x");
			XmlElement y=doc.CreateElement("y");
			x.AppendChild(y);
			doc.DocumentElement.AppendChild(x);

			u=new UndoManager(this);
			u.Attach(doc);

			u.Mark(null);

			XmlElement z=doc.CreateElement("z");
			SelectionManager.Change(Selection.Empty, x, z);

			u.Undo(null);
			u.Redo();

			Assert.AreEqual(1, z.ChildNodes.Count, "Expected z to still have one node after redo");
		}

		/// Internal XEditNet test.
		[Test]
		public void UndoWithPosition()
		{
			u=new UndoManager(this);
			u.Attach(doc);

			u.Mark(1);
			XmlElement elem=doc.CreateElement("test");
			
			doc.DocumentElement.AppendChild(elem);

			object o=u.Undo(2);

			Assert.IsTrue(o.Equals(99), "Expected 99 but got {0}", o);
		}

		/// Internal XEditNet test.
		[Test]
		public void RedoWithAttributes()
		{
			XmlElement elem=doc.CreateElement("test");

			u=new UndoManager(null);
			u.Attach(doc);

			// this was causing an error
			elem.SetAttribute("testattr", "testval");
			doc.DocumentElement.AppendChild(elem);

			u.Undo(null);
			u.Redo();
		}

		/// Internal XEditNet test.
		[Test]
		public void UndoAttributeChange()
		{
			XmlElement elem=doc.CreateElement("test");
			doc.DocumentElement.AppendChild(elem);
			XmlText t=doc.CreateTextNode("testval");
			XmlAttribute a=doc.CreateAttribute("testatt");
			a.AppendChild(t);

			u=new UndoManager(null);
			u.Attach(doc);

			elem.SetAttributeNode(a);

			u.Mark(null);

			Assert.AreEqual("testval", elem.GetAttribute("testatt"), "Wrong initial value for attribute");

			t.Value="testval2";
//			elem.SetAttribute("testatt", "testval2");
			Assert.AreEqual("testval2", elem.GetAttribute("testatt"), "Wrong modified value for attribute");

			u.Undo(null);
			Assert.AreEqual("testval", elem.GetAttribute("testatt"), "Wrong value for attribute after undo");
		}

		/// Internal XEditNet test.
		[Test]
		public void UndoAttributeDelete()
		{
			XmlElement elem=doc.CreateElement("test");
			doc.DocumentElement.AppendChild(elem);

			elem.SetAttribute("testatt", "testval");

			u=new UndoManager(null);
			u.Attach(doc);

			u.Mark(null);

			elem.RemoveAttribute("testatt");

			Assert.AreEqual("", elem.GetAttribute("testatt"), "Wrong value for attribute after undo");

			u.Undo(null);
			Assert.AreEqual("testval", elem.GetAttribute("testatt"), "Wrong initial value for attribute");
		}
	
		/// Internal XEditNet test.
		[Test]
		public void UndoAttributeMove()
		{
			XmlElement elem1=doc.CreateElement("test1");
			XmlElement elem2=doc.CreateElement("test2");

			doc.DocumentElement.AppendChild(elem1);
			doc.DocumentElement.AppendChild(elem2);

			XmlAttribute a=doc.CreateAttribute("testatt");
			a.Value="testval";

			elem1.SetAttributeNode(a);

			u=new UndoManager(null);
			u.Attach(doc);

			u.Mark(null);

			try
			{
				elem2.SetAttributeNode(a);
			} catch ( InvalidOperationException )
			{
				// this is what we're expecting - attrs cannot be moved
				return;
			}

			Assert.AreEqual("", elem1.GetAttribute("testatt"), "Wrong value for attribute before undo");

			u.Undo(null);
			Assert.AreEqual("testval", elem1.GetAttribute("testatt"), "Wrong value for attribute after undo");
		}

		public object ContextInfo
		{
			get { return 99; }
		}
	}
}
