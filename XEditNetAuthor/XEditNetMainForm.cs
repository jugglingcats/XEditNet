using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Forms.MRU;
using TD.SandBar;
using XEditNet;
using XEditNet.Location;
using XEditNet.Profile;
// TODO: M: remove or change the current caret indicator, and when all docs closed

namespace XEditNetAuthor
{
	public class XEditNetMainForm : Form, IMRUClient
	{
		private StatusBarPanel mainStatusPanel;
		private MenuItem menuItem1;
//		private XEditNet.XEditNetElementCtrl elements;
//		private XEditNet.XEditNetAttributeCtrl attributes;

//		private DockingManager dockingManager;
		private System.Windows.Forms.StatusBar statusBar;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;

		private static readonly String registryPath=@"Software\XEditNet\Author";
		private PersistWindowState state=new PersistWindowState();
		private MRUManager mru=new MRUManager();

		private static readonly string defaultFilter=
			"XML Files (*.xml)|*.xml|XHTML Files (*.xhtml; *.htm; *.html)|*.xhtml;*.htm;*.html|All Files (*.*)|*.*";
		private TD.SandBar.SandBarManager sandBarManager1;
		private TD.SandBar.ToolBarContainer leftSandBarDock;
		private TD.SandBar.ToolBarContainer rightSandBarDock;
		private TD.SandBar.ToolBarContainer bottomSandBarDock;
		private TD.SandBar.ToolBarContainer topSandBarDock;
		private TD.SandBar.MenuBar menuBar1;
		private TD.SandBar.MenuBarItem menuBarItem1;
		private TD.SandBar.MenuBarItem menuBarItem2;
		private TD.SandBar.MenuBarItem menuBarItem3;
		private TD.SandBar.MenuBarItem menuBarItem4;
		private TD.SandBar.MenuBarItem menuBarItem5;
		private TD.SandBar.MenuBarItem menuFile;
		private TD.SandBar.MenuButtonItem menuFileMru;
		private TD.SandBar.MenuButtonItem menuFileExit;
		private TD.SandBar.MenuBarItem menuItem3;
		private TD.SandBar.MenuBarItem menuItem4;
		private TD.SandBar.MenuButtonItem menuHelpUserGuide;
		private TD.SandBar.MenuButtonItem menuHelpAbout;
		private TD.SandBar.ToolBar toolbar;
		private TD.SandBar.MenuButtonItem menuFileOpen;
		private TD.SandBar.MenuButtonItem menuFileNew;
		private TD.SandBar.ButtonItem buttonItem2;
		private TD.SandBar.ButtonItem buttonItem4;
		private TD.SandBar.ButtonItem buttonFileNew;
		private TD.SandBar.ButtonItem buttonFileOpen;
		private TD.SandBar.MenuBarItem menuBarItem6;
		private TD.SandBar.MenuBarItem menuBarItem7;

		private IXEditNetEditorRegion activeChild;

		public XEditNetMainForm()
		{
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, false);
			SetStyle(ControlStyles.Opaque, true);

			InitializeComponent();

			state.RegistryPath=registryPath;
			state.Parent=this;

			mru.Initialize(this, menuFileMru, registryPath);

			Application.EnableVisualStyles();
			Application.DoEvents();
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			string[] args=Environment.GetCommandLineArgs();
			if ( args.Length > 1 )
			{
				for ( int n=1; n< args.Length; n++ )
					OpenFile(args[n]);
			}
			else
			{
				// TODO: M: put back in
				WelcomeForm wf=new WelcomeForm();
				wf.MdiParent = this;
				wf.WindowState=FormWindowState.Maximized;
				wf.Show();
			}
		}

		public void OpenFile(string filename)
		{
			FileInfo fi=new FileInfo(filename);
			Form child=(Form) FindExistingForm(fi);
			if ( child != null )
			{
				ActivateMdiChild(child);
				return;
			}
				
			OpenDocument(fi);
			mru.Add(fi.FullName);
		}

