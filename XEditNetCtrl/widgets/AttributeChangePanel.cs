using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using XEditNet.Dtd;
using XEditNet.Location;
using XEditNet.Validation;
// TODO: M: update in response to events not just text box changes (changes might come from elsewhere)

namespace XEditNet.Widgets
{
	/// <summary>
	/// Summary description for AttributeChangePanel.
	/// </summary>
	public class AttributeChangePanel : PanelBase
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private WidgetTextBox attributeText;
		private PanelEx mainPanel;
		private ImageList imageList;
		private IContainer components;
		private FlatButton addButton;
		private Label label1;
		private ComboBox comboDummy;

		private ArrayList controls=new ArrayList();

		public AttributeChangePanel()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			Keys k=(Keys) (Int16) keyData;

			if ( k.Equals(Keys.Escape) || k.Equals(Keys.Return) )
			{
				editor.Focus();
				return true;
			}

			return base.ProcessCmdKey (ref msg, keyData);
		}

		private void AttributeFilterTextChanged(object sender, EventArgs e)
		{
			FilterItems(attributeText.Text);
		}

		private void FilterItems(string filter)
		{
			mainPanel.Controls.Clear();

			mainPanel.SuspendLayout();

			int y=4;
			lock ( controls )
			{
				foreach ( AttributeWidget c in controls )
				{
					if ( IsMatch(c.AttributeName, filter) )
					{
						c.Location=new Point(4, y);
						c.Anchor=AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
						mainPanel.Controls.Add(c);
						y+=c.Height;
					}
				}
				
			}
			mainPanel.ResumeLayout();
		}

		protected override void UpdateChoices()
		{
			FilterItems(attributeText.Text);
		}

		protected override bool UpdateLocation()
		{
			// TODO: L: there's some duplication here with ElementListPanelBase impl

			ValidationManager vm=editor.ValidationManager;

			if ( (editor.Selection.IsEmpty || vm == null) )
				return SetEmpty();

			SelectionPoint sp=editor.Selection.Start;
			XmlNode n;
			XmlElement p=SelectionManager.GetInsertionContext(sp, out n);

			Console.WriteLine("Selection parent={0}, node={1}", p, n);

			if ( parent == p )
				// nothing changed (we don't care about the actual node)
				return false;

			node=n;
			parent=p;

			return true;
		}

		protected override void ProcessUpdate()
		{
			int y=4;
			ArrayList valid=new ArrayList();

			lock ( controls )
			{
				controls.Clear();

				AttributeWidget aw;
				foreach ( string name in ValidationManager.GetDefinedAttributeNames(parent) )
				{
					valid.Add(name);

					switch ( ValidationManager.GetAttributeType(parent, name) )
					{
						case AttributeType.Enumerated:
							aw=new AttributeWidgetEnum(parent, name, ValidationManager);
							break;

						default:
							aw=new AttributeWidgetText(parent, name, ValidationManager);
							break;
					}
					aw.Location=new Point(4, y);
					aw.Width=this.Width-1;
					aw.Height=22;
					controls.Add(aw);
					y+=aw.Height;
					Application.DoEvents();
				}

				if ( parent != null )
				{
					foreach ( XmlAttribute attr in parent.Attributes )
					{
						if ( valid.Contains(attr.Name) )
							continue;

						aw=new AttributeWidgetText(parent, attr.Name, ValidationManager);
						aw.Location=new Point(4, y);
						aw.Width=this.Width-1;
						aw.Height=22;
						controls.Add(aw);
						y+=aw.Height;
						Application.DoEvents();
					}
				}

				controls.Sort(new AttributeWidgetSorter());
			}
		}

		private class AttributeWidgetSorter : IComparer
		{
			public int Compare(object x, object y)
			{
				AttributeWidget aw1=(AttributeWidget) x;
				AttributeWidget aw2=(AttributeWidget) y;

				return aw1.AttributeName.CompareTo(aw2.AttributeName);
			}
		}

		private bool IsMatch(string s, string filter)
		{
			if ( filter.EndsWith(" ") )
				return s.Equals(filter.Split(' ')[0]);

			return s.StartsWith(filter);
		}

		private void AddAttribute(object sender, EventArgs e)
		{
			if ( attributeText.Text.Length == 0 )
			{
				MessageBox.Show(this, "Please enter the name of an attribute to create", "Add Attribute", MessageBoxButtons.OK,  MessageBoxIcon.Information);
				attributeText.Focus();
				return;
			}

			if ( parent.HasAttribute(attributeText.Text) )
			{
				MessageBox.Show(this, "The attribute you have specified already exists", "Add Attribute", MessageBoxButtons.OK,  MessageBoxIcon.Information);
				attributeText.Focus();
				return;
			}

			// TODO: H: this doesn't display (need to update controls)
			parent.SetAttribute(attributeText.Text, "");

			AttributeWidget aw=new AttributeWidgetText(parent, attributeText.Text, ValidationManager);
			aw.Location=new Point(4, 0);
			aw.Width=this.Width-1;
			aw.Height=22;
			controls.Add(aw);

			controls.Sort(new AttributeWidgetSorter());

			FilterItems(attributeText.Text);
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(AttributeChangePanel));
			this.attributeText = new XEditNet.Widgets.WidgetTextBox();
			this.mainPanel = new XEditNet.Widgets.PanelEx();
			this.imageList = new System.Windows.Forms.ImageList(this.components);
			this.addButton = new XEditNet.Widgets.FlatButton();
			this.label1 = new System.Windows.Forms.Label();
			this.comboDummy = new System.Windows.Forms.ComboBox();
			this.SuspendLayout();
			// 
			// attributeText
			// 
			this.attributeText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.attributeText.BackColor = System.Drawing.Color.FloralWhite;
			this.attributeText.Location = new System.Drawing.Point(8, 24);
			this.attributeText.Name = "attributeText";
			this.attributeText.Size = new System.Drawing.Size(184, 20);
			this.attributeText.TabIndex = 0;
			this.attributeText.Text = "";
			this.attributeText.TextChanged += new System.EventHandler(this.AttributeFilterTextChanged);
			// 
			// mainPanel
			// 
			this.mainPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.mainPanel.AutoScroll = true;
			this.mainPanel.BackColor = System.Drawing.Color.FloralWhite;
			this.mainPanel.Location = new System.Drawing.Point(8, 48);
			this.mainPanel.Name = "mainPanel";
			this.mainPanel.Size = new System.Drawing.Size(208, 176);
			this.mainPanel.TabIndex = 2;
			// 
			// imageList
			// 
			this.imageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth4Bit;
			this.imageList.ImageSize = new System.Drawing.Size(16, 16);
			this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
			this.imageList.TransparentColor = System.Drawing.Color.Magenta;
			// 
			// addButton
			// 
			this.addButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.addButton.ImageIndex = 0;
			this.addButton.ImageList = this.imageList;
			this.addButton.Location = new System.Drawing.Point(195, 25);
			this.addButton.Name = "addButton";
			this.addButton.Size = new System.Drawing.Size(16, 16);
			this.addButton.TabIndex = 1;
			this.addButton.Text = "Create New Attribute";
			this.addButton.Click += new System.EventHandler(this.AddAttribute);
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(6, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(100, 16);
			this.label1.TabIndex = 3;
			this.label1.Text = "Filter Attributes:";
			// 
			// comboDummy
			// 
			this.comboDummy.Location = new System.Drawing.Point(16, 64);
			this.comboDummy.Name = "comboDummy";
			this.comboDummy.Size = new System.Drawing.Size(121, 21);
			this.comboDummy.TabIndex = 4;
			this.comboDummy.Text = "comboBox1";
			// 
			// AttributeChangePanel
			// 
			this.Controls.Add(this.mainPanel);
			this.Controls.Add(this.comboDummy);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.attributeText);
			this.Controls.Add(this.addButton);
			this.Name = "AttributeChangePanel";
			this.Size = new System.Drawing.Size(223, 231);
			this.ResumeLayout(false);

		}
		#endregion
	}
}
