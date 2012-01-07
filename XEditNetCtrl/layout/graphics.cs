using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using XEditNet.Styles;
using XEditNet.XenGraphics;

// TODO: M: change to graphics namespace
namespace XEditNet.Layout
{
	public interface IGraphicsFactory
	{
		IGraphics CreateGraphics(Graphics graphics);
		IGraphics CreateGraphics(Control ctrl);
		ICaret CreateCaret(ScrollableControl ctrl);
	}

	public interface IGraphics : IDisposable
	{
		Graphics FGraphics
		{
			get;
		}

		void DrawBitmap(Bitmap bitmap, int x, int y, bool selected);
		object GetFontHandle(FontDesc desc);
		int GetFontAscent();
		int GetFontHeight();
		void PushFont(object handle);
		void PopFont();
		void DrawText(Rectangle rc, int yOffset, string text, Color col, Color bkCol, int startHighlight, int endHighlight);
		Size MeasureText(string name);
	}

	public abstract class GraphicsBase
	{
		protected Graphics graphics;

		public void DrawBitmap(System.Drawing.Bitmap bitmap, int x, int y, bool selected)
		{
			if ( selected )
				ControlPaint.DrawImageDisabled(graphics, bitmap, x, y, Color.Black);
			else
				graphics.DrawImage(bitmap, x, y);
		}

		public System.Drawing.Graphics FGraphics
		{
			get { return graphics; }
		}

		public abstract Size MeasureText(string text);
		public abstract void DrawText(Rectangle rc, int yOffset, string text, Color col, Color bkCol, int startHighlight, int endHighlight);

		public static Color InverseOf(Color col)
		{
			return Color.FromArgb(255-col.R, 255-col.G, 255-col.B);
		}
	}

//	public struct ColorModel
//	{
//		public Color Foreground;
//		public Color Background;
//		public Color ForegroundSelected;
//		public Color BackgroundSelected;
//
//		public ColorModel(Color fg, Color bg)
//		{
//			Foreground=fg;
//			Background=bg;
//			ForegroundSelected=InverseOf(fg);
//			BackgroundSelected=InverseOf(bg);
//		}
//
//		public ColorModel(Color fg, Color bg, Color fgSel, Color bgSel)
//		{
//			Foreground=fg;
//			Background=bg;
//			ForegroundSelected=fgSel;
//			BackgroundSelected=bgSel;
//		}
//		
//	}
}
