using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Xml;
using XEditNet.Validation;

namespace XEditNet.Widgets
{
	/// <summary>
	/// Summary description for ElementChangePanel.
	/// </summary>
	public class ElementChangePanel : ElementListPanelBase
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public ElementChangePanel()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call

		}

		protected override void PerformAction(XEditNet.Dtd.XmlName name)
		{
			XmlElement elem=XmlUtil.CreateElement(name, editor.Document);
			editor.Change(elem);
		}

		protected override void ProcessUpdate()
		{
			base.ProcessUpdate();

			ValidationManager vm=editor.ValidationManager;

			// TODO: E: entities!
			if ( parent == null || parent.ParentNode == null || parent.ParentNode.NodeType != XmlNodeType.Element )
				return;

			validItems=vm.GetValidElements((XmlElement) parent.ParentNode, parent, true);
			allItems=vm.GetAllElements();
		}

		protected override void UpdateChoices()
		{
			FilterItems(elementText.Text);
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
			this.SuspendLayout();
			// 
			// elementText
			// 
			this.elementText.Name = "elementText";
			// 
			// ElementChangePanel
			// 
			this.Name = "ElementChangePanel";
			this.ResumeLayout(false);

		}
		#endregion
	}
}
