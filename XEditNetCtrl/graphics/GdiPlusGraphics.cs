using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;
using XEditNet.Layout;
using XEditNet.Styles;
using XEditNet.XenGraphics;

namespace XEditNet
{
	internal class GdiPlusGraphicsFactory : IGraphicsFactory
	{
		public IGraphics CreateGraphics(Graphics graphics)
		{
			return new GdiPlusGraphics(graphics);
		}

		public IGraphics CreateGraphics(Control ctrl)
		{
			return new GdiPlusGraphics(ctrl);
		}

		public ICaret CreateCaret(ScrollableControl ctrl)
		{
			return new Caret(ctrl);
		}
	}

	/// <summary>
	/// Summary description for GdiPlusGraphics.
	/// </summary>
	internal class GdiPlusGraphics : GraphicsBase, IGraphics
	{
		private Hashtable fontHandles=new Hashtable();
		private Stack fontStack=new Stack();
		private bool disposeGraphics=false;
		private Bitmap memBitmap;
		private Size memBitmapSize;
		private Graphics memGraphics;

		public GdiPlusGraphics(Control ctrl)
		{
			graphics=ctrl.CreateGraphics();
			disposeGraphics=true;
		}

		public GdiPlusGraphics(Graphics gr)
		{
			graphics=gr;
		}

		public object GetFontHandle(FontDesc fd)
		{
			object ret=fontHandles[fd];
			if ( ret == null )
				ret=CreateFont(fd);

			return ret;
		}

		private Font CreateFont(FontDesc fd)
		{
			Font f=new Font(fd.Family, fd.Size, fd.Style);
			fontHandles[fd]=f;
			return f;
		}

		public int GetFontAscent()
		{
			int ascentDesign = CurrentFont.FontFamily.GetCellAscent(CurrentFont.Style);
			int ascentPixel = (int) (CurrentFont.Size * ascentDesign / CurrentFont.FontFamily.GetEmHeight(CurrentFont.Style));
			return ascentPixel;
		}

		public int GetFontHeight()
		{
			return CurrentFont.Height;
		}

		private Font CurrentFont
		{
			get { return (Font) fontStack.Peek(); }
		}

		public void PushFont(object handle)
		{
			fontStack.Push(handle);
		}

		public void PopFont()
		{
			fontStack.Pop();
		}

		private StringFormat DefaultStringFormat
		{
			get
			{
				StringFormat ret=StringFormat.GenericTypographic;
				ret.FormatFlags=StringFormatFlags.MeasureTrailingSpaces;
				return ret;
			}
		}

		private TextRenderingHint DefaultTextRenderingHint
		{
			get
			{
				return TextRenderingHint.AntiAlias;				
			}	
		}

		protected void DrawHighlightedText(Rectangle rc, int offset, string text, Color col, Color bkCol, int startIndex, int endIndex)
		{
			int dx1=startIndex > 0 ? MeasureText(text.Substring(0, startIndex)).Width : 0;
			int dx2=endIndex < text.Length ? MeasureText(text.Substring(0, endIndex)).Width : rc.Width;

			CreateGraphics(rc.Width, rc.Height);

			memGraphics.Clear(bkCol);
			memGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			memGraphics.TextRenderingHint=DefaultTextRenderingHint;
			memGraphics.DrawString(text, CurrentFont, new SolidBrush(col), 0, offset, DefaultStringFormat);

			graphics.DrawImageUnscaled(memBitmap, rc);

			ImageAttributes ia=new ImageAttributes();
			ColorMatrix cm=new ColorMatrix();

			cm.Matrix00=cm.Matrix11=cm.Matrix22=0.99f;
			cm.Matrix33=cm.Matrix44=1;
			cm.Matrix40=cm.Matrix41=cm.Matrix42=.04f;
			ia.SetColorMatrix(cm);

			memGraphics.DrawImage(memBitmap, new Rectangle(dx1, 0, dx2-dx1, rc.Height), dx1, 0, dx2-dx1, rc.Height, GraphicsUnit.Pixel, ia);

			cm=new ColorMatrix();
			cm.Matrix00=cm.Matrix11=cm.Matrix22=-1;
			ia.SetColorMatrix(cm);

			graphics.DrawImage(memBitmap, new Rectangle(rc.X+dx1, rc.Y, dx2-dx1, rc.Height), 
				dx1, 0, dx2-dx1, rc.Height, GraphicsUnit.Pixel, ia);

//			if ( dx1 > 0 )
//				graphics.DrawImage(bm, 
//					new Rectangle(rc.Location,  new Size(dx1, rc.Height)),
//					0, 0, dx1, rc.Height, GraphicsUnit.Pixel);


//			if ( dx2 < rc.Width )
//				graphics.DrawImage(bm, new Rectangle(rc.Location,  new Size(rc.Width-dx2, rc.Height)), 
//					dx2, 0, rc.Width-dx2, rc.Height, GraphicsUnit.Pixel);
		}

		public override void DrawText(Rectangle rc, int yOffset, string text, Color col, Color bkCol, int startHighlight, int endHighlight)
		{
			graphics.TextRenderingHint=DefaultTextRenderingHint;
			graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

			if ( endHighlight - startHighlight > 0 )
			{
//				Console.WriteLine("Highlighting from {0} to {1}", startHighlight, endHighlight);

				if ( endHighlight <= text.Length )
					DrawHighlightedText(rc, yOffset, text, col, bkCol, startHighlight, endHighlight);
				else
				{
					col=InverseOf(col);
					bkCol=InverseOf(bkCol);
					graphics.FillRectangle(new SolidBrush(bkCol), rc);
					graphics.DrawString(text, CurrentFont, new SolidBrush(col), rc.X, rc.Y+yOffset, DefaultStringFormat);
				}
			} 
			else
			{
				graphics.FillRectangle(new SolidBrush(bkCol), rc);
				graphics.DrawString(text, CurrentFont, new SolidBrush(col), rc.X, rc.Y+yOffset, DefaultStringFormat);
			}
		}

		public override Size MeasureText(string name)
		{
			graphics.TextRenderingHint=DefaultTextRenderingHint;
			return graphics.MeasureString(name, CurrentFont, -1, DefaultStringFormat).ToSize();
		}

		public void Dispose()
		{
			if ( disposeGraphics )
				graphics.Dispose();
		}

		private void CreateGraphics(int width, int height)
		{
			if ( memBitmapSize.Width >= width && memBitmapSize.Height >= height )
				return;

			if ( memGraphics != null )
				memGraphics.Dispose();

			if ( memBitmap != null )
				memBitmap.Dispose();

			memBitmapSize=new Size(Math.Max(width, memBitmapSize.Width), Math.Max(height, memBitmapSize.Height));
			memBitmap=new Bitmap(width, height, PixelFormat.Format24bppRgb);
			memGraphics=Graphics.FromImage(memBitmap);
		}
	}
}
