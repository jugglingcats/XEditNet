using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml;
using TD.SandBar;
using TD.SandDock;
using XEditNet;
using XEditNet.Keyboard;
using XEditNet.Location;
using XEditNet.Profile;
using XEditNet.Widgets;
using ToolBar = TD.SandBar.ToolBar;

namespace XEditNetAuthor
{
	public class XEditNetChildForm : Form, IXEditNetEditorRegion
	{
		private System.ComponentModel.IContainer components;
		private MenuBarItem menuBarItem1;
		private MenuBarItem menuBarItem2;
		private MenuBarItem menuBarItem3;
		private MenuBarItem menuBarItem4;
		private MenuBarItem menuBarItem5;
		private SandDockManager sandDockManager;
		private DockContainer leftSandDock;
		private DockContainer rightSandDock;
		private DockContainer bottomSandDock;
		private DockContainer topSandDock;
		private DockableWindow dockElementInsert;
		private DockableWindow dockElementChange;
		private ElementInsertPanel elementInsertPanel;
		private ElementChangePanel elementChangePanel;
		private DockableWindow dockAttributes;
		private AttributeChangePanel attributeChangePanel;
		private MenuBar menuBar1;
		private MenuBarItem menuBarItem6;
		private MenuBarItem menuBarItem8;
		private ToolBar toolBar1;
		private MenuButtonItem menuFileSave;
		private ToolBar toolBar2;
		private ButtonItem buttonItem1;
		private ContainerBar quickFixBar;
		private ContainerBarClientPanel containerBarClientPanel1;
		private QuickFixPanel quickFixPanel;
		private MenuButtonItem menuButtonItem1;
		private MenuBarItem menuBarItem7;
		private TD.SandBar.MenuButtonItem menuButtonItem2;
		private TD.SandBar.MenuButtonItem menuButtonItem3;
		private MenuBar subMenuBar=new MenuBar();
		private System.Windows.Forms.ImageList commandImageList;
		private System.Windows.Forms.ImageList quickFixImageList;
		private TD.SandBar.ButtonItem quickFixPreceeding;
		private TD.SandBar.ButtonItem quickFixFollowing;

		private XmlDocument document;
		public UserControl editorRegion;

		public XEditNetChildForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		}

		public XEditNetChildForm(UserControl editorRegion) : this()
		{
			editorRegion.AutoScroll = true;
			editorRegion.BackColor = System.Drawing.Color.White;
			editorRegion.Dock = System.Windows.Forms.DockStyle.Fill;
			editorRegion.Location = new System.Drawing.Point(232, 44);
			editorRegion.Name = "editor";
			editorRegion.Size = new System.Drawing.Size(368, 458);
			editorRegion.TabIndex = 6;

			this.editorRegion=editorRegion;

			Editor.ChangeAttributesActivated += new XEditNet.InterfaceActivationEventHandler(ChangeAttributesActivated);
			Editor.ChangeElementActivated += new XEditNet.InterfaceActivationEventHandler(ChangeElementActivated);
			Editor.InsertElementActivated += new XEditNet.InterfaceActivationEventHandler(InsertElementActivated);

			this.elementChangePanel.Editor=Editor;
			this.elementInsertPanel.Editor=Editor;
			this.attributeChangePanel.Editor=Editor;
			this.quickFixPanel.Editor=Editor;

			if ( editorRegion != null )
			{
				int index=Controls.IndexOf(menuBar1);
                Controls.Add(editorRegion);
				Controls.SetChildIndex(editorRegion, index);
			}

			UpdateMenu();

			ControlUtil.AddImage(quickFixImageList, typeof(XEditNetChildForm), "images.btnBack.bmp");
			ControlUtil.AddImage(quickFixImageList, typeof(XEditNetChildForm), "images.btnNext.bmp");
		}

