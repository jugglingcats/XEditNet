using System;
using System.Collections;
using System.Windows.Forms;
using System.Xml;
using System.Drawing;

using NUnit.Framework;

using XEditNet.Layout;
using XEditNet.Location;
using XEditNet.Dtd;
using XEditNet.Styles;
using XEditNet.XenGraphics.Native;

namespace XEditNet.TestSuite
{
	/// Layout tests.
	[TestFixture]
	public class LayoutTests
	{
		/// Internal XEditNet test.
		[Test]
		public void WhitespaceHandling1()
		{
			string test="aaaa bbbb  cccc";
			XmlDocument doc=new XmlDocument();
			doc.LoadXml("<doc>"+test+"</doc>");

			Stylesheet s=new Stylesheet();
			s.BindStyles(doc.NameTable);

//			Rectangle rc=new Rectangle(0, 0, 480, int.MaxValue);
			Rectangle rc=new Rectangle(0, 0, 110, int.MaxValue);

			using ( IGraphics gr=new DummyGraphics() )
			{
				DrawContext ctx=new DrawContext(gr, Point.Empty, rc, rc, null, new DocumentType(), null);
				LayoutEngine layoutEngine=new LayoutEngine(s);
				layoutEngine.Reflow(ctx, doc.DocumentElement);

				SelectionPoint start=new TextSelectionPoint(doc.DocumentElement.FirstChild, 11);
				Console.WriteLine("Getting caret pos for {0}", start);
				Rectangle c=layoutEngine.GetCaretPosition(gr, start, CaretDirection.None);
				Console.WriteLine("Char {0} at {1}", start.ToString(), c);
			}
		}

		/// Internal XEditNet test.
		[Test]
		public void HitTestHandling()
		{
			string test="<p><b>xxxxxxxxxxxxx</b></p>";
			XmlDocument doc=new XmlDocument();
			doc.LoadXml(test);

			Stylesheet s=//Stylesheet.Load("c:/tmp/website/site.style", doc.NameTable);
			new Stylesheet();
			s.BindStyles(doc.NameTable);

			Rectangle rc=new Rectangle(0, 0, 500, int.MaxValue);

			using ( IGraphics gr=new DummyGraphics() )
			{
				DrawContext ctx=new DrawContext(gr, Point.Empty, rc, rc, null, new DocumentType(), null);
				LayoutEngine layoutEngine=new LayoutEngine(s);

				layoutEngine.Reflow(ctx, doc.DocumentElement);

				layoutEngine.GetHitTestInfo(gr, new Point(141,16));
			}
		}

		/// Internal XEditNet test.
		[Test]
		public void CommentHitTestError()
		{
			string test="<p><!--OddPage--></p>";
			XmlDocument doc=new XmlDocument();
			doc.LoadXml(test);

			Stylesheet s=new Stylesheet();
			s.BindStyles(doc.NameTable);

			Rectangle rc=new Rectangle(0, 0, 500, int.MaxValue);

			using ( IGraphics gr=new DummyGraphics() )
			{
				DrawContext ctx=new DrawContext(gr, Point.Empty, rc, rc, null, new DocumentType(), null);
				LayoutEngine layoutEngine=new LayoutEngine(s);

				layoutEngine.Reflow(ctx, doc.DocumentElement);

				Console.WriteLine("Bounds {0}", layoutEngine.BoundingRect);
				for (int x=0; x< layoutEngine.BoundingRect.Width; x+=10)
				{
					HitTestInfo hti=layoutEngine.GetHitTestInfo(gr, new Point(x,8));
					SelectionPoint sp=hti.SelectionPoint;
					Console.WriteLine("Hit test at {0} = {1}", x, sp);
				}
			}
		}

		[Test]
		public void FontProblems()
		{
			Form frm=new Form();
			XEditNetCtrl ctrl=new XEditNetCtrl();
			XmlDocument doc=new XmlDocument();
			doc.LoadXml("<doc>x</doc>");
			ctrl.Attach(doc, true);
			frm.Controls.Add(ctrl);
			frm.ShowDialog();
//			frm.Update();
		}

		private class DummyCtrl : UserControl
		{
			public static void Doit(Graphics g)
			{
				for ( int n=0; n< 5; n++ )
				{
					Rectangle rc=new Rectangle(0, 0, 400, 30);

					using ( IGraphics gr=new Win32GraphicsFactory().CreateGraphics(g) )
					{
						Recurse(0, gr, rc);
					}
				}
			}

			private static void Recurse(int depth, IGraphics gr, Rectangle rc)
			{
				object o;
				FontDesc fd;
				fd=new FontDesc("Arial", 10, false, false, false);
				o=gr.GetFontHandle(fd);
				gr.PushFont(o);
				gr.MeasureText("my name is alfie");
				gr.DrawText(rc, 20, "hello", Color.Black, Color.White, -1, -1);
	
				fd=new FontDesc("Times New Roman", 10, false, false, false);
				o=gr.GetFontHandle(fd);
				gr.PushFont(o);
				gr.MeasureText("my name is alfie");
	
				fd=new FontDesc("Arial", 10, false, false, false);
				o=gr.GetFontHandle(fd);
				gr.PushFont(o);
				gr.MeasureText("my name is alfie");
	
				if ( depth < 100 )
					Recurse(depth+1, gr, rc);

				gr.PopFont();
				gr.PopFont();
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				Doit(CreateGraphics());
				Doit(e.Graphics);
			}
		}

		private class DummyGraphics : IGraphics
		{
			#region IGraphics Members

			IntPtr hdc;
			private Graphics graphics;
			private Stack fontStack=new Stack();

			public DummyGraphics()
			{
				this.hdc=Win32Util.CreateCompatibleDC(IntPtr.Zero);
				this.graphics=Graphics.FromHdc(hdc);
			}

			public Graphics FGraphics
			{
				get { return graphics; }
			}

			public void DrawBitmap(Bitmap bitmap, int x, int y, bool selected)
			{
			}

			public object GetFontHandle(FontDesc fd)
			{
				Win32Util.LOGFONT lf=new Win32Util.LOGFONT();
				lf.lfFaceName=fd.Family;
				lf.lfHeight=fd.Size;
				lf.lfItalic=(byte) (fd.Style & FontStyle.Italic);
				if ( (fd.Style & FontStyle.Bold) == FontStyle.Bold )
					lf.lfWeight=700;
				lf.lfItalic=(byte) (fd.Style & FontStyle.Underline);

				return Win32Util.CreateFont(lf);
			}

			public int GetFontAscent()
			{
				return 12;
			}

			public int GetFontHeight()
			{
				return 14;
			}

			public void PushFont(object handle)
			{
				IntPtr oldFont=Win32Util.SelectObject(hdc, (IntPtr) handle);
				fontStack.Push(oldFont);
			}

			public void PopFont()
			{
				object o=fontStack.Pop();
				Win32Util.SelectObject(hdc, (IntPtr) o);
			}

			public void DrawText(Rectangle rc, int yOffset, string text, Color col, Color bkCol, int startHighlight, int endHighlight)
			{
			}

			public Size MeasureText(string name)
			{
				int w=name.Length * 8;
				return new Size(w, GetFontHeight());
			}

			#endregion

			#region IDisposable Members

			public void Dispose()
			{
				graphics.Dispose();
				Win32Util.DeleteDC(hdc);
			}

			#endregion
		}

	}
}
