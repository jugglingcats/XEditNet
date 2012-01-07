using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Xsl;
using MultiOutput;
using XEditNet;
using XEditNet.Location;
using XEditNet.Profile;

namespace XenWebProfile
{
	/// <summary>
	/// Summary description for UserControl1.
	/// </summary>
	public class WebProfileEditorRegion : System.Windows.Forms.UserControl, IXEditNetEditorRegion
	{
		private XEditNet.XEditNetCtrl editor;
		private System.Windows.Forms.TabControl tabControl;
		private System.Windows.Forms.TabPage editorTab;
		private System.Windows.Forms.TabPage browserTab;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private WebBrowser webBrowser;

		public WebProfileEditorRegion()
		{
			// This call is required by the Windows.Forms Form Designer.
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

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            XEditNet.Location.Selection selection1 = new XEditNet.Location.Selection();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.editorTab = new System.Windows.Forms.TabPage();
            this.editor = new XEditNet.XEditNetCtrl();
            this.browserTab = new System.Windows.Forms.TabPage();
            this.webBrowser = new System.Windows.Forms.WebBrowser();
            this.tabControl.SuspendLayout();
            this.editorTab.SuspendLayout();
            this.browserTab.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.editorTab);
            this.tabControl.Controls.Add(this.browserTab);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Multiline = true;
            this.tabControl.Name = "tabControl";
            this.tabControl.Padding = new System.Drawing.Point(8, 6);
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(440, 384);
            this.tabControl.TabIndex = 0;
            this.tabControl.SelectedIndexChanged += new System.EventHandler(this.TabChanged);
            // 
            // editorTab
            // 
            this.editorTab.Controls.Add(this.editor);
            this.editorTab.Location = new System.Drawing.Point(4, 28);
            this.editorTab.Name = "editorTab";
            this.editorTab.Size = new System.Drawing.Size(432, 352);
            this.editorTab.TabIndex = 0;
            this.editorTab.Text = "Editor";
            // 
            // editor
            // 
            this.editor.AutoScroll = true;
            this.editor.BackColor = System.Drawing.Color.White;
            this.editor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editor.IsModified = false;
            this.editor.Location = new System.Drawing.Point(0, 0);
            this.editor.Name = "editor";
            this.editor.Selection = selection1;
            this.editor.Size = new System.Drawing.Size(432, 352);
            this.editor.TabIndex = 0;
            // 
            // browserTab
            // 
            this.browserTab.Controls.Add(this.webBrowser);
            this.browserTab.Location = new System.Drawing.Point(4, 28);
            this.browserTab.Name = "browserTab";
            this.browserTab.Size = new System.Drawing.Size(432, 352);
            this.browserTab.TabIndex = 1;
            this.browserTab.Text = "Browser Preview";
            // 
            // webBrowser1
            // 
            this.webBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowser.Location = new System.Drawing.Point(0, 0);
            this.webBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser.Name = "webBrowser1";
            this.webBrowser.Size = new System.Drawing.Size(432, 352);
            this.webBrowser.TabIndex = 0;
            // 
            // WebProfileEditorRegion
            // 
            this.Controls.Add(this.tabControl);
            this.Name = "WebProfileEditorRegion";
            this.Size = new System.Drawing.Size(440, 384);
            this.tabControl.ResumeLayout(false);
            this.editorTab.ResumeLayout(false);
            this.browserTab.ResumeLayout(false);
            this.ResumeLayout(false);

		}
		#endregion

		private void TabChanged(object sender, System.EventArgs e)
		{
			int tab=tabControl.SelectedIndex;
			if ( tab == 0 )
				return;

			XmlDocument doc=editor.Document;

			XmlProcessingInstruction stylePi=doc.SelectSingleNode(
				"processing-instruction('xeditnet-xsl')") as XmlProcessingInstruction;

			if ( stylePi == null )
			{
				MessageBox.Show(this, "No xeditnet-xsl processing instruction found");
				return;
			}

			Uri baseUri=new Uri(doc.BaseURI);
			try 
			{
				Uri uri=new Uri(baseUri, stylePi.Value);

				FileInfo fi=new FileInfo(baseUri.LocalPath);
				string outputDir=fi.DirectoryName;

				XslTransform xsl=new XslTransform();
				xsl.Load(uri.AbsolutePath);
				MultiXmlTextWriter mxtw=new MultiXmlTextWriter(outputDir+"/_default.html", null);
				try
				{
					xsl.Transform(doc, null, mxtw, null);
				} 
				finally
				{
					mxtw.Close();
				}
			}
			catch ( Exception ex )
			{
				MessageBox.Show(this, ex.Message);
				return;
			}

			Selection sel=editor.Selection;
			if ( sel.IsEmpty )
				return;

			string file="_default.html";

			// TODO: M: deal better with entity ref at caret
			if ( !sel.IsEmpty && sel.Start.Node.NodeType != XmlNodeType.EntityReference )
			{
				SelectionPoint sp=sel.Start;
				XmlElement n=sp.Node.SelectSingleNode("ancestor::*[@id != '']") as XmlElement;
				if ( n != null )
					file=n.GetAttribute("id")+".html";
			}

			Uri currentPage=new Uri(baseUri, file);
			webBrowser.Navigate(currentPage.LocalPath);
		}

		public XEditNetCtrl Editor
		{
			get { return editor; }
		}
	}
}