		private void UpdateMenu()
		{
			Hashtable images=new Hashtable();
			int imageIndex=0;

			foreach ( CommandMapping cmd in Editor.Commands )
			{
				if ( cmd.MenuPath == null )
					continue;

				string fullPath=cmd.MenuPath;

				string[] parts=fullPath.Split('/');
				string current="";
				int n=parts.Length;
				ToolbarItemBaseCollection list=menuBar1.Items;
				MenuItemBase menuNode=null;
				foreach ( string part in parts )
				{
					n--;

					if ( part.Length == 0 )
						continue;

					current+="/"+part;

					if ( n == 0 )
					{
						MenuButtonItem leaf=new MenuButtonItem(part);

						if ( cmd.ImagePath != null )
						{
							object o=images[cmd.ImagePath];
							if ( o != null )
							{
								IntPtr index=(IntPtr) o;
								leaf.Image=commandImageList.Images[index.ToInt32()];
							} 
							else
							{
								if ( ControlUtil.AddImage(commandImageList, typeof(XEditNetCtrl), cmd.ImagePath) )
								{
									leaf.Image=commandImageList.Images[imageIndex];
									images[cmd.ImagePath]=new IntPtr(imageIndex++);
								} 
							}

						}

						leaf.MergeIndex=0;
						leaf.MergeAction=ItemMergeAction.Add;
						if ( cmd.MenuIndex != -1 )
						{
							leaf.MergeIndex=cmd.MenuIndex;
							leaf.MergeAction=ItemMergeAction.Insert;
						}
						leaf.BeginGroup=cmd.MenuBreak;
						leaf.Tag=cmd;
						leaf.Activate+=new EventHandler(DispatchCommand);

						Shortcut[] keys=cmd.Keys;
						if ( keys.Length > 0 )
						{
							leaf.Shortcut=keys[0];
							if ( keys.Length > 1 )
								leaf.Shortcut2=keys[1];
						}
						// TODO: M: check for invalid input (button at top level)
						menuNode.Items.Add(leaf);
					} 
					else
					{
						menuNode=FindItem(list, part);
						if ( menuNode == null )
						{
							menuNode=new MenuBarItem(part);
							menuNode.MergeIndex=-1;
							menuNode.MergeAction=ItemMergeAction.MergeChildren;
							menuNode.BeginGroup=cmd.MenuBreak;

							list.Add(menuNode);							
						}
					}

					list=menuNode.Items;
				}
			}
		}

		private void DispatchCommand(object sender, EventArgs e)
		{
			ButtonItemBase item=(ButtonItemBase) sender;
			CommandMapping cmd=(CommandMapping) item.Tag;
			Editor.DispatchCommand(cmd);
		}

		private MenuItemBase FindItem(ToolbarItemBaseCollection list, string part)
		{
			foreach ( MenuItemBase n in list )
			{
				if ( n.Text.Equals(part) )
					return n;
			}
			return null;
		}

