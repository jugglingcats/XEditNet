using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using XEditNet.Location;
using XEditNet.Validation;

// TODO: H: preceding/following don't update when nodes inserted/removed

namespace XEditNet.Widgets
{
	/// <summary>
	/// Summary description for ElementListPanel.
	/// </summary>
	public class QuickFixPanel : PanelBase
	{
		private IContainer components;
		private TreeView elementList;

		private Button button1;
		private ComboBox comboBox1;
		private Label label1;
		private ImageList coreImageList;
		private const int iconMargin = 18;
		private const int iconOffset = 3;
		private const int textPadding = 3;

		private ValidationError[] errors=null;
		private XmlNode errorNode=null;
		private XmlNode preceding;
		private XmlNode following;

		// TODO: M: think about extracting interface for this

		public QuickFixPanel()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			InitImageList();
			UpdateChoices();
		}

		private void InitImageList()
		{
			ControlUtil.AddImage(coreImageList, "widgets.images.validationError.png");
			ControlUtil.AddImage(coreImageList, "widgets.images.bulbLarge.png");
			ControlUtil.AddImage(coreImageList, "widgets.images.elementNormal.png");
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			return base.ProcessCmdKey (ref msg, keyData);
		}

		protected override bool UpdateLocation()
		{
			ValidationManager vm=editor.ValidationManager;

			Selection sel = editor.Selection;

			if ( (sel.IsEmpty || vm == null) )
				return SetEmpty();

			XmlNode n;
			XmlElement p;
			if ( sel.IsSingleNode )
			{
				p=null;
				n=sel.Start.Node;
			} 
			else
			{
				SelectionPoint sp=sel.Start;
				p=SelectionManager.GetInsertionContext(sp, out n);
			}

			if ( node == n && parent == p )
				// nothing changed
				return false;

			node=n;
			parent=p;

			return true;
		}

		protected override void ProcessUpdate()
		{
			ValidationManager vm=editor.ValidationManager;

			errorNode=node;

			// TODO: M: this is getting silly - confusion over parent/node
			if ( parent != null && (node == null || node.NodeType != XmlNodeType.Text) )
				errorNode=parent;

			// TODO: M: optimise this - location may not have changed

			if ( errorNode == null )
				return;

			// TODO: H: check the performance of this for very large doc
			//			think about going around forward rather than looking back
			// TODO: H: have seen this throw null pointer
			XmlNodeList l=errorNode.SelectNodes("preceding::node()");
			int c=l.Count;
			XmlNode[] nodes=new XmlNode[c];
			
			foreach ( XmlNode n in l )
				nodes[--c]=n;

			preceding=FindErrorInList(nodes);
			following=FindErrorInList(errorNode.SelectNodes("following::node() | descendant::node()"));

			errors=new ValidationError[] {};
			while ( errorNode != null )
			{
				errors=vm.GetErrorDetails(errorNode);
				if ( errors.Length > 0 )
					break;

				errorNode=errorNode.ParentNode;
			}
		}

		private XmlNode FindErrorInList(IEnumerable list)
		{
			IValidationLookup vl=editor.ValidationManager.InvalidNodes;
			foreach ( XmlNode n in list )
			{
				if ( vl.Contains(n) )
					return n;
			}
			return null;
		}

		protected override void UpdateChoices()
		{
			// TODO: H: improve display of this
			if ( errorNode == null )
				label1.Text="No Validation Errors At Selection";
			else
				label1.Text=string.Format("Validation Errors For {0} '{1}'", errorNode.NodeType, errorNode.Name);

			elementList.Nodes.Clear();
			comboBox1.Items.Clear();

			comboBox1.Items.Add(errorNode == null ? "No Errors" : "All Errors");
			comboBox1.SelectedIndex=0;
			elementList.Visible=errorNode != null;

			if ( errors == null || errors.Length == 0 )
				return;

			foreach ( ValidationError ve in errors )
				comboBox1.Items.Add(ve);

		}

