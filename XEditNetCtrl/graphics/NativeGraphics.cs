using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using XEditNet;
using XEditNet.Layout;
using XEditNet.Styles;

namespace XEditNet.XenGraphics.Native
{
	internal class Win32Graphics : GraphicsBase, IGraphics
	{
		private IntPtr hdc;
		private Hashtable fontHandles=new Hashtable();
		private Stack fontStack=new Stack();
		private Graphics originatingGraphics;
		private static int refCount=0;

		public Win32Graphics(Graphics gr)
		{
			this.originatingGraphics=gr;
			this.hdc=gr.GetHdc();
			this.graphics=Graphics.FromHdc(hdc);
			refCount++;
			if ( refCount != 1 )
				Console.WriteLine("WARNING - more than one Win32Graphics in use ({0})", refCount);

		}

		public object GetFontHandle(FontDesc fd)
		{
			object ret=fontHandles[fd];
			if ( ret == null )
				ret=CreateFont(fd);

			return ret;
		}

		public int GetFontAscent()
		{
			return Win32Util.GetTextAscent(hdc);
		}

		public int GetFontHeight()
		{
			return Win32Util.GetTextHeight(hdc);
		}

		public void Dispose()
		{
			refCount--;

//			Console.WriteLine("Win32Graphics: Dispose");

			graphics.Dispose();

			while ( fontStack.Count > 0 )
				PopFont();

			foreach ( IntPtr fh in fontHandles.Values )
			{
				Win32Util.DeleteObject(fh);
//				Console.WriteLine("Deleted font {0}", fh.ToInt32()/*.ToString("X")*/);				
			}
			fontHandles.Clear();

			originatingGraphics.ReleaseHdc(hdc);
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

		private IntPtr CreateFont(FontDesc fd)
		{
//			Win32Util.LOGFONT lf=new Win32Util.LOGFONT();
//			lf.lfFaceName=fd.Family;
//			lf.lfHeight=fd.Size;
//			lf.lfItalic=(byte) (fd.Style & FontStyle.Italic);
//			if ( (fd.Style & FontStyle.Bold) == FontStyle.Bold )
//				lf.lfWeight=700;
//			lf.lfItalic=(byte) (fd.Style & FontStyle.Underline);
//			IntPtr ret=Win32Util.CreateFont(lf);

			Font f=new Font(fd.Family, fd.Size, fd.Style);
			IntPtr ret=f.ToHfont();
			fontHandles[fd]=ret;

//			Console.WriteLine("Created font {0}", ret);
			return ret;
		}

		private void DrawHighlightedText(Rectangle rc, int yOffset, string text, Color col, Color bkCol, int startIndex, int endIndex)
		{
			int xoff=0;
			int dx=0;
			Rectangle src;

			Color colInv=InverseOf(col);
			Color bkColInv=InverseOf(bkCol);

			if ( startIndex > 0 )
			{
				dx=MeasureText(text.Substring(0, startIndex)).Width;
				src=new Rectangle(rc.X, rc.Y, dx, rc.Height);
				DrawSimpleText(src, yOffset, text.Substring(0, startIndex), col, bkCol);
				xoff+=dx;
			}
			//				if ( inError )
			//					DrawErrorIndicator(dc, x, x, dx, y+baseline, false);

			if ( endIndex < text.Length )
			{
				int len=endIndex-startIndex; // length of selected text
				dx=MeasureText(text.Substring(startIndex, len)).Width;
				src=new Rectangle(rc.X+xoff, rc.Y, dx, rc.Height);
				FGraphics.FillRectangle(new SolidBrush(bkColInv), src);
				DrawSimpleText(src, yOffset, text.Substring(startIndex, len), colInv, bkColInv);
				xoff+=dx;

				//				if ( inError )
				//					DrawErrorIndicator(dc, x, x+xoff, dx, y+baseline, true);

				dx=MeasureText(text.Substring(endIndex)).Width;
				src=new Rectangle(rc.X+xoff, rc.Y, dx, rc.Height);
				DrawSimpleText(src, yOffset, text.Substring(endIndex), col, bkCol);

				//				if ( inError )
				//					DrawErrorIndicator(dc, x, x+xoff, Width-xoff, y+baseline, false);
			} 
			else
			{
				// everything else selected
				dx=MeasureText(text.Substring(startIndex)).Width;
				src=new Rectangle(rc.X+xoff, rc.Y, dx, rc.Height);
				FGraphics.FillRectangle(new SolidBrush(bkColInv), src);
				DrawSimpleText(src, yOffset, text.Substring(startIndex), colInv, bkColInv);
				//				if ( inError )
				//					DrawErrorIndicator(dc, x, x+xoff, Width-xoff, y+baseline, true);
			}
		} 

		public override void DrawText(Rectangle rc, int yOffset, string text, Color col, Color bkCol, int startHighlight, int endHighlight)
		{
			if ( endHighlight - startHighlight > 0 )
			{
				if ( endHighlight <= text.Length )
					DrawHighlightedText(rc, yOffset, text, col, bkCol, startHighlight, endHighlight);
				else
				{
					col=InverseOf(col);
					bkCol=InverseOf(bkCol);
					DrawSimpleText(rc, yOffset, text, col, bkCol);
				}
			} 
			else
			{
				DrawSimpleText(rc, yOffset, text, col, bkCol);
			}
		}

		private void DrawSimpleText(Rectangle rc, int yOffset, string text, Color col, Color bkCol)
		{
			Win32Util.SetTextColor(hdc, col);
			Win32Util.SetBkColor(hdc, bkCol);
			Win32Util.ExtTextOut(hdc, rc.X, rc.Y+yOffset, rc, text);
		}

		public override Size MeasureText(string text)
		{
			if ( text.Length == 0 )
				return Size.Empty;

			Win32Util.SIZE size;
			size.cx = 0;
			size.cy = 0;
			Win32Util.Win32API.GetTextExtentPoint32W(hdc, text, text.Length, ref size);
			return new Size(size.cx, size.cy);
		}


	}


