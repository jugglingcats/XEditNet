using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml;
using Gui.Wizard;
using XEditNet.Profile;
using XEditNet.Util;

namespace XEditNet.Profile.SimpleDtd
{
	/// <summary>
	/// Summary description for GenDtdWizard.
	/// </summary>
	public class GenDtdWizardCtrl : UserControl, ICreateWizardPlugin
	{
		private WizardPage pageEnterDtd;
		private Header header1;
		private Label label1;
		private Wizard wizard;
		private Button button1;
		private ComboBox comboDtdUrl;
		private WizardPage pageSelectRootElement;
		private Header header2;
		private Label label2;
		private ComboBox comboRootElement;
		private System.Windows.Forms.CheckBox checkBoxFilterElems;

		private ElementInfo[] elementInfo;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;

		public GenDtdWizardCtrl()
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(GenDtdWizardCtrl));
			this.wizard = new Gui.Wizard.Wizard();
			this.pageSelectRootElement = new Gui.Wizard.WizardPage();
			this.checkBoxFilterElems = new System.Windows.Forms.CheckBox();
			this.comboRootElement = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.header2 = new Gui.Wizard.Header();
			this.pageEnterDtd = new Gui.Wizard.WizardPage();
			this.comboDtdUrl = new System.Windows.Forms.ComboBox();
			this.button1 = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.header1 = new Gui.Wizard.Header();
			this.wizard.SuspendLayout();
			this.pageSelectRootElement.SuspendLayout();
			this.pageEnterDtd.SuspendLayout();
			this.SuspendLayout();
			// 
			// wizard
			// 
			this.wizard.Controls.Add(this.pageEnterDtd);
			this.wizard.Controls.Add(this.pageSelectRootElement);
			this.wizard.Dock = System.Windows.Forms.DockStyle.Fill;
			this.wizard.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.wizard.Location = new System.Drawing.Point(0, 0);
			this.wizard.Name = "wizard";
			this.wizard.Pages.AddRange(new Gui.Wizard.WizardPage[] {
																	   this.pageEnterDtd,
																	   this.pageSelectRootElement});
			this.wizard.Size = new System.Drawing.Size(480, 446);
			this.wizard.TabIndex = 0;
			// 
			// pageSelectRootElement
			// 
			this.pageSelectRootElement.Controls.Add(this.checkBoxFilterElems);
			this.pageSelectRootElement.Controls.Add(this.comboRootElement);
			this.pageSelectRootElement.Controls.Add(this.label2);
			this.pageSelectRootElement.Controls.Add(this.header2);
			this.pageSelectRootElement.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pageSelectRootElement.IsFinishPage = false;
			this.pageSelectRootElement.Location = new System.Drawing.Point(0, 0);
			this.pageSelectRootElement.Name = "pageSelectRootElement";
			this.pageSelectRootElement.Size = new System.Drawing.Size(480, 398);
			this.pageSelectRootElement.TabIndex = 2;
			this.pageSelectRootElement.ShowFromNext += new System.EventHandler(this.UpdateDtdInfo);
			// 
			// checkBoxFilterElems
			// 
			this.checkBoxFilterElems.Checked = true;
			this.checkBoxFilterElems.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxFilterElems.Location = new System.Drawing.Point(8, 128);
			this.checkBoxFilterElems.Name = "checkBoxFilterElems";
			this.checkBoxFilterElems.Size = new System.Drawing.Size(248, 16);
			this.checkBoxFilterElems.TabIndex = 3;
			this.checkBoxFilterElems.Text = "Only Show Top-level Elements in the DTD";
			this.checkBoxFilterElems.Click += new System.EventHandler(this.ToggleFilterItems);
			// 
			// comboRootElement
			// 
			this.comboRootElement.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.comboRootElement.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboRootElement.Location = new System.Drawing.Point(8, 96);
			this.comboRootElement.Name = "comboRootElement";
			this.comboRootElement.Size = new System.Drawing.Size(464, 21);
			this.comboRootElement.TabIndex = 2;
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(8, 72);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(168, 16);
			this.label2.TabIndex = 1;
			this.label2.Text = "&Elements:";
			// 
			// header2
			// 
			this.header2.BackColor = System.Drawing.SystemColors.Control;
			this.header2.CausesValidation = false;
			this.header2.Description = "Select a root element from the elements defined in the DTD/schema.";
			this.header2.Dock = System.Windows.Forms.DockStyle.Top;
			this.header2.Image = ((System.Drawing.Image)(resources.GetObject("header2.Image")));
			this.header2.Location = new System.Drawing.Point(0, 0);
			this.header2.Name = "header2";
			this.header2.Size = new System.Drawing.Size(480, 64);
			this.header2.TabIndex = 0;
			this.header2.Title = "Select Root Element";
			// 
			// pageEnterDtd
			// 
			this.pageEnterDtd.Controls.Add(this.comboDtdUrl);
			this.pageEnterDtd.Controls.Add(this.button1);
			this.pageEnterDtd.Controls.Add(this.label1);
			this.pageEnterDtd.Controls.Add(this.header1);
			this.pageEnterDtd.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pageEnterDtd.IsFinishPage = false;
			this.pageEnterDtd.Location = new System.Drawing.Point(0, 0);
			this.pageEnterDtd.Name = "pageEnterDtd";
			this.pageEnterDtd.Size = new System.Drawing.Size(480, 398);
			this.pageEnterDtd.TabIndex = 1;
			// 
			// comboDtdUrl
			// 
			this.comboDtdUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.comboDtdUrl.Location = new System.Drawing.Point(8, 96);
			this.comboDtdUrl.Name = "comboDtdUrl";
			this.comboDtdUrl.Size = new System.Drawing.Size(376, 21);
			this.comboDtdUrl.TabIndex = 4;
			// 
			// button1
			// 
			this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.button1.Location = new System.Drawing.Point(392, 96);
			this.button1.Name = "button1";
			this.button1.TabIndex = 3;
			this.button1.Text = "&Browse...";
			this.button1.Click += new System.EventHandler(this.BrowseForDTD);
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(8, 72);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(100, 16);
			this.label1.TabIndex = 1;
			this.label1.Text = "&DTD Location:";
			// 
			// header1
			// 
			this.header1.BackColor = System.Drawing.SystemColors.Control;
			this.header1.CausesValidation = false;
			this.header1.Description = "Enter or browse for the document type (DTD) that will be used for the new XML doc" +
				"ument.";
			this.header1.Dock = System.Windows.Forms.DockStyle.Top;
			this.header1.Image = ((System.Drawing.Image)(resources.GetObject("header1.Image")));
			this.header1.Location = new System.Drawing.Point(0, 0);
			this.header1.Name = "header1";
			this.header1.Size = new System.Drawing.Size(480, 64);
			this.header1.TabIndex = 0;
			this.header1.Title = "Select Document Type";
			// 
			// GenDtdWizard
			// 
			this.ClientSize = new System.Drawing.Size(480, 446);
			this.Controls.Add(this.wizard);
			this.Name = "GenDtdWizard";
			this.Text = "GenDtdWizard";
			this.wizard.ResumeLayout(false);
			this.pageSelectRootElement.ResumeLayout(false);
			this.pageEnterDtd.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		private void BrowseForDTD(object sender, System.EventArgs e)
		{
			FileDialog fd=new OpenFileDialog();
			fd.CheckFileExists=true;
			fd.DefaultExt="dtd";
			DialogResult ret=fd.ShowDialog();
			if ( ret == DialogResult.OK )
				comboDtdUrl.Text=fd.FileName;
		}

