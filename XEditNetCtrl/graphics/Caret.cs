using System;
using System.Drawing;
using System.Windows.Forms;
using XEditNet.Layout;
using Region = System.Drawing.Region;
using XEditNet.XenGraphics.Native;

namespace XEditNet.XenGraphics
{
	public interface ICaret
	{
		bool Visible { get; set; }
		int Height { get; }
		void Set(Point pt, int height);
		Point Location { get; }
	}

	/// <summary>
	/// Summary description for Caret.
	/// </summary>
	public class Caret : ICaret
	{
		private int height;
		private Point location;
		private bool enabled;
		private bool drawn;
		private bool focus;
		private ScrollableControl owner;
		private Timer timer;

		public Caret(ScrollableControl owner)
		{
			this.owner=owner;
			timer=new Timer();
			timer.Tick+=new EventHandler(Toggle);
			timer.Interval=500;
			timer.Enabled=true;

			owner.LostFocus+=new EventHandler(LeaveControl);
			owner.GotFocus+=new EventHandler(EnterControl);
			owner.Paint+=new PaintEventHandler(PaintComplete);
		}

		private void PaintComplete(object sender, PaintEventArgs e)
		{
//			Console.WriteLine("Got paint complete, drawn={0}", drawn);
			if ( drawn )
				Show(e.Graphics);
		}

		public void Toggle(object o, EventArgs e)
		{
			drawn=!drawn;
			owner.Invalidate(BoundingRectangle);
		}

		private void Show()
		{
			using ( Graphics g=owner.CreateGraphics() )
				Show(g);

			drawn=true;
		}

		private void Show(Graphics g)
		{
//			Console.WriteLine("Show caret, pos={0}, clip={1}", location, g.ClipBounds);
			Pen p=new Pen(Brushes.Black,  2);
			Point pt=location;
			pt.Offset(owner.AutoScrollPosition.X, owner.AutoScrollPosition.Y);
			g.DrawLine(p, pt.X+1,  pt.Y, pt.X+1, pt.Y+height-1);
		}

		private void Hide()
		{
			owner.Invalidate(BoundingRectangle);
			drawn=false;
		}

		private Rectangle BoundingRectangle
		{
			get
			{
				Rectangle rc=new Rectangle(location, new Size(2, height));

				// TODO: L: bit of a hack, shouldn't need to inflate, clip being returned is non-integral for some reason
				rc.Offset(owner.AutoScrollPosition);
				rc.Inflate(1,1);
				return rc;
			}
		}

		public void Set(Point location, int height)
		{
			Region rgn=new Region(BoundingRectangle);
		
			this.location=location;
			this.height=height;

			rgn.Exclude(BoundingRectangle);
			owner.Invalidate(rgn);

			ShowNew();
		}

		public int Height
		{
			get { return height; }
		}

		public Point Location
		{
			get { return location; }
		}

		public bool Visible
		{
			get { return enabled; }
			set
			{
				if ( enabled == value )
					return;

				Hide();
				enabled=value;
				ResetTimer();
			}
		}

		private void ResetTimer()
		{
			timer.Enabled=focus ? enabled : false;
			timer.Stop();
			timer.Start();
		}

		private void LeaveControl(object sender, EventArgs e)
		{
			focus=false;
			ResetTimer();
			Hide();
		}

		private void EnterControl(object sender, EventArgs e)
		{
			focus=true;
			if ( enabled )
				ShowNew();
		}

		private void ShowNew()
		{
			ResetTimer();
			Show();
		}
	}
}
