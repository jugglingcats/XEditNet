using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml;

namespace XEditNet.Profile
{
	/// <summary>
	/// Summary description for NewFileDialog.
	/// </summary>
	public class NewFileDialog : System.Windows.Forms.Form
	{
		private XEditNet.Profile.NewFileCtrl newFileCtrl;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public NewFileDialog()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
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

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.newFileCtrl = new XEditNet.Profile.NewFileCtrl();
			this.SuspendLayout();
			// 
			// newFileCtrl
			// 
			this.newFileCtrl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.newFileCtrl.Location = new System.Drawing.Point(0, 0);
			this.newFileCtrl.Name = "newFileCtrl";
			this.newFileCtrl.Size = new System.Drawing.Size(464, 438);
			this.newFileCtrl.TabIndex = 0;
			this.newFileCtrl.WizardFinished += new System.EventHandler(this.WizardFinished);
			// 
			// NewFileDialog
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(464, 438);
			this.Controls.Add(this.newFileCtrl);
			this.Name = "NewFileDialog";
			this.Text = "NewFileDialog";
			this.ResumeLayout(false);

		}

		public XmlDocument CreateNewDocument()
		{
			return newFileCtrl.CreateNewDocument();
		}

		#endregion

		private void WizardFinished(object sender, System.EventArgs e)
		{
			DialogResult=DialogResult.OK;
			Close();
		}
	}
}
