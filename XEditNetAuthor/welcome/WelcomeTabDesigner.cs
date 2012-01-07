using System;
using System.ComponentModel.Design;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace XEditNetAuthor.Welcome
{
	/// <summary>
	/// Summary description for WelcomeTabDesigner.
	/// </summary>
	public class WelcomeTabDesigner : ControlDesigner
	{
		public WelcomeTabDesigner()
		{
			//
			// TODO: Add constructor logic here
			//
		}
		/// <summary>
		/// Prevents the grid from being drawn on the WelcomeTabControl
		/// </summary>
//		protected override bool DrawGrid
//		{
//			get 
//			{ 
//				return base.DrawGrid && _allowGrid;
//			}
//		}

//		private bool _allowGrid = true;

		/// <summary>
		/// Simple way to ensure <see cref="WelcomeTabPage"/>s only contained here
		/// </summary>
		/// <param name="control"></param>
		/// <returns></returns>
//		public override bool CanParent(Control control)
//		{
//			if (control is WelcomeTabPage)
//				return true;
//
//			return false;
//		}
//
//		public override bool CanParent(ControlDesigner controlDesigner)
//		{
//			if (controlDesigner is WelcomeTabPageDesigner)
//				return true;
//
//			return false;
//		}

		protected override bool EnableDragRect
		{
			get
			{
				return false;
			}
		}

		protected override bool GetHitTest(Point point)
		{
			WelcomeTabControl wtc = this.Control as WelcomeTabControl;
		
			for ( int n=0; n< wtc.Pages.Count; n++ )
			{
				if ( wtc.BoundingRect(n).Contains(wtc.PointToClient(point)) )
					return true;
			}
			return false;
		}

		public override DesignerVerbCollection Verbs
		{
			get
			{
				DesignerVerbCollection verbs = new DesignerVerbCollection();
				verbs.Add(new DesignerVerb("Add Page", new EventHandler(handleAddPage)));

				return verbs;
			}
		}

		private void handleAddPage(object sender, EventArgs e)
		{
			WelcomeTabControl welcomeTabControl = this.Control as WelcomeTabControl;

			IDesignerHost h  = (IDesignerHost) GetService(typeof(IDesignerHost));
			IComponentChangeService c = (IComponentChangeService) GetService(typeof (IComponentChangeService));

			DesignerTransaction dt = h.CreateTransaction("Add Page");
			WelcomeTabPage page = (WelcomeTabPage) h.CreateComponent(typeof(WelcomeTabPage));
			c.OnComponentChanging(welcomeTabControl, null);
    
			//Add a new page to the collection
			welcomeTabControl.Pages.Add(page);
			welcomeTabControl.Controls.Add(page);
//			welcomeTabControl.ActivatePage(page);

			c.OnComponentChanged(welcomeTabControl, null, null, null);
			dt.Commit();
		}	

//		protected override void OnPaintAdornments(PaintEventArgs pe)
//		{
//			_allowGrid = false;
//			base.OnPaintAdornments (pe);
//			_allowGrid = true;
//		}
	}
}
