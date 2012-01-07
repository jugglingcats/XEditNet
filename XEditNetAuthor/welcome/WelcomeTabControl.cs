using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using XEditNetAuthor.Welcome;

namespace XEditNetAuthor.Welcome
{
	/// <summary>
	/// Summary description for WelcomeTabControl.
	/// </summary>
	[Designer(typeof(WelcomeTabDesigner))]
	public class WelcomeTabControl : System.Windows.Forms.UserControl
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private WelcomePageCollection pages;
		private WelcomeTabPage activePage;
		private int hoverIndex=-1;

		private const int margin=10;
		private Color highlightColor;

		public WelcomeTabControl()
		{
			pages=new WelcomePageCollection(this);

			SetStyle(ControlStyles.ResizeRedraw, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.ContainerControl, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

		}

		protected override void OnBackColorChanged(EventArgs e)
		{
			int r=Math.Min(255, BackColor.R+10);
			int g=Math.Min(255, BackColor.G+10);
			int b=Math.Min(255, BackColor.B+10);
			highlightColor=SystemColors.Control; // Color.FromArgb(r, g, b);
		}

		protected override void OnLoad(EventArgs e)
		{
			SizeAll();
			if ( pages.Count > 0 )
				ActivatePage(0);
		}

		private void SizeAll()
		{
			foreach ( WelcomeTabPage wtp in pages )
				InitPage(wtp);
		}

		private int LeftEdgeChildren
		{
			get
			{
				return 300;
			}
		}

		private GraphicsPath HighlightPath(int n)
		{
			const int radius=20;

			Rectangle rc=BoundingRect(n);
			rc.Inflate(margin, margin);

			Point pt=rc.Location;

			GraphicsPath gp=new GraphicsPath();

			int right=LeftEdgeChildren-margin*4;

			gp.AddArc(pt.X, pt.Y, radius, radius, 270, -90);
			gp.AddArc(pt.X, rc.Bottom-radius, radius, radius, 180, -90);
			gp.AddArc(right, rc.Bottom, radius, radius, 270, 90);
			gp.AddArc(right+radius, Height-radius, radius, radius, 180, -90);
			gp.AddArc(Width-radius, Height-radius, radius, radius, 90, -90);
			gp.AddArc(Width-radius, 0, radius, radius, 0, -90);
			gp.AddArc(right+radius, 0, radius, radius, 270, -90);
			gp.AddArc(right, pt.Y-radius, radius, radius, 0, 90);

			gp.CloseFigure();
			return gp;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;

			Pen p=new Pen(Color.Silver, 2);
			for ( int n=0; n< Pages.Count; n++ )
			{
				Rectangle rc=BoundingRect(n);

				Brush textBrush=Brushes.White;

				if ( Pages[n].Equals(activePage) )
				{
					using ( GraphicsPath gp=HighlightPath(n) )
					{
						e.Graphics.FillPath(new SolidBrush(highlightColor), gp);
					}
					textBrush=Brushes.Black;
						// e.Graphics.FillPath(SystemBrushes.ControlLight, gp);
						// e.Graphics.DrawPath(new Pen(Brushes.Red, 5), gp);
				}

				WelcomeTabPage page=Pages[n];
				if ( page.ImageList != null )
				{
					int dx=(rc.Width-page.ImageList.ImageSize.Width) / 2;
					int dy=(rc.Height-page.ImageList.ImageSize.Height) / 2;
					page.ImageList.Draw(e.Graphics, rc.X+dx, rc.Y+dy, page.ImageIndex);
				}

				if ( n == hoverIndex )
					DrawRoundRect(e.Graphics, p, rc, 10);

				Rectangle rc2=new Rectangle(rc.Right+margin, rc.Top+25, LeftEdgeChildren-rc.Right-margin*4, 180);
				rc.Inflate(-margin, -margin);


				StringFormat fmt=new StringFormat();
				fmt.Alignment=StringAlignment.Center;
				fmt.LineAlignment=StringAlignment.Center;
				e.Graphics.DrawString(Pages[n].ButtonText, Font, textBrush, rc, fmt);

//				Brush fb=new SolidBrush(Color.FromArgb(240,240,240));
				Font f1=new Font("Arial", 10, FontStyle.Bold);
				Font f2=new Font("Arial", 10);

				e.Graphics.DrawString(Pages[n].Title, f1, textBrush, rc2.X, rc2.Y-20);
				e.Graphics.DrawString(Pages[n].Description, f2, textBrush, rc2);
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			int index=HitTest(new Point(e.X, e.Y));
			if ( index != hoverIndex )
			{
				if ( hoverIndex >= 0 )
					InvalidateArea(hoverIndex);

				hoverIndex=index;

				if ( hoverIndex >= 0 )
					InvalidateArea(hoverIndex);
			}
		}

		private void InvalidateArea(int index)
		{
			Rectangle rc=BoundingRect(index);
			rc.Inflate(3, 3);
			Invalidate(rc);
		}

		private int HitTest(Point point)
		{
			for ( int n=0; n< Pages.Count; n++ )
			{
				if ( BoundingRect(n).Contains(point) )
					return n;
			}
			return -1;
		}

		public Rectangle BoundingRect(int item)
		{
			Point pt=new Point(margin, margin*4+100*item);
			return new Rectangle(pt, new Size(80, 80));
		}

		protected override void OnClick(EventArgs e)
		{
			Point pt=Cursor.Position;
			pt=PointToClient(pt);

			for ( int n=0; n< Pages.Count; n++ )
			{
				if ( BoundingRect(n).Contains(pt) )
				{
					ActivatePage(n);
					return;
				}
			}
		}

		private void DrawRoundRect(Graphics g, Pen p, float X, float Y, float width, float height, float radius)
		{
			GraphicsPath gp=new GraphicsPath();
			gp.AddLine(X + radius, Y, X + width - (radius*2), Y);
			gp.AddArc(X + width - (radius*2), Y, radius*2, radius*2, 270, 90);
			gp.AddLine(X + width, Y + radius, X + width, Y + height - (radius*2));
			gp.AddArc(X + width - (radius*2), Y + height - (radius*2), radius*2, radius*2,0,90);
			gp.AddLine(X + width - (radius*2), Y + height, X + radius, Y + height);
			gp.AddArc(X, Y + height - (radius*2), radius*2, radius*2, 90, 90);
			gp.AddLine(X, Y + height - (radius*2), X, Y + radius);
			gp.AddArc(X, Y, radius*2, radius*2, 180, 90);
			gp.CloseFigure();
			g.DrawPath(p, gp);
			gp.Dispose();
		}

		private void DrawRoundRect(Graphics g, Pen p, Rectangle rc, float radius)
		{
			DrawRoundRect(g, p, rc.X, rc.Y, rc.Width, rc.Height, radius);
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public WelcomePageCollection Pages
		{
			get
			{
				return pages;
			}
		}

		protected internal void ActivatePage(int index)
		{
			//If the new page is invalid
			if ( index < 0 )
			{
				return;
			}
		

			//Change to the new Page
			WelcomeTabPage tWizPage = ((WelcomeTabPage) pages[index]);

			//Really activate the page
			ActivatePage(tWizPage);
		}


		public void ActivatePage(WelcomeTabPage page)
		{
			if (activePage != null)
			{
				activePage.Visible = false;
			}

			//Activate the new page
			activePage = page;

			if (activePage != null)
			{
				//Ensure that this panel displays inside the WelcomeTabControl
				activePage.Parent = this;
				if (this.Contains(activePage) == false)
					this.Container.Add(activePage);

				InitPage(activePage);

				activePage.Visible = true;
				activePage.BringToFront();
				activePage.FocusFirstTabIndex();
			}

			Invalidate(true);
		}

		private void InitPage(WelcomeTabPage page)
		{
			page.Location=new Point(LeftEdgeChildren-margin, margin);
			page.Anchor=AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
			page.Width=Width-LeftEdgeChildren;
			page.Height=Height-margin*2;
			page.BackColor=highlightColor;
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			// 
			// WelcomeTabControl
			// 
			this.Name = "WelcomeTabControl";
			this.Size = new System.Drawing.Size(792, 424);

		}

		public int PageIndex
		{
			get
			{
				return pages.IndexOf(activePage);
			}
			set
			{
				//Do I have any pages?
				if(pages.Count == 0)
				{
					//No then show nothing
					ActivatePage(-1);
					return;
				}
				// Validate the page asked for
				if (value < -1 || value >= pages.Count)
				{
					throw new ArgumentOutOfRangeException("PageIndex",
						value,
						"The page index must be between 0 and "+Convert.ToString(pages.Count-1)
						);
				}
				//Select the new page
				ActivatePage(value);
			}
		}

		#endregion
	}
}
