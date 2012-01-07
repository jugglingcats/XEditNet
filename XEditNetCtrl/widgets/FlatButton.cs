using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace XEditNet.Widgets
{
	public class FlatButton : Button
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private Container components = null;
		private bool hover;

		public FlatButton()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call

		}

		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter (e);
			hover=true;
			Invalidate();
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave (e);
			hover=false;
			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if ( hover )
			{
				using ( Brush brush = new LinearGradientBrush(ClientRectangle, 
					Color.FromArgb(255,242,199), Color.FromArgb(255,213,147), LinearGradientMode.Vertical) )
				{
					e.Graphics.FillRectangle(brush, ClientRectangle);
				}
			} else
			{
				e.Graphics.FillRectangle(SystemBrushes.Control, ClientRectangle);
			}

			if ( this.ImageList != null )
				this.ImageList.Draw(e.Graphics, 0, 0, 0);

			if ( hover )
				e.Graphics.DrawRectangle(new Pen(Color.FromArgb(75,75,111)), 0, 0, Width-1, Height-1);

			if ( Focused )
			{
				Rectangle rc=ClientRectangle;
				rc.Inflate(-1, -1);
				ControlPaint.DrawFocusRectangle(e.Graphics, rc);
			}
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
			components = new System.ComponentModel.Container();
		}
		#endregion
	}
}
