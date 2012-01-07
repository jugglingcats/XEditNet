using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace XEditNet.Widgets
{
	/// <summary>
	/// Summary description for ControlUtil.
	/// </summary>
	public class ControlUtil
	{
		public static void DrawButton(Graphics g, Rectangle rc)
		{
			Brush brush=new LinearGradientBrush(rc, Color.FromArgb(255,242,199), 
				Color.FromArgb(255,213,147), LinearGradientMode.Vertical);

			g.FillRectangle(brush, rc);

			rc.Width--;
			rc.Height--;
			g.DrawRectangle(new Pen(Color.FromArgb(75,75,111)), rc);
		}

		public static bool AddImage(ImageList l, string name)
		{
			Assembly a = typeof(ElementListPanelBase).Assembly;
			string fname = a.FullName.Split(',')[0]+"."+name;
			Stream stm = a.GetManifestResourceStream(fname);

			if ( stm != null )
			{
				Bitmap bm=new Bitmap(stm);
				l.Images.Add(bm);
				return true;
			}
			return false;
		}
	}
}
