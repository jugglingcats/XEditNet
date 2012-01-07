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
	internal class AttributeWidgetEnum : AttributeWidget
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private ComboBox combo;

		public AttributeWidgetEnum(XmlElement element, string attrName, IValidationProvider val) : base(element, attrName, val)
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			combo=new ComboBox();
			string curVal=element.GetAttribute(attrName);

			combo.Items.Add(string.Empty);
			combo.Items.AddRange(val.GetEnumValues(element, attrName));
			combo.SelectedItem=curVal;
			combo.DropDownStyle=ComboBoxStyle.DropDownList;
			combo.Height-=2;
			combo.SelectionChangeCommitted+=new EventHandler(ComboSelChanged);

			int xstart=deleteButton.Location.X+deleteButton.Width;
			combo.Location=new Point(xstart+5, 0);
			// TODO: L: why is this -25 ??
			combo.Width=ClientRectangle.Width-combo.Location.X-25;
			combo.Anchor |= AnchorStyles.Right;
			Controls.Add(combo);
		}

		private void ComboSelChanged(object sender, EventArgs e)
		{
			string text=combo.SelectedItem as string;
			if ( text.Equals(string.Empty) )
				element.RemoveAttribute(attrName);
			else
				element.SetAttribute(attrName, text);

			UpdateState();
		}

		protected override void RemoveAttribute()
		{
			combo.SelectedItem=string.Empty;
			base.RemoveAttribute();
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
