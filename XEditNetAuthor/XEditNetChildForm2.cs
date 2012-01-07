using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using XEditNet.Profile;

namespace XEditNetAuthor
{
    public partial class XEditNetChildForm2 : DockContent
    {
        private UserControl editorRegion;

        public XEditNetChildForm2()
        {
            InitializeComponent();
        }

        public IXEditNetEditorRegion EditorRegion
        {
            get { return editorRegion as IXEditNetEditorRegion; }
        }

        public XEditNetChildForm2(UserControl editorRegion)
        {
            InitializeComponent();

            editorRegion.AutoScroll = true;
            editorRegion.BackColor = System.Drawing.Color.White;
            editorRegion.Dock = System.Windows.Forms.DockStyle.Fill;
            editorRegion.Location = new System.Drawing.Point(232, 44);
            editorRegion.Name = "editor";
            editorRegion.Size = new System.Drawing.Size(368, 458);
            editorRegion.TabIndex = 6;

            this.editorRegion = editorRegion;

            Controls.Add(editorRegion);
//            Controls.SetChildIndex(editorRegion, index);

            //Editor.ChangeAttributesActivated += new XEditNet.InterfaceActivationEventHandler(ChangeAttributesActivated);
            //Editor.ChangeElementActivated += new XEditNet.InterfaceActivationEventHandler(ChangeElementActivated);
            //Editor.InsertElementActivated += new XEditNet.InterfaceActivationEventHandler(InsertElementActivated);

            //this.elementChangePanel.Editor = Editor;
            //this.elementInsertPanel.Editor = Editor;
            //this.attributeChangePanel.Editor = Editor;
            //this.quickFixPanel.Editor = Editor;

            //if (editorRegion != null)
            //{
            //    int index = Controls.IndexOf(menuBar1);
            //    Controls.Add(editorRegion);
            //    Controls.SetChildIndex(editorRegion, index);
            //}

            //UpdateMenu();
        }
    }
}
