using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Reflection;
using System.Xml;
using Gui.Wizard;
using XEditNet.Profile.Registered;

namespace XEditNet.Profile
{
	/// <summary>
	/// Summary description for ControllerWizard.
	/// </summary>
	public class NewFileCtrl : UserControl
	{
		private Gui.Wizard.Wizard wizard1;
		private Gui.Wizard.WizardPage pageWizardSelect;
		private Gui.Wizard.Header header1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TreeView treeView1;
		private WizardPage dummy=new WizardPage();
		private ICreateWizardPlugin wizardPlugin;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private IXEditNetProfile currentProfile;

		public event EventHandler WizardFinished;

		public NewFileCtrl()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			wizard1.Pages.Add(dummy);
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			if ( !this.DesignMode )
			{
				LoadProfilesFromAssembly();
				LoadRegisteredProfiles();
			}
		}

		private void LoadRegisteredProfiles()
		{
			RegisteredTypes types=RegisteredTypes.Load();
			foreach ( ProfileInfo pi in types.Types )
			{
				RegisteredTypeProfile rtp=new RegisteredTypeProfile(pi);

				AddProfile(rtp);
			}
		}

		private void LoadProfilesFromAssembly()
		{
			Assembly asm=typeof(NewFileDialog).Assembly;
			foreach ( Type t in asm.GetTypes() )
			{
				if ( t.GetInterface(typeof(IXEditNetProfile).FullName) != null )
				{
					try
					{
						IXEditNetProfile xnp=(IXEditNetProfile) t.Assembly.CreateInstance(t.FullName);
						AddProfile(xnp);
					}
					catch ( MissingMethodException )
					{
						// TODO: L: hack!
					}
				}
			}
		}

