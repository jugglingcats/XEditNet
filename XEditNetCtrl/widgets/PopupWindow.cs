using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

using XEditNet.Dtd;

namespace XEditNet.Widgets
{
	internal class PopupWindow : System.Windows.Forms.Form
	{
		private System.ComponentModel.IContainer components;
		private Control ctrl; // =new ElementListPanelBase();

		public PopupWindow(Control ctrl)
		{
			InitializeComponent();
			this.ctrl=ctrl;
			ctrl.Dock=DockStyle.Fill;
			Controls.Add(ctrl);
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

		protected override void OnDeactivate(EventArgs e)
		{
			Close();
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(PopupWindow));
			// 
			// PopupElements
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.BackColor = System.Drawing.Color.FloralWhite;
			this.ClientSize = new System.Drawing.Size(223, 231);
			this.ControlBox = false;
			this.ForeColor = System.Drawing.SystemColors.ControlText;
			this.KeyPreview = true;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "PopupElements";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
		}

		#endregion

	}
}