	public class Win32GraphicsFactory : IGraphicsFactory
	{
		public IGraphics CreateGraphics(Graphics graphics)
		{
			return new Win32Graphics(graphics);
		}

		IGraphics XEditNet.Layout.IGraphicsFactory.CreateGraphics(Control ctrl)
		{
			Graphics gr=ctrl.CreateGraphics();
			IGraphics ret=new Win32Graphics(gr);
			return ret;
		}

		public ICaret CreateCaret(ScrollableControl ctrl)
		{
			return new NativeCaret(ctrl);
		}

		private class NativeCaret : ICaret
		{
			private ScrollableControl owner;
			private int height;
			private bool visible;
			private Point location;

			public NativeCaret(ScrollableControl ctrl)
			{
				this.owner=ctrl;
				owner.GotFocus+=new EventHandler(ControlEntered);
				owner.Enter+=new EventHandler(ControlEntered);
				owner.Leave+=new EventHandler(ControlLeft);
			}

			public void Update()
			{
				Win32Util.Win32API.DestroyCaret();
				
				if ( !Win32Util.Win32API.CreateCaret(owner.Handle, IntPtr.Zero, 2, height) )
					Console.WriteLine("Failed to create caret!!!");

				if ( visible )
					Win32Util.Win32API.ShowCaret(owner.Handle);
				else
					Win32Util.Win32API.HideCaret(owner.Handle);

				Point pt=location;
				pt.Offset(owner.AutoScrollPosition.X, owner.AutoScrollPosition.Y);
				Win32Util.Win32API.SetCaretPos(pt.X, pt.Y);
			}


			public void ControlEntered(object sender, EventArgs e)
			{
				Update();
			}

			public void ControlLeft(object sender, EventArgs e)
			{
				Win32Util.Win32API.DestroyCaret();
			}

			public bool Visible
			{
				get { return visible; }
				set 
				{
					if ( visible != value )
					{
						visible=value;
						Update();
					}
				}
			}

			public int Height
			{
				get { return height; }
			}

			public void Set(Point pt, int height)
			{
				this.height=height;
				this.location=pt;
				Update();
			}

			public Point Location
			{
				get { return location; }
			}
		}
	}
}

