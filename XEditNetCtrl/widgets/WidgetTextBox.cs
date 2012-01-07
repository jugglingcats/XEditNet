using System;
using System.Windows.Forms;

namespace XEditNet.Widgets
{
	public class WidgetTextBox : TextBox
	{
		protected override bool IsInputKey(Keys keyData)
		{
			if ( keyData == Keys.Down || keyData == Keys.Enter || keyData == Keys.Up )
				return false;

			return base.IsInputKey(keyData);
		}
	}
}
