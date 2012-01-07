using System;
using System.ComponentModel.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace XEditNetAuthor.Welcome
{
	/// <summary>
	/// Summary description for WelcomeTabPage.
	/// </summary>
	public class WelcomeTabPage : Panel
	{
		public Button button=new Button();
		private string buttonText;
		private ImageList imageList;
		private int imageIndex;

		private string title;
		private string description;

		public string Title
		{
			get { return title; }
			set { title = value; }
		}

		public string Description
		{
			get { return description; }
			set { description = value; }
		}

		public WelcomeTabPage()
		{
		}

		public ImageList ImageList
		{
			get { return imageList; }
			set { imageList=value; }
		}

		public int ImageIndex
		{
			get { return imageIndex; }
			set { imageIndex=value; }
		}

		public string ButtonText
		{
			get { return buttonText; }
			set { buttonText=value; }
		}

		public void FocusFirstTabIndex()
		{
			//Activate the first control in the Panel
			Control found = null;
			//find the control with the lowest 
			foreach (Control control in this.Controls)
			{
				if (control.CanFocus && (found == null || control.TabIndex < found.TabIndex))
				{
					found = control;
				}
			}
			//Have we actually found anything
			if (found != null)
			{
				//Focus the found control
				found.Focus();
			}
			else
			{
				//Just focus the wizard Page
				this.Focus();
			}
		}
	}

	public class WelcomeTabPageDesigner : ParentControlDesigner
	{
		public override SelectionRules SelectionRules
		{
			get
			{
				return SelectionRules.None;
			}
		}

		public override DesignerVerbCollection Verbs
		{
			get
			{
				DesignerVerbCollection verbs = new DesignerVerbCollection();
				verbs.Add(new DesignerVerb("Remove Page", new EventHandler(handleRemovePage)));

				return verbs;
			}
		}

		private void handleRemovePage(object sender, EventArgs e)
		{			
			WelcomeTabPage page = this.Control as WelcomeTabPage;

			IDesignerHost h  = (IDesignerHost) GetService(typeof(IDesignerHost));
			IComponentChangeService c = (IComponentChangeService) GetService(typeof (IComponentChangeService));

			DesignerTransaction dt = h.CreateTransaction("Remove Page");
			
			if (page.Parent is WelcomeTabControl)
			{
				WelcomeTabControl welcomeTabControl = page.Parent as WelcomeTabControl;

				c.OnComponentChanging(welcomeTabControl, null);
				//Drop from WelcomeTabControl
				welcomeTabControl.Pages.Remove(page);
				welcomeTabControl.Controls.Remove(page);
				c.OnComponentChanged(welcomeTabControl, null, null, null);
				h.DestroyComponent(page);
			}
			else
			{
				c.OnComponentChanging(page, null);
				//Mark for destruction
				page.Dispose();
				c.OnComponentChanged(page, null, null, null);
			}
			dt.Commit();
		}

	}
}
