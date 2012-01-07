using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using XEditNet.Profile;
using XEditNetAuthor.Welcome;

namespace XEditNetAuthor
{
	/// <summary>
	/// Summary description for WelcomeForm.
	/// </summary>
	public class WelcomeForm : System.Windows.Forms.Form
	{
		private XEditNetAuthor.Welcome.WelcomeTabControl wtc;
		private XEditNetAuthor.Welcome.WelcomeTabPage welcomeTabPage1;
		private XEditNetAuthor.Welcome.WelcomeTabPage welcomeTabPage2;
		private XEditNetAuthor.Welcome.WelcomeTabPage welcomeTabPage4;
		private XEditNetAuthor.Welcome.WelcomeTabPage samplesPanel;
		private System.Windows.Forms.ListView listView1;
		private System.Windows.Forms.ColumnHeader colName;
		private System.Windows.Forms.ColumnHeader colDescription;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private int startW;
		private XEditNet.Profile.NewFileCtrl newFileCtrl1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private int startH;

		public WelcomeForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			FileInfo fi=new FileInfo("samples/samples.xml");
			SampleList sl=SampleList.FromXml(fi.FullName);
			foreach ( Sample s in sl.Samples )
			{
				ListViewItem lvi=new ListViewItem(new string[] {s.Name, s.Description});
				lvi.Tag=new Uri(new Uri(fi.FullName), s.File);
				listView1.Items.Add(lvi);
			}

			startW=wtc.Width;
			startH=wtc.Height;

//			NewFileWizard nfw=new NewFileWizard();
//			welcomeTabPage2.Controls.Add(nfw);
//			nfw.Dock=DockStyle.Fill;
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);

			Point pt=new Point(
				(ClientRectangle.Width - startW) / 2,
				(ClientRectangle.Height - startH) / 2
			);

			if ( pt.X < 0 )
			{
				pt.X=0;
				wtc.Width=ClientRectangle.Width;
			}

			if ( pt.Y < 0 )
			{
				pt.Y=0;
				wtc.Height=ClientRectangle.Height;
			}