		private void UpdateDtdInfo(object sender, System.EventArgs e)
		{
			try
			{
				elementInfo=DtdInfo.GetAllElements(new Uri(comboDtdUrl.Text));
				PopulateCombo();
			}
			catch ( Exception ex )
			{
				MessageBox.Show("Could not read DTD\n"+ex.Message, "Create Document", MessageBoxButtons.OK,  MessageBoxIcon.Error);
			}
		}

		private void PopulateCombo()
		{
			comboRootElement.Items.Clear();

			foreach ( ElementInfo ei in elementInfo )
			{
				if ( ei.IsRootElement || !checkBoxFilterElems.Checked )
					comboRootElement.Items.Add(ei.LocalName);
			}
			if ( comboRootElement.Items.Count > 0 )
				comboRootElement.SelectedIndex=0;
		}

		private void ToggleFilterItems(object sender, System.EventArgs e)
		{
			PopulateCombo();
		}

		public WizardPage[] Pages
		{
			get
			{
				WizardPage[] pages=new WizardPage[wizard.Pages.Count];
				int n=0;
				foreach ( WizardPage p in wizard.Pages )
					pages[n++]=p;

				return pages;
			}
		}

		public XmlDocument CreateDocument()
		{
			XmlDocument doc=new XmlDocument();

			try
			{
				Uri uri=new Uri(comboDtdUrl.Text);
				XmlDocumentType dtd=doc.CreateDocumentType(comboRootElement.Text, null, uri.AbsoluteUri, null);
				doc.AppendChild(dtd);
			} 
			catch ( Exception e )
			{
				MessageBox.Show(this, "An error occurred reading the DTD\n"+e.Message, "Create Document", MessageBoxButtons.OK,  MessageBoxIcon.Error);	
				return null;
			}

			XmlElement root=doc.CreateElement(comboRootElement.Text);
			doc.AppendChild(root);

			return doc;
		}

	}
}
