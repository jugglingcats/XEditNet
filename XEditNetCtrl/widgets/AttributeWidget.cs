using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using System.Xml;
using XEditNet.Validation;

namespace XEditNet.Widgets
{
	internal class AttributeWidget : UserControl
	{
		private System.ComponentModel.IContainer components = null;

		protected readonly XmlElement element;
		protected readonly string attrName;
		protected readonly IValidationProvider validator;

		protected Label label;
		protected Button deleteButton;

		private static ImageList globalImageList=new ImageList();
		protected bool fixedAttribute;
		protected bool requiredAttribute;

		// TODO: pass in image list rather than rely on static
		public AttributeWidget(XmlElement element, string attrName, IValidationProvider val)
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
			InitImageList();

			this.element=element;
			this.attrName=attrName;
			this.validator=val;

			label=new Label();
			label.Location=new Point(0, 2);
			label.Text=attrName;
			label.Width=80;

			fixedAttribute = val.IsAttributeFixed(element, attrName);
			requiredAttribute = val.IsAttributeRequired(element, attrName);

			FlatButton b=new FlatButton();
			b.Location=new Point(label.Width, 2);
			b.ImageList=globalImageList;
			b.ImageIndex=0;
			b.Size = new System.Drawing.Size(16, 16);
			b.FlatStyle = FlatStyle.Popup;
			deleteButton=b;
			deleteButton.Click+=new EventHandler(DeleteAttribute);

			UpdateState();

			Controls.Add(label);
//			Controls.Add(toolBar1);
			Controls.Add(deleteButton);
		}

		private void InitImageList()
		{
			lock ( globalImageList )
			{
				if ( globalImageList.Images.Count == 0 )
				{
					globalImageList.ImageSize = new System.Drawing.Size(12, 12);
					globalImageList.ColorDepth=ColorDepth.Depth32Bit;
					globalImageList.TransparentColor = System.Drawing.Color.Transparent;
					ControlUtil.AddImage(globalImageList, "widgets.images.removeAttribute.png");
				}
			}
		}

		public string AttributeName
		{
			get { return attrName; }
		}

		protected void DeleteAttribute(object sender, EventArgs args)
		{
			RemoveAttribute();
		}

		protected void UpdateState()
		{
			bool isValid=validator.IsAttributeValid(element, attrName, element.GetAttributeNode(attrName));

			label.ForeColor=isValid ? Color.Black : Color.Red;
			label.Font=requiredAttribute || !isValid ? new Font(Font, FontStyle.Bold) : Font;

			XmlAttribute a=element.GetAttributeNode(attrName);
			deleteButton.Visible = (a != null && a.Specified);
		}

		protected virtual void RemoveAttribute()
		{
			element.RemoveAttribute(attrName);
			UpdateState();
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

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(AttributeWidget));
		}
		#endregion
	}
}

