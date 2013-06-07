using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using XEditNet.Profile;
using System.Xml;
using WeifenLuo.WinFormsUI.Docking;
using XEditNet.Widgets;
using XEditNet;
using System.IO;
using System.Collections;

namespace XEditNetAuthor
{

    public partial class XEditNetMainForm2 : Form
    {
        private int childFormNumber = 0;

        private IXEditNetEditorRegion currentRegion=null;
        private static readonly string DEFAULT_FILE_FILTER=
			"XML Files (*.xml)|*.xml|XHTML Files (*.xhtml; *.htm; *.html)|*.xhtml;*.htm;*.html|All Files (*.*)|*.*";

        private Dictionary<Type, WidgetPanel> panels = new Dictionary<Type, WidgetPanel>();

        //private DockContent elementChangePanel = new DockContent();
        //private DockContent elementInsertPanel = new DockContent();
        //private DockContent quickFixPanel = new DockContent();


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.Run(new XEditNetMainForm2());
        }

        public XEditNetMainForm2()
        {
            InitializeComponent();

            AddPanel(new ElementChangePanel(), DockState.DockRight, "Change", DockAreas.DockRight | DockAreas.DockLeft | DockAreas.Float);
            AddPanel(new ElementInsertPanel(), DockState.DockRight, "Insert", DockAreas.DockRight | DockAreas.DockLeft | DockAreas.Float);
            AddPanel(new AttributeChangePanel(), DockState.DockRight, "Attributes", DockAreas.DockRight | DockAreas.DockLeft | DockAreas.Float);
            AddPanel(new QuickFixPanel(), DockState.DockLeftAutoHide, "Quick Fix", DockAreas.DockRight | DockAreas.DockLeft | DockAreas.Float);

            //elementChangePanel.Controls.Add(new ElementChangePanel());
            //elementChangePanel.Controls[0].Dock = DockStyle.Fill;
            //elementChangePanel.ShowHint = DockState.DockRight;
            //elementChangePanel.ShowIcon = false;
            //elementChangePanel.Name = "elementChangePanel";
            //elementChangePanel.Text = "Change";
            //elementChangePanel.DockAreas = DockAreas.DockRight | DockAreas.DockLeft | DockAreas.Float;
            //elementChangePanel.Show(dockPanel1);

            //elementInsertPanel.Controls.Add(new ElementInsertPanel());
            //elementInsertPanel.Controls[0].Dock = DockStyle.Fill;
            //elementInsertPanel.ShowHint = DockState.DockRight;
            //elementInsertPanel.ShowIcon = false;
            //elementInsertPanel.Name = "elementInsertPanel";
            //elementInsertPanel.Text = "Insert";
            //elementInsertPanel.DockAreas = DockAreas.DockRight | DockAreas.DockLeft | DockAreas.Float;
            //elementInsertPanel.Show(dockPanel1);

            //quickFixPanel.Controls.Add(new QuickFixPanel());
            //quickFixPanel.Controls[0].Dock = DockStyle.Fill;
            //quickFixPanel.ShowHint = DockState.DockLeft;
            //quickFixPanel.ShowIcon = false;
            //quickFixPanel.Name = "elementInsertPanel";
            //quickFixPanel.Text = "Insert";
            //quickFixPanel.DockAreas = DockAreas.DockRight | DockAreas.DockLeft | DockAreas.Float;
            //quickFixPanel.Show(dockPanel1);
        
        }

        private void AddPanel(PanelBase nestedPanel, DockState dockState, string title, DockAreas dockAreas)
        {
            WidgetPanel panel = new WidgetPanel(nestedPanel);
            panel.ShowHint = dockState;
            panel.ShowIcon = false;
            panel.Name = nestedPanel.GetType().FullName;
            panel.Text = title;
            panel.DockAreas = dockAreas;
            panel.Show(dockPanel1);

            panels.Add(nestedPanel.GetType(), panel);
        }

        //private ElementInsertPanel ElementInsert
        //{
        //    get { return elementInsertPanel.Controls[0] as ElementInsertPanel; }
        //}

        //private ElementChangePanel ElementChange
        //{
        //    get { return elementChangePanel.Controls[0] as ElementChangePanel; }
        //}

        //private QuickFixPanel QuickFix
        //{
        //    get { return quickFixPanel.Controls[0] as QuickFixPanel; }
        //}

        private void ShowNewForm(object sender, EventArgs e)
        {
            NewFileDialog dlg = new NewFileDialog();
            DialogResult res = dlg.ShowDialog(this);
            if (res == DialogResult.Cancel)
                return;

            XmlDocument doc = dlg.CreateNewDocument();
            OpenNew(doc);
        }

