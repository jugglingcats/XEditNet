using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using XEditNet;
using XEditNet.Profile;

namespace XEditNetAuthor
{
	/// <summary>
	/// Summary description for XEditNetDefaultEditorRegion.
	/// </summary>
	public class XEditNetDefaultEditorRegion : System.Windows.Forms.UserControl, IXEditNetEditorRegion
	{
		private XEditNet.XEditNetCtrl editor;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public XEditNetDefaultEditorRegion()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call

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
			this.editor = new XEditNet.XEditNetCtrl();
			this.SuspendLayout();
			// 
			// xEditNetCtrl1
			// 
			this.editor.AutoScroll = true;
			this.editor.BackColor = System.Drawing.Color.White;
			this.editor.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.editor.Dock = System.Windows.Forms.DockStyle.Fill;
			this.editor.IsModified = false;
			this.editor.Location = new System.Drawing.Point(0, 0);
			this.editor.Name = "editor";
			this.editor.Size = new System.Drawing.Size(504, 336);
			this.editor.TabIndex = 0;
			// 
			// XEditNetDefaultEditorRegion
			// 
			this.Controls.Add(this.editor);
			this.Name = "XEditNetDefaultEditorRegion";
			this.Size = new System.Drawing.Size(504, 336);
			this.ResumeLayout(false);

		}
		#endregion

		public XEditNetCtrl Editor
		{
			get { return editor; }
		}
	}
}