		private IXEditNetEditorRegion FindExistingForm(FileInfo fi)
		{
			foreach ( Form child in this.MdiChildren )
			{
				IXEditNetEditorRegion f=child as IXEditNetEditorRegion;
				if ( f == null )
					continue;

				if ( child.Text.Equals(fi.Name) )
					return f;
			}
			return null;
		}

		public void OpenDocument(FileInfo fi)
		{
			string title=fi == null ? "New Document" : fi.Name;

			try 
			{
				bool valid;
				XmlDocument doc=XEditNetCtrl.LoadDocument(fi.FullName, true, out valid);
				if ( doc == null )
					return;

				Form f=GetMdiForm(doc, valid);
				f.Text=title;
				f.Closing+=new CancelEventHandler(ChildClosing);
				f.MdiParent = this;
				f.WindowState=FormWindowState.Maximized;
				f.Show();
				f.Tag=fi;
			} 
			catch ( XmlException e )
			{
				// TODO: M: lots of things can cause this error, eg. trying to run xpath anywhere
				MessageBox.Show(this, "XML error reading document\n"+e.Message, "Open File", MessageBoxButtons.OK,  MessageBoxIcon.Error);
			}
			catch ( Exception e )
			{
				MessageBox.Show(this, e.Message);
			}
		}

		private static Form GetMdiForm(XmlDocument doc, bool valid)
		{
			IXEditNetProfile prof=ProfileProvider.GetProfile(doc);
	
			UserControl userControl=null;

			if ( prof != null )
				userControl=prof.GetEditorRegion(doc);

			if ( userControl == null )
				userControl=new XEditNetDefaultEditorRegion();

			IXEditNetEditorRegion r=userControl as IXEditNetEditorRegion;
			if ( r == null )
				throw new InvalidOperationException("User control returned by profile does not implement "+typeof(IXEditNetEditorRegion));

			if ( prof != null && prof.Info.Stylesheet != null && prof.Info.Stylesheet.Length > 0 )
				r.Editor.SetStylesheet(prof.Info.Stylesheet);

			r.Editor.Attach(doc, valid);
			XEditNetChildForm form=new XEditNetChildForm(userControl);

			return form;
		}

