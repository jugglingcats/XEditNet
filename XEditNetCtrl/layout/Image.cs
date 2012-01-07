using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using XEditNet.Location;
using XEditNet.Styles;

namespace XEditNet.Layout
{
	internal class Image : BlockImpl, IReflowObject
	{
		public static Bitmap error;

		public Image(IContainer parent, XmlElement e, Style s) : base(parent, e, s)
		{
		}

		public override void Reflow(DrawContext dc, BoundingContext bounds, bool incremental)
		{
			ReflowStart(dc);

			if ( incremental )
				bounds=bounds.Narrow(style.Left, style.Right);

			MarkupItem tag=new StartTag(CurrentLine, ElementNode);
			ReflowMarkup(tag, dc, style, bounds);

			ImageStyle s=style as ImageStyle;
			if ( s == null )
				throw new InvalidOperationException("Expected style for image to be Custom");

			string imgPath=ElementNode.GetAttribute(s.SourceAttribute);
			if ( imgPath == null )
				throw new InvalidOperationException("SourceAttribute for image is missing");

			// think about relative to doc
			Bitmap bm;
			
			Uri docUri=new Uri(elementNode.BaseURI);
			Uri uri=new Uri(docUri, imgPath);
			if ( uri.IsFile && File.Exists(uri.AbsolutePath) )
				bm=new Bitmap(uri.AbsolutePath);
			else
				bm=ErrorBitmap;

			ImageLineItem ili=new ImageLineItem(CurrentLine, ElementNode, bm);
			ReflowMarkup(ili, dc, style, bounds);

			tag=new EndTag(CurrentLine, ElementNode);
			ReflowMarkup(tag, dc, style, bounds);
		
			ReflowEnd(dc);
		}

		private Bitmap ErrorBitmap
		{
			get
			{
				if ( error != null )
					return error;

				Assembly a = typeof(Image).Assembly;
				string name = a.FullName.Split(',')[0]+".error.bmp";
				Stream stm = a.GetManifestResourceStream(name);
				
				try
				{
					error=new Bitmap(new Bitmap(stm));
					return error;
				}
				finally
				{
					stm.Close();
				}
			}
		}
	}

	internal class ImageLineItem : LineItemBase
	{
		private XmlElement element;
		private Bitmap bitmap;

		public ImageLineItem(IContainer parent, XmlElement elem, Bitmap bitmap) : base(parent)
		{
			this.element=elem;
			this.bitmap=bitmap;
		}

		public override int Compose(DrawContext dc, Style c, out ItemMetrics bi)
		{
			width=bitmap.Width;
			height=bitmap.Height;
			ascent=bitmap.Height/2;
			bi=new ItemMetrics(this);

			return Width;
		}

		public override void Draw(DrawContext dc, int x, int y, int baseline, int height, Style s)
		{
			dc.Graphics.DrawBitmap(bitmap, dc.Origin.X+x, dc.Origin.Y+y, selection != null);
		}

		public override HitTestInfo GetHitTestInfo(IGraphics gr, int x, int y, Point pt)
		{
			ElementSelectionPoint esp=new ElementSelectionPoint(Node, TagType.EndTag);

			Line l=(Line) Parent;
			Rectangle rcCaret=Rectangle.Empty;

			LineItemContext ili=new LineItemContext(l.Height, l.Baseline, this, new Point(x,y));
			return new HitTestInfo(esp, ili, false, rcCaret);
		}

		public override void GetCaretPosition(IGraphics gr, int x, int y, SelectionPoint sp, ref CaretPositionInfo cpi)
		{
			if ( ContainsSelectionPoint(sp) )
				cpi.UpdateLocation(x, y, Height, CaretSetting.Accurate);
			else
				cpi.UpdateLocation(x+Width, y, Height, CaretSetting.Fallback);
		}

		public override SelectionPoint GetSelectionPoint(bool atEnd)
		{
			return new ElementSelectionPoint(Node, TagType.EndTag);
		}

		public override bool ContainsSelectionPointInternal(SelectionPoint sp)
		{
			return true;
		}

		public override XmlNode Node
		{
			get { return element; }
		}
	}
}