		public XEditNetCtrl Editor
		{
			get 
			{
				return ((IXEditNetEditorRegion) editorRegion).Editor;
			}
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

		public void Attach(XmlDocument doc, bool valid)
		{
			Editor.Attach(doc, valid);
			this.document=doc;
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.menuBarItem1 = new TD.SandBar.MenuBarItem();
			this.menuBarItem2 = new TD.SandBar.MenuBarItem();
			this.menuBarItem3 = new TD.SandBar.MenuBarItem();
			this.menuBarItem4 = new TD.SandBar.MenuBarItem();
			this.menuBarItem5 = new TD.SandBar.MenuBarItem();
			this.sandDockManager = new TD.SandDock.SandDockManager();
			this.leftSandDock = new TD.SandDock.DockContainer();
			this.rightSandDock = new TD.SandDock.DockContainer();
			this.dockElementInsert = new TD.SandDock.DockableWindow();
			this.elementInsertPanel = new XEditNet.Widgets.ElementInsertPanel();
			this.dockElementChange = new TD.SandDock.DockableWindow();
			this.elementChangePanel = new XEditNet.Widgets.ElementChangePanel();
			this.dockAttributes = new TD.SandDock.DockableWindow();
			this.attributeChangePanel = new XEditNet.Widgets.AttributeChangePanel();
			this.bottomSandDock = new TD.SandDock.DockContainer();
			this.topSandDock = new TD.SandDock.DockContainer();
			this.menuBar1 = new TD.SandBar.MenuBar();
			this.menuBarItem6 = new TD.SandBar.MenuBarItem();
			this.menuFileSave = new TD.SandBar.MenuButtonItem();
			this.menuButtonItem1 = new TD.SandBar.MenuButtonItem();
			this.menuBarItem7 = new TD.SandBar.MenuBarItem();
			this.menuButtonItem2 = new TD.SandBar.MenuButtonItem();
			this.menuBarItem8 = new TD.SandBar.MenuBarItem();
			this.menuButtonItem3 = new TD.SandBar.MenuButtonItem();
			this.toolBar1 = new TD.SandBar.ToolBar();
			this.toolBar2 = new TD.SandBar.ToolBar();
			this.buttonItem1 = new TD.SandBar.ButtonItem();
			this.quickFixBar = new TD.SandBar.ContainerBar();
			this.containerBarClientPanel1 = new TD.SandBar.ContainerBarClientPanel();
			this.quickFixPanel = new XEditNet.Widgets.QuickFixPanel();
			this.quickFixImageList = new System.Windows.Forms.ImageList(this.components);
			this.quickFixPreceeding = new TD.SandBar.ButtonItem();
			this.quickFixFollowing = new TD.SandBar.ButtonItem();
			this.commandImageList = new System.Windows.Forms.ImageList(this.components);
			this.rightSandDock.SuspendLayout();
			this.dockElementInsert.SuspendLayout();
			this.dockElementChange.SuspendLayout();
			this.dockAttributes.SuspendLayout();
			this.quickFixBar.SuspendLayout();
			this.containerBarClientPanel1.SuspendLayout();
			this.SuspendLayout();
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
			// sandDockManager
			// 
			this.sandDockManager.OwnerForm = this;
			this.sandDockManager.Renderer = new TD.SandDock.Rendering.Office2003Renderer();
			// 
			// leftSandDock
			// 
			this.leftSandDock.Dock = System.Windows.Forms.DockStyle.Left;
			this.leftSandDock.LayoutSystem = new TD.SandDock.SplitLayoutSystem(250, 400);
			this.leftSandDock.Location = new System.Drawing.Point(0, 22);
			this.leftSandDock.Manager = this.sandDockManager;
			this.leftSandDock.Name = "leftSandDock";
			this.leftSandDock.Size = new System.Drawing.Size(0, 480);
			this.leftSandDock.TabIndex = 0;
			// 
			// rightSandDock
			// 
			this.rightSandDock.Controls.Add(this.dockElementInsert);
			this.rightSandDock.Controls.Add(this.dockElementChange);
			this.rightSandDock.Controls.Add(this.dockAttributes);
			this.rightSandDock.Dock = System.Windows.Forms.DockStyle.Right;
			this.rightSandDock.LayoutSystem = new TD.SandDock.SplitLayoutSystem(250, 400, System.Windows.Forms.Orientation.Horizontal, new TD.SandDock.LayoutSystemBase[] {
																																											  new TD.SandDock.ControlLayoutSystem(212, 238, new TD.SandDock.DockableWindow[] {
																																																															  this.dockElementInsert,
																																																															  this.dockElementChange}, this.dockElementInsert),
																																											  new TD.SandDock.ControlLayoutSystem(212, 238, new TD.SandDock.DockableWindow[] {
																																																															  this.dockAttributes}, this.dockAttributes)});
			this.rightSandDock.Location = new System.Drawing.Point(600, 22);
			this.rightSandDock.Manager = this.sandDockManager;
			this.rightSandDock.Name = "rightSandDock";
			this.rightSandDock.Size = new System.Drawing.Size(216, 480);
			this.rightSandDock.TabIndex = 1;
			// 
			// dockElementInsert
			// 
			this.dockElementInsert.Controls.Add(this.elementInsertPanel);
			this.dockElementInsert.Guid = new System.Guid("63fe64f8-5444-45cb-b17d-17f8baf0c91d");
			this.dockElementInsert.Location = new System.Drawing.Point(4, 25);
			this.dockElementInsert.Name = "dockElementInsert";
			this.dockElementInsert.Size = new System.Drawing.Size(212, 190);
			this.dockElementInsert.TabIndex = 1;
			this.dockElementInsert.Text = "Insert";
			// 
			// elementInsertPanel
			// 
			this.elementInsertPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.elementInsertPanel.Location = new System.Drawing.Point(0, 0);
			this.elementInsertPanel.Name = "elementInsertPanel";
			this.elementInsertPanel.Size = new System.Drawing.Size(212, 190);
			this.elementInsertPanel.TabIndex = 0;
			// 
			// dockElementChange
			// 
			this.dockElementChange.Controls.Add(this.elementChangePanel);
			this.dockElementChange.Guid = new System.Guid("cb2f6fbf-41de-4588-a08b-f4f00d505fa7");
			this.dockElementChange.Location = new System.Drawing.Point(4, 25);
			this.dockElementChange.Name = "dockElementChange";
			this.dockElementChange.Size = new System.Drawing.Size(212, 190);
			this.dockElementChange.TabIndex = 1;
			this.dockElementChange.Text = "Change";
			// 
			// elementChangePanel
			// 
			this.elementChangePanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.elementChangePanel.Location = new System.Drawing.Point(0, 0);
			this.elementChangePanel.Name = "elementChangePanel";
			this.elementChangePanel.Size = new System.Drawing.Size(212, 190);
			this.elementChangePanel.TabIndex = 0;
			// 
			// dockAttributes
			// 
			this.dockAttributes.Controls.Add(this.attributeChangePanel);
			this.dockAttributes.Guid = new System.Guid("7a4f064f-d569-4ab2-8133-ca0b6b8c4151");
			this.dockAttributes.Location = new System.Drawing.Point(4, 267);
			this.dockAttributes.Name = "dockAttributes";
			this.dockAttributes.Size = new System.Drawing.Size(212, 190);
			this.dockAttributes.TabIndex = 2;
			this.dockAttributes.Text = "Attributes";
			// 
			// attributeChangePanel
			// 
			this.attributeChangePanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.attributeChangePanel.Location = new System.Drawing.Point(0, 0);
			this.attributeChangePanel.Name = "attributeChangePanel";
			this.attributeChangePanel.Size = new System.Drawing.Size(212, 190);
			this.attributeChangePanel.TabIndex = 0;
			// 
			// bottomSandDock
			// 
			this.bottomSandDock.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.bottomSandDock.LayoutSystem = new TD.SandDock.SplitLayoutSystem(250, 400);
			this.bottomSandDock.Location = new System.Drawing.Point(0, 502);
			this.bottomSandDock.Manager = this.sandDockManager;
			this.bottomSandDock.Name = "bottomSandDock";
			this.bottomSandDock.Size = new System.Drawing.Size(816, 0);
			this.bottomSandDock.TabIndex = 2;
			// 
			// topSandDock
			// 
			this.topSandDock.Dock = System.Windows.Forms.DockStyle.Top;
			this.topSandDock.LayoutSystem = new TD.SandDock.SplitLayoutSystem(250, 400);
			this.topSandDock.Location = new System.Drawing.Point(0, 22);
			this.topSandDock.Manager = this.sandDockManager;
			this.topSandDock.Name = "topSandDock";
			this.topSandDock.Size = new System.Drawing.Size(816, 0);
			this.topSandDock.TabIndex = 3;
			// 
			// menuBar1
			// 
			this.menuBar1.AllowMerge = true;
			this.menuBar1.Guid = new System.Guid("dc0a091b-fb9a-4a33-8bf0-a9a24af4064c");
			this.menuBar1.Items.AddRange(new TD.SandBar.ToolbarItemBase[] {
																			  this.menuBarItem6,
																			  this.menuBarItem7,
																			  this.menuBarItem8});
			this.menuBar1.Location = new System.Drawing.Point(232, 22);
			this.menuBar1.Name = "menuBar1";
			this.menuBar1.OwnerForm = this;
			this.menuBar1.Size = new System.Drawing.Size(368, 22);
			this.menuBar1.TabIndex = 0;
			this.menuBar1.Text = "menuBar1";
			this.menuBar1.Visible = false;
			// 
			// menuBarItem6
			// 
			this.menuBarItem6.Items.AddRange(new TD.SandBar.ToolbarItemBase[] {
																				  this.menuFileSave,
																				  this.menuButtonItem1});
			this.menuBarItem6.Text = "&File";
			// 
			// menuFileSave
			// 
			this.menuFileSave.BeginGroup = true;
			this.menuFileSave.MergeAction = TD.SandBar.ItemMergeAction.Insert;
			this.menuFileSave.MergeIndex = 2;
			this.menuFileSave.Text = "Save";
			this.menuFileSave.Activate += new System.EventHandler(this.SaveFile);
			// 
			// menuButtonItem1
			// 
			this.menuButtonItem1.MergeAction = TD.SandBar.ItemMergeAction.Insert;
			this.menuButtonItem1.MergeIndex = 3;
			this.menuButtonItem1.Text = "&Close";
			this.menuButtonItem1.Activate += new System.EventHandler(this.CloseFile);
			// 
			// menuBarItem7
			// 
			this.menuBarItem7.Items.AddRange(new TD.SandBar.ToolbarItemBase[] {
																				  this.menuButtonItem2});
			this.menuBarItem7.Text = "&Edit";
			// 
			// menuButtonItem2
			// 
			this.menuButtonItem2.MergeAction = TD.SandBar.ItemMergeAction.Add;
			this.menuButtonItem2.MergeIndex = 0;
			this.menuButtonItem2.Text = "&Test";
			// 
			// menuBarItem8
			// 
			this.menuBarItem8.Items.AddRange(new TD.SandBar.ToolbarItemBase[] {
																				  this.menuButtonItem3});
			this.menuBarItem8.Text = "&View";
			// 
			// menuButtonItem3
			// 
			this.menuButtonItem3.MergeAction = TD.SandBar.ItemMergeAction.Add;
			this.menuButtonItem3.MergeIndex = 0;
			this.menuButtonItem3.Text = "&ViewTest";
			// 
			// toolBar1
			// 
			this.toolBar1.DockLine = 1;
			this.toolBar1.Guid = new System.Guid("88196026-7bd7-4954-85b7-34999dcd37cd");
			this.toolBar1.Location = new System.Drawing.Point(2, 24);
			this.toolBar1.Name = "toolBar1";
			this.toolBar1.Size = new System.Drawing.Size(24, 18);
			this.toolBar1.TabIndex = 1;
			this.toolBar1.Text = "toolBar1";
			// 
			// toolBar2
			// 
			this.toolBar2.Guid = new System.Guid("458f1dcc-720e-4fb6-b794-41d5a9f186e2");
			this.toolBar2.Items.AddRange(new TD.SandBar.ToolbarItemBase[] {
																			  this.buttonItem1});
			this.toolBar2.Location = new System.Drawing.Point(0, 0);
			this.toolBar2.Name = "toolBar2";
			this.toolBar2.Size = new System.Drawing.Size(816, 22);
			this.toolBar2.TabIndex = 4;
			this.toolBar2.Text = "";
			this.toolBar2.Visible = false;
			// 
			// buttonItem1
			// 
			this.buttonItem1.MergeAction = TD.SandBar.ItemMergeAction.Insert;
			this.buttonItem1.MergeIndex = 2;
			this.buttonItem1.Text = "xxxx";
			// 
			// quickFixBar
			// 
			this.quickFixBar.AddRemoveButtonsVisible = false;
			this.quickFixBar.AllowHorizontalDock = false;
			this.quickFixBar.Controls.Add(this.containerBarClientPanel1);
			this.quickFixBar.Dock = System.Windows.Forms.DockStyle.Left;
			this.quickFixBar.Flow = TD.SandBar.ToolBarLayout.Horizontal;
			this.quickFixBar.Guid = new System.Guid("0e1e9ede-d2fe-4307-a683-d2e3ea4f4e5f");
			this.quickFixBar.ImageList = this.quickFixImageList;
			this.quickFixBar.Items.AddRange(new TD.SandBar.ToolbarItemBase[] {
																				 this.quickFixPreceeding,
																				 this.quickFixFollowing});
			this.quickFixBar.Location = new System.Drawing.Point(0, 22);
			this.quickFixBar.Name = "quickFixBar";
			this.quickFixBar.Size = new System.Drawing.Size(232, 480);
			this.quickFixBar.TabIndex = 5;
			this.quickFixBar.Text = "Quick Fix";
			// 
			// containerBarClientPanel1
			// 
			this.containerBarClientPanel1.Controls.Add(this.quickFixPanel);
			this.containerBarClientPanel1.Location = new System.Drawing.Point(2, 45);
			this.containerBarClientPanel1.Name = "containerBarClientPanel1";
			this.containerBarClientPanel1.Size = new System.Drawing.Size(228, 433);
			this.containerBarClientPanel1.TabIndex = 0;
			// 
			// quickFixPanel
			// 
			this.quickFixPanel.BackColor = System.Drawing.Color.Transparent;
			this.quickFixPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.quickFixPanel.Location = new System.Drawing.Point(0, 0);
			this.quickFixPanel.Name = "quickFixPanel";
			this.quickFixPanel.Size = new System.Drawing.Size(228, 433);
			this.quickFixPanel.TabIndex = 0;
			this.quickFixPanel.FinishUpdate += new System.EventHandler(this.QuickFixUpdated);
			// 
			// quickFixImageList
			// 
			this.quickFixImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
			this.quickFixImageList.ImageSize = new System.Drawing.Size(16, 16);
			this.quickFixImageList.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// quickFixPreceeding
			// 
			this.quickFixPreceeding.ImageIndex = 0;
			this.quickFixPreceeding.Activate += new System.EventHandler(this.GotoPrecedingError);
			// 
			// quickFixFollowing
			// 
			this.quickFixFollowing.ImageIndex = 1;
			this.quickFixFollowing.Activate += new System.EventHandler(this.GotoFollowingError);
			// 
			// commandImageList
			// 
			this.commandImageList.ImageSize = new System.Drawing.Size(16, 16);
			this.commandImageList.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// XEditNetChildForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(816, 502);
			this.Controls.Add(this.menuBar1);
			this.Controls.Add(this.quickFixBar);
			this.Controls.Add(this.leftSandDock);
			this.Controls.Add(this.rightSandDock);
			this.Controls.Add(this.bottomSandDock);
			this.Controls.Add(this.topSandDock);
			this.Controls.Add(this.toolBar2);
			this.Name = "XEditNetChildForm";
			this.Text = "XEditNetChildForm";
			this.rightSandDock.ResumeLayout(false);
			this.dockElementInsert.ResumeLayout(false);
			this.dockElementChange.ResumeLayout(false);
			this.dockAttributes.ResumeLayout(false);
			this.quickFixBar.ResumeLayout(false);
			this.containerBarClientPanel1.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		private void InsertElementActivated(object sender, InterfaceActivationEventArgs e)
		{
            ActivateDockElement(dockElementInsert, e);
		}

        private void ActivateDockElement(DockableWindow dockElement, InterfaceActivationEventArgs e)
        {
            if (dockElement.DockSituation != DockSituation.None)
            {
                if (!dockElement.IsOpen)
                    dockElement.Open();

                dockElement.Activate();
                e.Handled = true;
            }
        }

		private void ChangeElementActivated(object sender, InterfaceActivationEventArgs e)
		{
            ActivateDockElement(dockElementChange, e);
		}

		private void ChangeAttributesActivated(object sender, InterfaceActivationEventArgs e)
		{
            ActivateDockElement(dockAttributes, e);
		}

		private void QuickFixUpdated(object sender, System.EventArgs e)
		{
			quickFixPreceeding.Enabled=quickFixPanel.HasPreceding;
			quickFixFollowing.Enabled=quickFixPanel.HasFollowing;
		}

		private void GotoFollowingError(object sender, System.EventArgs e)
		{
			XmlNode n=quickFixPanel.FollowingError;
			if ( n != null )
				Editor.Selection=Editor.SelectionManager.CreateSelection(n);
		}

		private void GotoPrecedingError(object sender, System.EventArgs e)
		{
			XmlNode n=quickFixPanel.PrecedingError;
			if ( n != null )
				Editor.Selection=Editor.SelectionManager.CreateSelection(n);
		}

		private void CloseFile(object sender, System.EventArgs e)
		{
			Close();
		}

		private void SaveFile(object sender, System.EventArgs e)
		{
			((XEditNetMainForm) ParentForm).SaveCurrentFile();
		}

		public ToolBar SubMenu
		{
			get { return subMenuBar; }
		}
	}
}