		private void ChildClosing(object sender, CancelEventArgs e)
		{
			XEditNetChildForm child=(XEditNetChildForm) sender;
			if ( !ConfirmSave(child.Editor, (FileInfo) ((Form) child).Tag) )
				e.Cancel=true;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(XEditNetMainForm));
			this.menuItem1 = new System.Windows.Forms.MenuItem();
			this.mainStatusPanel = new System.Windows.Forms.StatusBarPanel();
			this.statusBar = new System.Windows.Forms.StatusBar();
			this.sandBarManager1 = new TD.SandBar.SandBarManager();
			this.leftSandBarDock = new TD.SandBar.ToolBarContainer();
			this.rightSandBarDock = new TD.SandBar.ToolBarContainer();
			this.bottomSandBarDock = new TD.SandBar.ToolBarContainer();
			this.topSandBarDock = new TD.SandBar.ToolBarContainer();
			this.menuBar1 = new TD.SandBar.MenuBar();
			this.menuFile = new TD.SandBar.MenuBarItem();
			this.menuFileNew = new TD.SandBar.MenuButtonItem();
			this.menuFileOpen = new TD.SandBar.MenuButtonItem();
			this.menuFileMru = new TD.SandBar.MenuButtonItem();
			this.menuFileExit = new TD.SandBar.MenuButtonItem();
			this.menuBarItem6 = new TD.SandBar.MenuBarItem();
			this.menuBarItem7 = new TD.SandBar.MenuBarItem();
			this.menuItem3 = new TD.SandBar.MenuBarItem();
			this.menuItem4 = new TD.SandBar.MenuBarItem();
			this.menuHelpUserGuide = new TD.SandBar.MenuButtonItem();
			this.menuHelpAbout = new TD.SandBar.MenuButtonItem();
			this.toolbar = new TD.SandBar.ToolBar();
			this.buttonFileNew = new TD.SandBar.ButtonItem();
			this.buttonFileOpen = new TD.SandBar.ButtonItem();
			this.menuBarItem1 = new TD.SandBar.MenuBarItem();
			this.menuBarItem2 = new TD.SandBar.MenuBarItem();
			this.menuBarItem3 = new TD.SandBar.MenuBarItem();
			this.menuBarItem4 = new TD.SandBar.MenuBarItem();
			this.menuBarItem5 = new TD.SandBar.MenuBarItem();
			this.buttonItem2 = new TD.SandBar.ButtonItem();
			this.buttonItem4 = new TD.SandBar.ButtonItem();
			((System.ComponentModel.ISupportInitialize)(this.mainStatusPanel)).BeginInit();
			this.topSandBarDock.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuItem1
			// 
			this.menuItem1.Index = -1;
			this.menuItem1.Text = "";
			// 
			// statusBar
			// 
			this.statusBar.Location = new System.Drawing.Point(0, 531);
			this.statusBar.Name = "statusBar";
			this.statusBar.Size = new System.Drawing.Size(888, 22);
			this.statusBar.TabIndex = 3;
			// 
			// sandBarManager1
			// 
			this.sandBarManager1.OwnerForm = this;
			// 
			// leftSandBarDock
			// 
			this.leftSandBarDock.Dock = System.Windows.Forms.DockStyle.Left;
			this.leftSandBarDock.Guid = new System.Guid("5f0063a2-6d31-4737-bbb0-8a9c9e2c8e14");
			this.leftSandBarDock.Location = new System.Drawing.Point(0, 50);
			this.leftSandBarDock.Manager = this.sandBarManager1;
			this.leftSandBarDock.Name = "leftSandBarDock";
			this.leftSandBarDock.Size = new System.Drawing.Size(0, 503);
			this.leftSandBarDock.TabIndex = 5;
			// 
			// rightSandBarDock
			// 
			this.rightSandBarDock.Dock = System.Windows.Forms.DockStyle.Right;
			this.rightSandBarDock.Guid = new System.Guid("cc416637-af15-40c6-bdb2-b595b79b46f7");
			this.rightSandBarDock.Location = new System.Drawing.Point(888, 50);
			this.rightSandBarDock.Manager = this.sandBarManager1;
			this.rightSandBarDock.Name = "rightSandBarDock";
			this.rightSandBarDock.Size = new System.Drawing.Size(0, 503);
			this.rightSandBarDock.TabIndex = 6;
			// 
			// bottomSandBarDock
			// 
			this.bottomSandBarDock.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.bottomSandBarDock.Guid = new System.Guid("746bc677-7bd1-43fd-9c76-ab8f67c071f6");
			this.bottomSandBarDock.Location = new System.Drawing.Point(0, 553);
			this.bottomSandBarDock.Manager = this.sandBarManager1;
			this.bottomSandBarDock.Name = "bottomSandBarDock";
			this.bottomSandBarDock.Size = new System.Drawing.Size(888, 0);
			this.bottomSandBarDock.TabIndex = 7;
			// 
			// topSandBarDock
			// 
			this.topSandBarDock.Controls.Add(this.menuBar1);
			this.topSandBarDock.Controls.Add(this.toolbar);
			this.topSandBarDock.Dock = System.Windows.Forms.DockStyle.Top;
			this.topSandBarDock.Guid = new System.Guid("9dc621f6-032c-409c-ab45-7f5768d88248");
			this.topSandBarDock.Location = new System.Drawing.Point(0, 0);
			this.topSandBarDock.Manager = this.sandBarManager1;
			this.topSandBarDock.Name = "topSandBarDock";
			this.topSandBarDock.Size = new System.Drawing.Size(888, 50);
			this.topSandBarDock.TabIndex = 8;
			// 
			// menuBar1
			// 
			this.menuBar1.AllowMerge = true;
			this.menuBar1.Guid = new System.Guid("4f520204-743b-4927-9dea-ef3e685de2f0");
			this.menuBar1.Items.AddRange(new TD.SandBar.ToolbarItemBase[] {
																			  this.menuFile,
																			  this.menuBarItem6,
																			  this.menuBarItem7,
																			  this.menuItem3,
																			  this.menuItem4});
			this.menuBar1.Location = new System.Drawing.Point(2, 0);
			this.menuBar1.Name = "menuBar1";
			this.menuBar1.OwnerForm = this;
			this.menuBar1.ShowMdiSystemMenu = false;
			this.menuBar1.Size = new System.Drawing.Size(886, 24);
			this.menuBar1.TabIndex = 0;
			this.menuBar1.Text = "menuBar1";
			this.menuBar1.ButtonClick += new TD.SandBar.ToolBar.ButtonClickEventHandler(this.menuBar1_ButtonClick);
			// 
			// menuFile
			// 
			this.menuFile.Items.AddRange(new TD.SandBar.ToolbarItemBase[] {
																			  this.menuFileNew,
																			  this.menuFileOpen,
																			  this.menuFileMru,
																			  this.menuFileExit});
			this.menuFile.Text = "&File";
			this.menuFile.BeforePopup += new TD.SandBar.MenuItemBase.BeforePopupEventHandler(this.menuFile_BeforePopup);
			// 
			// menuFileNew
			// 
			this.menuFileNew.Image = ((System.Drawing.Image)(resources.GetObject("menuFileNew.Image")));
			this.menuFileNew.Shortcut = System.Windows.Forms.Shortcut.CtrlN;
			this.menuFileNew.Text = "&New";
			this.menuFileNew.Activate += new System.EventHandler(this.menuFileNew_Click);
			// 
			// menuFileOpen
			// 
			this.menuFileOpen.Image = ((System.Drawing.Image)(resources.GetObject("menuFileOpen.Image")));
			this.menuFileOpen.Shortcut = System.Windows.Forms.Shortcut.CtrlO;
			this.menuFileOpen.Text = "&Open...";
			this.menuFileOpen.Activate += new System.EventHandler(this.menuFileOpen_Click);
			// 
			// menuFileMru
			// 
			this.menuFileMru.BeginGroup = true;
			this.menuFileMru.Text = "Recent &Files";
			// 
			// menuFileExit
			// 
			this.menuFileExit.Text = "E&xit";
			this.menuFileExit.Activate += new System.EventHandler(this.FileExit);
			// 
			// menuBarItem6
			// 
			this.menuBarItem6.Text = "&Edit";
			// 
			// menuBarItem7
			// 
			this.menuBarItem7.Text = "&View";
			// 
			// menuItem3
			// 
			this.menuItem3.Text = "&Window";
			// 
			// menuItem4
			// 
			this.menuItem4.Items.AddRange(new TD.SandBar.ToolbarItemBase[] {
																			   this.menuHelpUserGuide,
																			   this.menuHelpAbout});
			this.menuItem4.Text = "&Help";
			// 
			// menuHelpUserGuide
			// 
			this.menuHelpUserGuide.Text = "Table of &Contents";
			this.menuHelpUserGuide.Activate += new System.EventHandler(this.menuHelpUserGuide_Click);
			// 
			// menuHelpAbout
			// 
			this.menuHelpAbout.BeginGroup = true;
			this.menuHelpAbout.Text = "&About...";
			this.menuHelpAbout.Activate += new System.EventHandler(this.menuHelpAbout_Click);
			// 
			// toolbar
			// 
			this.toolbar.DockLine = 1;
			this.toolbar.Guid = new System.Guid("b4123575-47eb-4cc2-830d-dbc0514edfd5");
			this.toolbar.Items.AddRange(new TD.SandBar.ToolbarItemBase[] {
																			 this.buttonFileNew,
																			 this.buttonFileOpen});
			this.toolbar.Location = new System.Drawing.Point(2, 24);
			this.toolbar.Name = "toolbar";
			this.toolbar.Size = new System.Drawing.Size(70, 26);
			this.toolbar.TabIndex = 1;
			this.toolbar.Text = "toolBar1";
			// 
			// buttonFileNew
			// 
			this.buttonFileNew.BuddyMenu = this.menuFileNew;
			this.buttonFileNew.Image = ((System.Drawing.Image)(resources.GetObject("buttonFileNew.Image")));
			this.buttonFileNew.ToolTipText = "New";
			this.buttonFileNew.Activate += new System.EventHandler(this.buttonFileNew_Activate);
			// 
			// buttonFileOpen
			// 
			this.buttonFileOpen.BuddyMenu = this.menuFileOpen;
			this.buttonFileOpen.Image = ((System.Drawing.Image)(resources.GetObject("buttonFileOpen.Image")));
			this.buttonFileOpen.ToolTipText = "Open...";
			// 
			// menuBarItem1
			// 
			this.menuBarItem1.Text = "&File";
			// 
			// menuBarItem2
			// 
			this.menuBarItem2.Text = "&Edit";
			// 
			// menuBarItem3
			// 
			this.menuBarItem3.Text = "&View";
			// 
			// menuBarItem4
			// 
			this.menuBarItem4.MdiWindowList = true;
			this.menuBarItem4.Text = "&Window";
			// 
			// menuBarItem5
			// 
			this.menuBarItem5.Text = "&Help";
			// 
			// XEditNetMainForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(888, 553);
			this.Controls.Add(this.statusBar);
			this.Controls.Add(this.leftSandBarDock);
			this.Controls.Add(this.rightSandBarDock);
			this.Controls.Add(this.bottomSandBarDock);
			this.Controls.Add(this.topSandBarDock);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.IsMdiContainer = true;
			this.Name = "XEditNetMainForm";
			this.Text = "XEditNet Author";
			this.MdiChildActivate += new System.EventHandler(this.XEditNetMainForm_MdiChildActivate);
			((System.ComponentModel.ISupportInitialize)(this.mainStatusPanel)).EndInit();
			this.topSandBarDock.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion


