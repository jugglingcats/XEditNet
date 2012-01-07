using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using XEditNet.Dtd;
using XEditNet.Location;
using XEditNet.Validation;

namespace XEditNet.Widgets
{
	/// <summary>
	/// Summary description for ElementListPanel.
	/// </summary>
	public class ElementListPanelBase : PanelBase
	{
		private System.ComponentModel.IContainer components;
		protected WidgetTextBox elementText;
		private System.Windows.Forms.TreeView elementList;
		private System.Windows.Forms.CheckBox showValid;
		private System.Windows.Forms.ImageList imageList;

		protected ElementListItem[] allItems;
		protected ElementListItem[] validItems;
		private System.Windows.Forms.Label label1;

		public ElementListPanelBase()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			InitImageList();
		}

		private void InitImageList()
		{
			ControlUtil.AddImage(imageList, "widgets.images.elementNormal.png");
			ControlUtil.AddImage(imageList, "widgets.images.elementChoice.png");
			ControlUtil.AddImage(imageList, "widgets.images.elementRequired.png");
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			Keys k=(Keys) (Int16) keyData;

			if ( k.Equals(Keys.Escape) || k.Equals(Keys.Return) )
			{
				if ( k.Equals(Keys.Enter) )
					OnItemSelected();
				else if ( k.Equals(Keys.Escape) )
					editor.Focus();
				
				return true;
			}

			return base.ProcessCmdKey (ref msg, keyData);
		}

		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			this.elementText.Focus();
		}

		protected void OnItemSelected()
		{
			XmlName name=SelectedItem;
			if ( name == null )
			{
				if ( editor.ValidationManager.HasElements )
				{
					MessageBox.Show(this, "Please select a valid element from the DTD", "Insert Element", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
				name=Filter;
				if ( name == null || name.QualifiedName.Length == 0 )
				{
					MessageBox.Show(this, "Please enter an element name or choose one from the list", "Insert Element", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}

			PerformAction(name);

			editor.Focus();
		}

		protected virtual void PerformAction(XmlName name)
		{
		}

		private void ElementTextChanged(object sender, System.EventArgs e)
		{
			FilterItems(elementText.Text);
		}

		protected void FilterItems(string filter)
		{
			elementList.BeginUpdate();
			elementList.Nodes.Clear();

			ICollection col=showValid.Checked ? validItems : allItems;
			if ( col == null )
			{
				elementList.EndUpdate();
				return;
			}

			foreach ( ElementListItem s in col )
			{
				if ( filter.Length == 0 || IsMatch(s.Name.QualifiedName, filter) )
				{
					string title=s.Name.QualifiedName;
					TreeNode n=new TreeNode(title);
					n.Tag=s.Name;

					if ( s.IsRequired )
					{
						if ( s.IsChoice )
						{
							n.ImageIndex=1;
							n.SelectedImageIndex=1;
						}
						else
						{
							n.ImageIndex=2;
							n.SelectedImageIndex=2;
						}
					}
					else
						n.NodeFont=new Font(elementList.Font, FontStyle.Regular);

					elementList.Nodes.Add(n);
				}
			}
			if ( elementList.Nodes.Count > 0 )
				elementList.SelectedNode=elementList.Nodes[0];

			elementList.EndUpdate();
		}

		private bool IsMatch(string s, string filter)
		{
			if ( filter == null )
				return true;

			if ( filter.EndsWith(" ") )
				return s.Equals(filter.Split(' ')[0]);

			return s.StartsWith(filter);
		}

		protected override bool UpdateLocation()
		{
			ValidationManager vm=editor.ValidationManager;

			if ( (editor.Selection.IsEmpty || vm == null) )
				return SetEmpty();

			SelectionPoint sp=editor.Selection.Start;
			XmlNode n;
			XmlElement p=SelectionManager.GetInsertionContext(sp, out n);

			if ( node == n && parent == p )
				// nothing changed
				return false;

			node=n;
			parent=p;

			return true;
		}

		private void ToggleShowValid(object sender, System.EventArgs e)
		{
			FilterItems(elementText.Text);
			elementText.Focus();
		}

/*
		private void Load(object sender, System.EventArgs e)
		{
			FilterItems("");
		}
*/

		private void DoubleClickElement(object sender, System.EventArgs e)
		{
			OnItemSelected();
		}

		// TODO: L: think about namespaces (not important now because not stored in DTD)
		public XmlName Filter
		{
			get 
			{ 
				if ( elementText.Text.Length == 0 )
					return null;

				return new XmlName(elementText.Text);
			}
		}

		public XmlName SelectedItem
		{
			get 
			{
				TreeNode sel=elementList.SelectedNode;
				return sel == null ? null : (XmlName) sel.Tag;
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

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.elementText = new XEditNet.Widgets.WidgetTextBox();
			this.elementList = new System.Windows.Forms.TreeView();
			this.imageList = new System.Windows.Forms.ImageList(this.components);
			this.showValid = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// elementText
			// 
			this.elementText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.elementText.BackColor = System.Drawing.Color.FloralWhite;
			this.elementText.Location = new System.Drawing.Point(8, 24);
			this.elementText.Name = "elementText";
			this.elementText.Size = new System.Drawing.Size(208, 20);
			this.elementText.TabIndex = 1;
			this.elementText.Text = "";
			this.elementText.TextChanged += new System.EventHandler(this.ElementTextChanged);
			this.elementText.Enter += new System.EventHandler(this.ElementTextFocus);
			// 
			// elementList
			// 
			this.elementList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.elementList.BackColor = System.Drawing.Color.FloralWhite;
			this.elementList.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.elementList.FullRowSelect = true;
			this.elementList.HideSelection = false;
			this.elementList.ImageList = this.imageList;
			this.elementList.Location = new System.Drawing.Point(8, 48);
			this.elementList.Name = "elementList";
			this.elementList.ShowRootLines = false;
			this.elementList.Size = new System.Drawing.Size(208, 152);
			this.elementList.Sorted = true;
			this.elementList.TabIndex = 2;
			this.elementList.DoubleClick += new System.EventHandler(this.DoubleClickElement);
			// 
			// imageList
			// 
			this.imageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
			this.imageList.ImageSize = new System.Drawing.Size(14, 14);
			this.imageList.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// showValid
			// 
			this.showValid.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.showValid.Checked = true;
			this.showValid.CheckState = System.Windows.Forms.CheckState.Checked;
			this.showValid.Location = new System.Drawing.Point(8, 203);
			this.showValid.Name = "showValid";
			this.showValid.Size = new System.Drawing.Size(192, 24);
			this.showValid.TabIndex = 4;
			this.showValid.Text = "Only Show &Valid Items";
			this.showValid.CheckedChanged += new System.EventHandler(this.ToggleShowValid);
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(6, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(100, 16);
			this.label1.TabIndex = 5;
			this.label1.Text = "Filter Elements:";
			// 
			// ElementListPanelBase
			// 
			this.Controls.Add(this.label1);
			this.Controls.Add(this.showValid);
			this.Controls.Add(this.elementList);
			this.Controls.Add(this.elementText);
			this.Name = "ElementListPanelBase";
			this.Size = new System.Drawing.Size(223, 231);
			this.ResumeLayout(false);

		}
		#endregion

		private void ElementTextFocus(object sender, System.EventArgs e)
		{
			elementText.SelectAll();
		}

	}
}
