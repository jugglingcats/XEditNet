using System;
using System.Xml;
using NUnit.Framework;
using XEditNet.Location;
using WhitespaceHandling = XEditNet.Location.WhitespaceHandling;

namespace XEditNet.TestSuite
{
	/// Stylesheet tests.
	[TestFixture]
	public class SelectionTests
	{
		/// Internal XEditNet test.
		[Test]
		public void MoveNextAndPrevious()
		{
			XmlDocument doc=new XmlDocument();

			doc.LoadXml("<doc/>");

			XmlElement e=doc.CreateElement("p");

			XmlText t=doc.CreateTextNode("\r\n\r\nabc\r\n");
			e.AppendChild(t);
			t=doc.CreateTextNode("\r\n\r\n");
			e.AppendChild(t);
			t=doc.CreateTextNode("\r\ndef\r\n");
			e.AppendChild(t);
			doc.DocumentElement.AppendChild(e);
			
			t=e.FirstChild as XmlText;
			TextSelectionPoint tsp=new TextSelectionPoint(t, 0);
			RunTest(tsp, new int[] {0, 4, 5, 6, 7, 2, 3, 4, 5}, WhitespaceHandling.Default, false);
			RunTest(tsp, new int[] {0, 2, 4, 5, 6, 7, 0, 2, 0, 2, 3, 4, 5}, WhitespaceHandling.Preserve, false);

			t=e.LastChild as XmlText;
			tsp=new TextSelectionPoint(t, t.Value.Length - 1);
			RunTest(tsp, new int[] {0, 4, 5, 6, 7, 2, 3, 4, 5}, WhitespaceHandling.Default, true);
			RunTest(tsp, new int[] {0, 2, 4, 5, 6, 7, 0, 2, 0, 2, 3, 4, 5}, WhitespaceHandling.Preserve, true);

			e.RemoveAll();
			t=doc.CreateTextNode("\n\nabc\n");
			e.AppendChild(t);
			t=doc.CreateTextNode("\n\n");
			e.AppendChild(t);
			t=doc.CreateTextNode("\ndef\n");
			e.AppendChild(t);
			doc.DocumentElement.AppendChild(e);

			t=e.FirstChild as XmlText;
			tsp=new TextSelectionPoint(t, 0);
			RunTest(tsp, new int[] {0, 2, 3, 4, 5, 1, 2, 3, 4}, WhitespaceHandling.Default, false);
			RunTest(tsp, new int[] {0, 1, 2, 3, 4, 5, 0, 1, 0, 1, 2, 3, 4}, WhitespaceHandling.Preserve, false);

			t=e.LastChild as XmlText;
			tsp=new TextSelectionPoint(t, t.Value.Length - 1);
			RunTest(tsp, new int[] {0, 2, 3, 4, 5, 1, 2, 3, 4}, WhitespaceHandling.Default, true);
			RunTest(tsp, new int[] {0, 1, 2, 3, 4, 5, 0, 1, 0, 1, 2, 3, 4}, WhitespaceHandling.Preserve, true);
		}

		private static void RunTest(TextSelectionPoint tsp, int[] indexes, WhitespaceHandling ws, bool reverse)
		{
			if ( reverse )
				Array.Reverse(indexes);

			Console.WriteLine("Running movement tests for {0}, ws={1}, reverse={2}", tsp, ws, reverse);

			TextSelectionHelper tsh=new TextSelectionHelper(tsp, ws);

			int n=0;
			while ( tsp != null )
			{
				Console.WriteLine("Scanning: '{0}' (={1})", tsp.Char, (int) tsp.Char);
				Assert.IsTrue(n < indexes.Length, "Too many iterations, expected {0}", indexes.Length);
				Assert.IsTrue(indexes[n] == tsp.Index, "Wrong value at iteration {0} (expected {1}, got {2}", n, indexes[n], tsp.Index);
				if ( reverse )
					tsp=tsh.Previous;
				else
					tsp=tsh.Next;
				n++;
			}
		}
	}
}