		private void menuFileOpen_Click(object sender, EventArgs e)
		{
			OpenFileDialog dlg=new OpenFileDialog();
			dlg.AddExtension=true;
			dlg.CheckFileExists=true;
			dlg.CheckPathExists=true;
			dlg.DefaultExt="xml";
			dlg.Filter=defaultFilter;
			dlg.Multiselect=true;
			DialogResult ret=dlg.ShowDialog(this);
			if ( ret == DialogResult.Cancel )
				return;

			foreach ( string s in dlg.FileNames )
				OpenFile(s);
		}

		private bool ConfirmSave(XEditNetCtrl editor, FileInfo fi)
		{
			if ( editor == null || !editor.IsModified )
				return true;

			string msg=string.Format("Do you want to save the changes to {0}", fi.Name);
			DialogResult dr=MessageBox.Show(this, msg, "XEditNet Author", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
			if ( dr == DialogResult.Cancel )
				return false;

			if ( dr == DialogResult.Yes )
			{
				if ( fi == null )
					fi=GetSaveAsFileInfo();

				if ( fi == null )
					return false;

				SaveFile(editor, fi);
			}		
			return true;
		}

		private void SaveFile(XEditNetCtrl editor, FileInfo fi)
		{
			// TODO: M: show wait cursor
			XmlTextWriter xtw=new XmlTextWriter(fi.FullName, Encoding.UTF8);
			try
			{
				editor.Document.Save(xtw);
				editor.IsModified=false;
			}
			finally 
			{
				xtw.Close();
			}
		}

		private FileInfo GetSaveAsFileInfo()
		{
			SaveFileDialog fd=new SaveFileDialog();
			fd.AddExtension=true;
			fd.DefaultExt="xml";
			fd.Title="Save XML";
			fd.Filter=defaultFilter;
			DialogResult dr=fd.ShowDialog(this);
			if ( dr == DialogResult.Cancel )
				return null;
						
			return new FileInfo(fd.FileName);
		}

		public void OpenMRUFile(string fileName)
		{
			OpenFile(fileName);
		}

		public void SaveCurrentFile()
		{
			if ( CurrentChild == null )
				return;

			if ( CurrentFileInfo == null )
				FileSaveAs();
			else
				SaveFile(CurrentChild.Editor, CurrentFileInfo);
		}

		private void menuFileClose_Click(object sender, EventArgs e)
		{
			((Form) CurrentChild).Close();
		}

		private IXEditNetEditorRegion CurrentChild
		{
			get 
			{
				return (IXEditNetEditorRegion) this.ActiveMdiChild;
			}
		}

		private FileInfo CurrentFileInfo
		{
			get 
			{
				if ( CurrentChild == null )
					return null;

				return (FileInfo) ((Form) CurrentChild).Tag;
			}
			set
			{
				if ( CurrentChild == null )
					throw new ArgumentException("No page to set info for");

				((Form) CurrentChild).Tag=value;
				((Form) CurrentChild).Text=value.Name;
			}
		}

		private XEditNetCtrl CurrentEditor
		{
			get 
			{
				if ( CurrentChild == null )
					return null;

				return CurrentChild.Editor;
			}
		}

		private void menuFileSaveAs_Click(object sender, EventArgs e)
		{
			XEditNetCtrl editor=CurrentEditor;
			if ( editor == null )
				return;

			FileSaveAs();
		}

		private void FileSaveAs()
		{
			FileInfo fi=GetSaveAsFileInfo();
			if ( fi != null )
			{
				SaveFile(CurrentEditor, fi);
				mru.Add(fi.FullName);
				CurrentFileInfo=fi;
			}
		}

		private void menuFileNew_Click(object sender, EventArgs e)
		{
			NewFileDialog dlg=new NewFileDialog();
			DialogResult res=dlg.ShowDialog(this);
			if ( res == DialogResult.Cancel )
				return;

			XmlDocument doc=dlg.CreateNewDocument();
			OpenNew(doc);
		}

		public void OpenNew(XmlDocument doc)
		{
			Form f=GetMdiForm(doc, false);
			f.Text="Untitled";
			f.MdiParent = this;
			f.Show();
		}

		private void XEditNetMainForm_MdiChildActivate(object sender, EventArgs e)
		{
			if ( activeChild != null )
				activeChild.Editor.SelectionChanged-=new SelectionChangedEventHandler(SelectionChanged);

			IXEditNetEditorRegion child=this.ActiveMdiChild as IXEditNetEditorRegion;
			if ( child == null )
				return;

			activeChild=child;
			activeChild.Editor.SelectionChanged+=new SelectionChangedEventHandler(SelectionChanged);

			UpdateSelectionStatus();
		}

		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
			pevent.Graphics.FillRectangle(Brushes.Red, ClientRectangle);
		}

