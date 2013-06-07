using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WeifenLuo.WinFormsUI.Docking;
using XEditNet.Widgets;
using System.Windows.Forms;

namespace XEditNetAuthor
{
    class WidgetPanel : DockContent
    {
        private PanelBase panel;

        public WidgetPanel(PanelBase nestedPanel)
        {
            this.panel= nestedPanel;
            Controls.Add(nestedPanel);
            panel.Dock = DockStyle.Fill;
        }

        public PanelBase Nested
        {
            get { return panel; }
        }
    }
}