        public void OpenNew(XmlDocument doc)
        {
            DockContent childForm = GetMdiForm(doc, false);
            childForm.DockAreas = DockAreas.Document | DockAreas.Float;
            childForm.ShowHint = DockState.Document;
            childForm.Activated +=new EventHandler(childForm_Activated);
            if (dockPanel1.DocumentStyle == DocumentStyle.SystemMdi)
            {
                childForm.MdiParent = this;
                childForm.Show(dockPanel1);
            }
            else
            {
                childForm.Show(this.dockPanel1);
            }

            //childForm.MdiParent = this;
            //childForm.Text = "Window " + childFormNumber++;
            //childForm.Show();
        }

        public void childForm_Activated(object sender, EventArgs e)
        {
            XEditNetChildForm2 form = sender as XEditNetChildForm2;
            if (sender != null)
            {
                IXEditNetEditorRegion r = form.EditorRegion;
                if (currentRegion != null)
                {
                    currentRegion.Editor.InsertElementActivated -= new XEditNet.InterfaceActivationEventHandler(InsertElementActivated);
                    currentRegion.Editor.ChangeElementActivated -= new XEditNet.InterfaceActivationEventHandler(ChangeElementActivated);
                }

                r.Editor.InsertElementActivated += new XEditNet.InterfaceActivationEventHandler(InsertElementActivated);
                r.Editor.ChangeElementActivated += new XEditNet.InterfaceActivationEventHandler(ChangeElementActivated);

                foreach (WidgetPanel p in panels.Values)
                {
                    p.Nested.Editor = r.Editor;
                }

                currentRegion = r;
            }
        }

        private void InsertElementActivated(object sender, InterfaceActivationEventArgs e)
        {
            ActivateDockElement(typeof(ElementInsertPanel), e);
        }

        private void ChangeElementActivated(object sender, InterfaceActivationEventArgs e)
        {
            ActivateDockElement(typeof(ElementChangePanel), e);
        }

        private void ActivateDockElement(Type panelType, InterfaceActivationEventArgs e)
        {
            WidgetPanel dockElement = panels[panelType];
            if (dockElement == null)
                throw new InvalidOperationException("Failed to find panel for: " + panelType.ToString());
 
            if (dockElement.DockState != DockState.Hidden)
            {
                dockElement.Activate();
                e.Handled = true;
            }
        }

        private static DockContent GetMdiForm(System.Xml.XmlDocument doc, bool valid)
        {
            IXEditNetProfile prof = ProfileProvider.GetProfile(doc);

            UserControl userControl = null;

            if (prof != null)
                userControl = prof.GetEditorRegion(doc);

            if (userControl == null)
                userControl = new XEditNetDefaultEditorRegion();

            IXEditNetEditorRegion r = userControl as IXEditNetEditorRegion;
            if (r == null)
                throw new InvalidOperationException("User control returned by profile does not implement " + typeof(IXEditNetEditorRegion));

            if (prof != null && prof.Info.Stylesheet != null && prof.Info.Stylesheet.Length > 0)
                r.Editor.SetStylesheet(prof.Info.Stylesheet);

            r.Editor.Attach(doc, valid);
            XEditNetChildForm2 form = new XEditNetChildForm2(userControl);

            return form;
        }

        private void OpenFile(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
			dlg.AddExtension=true;
			dlg.CheckFileExists=true;
			dlg.CheckPathExists=true;
			dlg.DefaultExt="xml";
            dlg.Filter = DEFAULT_FILE_FILTER;
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                string fileName = dlg.FileName;
    			FileInfo fi=new FileInfo(fileName);
                string title = fi == null ? "New Document" : fi.Name;

                try
                {
                    bool valid;
                    XmlDocument doc = XEditNetCtrl.LoadDocument(fi.FullName, true, out valid);
                    if (doc == null)
                        return;

                    DockContent childForm = GetMdiForm(doc, valid);
                    childForm.Text = title;
                    //f.Closing += new CancelEventHandler(ChildClosing);
                    childForm.MdiParent = this;
                    childForm.DockAreas = DockAreas.Document | DockAreas.Float;
                    childForm.ShowHint = DockState.Document;
                    childForm.Activated += new EventHandler(childForm_Activated);
                    childForm.Show(this.dockPanel1);
                    childForm.Show();
                    childForm.Tag = fi;
                }
                catch (XmlException ex)
                {
                    // TODO: M: lots of things can cause this error, eg. trying to run xpath anywhere
                    MessageBox.Show(this, "XML error reading document\n" + ex.Message, "Open File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message);
                }
            }
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            saveFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string FileName = saveFileDialog.FileName;
            }
        }

        private void ExitToolsStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void CutToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void CopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void PasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void ToolBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStrip.Visible = toolBarToolStripMenuItem.Checked;
        }

        private void StatusBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            statusStrip.Visible = statusBarToolStripMenuItem.Checked;
        }

        private void CascadeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.Cascade);
        }

        private void TileVerticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileVertical);
        }

        private void TileHorizontalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileHorizontal);
        }

        private void ArrangeIconsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.ArrangeIcons);
        }

        private void CloseAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form childForm in MdiChildren)
            {
                childForm.Close();
            }
        }

    }
}
