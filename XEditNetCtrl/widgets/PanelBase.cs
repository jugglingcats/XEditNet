using System;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using XEditNet.Location;
using XEditNet.Validation;

namespace XEditNet.Widgets
{
	/// <summary>
	/// Summary description for PanelBase.
	/// </summary>
	public class PanelBase : UserControl
	{
		protected XEditNetCtrl editor;
		private Thread updateThread;
		protected XmlElement parent;
		protected XmlNode node;
		private bool enableSelTracking=true;
		private bool docAttached=false;
		private bool created=false;

		public event EventHandler FinishUpdate;

		public PanelBase()
		{
			updateThread=null;
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			if ( enableSelTracking == false )
			{
				// initialise
				UpdateLocation();
				ProcessUpdate();
			}
			// we call this here because selection may 
			// have been changed before load
			UpdateChoices();
		}

		protected void Attach()
		{
			editor.SelectionChanged+=new SelectionChangedEventHandler(SelectionChanged);
            ForceChange(false);
		}

		protected void Detach()
		{
			editor.SelectionChanged-=new SelectionChangedEventHandler(SelectionChanged);
			if ( docAttached )
			{
				DetachDoc();
				docAttached=false;
			}
			parent=null;
			node=null;
		}

		protected virtual void UpdateChoices()
		{
		}

		internal ValidationManager ValidationManager
		{
			get { return editor.ValidationManager; }
		}

		protected virtual bool UpdateLocation()
		{
			return false;
		}

		protected bool SetEmpty()
		{
			if ( parent == null && node == null )
				return false;

			parent=null;
			node=null;

			return true;
		}

		private void SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if ( !docAttached )
			{
				AttachDoc();
				docAttached=true;
			}

			ForceChange(false);
		}

		protected virtual void AttachDoc()
		{
		}

		protected virtual void DetachDoc()
		{
		}

		protected void ForceChange(bool force)
		{
			if ( updateThread != null && updateThread.IsAlive )
			{
				try
				{
					updateThread.Abort();
					updateThread.Join();
				}
				catch ( ThreadStateException )
				{
				}
				updateThread=null;
			}

			bool changed=UpdateLocation();
			if ( changed | force )
			{
				updateThread=new Thread(new ThreadStart(StartUpdate));
				updateThread.Priority=ThreadPriority.Lowest;
				updateThread.Start();
			}
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			created=true;
		}

		public void StartUpdate()
		{
			try
			{
				Thread.Sleep(150);
				ProcessUpdate();
				// TODO: L: this can fail if handle not created
				// TODO: C: this can hang on app close (added IsDisposed check)
				if ( created && !IsDisposed )
					Invoke(new EventHandler(InternalFinishUpdate));
			}
			catch ( ThreadAbortException )
			{
			}
		}

		protected virtual void ProcessUpdate()
		{
		}

		private void InternalFinishUpdate(object sender, EventArgs e)
		{
			UpdateChoices();
			OnFinishUpdate(new EventArgs());
		}

		private void OnFinishUpdate(EventArgs args)
		{
			if ( FinishUpdate != null )
				FinishUpdate(this, args);
		}

		public XEditNetCtrl Editor
		{
			get { return editor; }
			set
			{
				if ( editor != null )
					Detach();

				editor=value;
				if ( editor != null && enableSelTracking )
					Attach();
			}
		}

		public bool EnableSelectionTracking
		{
			set
			{
				enableSelTracking=value;
				if ( editor != null )
				{
					Detach();
					if ( value )
						Attach();
				}
			}
		}


	}
}