		// TODO: L: create a custom class for this stuff
		private void DrawItem(object sender, DrawItemEventArgs e)
		{
			string text=string.Empty;
			if ( e.Index >= 0 )
				text=comboBox1.Items[e.Index].ToString();

			e.DrawBackground();

			Rectangle rc=e.Bounds;
			rc.Width-=iconMargin;
			rc.Offset(iconMargin, 0);

			int dy=(rc.Height - coreImageList.ImageSize.Height) / 2;
			Point pt=new Point(e.Bounds.X+iconOffset, e.Bounds.Y+iconOffset);

			bool inHeader=(e.State & DrawItemState.ComboBoxEdit) == DrawItemState.ComboBoxEdit;
			if ( inHeader )
			{
				pt.Y+=dy-iconOffset;

				SizeF tsz=e.Graphics.MeasureString(text, e.Font, e.Bounds.Width-iconMargin);
				int dy2=(rc.Height - tsz.ToSize().Height) / 2;
				if ( dy2 < 0)
					dy2=0;

				rc.Y+=dy2-textPadding;
			}

			if ( e.Index > 0 )
				coreImageList.Draw(e.Graphics, pt, 0);
			else
				coreImageList.Draw(e.Graphics, pt, 1);

			rc.Y+=textPadding;
			StringFormat sf=new StringFormat(StringFormatFlags.FitBlackBox | StringFormatFlags.LineLimit);
			e.Graphics.DrawString(text, e.Font, new SolidBrush(e.ForeColor), rc, sf);

			if ( !inHeader && e.Index != comboBox1.SelectedIndex+1 )
			{
				e.Graphics.DrawLine(SystemPens.ControlDark, e.Bounds.Left, e.Bounds.Bottom-1, 
					e.Bounds.Right, e.Bounds.Bottom-1);
			}

			e.DrawFocusRectangle();
		}

		private void MeasureItem(object sender, MeasureItemEventArgs e)
		{
			Font f=comboBox1.Font;

			string text=string.Empty;
			if ( e.Index >= 0 )
				text=comboBox1.Items[e.Index].ToString();

			SizeF tsz=e.Graphics.MeasureString(text, f, comboBox1.DropDownWidth-iconMargin);
			e.ItemHeight=Math.Max(tsz.ToSize().Height, coreImageList.ImageSize.Height);
			e.ItemHeight+=textPadding*2;
			e.ItemWidth=tsz.ToSize().Width+iconMargin;
		}

		private void ErrorSelected(object sender, EventArgs e)
		{
			elementList.BeginUpdate();
			elementList.Nodes.Clear();
			QuickFix[] fixes;
			
			if ( errorNode == null )
			{
				elementList.EndUpdate();
				return;
			}

			if ( comboBox1.SelectedIndex == 0 )
				fixes=editor.ValidationManager.GetQuickFixes(errorNode);
			else
			{
				ValidationError ve=comboBox1.SelectedItem as ValidationError;
				Debug.Assert(ve != null, "Expected ValidationError in combo!");

				fixes=editor.ValidationManager.GetQuickFixes(ve);
			}

			QuickFixSorter s=new QuickFixSorter(fixes, 5);

			AddItems(s.TopItems, elementList.Nodes, true);

			elementList.EndUpdate();
		}

		private void AddItems(ICollection items, TreeNodeCollection parent, bool topLevel)
		{
			foreach ( object o in items )
			{
				if ( o is QuickFix )
				{
					QuickFix qf=(QuickFix) o;
					string text=qf.SubText;
					if ( topLevel )
						text=string.Format("{0} '{1}'", qf.MainText, qf.SubText);

					TreeNode tnSub=new TreeNode(text);
					parent.Add(tnSub);
					tnSub.Tag=qf;
					tnSub.ImageIndex=2;
					tnSub.SelectedImageIndex=2;
				}
				else
				{
					// must be group
					QuickFixGroup qfg=(QuickFixGroup) o;
					TreeNode tnMain=new TreeNode(qfg.Name);
					parent.Add(tnMain);
					tnMain.ImageIndex=1;
					tnMain.SelectedImageIndex=1;
					AddItems(qfg.Items, tnMain.Nodes, false);
					if ( topLevel )
						tnMain.Expand();
				}
			}
		}


//			Hashtable ht=new Hashtable();
//
//			foreach ( QuickFix qf in fixes )
//			{
//				string main=qf.MainText;
//				TreeNode tnMain=ht[main] as TreeNode;
//				if ( tnMain == null )
//				{
//					tnMain=new TreeNode(main);
//					tnMain.ImageIndex=1;
//					tnMain.SelectedImageIndex=1;
//					elementList.Nodes.Add(tnMain);
//					ht[main]=tnMain;
//				}
//
//				string sub=qf.SubText;
//
//				if ( sub != null )
//				{
//					if ( tnMain.Tag == null && tnMain.Nodes.Count == 0 )
//					{
//						tnMain.Text+=" "+sub;
//						tnMain.Tag=qf;
//					}
//					else
//					{
//						if ( tnMain.Tag != null )
//						{
//							QuickFix qfPrev=tnMain.Tag as QuickFix;
//
//							TreeNode tnPrev=new TreeNode(qfPrev.SubText);
//							tnPrev.Tag=qfPrev;
//							tnPrev.ImageIndex=2;
//							tnPrev.SelectedImageIndex=2;
//
//							tnMain.Text=main;
//							tnMain.Tag=null;
//							tnMain.Nodes.Add(tnPrev);
//						}
//						TreeNode tnSub=new TreeNode(sub);
//						tnMain.Nodes.Add(tnSub);
//						tnMain.Expand();
//						tnSub.Tag=qf;
//						tnSub.ImageIndex=2;
//						tnSub.SelectedImageIndex=2;
//					}
//				}
//				else
//				{
//					tnMain.Tag=qf;
//				}
//			}