		private void AddProfile(IXEditNetProfile xnp)
		{
			string groupName=xnp.Info.Group;
			string itemName=xnp.Info.Name;
	
			TreeNodeCollection parentList=treeView1.Nodes;
			if ( groupName != null )
			{
				bool found=false;
				foreach ( TreeNode n in parentList )
				{
					if ( n.Text == groupName )
					{
						parentList=n.Nodes;
						found=true;								
					}
				}
				if ( !found )
				{
					TreeNode n=new TreeNode(groupName);
					parentList.Add(n);
					parentList=n.Nodes;
				}
			}
			TreeNode itemNode=parentList.Add(itemName);
			itemNode.Tag=xnp;
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(NewFileCtrl));
			this.wizard1 = new Gui.Wizard.Wizard();
			this.pageWizardSelect = new Gui.Wizard.WizardPage();
			this.treeView1 = new System.Windows.Forms.TreeView();
			this.label1 = new System.Windows.Forms.Label();
			this.header1 = new Gui.Wizard.Header();
			this.wizard1.SuspendLayout();
			this.pageWizardSelect.SuspendLayout();
			this.SuspendLayout();
			// 
			// wizard1
			// 
			this.wizard1.CloneOnFinish = true;
			this.wizard1.Controls.Add(this.pageWizardSelect);
			this.wizard1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.wizard1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.wizard1.Location = new System.Drawing.Point(0, 0);
			this.wizard1.Name = "wizard1";
			this.wizard1.Pages.AddRange(new Gui.Wizard.WizardPage[] {
																		this.pageWizardSelect});
			this.wizard1.Size = new System.Drawing.Size(488, 475);
			this.wizard1.TabIndex = 0;
			this.wizard1.FinishSelected += new System.EventHandler(this.WizardFinishSelected);
			// 
			// pageWizardSelect
			// 
			this.pageWizardSelect.Controls.Add(this.treeView1);
			this.pageWizardSelect.Controls.Add(this.label1);
			this.pageWizardSelect.Controls.Add(this.header1);
			this.pageWizardSelect.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pageWizardSelect.IsFinishPage = false;
			this.pageWizardSelect.Location = new System.Drawing.Point(0, 0);
			this.pageWizardSelect.Name = "pageWizardSelect";
			this.pageWizardSelect.Size = new System.Drawing.Size(488, 427);
			this.pageWizardSelect.TabIndex = 1;
			this.pageWizardSelect.ShowFromBack += new System.EventHandler(this.FirstPageRedisplayed);
			this.pageWizardSelect.CloseFromNext += new Gui.Wizard.PageEventHandler(this.ProfileSelected);
			// 
			// treeView1
			// 
			this.treeView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.treeView1.HideSelection = false;
			this.treeView1.ImageIndex = -1;
			this.treeView1.Location = new System.Drawing.Point(8, 96);
			this.treeView1.Name = "treeView1";
			this.treeView1.Scrollable = false;
			this.treeView1.SelectedImageIndex = -1;
			this.treeView1.Size = new System.Drawing.Size(472, 320);
			this.treeView1.TabIndex = 2;
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(8, 72);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(100, 16);
			this.label1.TabIndex = 1;
			this.label1.Text = "&Wizards:";
			// 
			// header1
			// 
			this.header1.BackColor = System.Drawing.SystemColors.Control;
			this.header1.CausesValidation = false;
			this.header1.Description = "The list below shows the types of document currently registered with XEditNet";
			this.header1.Dock = System.Windows.Forms.DockStyle.Top;
			this.header1.Image = ((System.Drawing.Image)(resources.GetObject("header1.Image")));
			this.header1.Location = new System.Drawing.Point(0, 0);
			this.header1.Name = "header1";
			this.header1.Size = new System.Drawing.Size(488, 64);
			this.header1.TabIndex = 0;
			this.header1.Title = "Select a Wizard";
			// 
			// NewFileCtrl
			// 
			this.Controls.Add(this.wizard1);
			this.Name = "NewFileCtrl";
			this.Size = new System.Drawing.Size(488, 475);
			this.wizard1.ResumeLayout(false);
			this.pageWizardSelect.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		private void ProfileSelected(object sender, Gui.Wizard.PageEventArgs e)
		{
			IXEditNetProfile tmp=null;
			
			if ( treeView1.SelectedNode == null )
			{
				MessageBox.Show(this, "Please select a wizard to create a new document", "Select Profile", MessageBoxButtons.OK, MessageBoxIcon.Stop);
				return;
			}
			object o=treeView1.SelectedNode.Tag;
			if ( o is IXEditNetProfile )
				tmp=(IXEditNetProfile) o;
			else if ( o is ProfileInfo )
				tmp=new RegisteredTypeProfile((ProfileInfo) o);

			if ( tmp != null && tmp == currentProfile )
				return; // use previous wizard instance

			currentProfile=tmp;
			if ( currentProfile == null )
				throw new InvalidOperationException("Tag for selected node is null!");

			while ( wizard1.Pages.Count > 1 )
				wizard1.Pages.RemoveAt(1);

			wizardPlugin=currentProfile.GetCreateWizardPlugin();
			if ( wizardPlugin == null )
			{
				OnWizardFinished(new EventArgs());
				return;
			}
			wizard1.Pages.AddRange(wizardPlugin.Pages);

			wizard1.PageIndex=0;
			e.Page=wizard1.Pages[1];
		}

		private void OnWizardFinished(EventArgs eventArgs)
		{
			if ( WizardFinished != null )
				WizardFinished(this, eventArgs);
		}

		private void FirstPageRedisplayed(object sender, System.EventArgs e)
		{
		}

		public bool CloseOnFinish
		{
			get { return wizard1.CloneOnFinish; }
			set { wizard1.CloneOnFinish = value; }
		}

		public XmlDocument CreateNewDocument()
		{
			if ( wizardPlugin == null )
				return null;

			return wizardPlugin.CreateDocument();
		}

		private void WizardFinishSelected(object sender, System.EventArgs e)
		{
			OnWizardFinished(e);
		}
	}
}
