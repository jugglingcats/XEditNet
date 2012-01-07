using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using XEditNet.XenGraphics;

namespace XEditNet.Widgets
{
	/// <summary>
	/// Summary description for QuickFixIndicator.
	/// </summary>
	public class QuickFixIndicator
	{
		private Control parent;
		private bool visible=false;
		private bool drawnFull=false;
		private Point location=Point.Empty;
		private static readonly int buttonMargin=14;
		private ImageList imageList;
		private ICaret caret;
		private bool caretVisible;
		public XmlNode node=null;

		public QuickFixIndicator(Control parent, ICaret caret)
		{
			this.parent=parent;
			this.caret=caret;

			imageList=new ImageList();
			imageList.ImageSize=new Size(14,14);
			imageList.ColorDepth=ColorDepth.Depth32Bit;

			LoadImage("Bulb.png");
			LoadImage("BulbArrow.png");
		}

		private void LoadImage(string name)
		{
			Assembly a = typeof(QuickFixIndicator).Assembly;
			string fname = a.FullName.Split(',')[0]+".widgets.images."+name;
			Stream stm = a.GetManifestResourceStream(fname);
	
			if ( stm != null )
			{
				Bitmap bm=new Bitmap(stm);
				imageList.Images.Add(bm);
			}
		}

		public Rectangle CurrentBounds
		{
			get
			{
				Rectangle rc=new Rectangle(location, new Size(14,14));
				if ( drawnFull )
				{
					rc.Inflate(3,3);
					rc.Width+=buttonMargin;
				}
				return rc;
			}
		}

		public bool Update(XmlNode n, Point location, Point cursor)
		{
			Rectangle bounds=CurrentBounds;
			bool wasDrawnFull=drawnFull;

			this.location=location;
			this.node=n;
			this.drawnFull=bounds.Contains(cursor);
			if ( !visible )
			{
				this.caretVisible=caret.Visible;
				caret.Visible=false;
			}

			if ( visible && !bounds.Equals(CurrentBounds) )
			{
				Region rgn=new Region(bounds);
				if ( drawnFull && wasDrawnFull )
					rgn.Exclude(CurrentBounds);

				parent.Invalidate(rgn);
				parent.Update();
				Console.WriteLine("Invalidate!");
			}

			if ( !visible || !bounds.Equals(CurrentBounds) )
			{
				using ( Graphics g=parent.CreateGraphics() )
				{
					if ( drawnFull )
					{
						ControlUtil.DrawButton(g, CurrentBounds);
						Point pt=location;
						pt.Offset(14,0);
						imageList.Draw(g, pt, 1);
					}
					imageList.Draw(g, location, 0);

				}
			}

			visible=true;
			return drawnFull;
		}

		public void Hide()
		{
			if ( !visible )
				return;

			visible=false;
			node=null;
			parent.Invalidate(CurrentBounds);
			caret.Visible=caretVisible;
		}

		public bool Click(Point pt)
		{
			if ( !visible )
				return false;

			return CurrentBounds.Contains(pt);
		}

		public XmlNode Node
		{
			get { return node; }
		}
	}
}