		private void QuickFixSelected(object sender, EventArgs e)
		{
			TreeNode node=elementList.SelectedNode;
			if ( node == null )
				return;

			QuickFix qf=node.Tag as QuickFix;
			if ( qf == null )
				return;

			editor.PerformQuickFix(qf);
			// TODO: L: see if this is called twice
			UpdateLocation();
			ProcessUpdate();
			UpdateChoices();
			editor.Focus();
		}

		public bool HasPreceding
		{
			get { return preceding != null; }
		}

		public bool HasFollowing
		{
			get { return following != null; }
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
			this.elementList = new System.Windows.Forms.TreeView();
			this.coreImageList = new System.Windows.Forms.ImageList(this.components);
			this.button1 = new System.Windows.Forms.Button();
			this.comboBox1 = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// elementList
			// 
			this.elementList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.elementList.BackColor = System.Drawing.Color.FloralWhite;
			this.elementList.FullRowSelect = true;
			this.elementList.HideSelection = false;
			this.elementList.ImageList = this.coreImageList;
			this.elementList.Location = new System.Drawing.Point(8, 64);
			this.elementList.Name = "elementList";
			this.elementList.ShowPlusMinus = false;
			this.elementList.ShowRootLines = false;
			this.elementList.Size = new System.Drawing.Size(273, 160);
			this.elementList.Sorted = true;
			this.elementList.TabIndex = 1;
			this.elementList.DoubleClick += new System.EventHandler(this.QuickFixSelected);
			// 
			// coreImageList
			// 
			this.coreImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
			this.coreImageList.ImageSize = new System.Drawing.Size(14, 14);
			this.coreImageList.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// button1
			// 
			this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.button1.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.button1.Location = new System.Drawing.Point(8, 232);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(272, 23);
			this.button1.TabIndex = 6;
			this.button1.Text = "&Automatically Fix";
			// 
			// comboBox1
			// 
			this.comboBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.comboBox1.BackColor = System.Drawing.Color.FloralWhite;
			this.comboBox1.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
			this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox1.DropDownWidth = 400;
			this.comboBox1.IntegralHeight = false;
			this.comboBox1.ItemHeight = 30;
			this.comboBox1.Location = new System.Drawing.Point(8, 26);
			this.comboBox1.MaxDropDownItems = 100;
			this.comboBox1.Name = "comboBox1";
			this.comboBox1.Size = new System.Drawing.Size(272, 36);
			this.comboBox1.TabIndex = 7;
			this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.ErrorSelected);
			this.comboBox1.MeasureItem += new System.Windows.Forms.MeasureItemEventHandler(this.MeasureItem);
			this.comboBox1.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.DrawItem);
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.label1.Location = new System.Drawing.Point(8, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(272, 16);
			this.label1.TabIndex = 8;
			this.label1.Text = "Validation Errors for Element \'para\'";
			// 
			// QuickFixPanel
			// 
			this.Controls.Add(this.label1);
			this.Controls.Add(this.comboBox1);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.elementList);
			this.Name = "QuickFixPanel";
			this.Size = new System.Drawing.Size(288, 264);
			this.ResumeLayout(false);

		}

		public XmlNode PrecedingError
		{
			get { return preceding; }
		}

		public XmlNode FollowingError
		{
			get { return following; }
		}

		#endregion
	}

}
