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
	internal class AttributeWidgetText : AttributeWidget
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private TextBox text;

		public AttributeWidgetText(XmlElement element, string attrName, IValidationProvider val) : base(element, attrName, val)
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			text=new TextBox();
			string curVal=element.GetAttribute(attrName);
			text.Text=curVal;
			text.TextChanged+=new EventHandler(TextBoxChanged);

			int xstart=deleteButton.Location.X+deleteButton.Width;
			text.Location=new Point(xstart+5, 0);
			// TODO: L: why is this -25 ??
			text.Width=ClientRectangle.Width-text.Location.X-25;
			text.Anchor |= AnchorStyles.Right;
			Controls.Add(text);
		}

		protected override void RemoveAttribute()
		{
			// TODO: L: disable events then re-enable
			text.Clear();
			base.RemoveAttribute();
		}

		private void TextBoxChanged(object sender, EventArgs e)
		{
			element.SetAttribute(attrName, text.Text);
			UpdateState();
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
		}
		#endregion

	}
}