			wtc.Location=pt;
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
			this.wtc = new XEditNetAuthor.Welcome.WelcomeTabControl();
			this.welcomeTabPage4 = new XEditNetAuthor.Welcome.WelcomeTabPage();
			this.welcomeTabPage1 = new XEditNetAuthor.Welcome.WelcomeTabPage();
			this.samplesPanel = new XEditNetAuthor.Welcome.WelcomeTabPage();
			this.listView1 = new System.Windows.Forms.ListView();
			this.colName = new System.Windows.Forms.ColumnHeader();
			this.colDescription = new System.Windows.Forms.ColumnHeader();
			this.welcomeTabPage2 = new XEditNetAuthor.Welcome.WelcomeTabPage();
			this.newFileCtrl1 = new XEditNet.Profile.NewFileCtrl();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.wtc.SuspendLayout();
			this.welcomeTabPage4.SuspendLayout();
			this.welcomeTabPage1.SuspendLayout();
			this.samplesPanel.SuspendLayout();
			this.welcomeTabPage2.SuspendLayout();
			this.SuspendLayout();
			// 
			// wtc
			// 
			this.wtc.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.wtc.Controls.Add(this.welcomeTabPage1);
			this.wtc.Controls.Add(this.welcomeTabPage4);
			this.wtc.Controls.Add(this.welcomeTabPage2);
			this.wtc.Controls.Add(this.samplesPanel);
			this.wtc.Location = new System.Drawing.Point(16, 16);
			this.wtc.Name = "wtc";
			this.wtc.PageIndex = 2;
			this.wtc.Pages.AddRange(new XEditNetAuthor.Welcome.WelcomeTabPage[] {
																					this.welcomeTabPage2,
																					this.samplesPanel,
																					this.welcomeTabPage1,
																					this.welcomeTabPage4});
			this.wtc.Size = new System.Drawing.Size(824, 552);
			this.wtc.TabIndex = 0;
			// 
			// welcomeTabPage4
			// 
			this.welcomeTabPage4.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.welcomeTabPage4.BackColor = System.Drawing.SystemColors.Control;
			this.welcomeTabPage4.ButtonText = "Tips";
			this.welcomeTabPage4.Controls.Add(this.label2);
			this.welcomeTabPage4.Description = "Learn how to get the best from XEditNet";
			this.welcomeTabPage4.ImageIndex = 0;
			this.welcomeTabPage4.ImageList = null;
			this.welcomeTabPage4.Location = new System.Drawing.Point(290, 10);
			this.welcomeTabPage4.Name = "welcomeTabPage4";
			this.welcomeTabPage4.Size = new System.Drawing.Size(524, 532);
			this.welcomeTabPage4.TabIndex = 3;
			this.welcomeTabPage4.Title = "Hints And Tips";
			this.welcomeTabPage4.Visible = false;
			// 
			// welcomeTabPage1
			// 
			this.welcomeTabPage1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.welcomeTabPage1.BackColor = System.Drawing.SystemColors.Control;
			this.welcomeTabPage1.ButtonText = "Recent";
			this.welcomeTabPage1.Controls.Add(this.label1);
			this.welcomeTabPage1.Description = "Open a file you have recently edited";
			this.welcomeTabPage1.ImageIndex = 0;
			this.welcomeTabPage1.ImageList = null;
			this.welcomeTabPage1.Location = new System.Drawing.Point(290, 10);
			this.welcomeTabPage1.Name = "welcomeTabPage1";
			this.welcomeTabPage1.Size = new System.Drawing.Size(524, 532);
			this.welcomeTabPage1.TabIndex = 0;
			this.welcomeTabPage1.Title = "Recent Files";
			this.welcomeTabPage1.Visible = false;
			// 
			// samplesPanel
			// 
			this.samplesPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.samplesPanel.AutoScroll = true;
			this.samplesPanel.BackColor = System.Drawing.SystemColors.Control;
			this.samplesPanel.ButtonText = "Samples";
			this.samplesPanel.Controls.Add(this.listView1);
			this.samplesPanel.Description = "Browse samples showing the features of XEditNet";
			this.samplesPanel.ImageIndex = 0;
			this.samplesPanel.ImageList = null;
			this.samplesPanel.Location = new System.Drawing.Point(290, 10);
			this.samplesPanel.Name = "samplesPanel";
			this.samplesPanel.Size = new System.Drawing.Size(524, 532);
			this.samplesPanel.TabIndex = 2;
			this.samplesPanel.Title = "Explore Samples";
			// 
			// listView1
			// 
			this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
																						this.colName,
																						this.colDescription});
			this.listView1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listView1.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.listView1.FullRowSelect = true;
			this.listView1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.listView1.Location = new System.Drawing.Point(0, 0);
			this.listView1.MultiSelect = false;
			this.listView1.Name = "listView1";
			this.listView1.Size = new System.Drawing.Size(524, 532);
			this.listView1.TabIndex = 0;
			this.listView1.View = System.Windows.Forms.View.Details;
			this.listView1.DoubleClick += new System.EventHandler(this.OpenSample);
			// 
			// colName
			// 
			this.colName.Text = "Name";
			this.colName.Width = 121;
			// 
			// colDescription
			// 
			this.colDescription.Text = "Description";
			this.colDescription.Width = 399;
			// 
			// welcomeTabPage2
			// 
			this.welcomeTabPage2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.welcomeTabPage2.BackColor = System.Drawing.SystemColors.Control;
			this.welcomeTabPage2.ButtonText = "New";
			this.welcomeTabPage2.Controls.Add(this.newFileCtrl1);
			this.welcomeTabPage2.Description = "Create a new XML file or document from profile";
			this.welcomeTabPage2.ImageIndex = 0;
			this.welcomeTabPage2.ImageList = null;
			this.welcomeTabPage2.Location = new System.Drawing.Point(290, 10);
			this.welcomeTabPage2.Name = "welcomeTabPage2";
			this.welcomeTabPage2.Size = new System.Drawing.Size(524, 532);
			this.welcomeTabPage2.TabIndex = 1;
			this.welcomeTabPage2.Title = "Create New File";
			// 
			// newFileCtrl1
			// 
			this.newFileCtrl1.BackColor = System.Drawing.SystemColors.Control;
			this.newFileCtrl1.CloseOnFinish = false;
			this.newFileCtrl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.newFileCtrl1.Location = new System.Drawing.Point(0, 0);
			this.newFileCtrl1.Name = "newFileCtrl1";
			this.newFileCtrl1.Size = new System.Drawing.Size(524, 532);
			this.newFileCtrl1.TabIndex = 0;
			this.newFileCtrl1.WizardFinished += new System.EventHandler(this.NewFileWizardFinished);
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(176, 216);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(192, 23);
			this.label1.TabIndex = 0;
			this.label1.Text = "Not currently implemented";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(176, 216);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(192, 23);
			this.label2.TabIndex = 1;
			this.label2.Text = "Not currently implemented";
			// 
			// WelcomeForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.AutoScroll = true;
			this.BackColor = System.Drawing.SystemColors.ControlDark;
			this.ClientSize = new System.Drawing.Size(856, 582);
			this.Controls.Add(this.wtc);
			this.Name = "WelcomeForm";
			this.Text = "Welcome";
			this.wtc.ResumeLayout(false);
			this.welcomeTabPage4.ResumeLayout(false);
			this.welcomeTabPage1.ResumeLayout(false);
			this.samplesPanel.ResumeLayout(false);
			this.welcomeTabPage2.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		private void OpenSample(object sender, System.EventArgs e)
		{
			IList col=listView1.SelectedItems;
			if ( col.Count == 0 )
				return;

			ListViewItem lvi=(ListViewItem) col[0];
			Uri uri=(Uri) lvi.Tag;

			XEditNetMainForm2 xenmf=(XEditNetMainForm2) ParentForm;

			Cursor c=this.Cursor;
			Cursor=Cursors.WaitCursor;
            //xenmf.OpenFile(uri.AbsolutePath);
			Cursor=c;
		}

		private void NewFileWizardFinished(object sender, System.EventArgs e)
		{
			XmlDocument doc=newFileCtrl1.CreateNewDocument();
            //((XEditNetMainForm2) ParentForm).OpenNew(doc);
		}
	}
}
