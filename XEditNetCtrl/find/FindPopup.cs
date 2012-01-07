using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;

using XEditNet.Location;

namespace XEditNet
{
	/// <summary>
	/// Summary description for FindPopup.
	/// </summary>
	internal class FindPopup : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textXPath;
		private System.Windows.Forms.Button btnFind;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		XEditNetCtrl context;

		public FindPopup(XEditNetCtrl context)
		{
			this.context=context;

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
			this.label1 = new System.Windows.Forms.Label();
			this.textXPath = new System.Windows.Forms.TextBox();
			this.btnFind = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(8, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(100, 16);
			this.label1.TabIndex = 0;
			this.label1.Text = "XPath:";
			// 
			// textXPath
			// 
			this.textXPath.Location = new System.Drawing.Point(8, 27);
			this.textXPath.Name = "textXPath";
			this.textXPath.Size = new System.Drawing.Size(328, 20);
			this.textXPath.TabIndex = 1;
			this.textXPath.Text = "";
			// 
			// btnFind
			// 
			this.btnFind.Location = new System.Drawing.Point(8, 64);
			this.btnFind.Name = "btnFind";
			this.btnFind.TabIndex = 2;
			this.btnFind.Text = "Find";
			this.btnFind.Click += new System.EventHandler(this.btnFind_Click);
			// 
			// FindPopup
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(344, 126);
			this.Controls.Add(this.btnFind);
			this.Controls.Add(this.textXPath);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "FindPopup";
			this.Text = "FindPopup";
			this.Load += new System.EventHandler(this.FindPopup_Load);
			this.ResumeLayout(false);

		}
		#endregion

		private void FindPopup_Load(object sender, System.EventArgs e)
		{
		
		}

		private void btnFind_Click(object sender, System.EventArgs e)
		{
			Selection sel=context.Selection;
			if ( sel.IsEmpty )
				return;

			SelectionPoint sp=sel.Start;

			XmlNode n=sp.Node.SelectSingleNode(textXPath.Text);

			if ( n != null )
			{
				
				sp=SelectionManager.CreateSelectionPoint(n, false);
				context.Selection=new Selection(sp);
			}
		}
	}
}
