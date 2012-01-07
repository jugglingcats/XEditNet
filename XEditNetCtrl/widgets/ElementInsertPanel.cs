using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Xml;
using XEditNet.Dtd;
using XEditNet.Validation;

namespace XEditNet.Widgets
{
	/// <summary>
	/// Summary description for ElementInsertPanel.
	/// </summary>
	public class ElementInsertPanel : ElementListPanelBase
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public ElementInsertPanel()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call

		}

		protected override void AttachDoc()
		{
			editor.Document.NodeInserted+=new XmlNodeChangedEventHandler(NodeInserted);
			editor.Document.NodeRemoved+=new XmlNodeChangedEventHandler(NodeRemoved);
		}

		protected override void DetachDoc()
		{
			editor.Document.NodeInserted-=new XmlNodeChangedEventHandler(NodeInserted);
			editor.Document.NodeRemoved-=new XmlNodeChangedEventHandler(NodeRemoved);
		}

		protected void NodeInserted(object sender, XmlNodeChangedEventArgs e)
		{
			if ( XmlUtil.IsNamespaceAttribute(e.Node, e.NewParent) )
				return;

			if ( e.Node.NodeType == XmlNodeType.Element && e.NewParent.Equals(parent) )
				ForceChange(true);
		}

		protected void NodeRemoved(object sender, XmlNodeChangedEventArgs e)
		{
			if ( XmlUtil.IsNamespaceAttribute(e.Node, e.OldParent) )
				return;

			if ( e.Node.NodeType == XmlNodeType.Element && e.OldParent.Equals(parent) )
				ForceChange(true);
		}


		protected override void PerformAction(XEditNet.Dtd.XmlName name)
		{
			if ( editor.Document == null )
			{
				XmlDocument doc=new XmlDocument();
				editor.Attach(doc, true);
			} 
			else
				editor.CreateUndoPoint();

			XmlElement elem=XmlUtil.CreateElement(name, editor.Document);
			editor.Insert(elem);
		}

		protected override void ProcessUpdate()
		{
			base.ProcessUpdate();

			ValidationManager vm=editor.ValidationManager;

			validItems=vm.GetValidElements(parent, node, false);
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
			components = new System.ComponentModel.Container();
		}

		#endregion
	}
}
