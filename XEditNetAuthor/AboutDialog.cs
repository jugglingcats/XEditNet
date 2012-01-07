using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

using XEditNet;

namespace XEditNetAuthor
{
	/// <summary>
	/// Summary description for About.
	/// </summary>
	public class AboutDialog : System.Windows.Forms.Form
	{
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.PictureBox pictureBox2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label authorVersion;
		private System.Windows.Forms.Label ctrlVersion;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public AboutDialog()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(AboutDialog));
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.pictureBox2 = new System.Windows.Forms.PictureBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.authorVersion = new System.Windows.Forms.Label();
			this.ctrlVersion = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// pictureBox1
			// 
			this.pictureBox1.Enabled = false;
			this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
			this.pictureBox1.Location = new System.Drawing.Point(8, 8);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(80, 88);
			this.pictureBox1.TabIndex = 0;
			this.pictureBox1.TabStop = false;
			// 
			// pictureBox2
			// 
			this.pictureBox2.Enabled = false;
			this.pictureBox2.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox2.Image")));
			this.pictureBox2.Location = new System.Drawing.Point(92, 8);
			this.pictureBox2.Name = "pictureBox2";
			this.pictureBox2.Size = new System.Drawing.Size(228, 88);
			this.pictureBox2.TabIndex = 1;
			this.pictureBox2.TabStop = false;
			// 
			// label1
			// 
			this.label1.Enabled = false;
			this.label1.Location = new System.Drawing.Point(16, 104);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(184, 16);
			this.label1.TabIndex = 2;
			this.label1.Text = "XEditNet Author";
			// 
			// label2
			// 
			this.label2.Enabled = false;
			this.label2.Location = new System.Drawing.Point(16, 120);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(184, 16);
			this.label2.TabIndex = 3;
			this.label2.Text = "XEditNetCtrl";
			// 
			// authorVersion
			// 
			this.authorVersion.Enabled = false;
			this.authorVersion.Location = new System.Drawing.Point(208, 104);
			this.authorVersion.Name = "authorVersion";
			this.authorVersion.Size = new System.Drawing.Size(104, 16);
			this.authorVersion.TabIndex = 2;
			this.authorVersion.Text = "ALPHA";
			this.authorVersion.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// ctrlVersion
			// 
			this.ctrlVersion.Enabled = false;
			this.ctrlVersion.Location = new System.Drawing.Point(208, 120);
			this.ctrlVersion.Name = "ctrlVersion";
			this.ctrlVersion.Size = new System.Drawing.Size(104, 16);
			this.ctrlVersion.TabIndex = 2;
			this.ctrlVersion.Text = "ALPHA";
			this.ctrlVersion.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// label5
			// 
			this.label5.Enabled = false;
			this.label5.Location = new System.Drawing.Point(16, 144);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(288, 16);
			this.label5.TabIndex = 3;
			this.label5.Text = "Copyright (c) 2004 XEditNet Ltd. All rights reserved.";
			// 
			// AboutDialog
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.BackColor = System.Drawing.Color.White;
			this.ClientSize = new System.Drawing.Size(328, 168);
			this.ControlBox = false;
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.pictureBox2);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.authorVersion);
			this.Controls.Add(this.ctrlVersion);
			this.Controls.Add(this.label5);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "AboutDialog";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Click += new System.EventHandler(this.About_Click);
			this.Load += new System.EventHandler(this.AboutDialog_Load);
			this.ResumeLayout(false);

		}
		#endregion

		private void About_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}

		private void AboutDialog_Load(object sender, System.EventArgs e)
		{
			authorVersion.Text=this.GetType().Assembly.GetName().Version.ToString();
			ctrlVersion.Text=typeof(XEditNetCtrl).Assembly.GetName().Version.ToString();
		}
	}
}
