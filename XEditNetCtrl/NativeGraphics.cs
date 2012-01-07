using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using XEditNet;
using XEditNet.Layout;
using XEditNet.Styles;

namespace XEditNet
{
	/// <summary>
	/// Summary description for graphics.
	/// </summary>
	internal class Win32Graphics : GraphicsBase, IGraphics
	{
		public IntPtr hdc;
		private Hashtable fontHandles=new Hashtable();
		private Stack fontStack=new Stack();
		private bool deleteHdcOnDispose=false;
		private Graphics originatingGraphics;

//		public Win32Graphics(IntPtr hdc)
//		{
//			this.hdc=hdc;
//			graphics=Graphics.FromHdc(hdc);
//		}

		public Win32Graphics(Graphics gr)
		{
			this.originatingGraphics=gr;
			this.hdc=gr.GetHdc();
			this.graphics=Graphics.FromHdc(hdc);
		}

//		public Win32Graphics()
//		{
//			this.hdc=Win32Util.CreateCompatibleDC(IntPtr.Zero);
//			graphics=Graphics.FromHdc(hdc);
//			deleteHdcOnDispose=true;
//		}

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

		public void PushFont(object handle)
		{
			IntPtr oldFont=Win32Util.SelectObject(hdc, (IntPtr) handle);
			fontStack.Push(oldFont);
		}

		public void PopFont()
		{
			object o=fontStack.Pop();
			if ( o != null )
				Win32Util.SelectObject(hdc, (IntPtr) o);
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

		private IntPtr CreateFont(FontDesc fd)
		{
			IntPtr ret;
			Font f=new Font(fd.Family, fd.Size, fd.Style);
			ret=f.ToHfont();
			fontHandles[fd]=ret;
			f.Dispose();
			return ret;
		}

		public new void Dispose()
		{
			if ( true )
				throw new InvalidOperationException("here");

			if ( originatingGraphics != null && !hdc.Equals(IntPtr.Zero) )
			{				
				originatingGraphics.ReleaseHdc(hdc);
				Console.WriteLine("Released HDC");
			} 
			else
			{
				Console.WriteLine("HDC not released! Originating={0}, HDC={1}", originatingGraphics, hdc);
			}

			graphics.Dispose();

//			if ( deleteHdcOnDispose )
				Win32Util.DeleteDC(hdc);

			foreach ( IntPtr fh in fontHandles.Values )
				Win32Util.DeleteObject(fh);
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
	}
}