		public void SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateSelectionStatus();
		}

		private void UpdateSelectionStatus()
		{
			IXEditNetEditorRegion child=(IXEditNetEditorRegion) this.ActiveMdiChild;
			Selection sel=child.Editor.Selection;
			this.statusBar.Text=sel == null ? "" : sel.ToString();
		}

		private void FileExit(object sender, EventArgs e)
		{
			Close();
		}

		private void menuHelpUserGuide_Click(object sender, EventArgs e)
		{
			ShowHelp("userguide.chm");
		}

		private void ShowHelp(string helpfile)
		{
			Uri uri=new Uri(GetType().Assembly.CodeBase);
			Uri help=new Uri(uri, helpfile);
//			MessageBox.Show(help.LocalPath);

			Help.ShowHelp(this, help.LocalPath, HelpNavigator.TableOfContents);
		}

		private void menuHelpAbout_Click(object sender, EventArgs e)
		{
			AboutDialog a=new AboutDialog();
			a.ShowDialog(this);
		}

		private void menuFile_BeforePopup(object sender, TD.SandBar.MenuPopupEventArgs e)
		{
		
		}

		private void buttonFileNew_Activate(object sender, System.EventArgs e)
		{
		
		}

		private void menuBar1_ButtonClick(object sender, TD.SandBar.ToolBarItemEventArgs e)
		{
		
		}
	}
}
