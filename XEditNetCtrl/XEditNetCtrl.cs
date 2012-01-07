using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Timers;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;
using XEditNet.Keyboard;
using XEditNet.Layout;
using XEditNet.Licensing;
using XEditNet.Location;
using XEditNet.Styles;
using XEditNet.Undo;
using XEditNet.Util;
using XEditNet.Validation;
using XEditNet.Widgets;
using XEditNet.XenGraphics;
using XEditNet.XenGraphics.Native;
using IContainer = System.ComponentModel.IContainer;
using Timer = System.Timers.Timer;
using WhitespaceHandling = XEditNet.Location.WhitespaceHandling;
// TODO: H: when document end tag is part of selection, does not invalidate correctly
// TODO: L: backspace over tag doesn't always invalidate correctly - think fixed
// TODO: M: page up / page down is not full page
// TODO: H: float over invalid empty element doesn't show highlight
// TODO: C: vertical scroll resets hscroll pos

namespace XEditNet
{
	/// <summary>
	/// The main control for XEditNet.
	/// </summary>
	[LicenseProvider(typeof(XEditNetLicenseProvider))]
	public class XEditNetCtrl : UserControl, IMessageFilter, IUndoContextProvider, ICommandTarget
	{
		private IContainer components;
		private LayoutEngine layoutEngine;
		private XmlDocument doc;
		private Uri docUri;
//		private VScrollBar ScrollBarV;
//		private static readonly int SCROLLY_SMALL=4;
//		private static readonly int SCROLLY_LARGE=32;
		private Size currentSize;
		private bool disableEvents=false;
		private ToolTip tooltip;
		private Selection currentSelection=new Selection();
		private Selection newSelection=null;
		private ICaret caret;
		private Point caretLocation;
		private Point mouseDownPoint=Point.Empty;
		private bool mouseSelection=false;
		private Timer scrollTimer;
		private int scrollTimerIncrement;
		private ValidationManager validationManager=new ValidationManager();
		private UndoManager undoManager;
		private int linePosition=-1;
		private System.Windows.Forms.Timer reflowTimer;
		private Stylesheet stylesheet;
		private CommandMapper commandMapper;
		private bool licenceChecked;
		private bool licenceValid=false;
		private DateTime licenceExpiryDate;
		private SelectionManager selectionManager;
		private NoDocumentControl nodocWnd=null;
		private FindPopup findWindow;
		private IGraphicsFactory grFactory=new Win32GraphicsFactory();

		private BorderStyle borderStyle=BorderStyle.None;
		private ContextMenu quickFixMenu;
		private MenuItem menuQuickFixDelete;
		private MenuItem menuQuickFixWrap;
		private MenuItem menuQuickFixChange;
		private QuickFixIndicator quickFixIndicator;
		private FileInfo stylesheetFile;

		/// <summary>
		/// Occurs when the selection within the attached XmlDocument has changed.
		/// </summary>
		public event SelectionChangedEventHandler SelectionChanged;
		public event InterfaceActivationEventHandler InsertElementActivated;
		public event InterfaceActivationEventHandler ChangeElementActivated;
		public event InterfaceActivationEventHandler ChangeAttributesActivated;

		private int OffsetX
		{
			get { return -AutoScrollPosition.X; }
		}
		private int OffsetY
		{
			get { return -AutoScrollPosition.Y; }
			set { AutoScrollPosition=new Point(OffsetX, value); }
		}

		/// <summary>
		/// The main control for XEditNet applications.
		/// </summary>
		// TODO: D: add remarks section
		public XEditNetCtrl()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

//			if ( !this.DesignMode )
//			{
//				Application.EnableVisualStyles();
//				Application.DoEvents();
//			}

			SetStyle(ControlStyles.DoubleBuffer, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.ResizeRedraw, true);

			nodocWnd=new NoDocumentControl();
			nodocWnd.Parent=this;

			findWindow=new FindPopup(this);

			if ( this.DesignMode )
				return;

			caret=grFactory.CreateCaret(this);
			caret.Visible=true;
			quickFixIndicator=new QuickFixIndicator(this, caret);

			Stylesheet=new Stylesheet();
			undoManager=new UndoManager(this);
			commandMapper=CommandMapper.CreateInstance(this);
		}

		public void EditFind()
		{
			findWindow.Show();
		}

		/// <seebase/>
		protected override void OnLoad(EventArgs e)
		{
//			Console.WriteLine("XEditNetCtrl: OnLoad");

			if ( this.DesignMode )
				return;

			if ( !DoLicenseCheck(false) )
				Detach();

			base.OnLoad(e);

			this.AutoScroll=true;
//			this.AutoScrollMinSize=new Size(0, 0);
		}

		protected override CreateParams CreateParams
		{
			get
			{
//				VScroll=true;
				CreateParams cp=base.CreateParams;
//				cp.Style |= 0x200000;
				
				if ( borderStyle == BorderStyle.Fixed3D )
				{
					cp.ExStyle &= ~512; 
					cp.Style &= ~8388608 /*WS_BORDER*/; 
					cp.ExStyle = cp.ExStyle | 512 /*WS_EX_DLGFRAME*/; 
				}
				return cp;
			}
		}

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 276:
                    //					Console.WriteLine("HSCROLL!");
                    break;
                case 277:
                    ScrollVertical(ref m);
                    return;
            }

            base.WndProc(ref m);
        }

		private void ScrollVertical(ref Message m)
		{
			Rectangle rc;
			int offset;
			int maxValue;
			ScrollEventType eventType;

			if ( m.LParam != IntPtr.Zero) 
			{
				base.WndProc(ref m);
				return;
			}

			eventType = (ScrollEventType) (m.WParam.ToInt32() & 0xFFFF);

			int dy=(int) (1.0 * VisibleRectangle.Height / 10);
			if ( dy < 5 )
				dy=5;

			rc = this.ClientRectangle;
			offset = - AutoScrollPosition.Y;
			int newOffset;
			maxValue = this.DisplayRectangle.Height - rc.Height;
//			Console.WriteLine("VSCROLL: {0}", eventType);
			switch (eventType) 
			{
				case ScrollEventType.ThumbPosition:
					goto case ScrollEventType.ThumbTrack;
				case ScrollEventType.ThumbTrack:
					base.WndProc(ref m);
					ResetCaretPosition();
					return;
				case ScrollEventType.SmallDecrement:
					Logger.Log("SmallDecrement {0}, {1}", DateTime.Now.Millisecond, SystemInformation.DoubleClickTime);

					newOffset=offset;
					if ( offset > dy ) 
						newOffset=offset-dy;
					else
						newOffset=0;

					offset-=1;
					while ( offset > newOffset+4 )
					{
						AutoScrollPosition=new Point(DisplayRectangle.X, offset);
						offset-=4;
						Update();
					}
					offset=newOffset;
					break;

				case ScrollEventType.SmallIncrement:
                    Logger.Log("SmallIncrement {0}, {1}", DateTime.Now.Millisecond, SystemInformation.DoubleClickTime);

                    newOffset = offset;
					if ( offset < maxValue - dy ) 
						newOffset = offset + dy;
					else
						newOffset = maxValue;

					offset+=1;
					while ( offset < newOffset-4 )
					{
						AutoScrollPosition=new Point(DisplayRectangle.X, offset);
						offset+=4;
						Update();
					}
					offset=newOffset;
					break;
					
				case ScrollEventType.LargeDecrement:
                    Logger.Log("LargeDecrement {0}, {1}", DateTime.Now.Millisecond, SystemInformation.DoubleClickTime);

                    newOffset = offset;
					if ( offset > rc.Height ) 
						newOffset=offset-rc.Height;
					else
						newOffset=0;

					offset-=dy;
					while ( offset > newOffset )
					{
						AutoScrollPosition=new Point(DisplayRectangle.X, offset);
						offset-=dy;
						Update();
					}
					offset=newOffset;
					break;
				case ScrollEventType.LargeIncrement:
                    Logger.Log("LargeIncrement {0}, {1}", DateTime.Now.Millisecond, SystemInformation.DoubleClickTime);

                    newOffset = offset;
					if ( offset < maxValue - rc.Height ) 
						newOffset = offset + rc.Height;
					else
						newOffset = maxValue;

					offset+=dy;
					while ( offset < newOffset )
					{
						AutoScrollPosition=new Point(DisplayRectangle.X, offset);
						offset+=dy;
						Update();
					}
					offset=newOffset;
					break;
				case ScrollEventType.First:
					offset = 0;
					break;
				case ScrollEventType.Last:
					offset = maxValue;
					break;
				case ScrollEventType.EndScroll:
					// nothing to do
					break;
				default:
//					Console.WriteLine("Nothing to do, event is {0}!", eventType);
					break;

			}
			AutoScrollPosition=new Point(DisplayRectangle.X, offset);
//			UpdateCaretPosition();

			
			//			if ( this.GetScrollState(16) || eventType != ScrollEventType.ThumbTrack ) 
//			{
//				this.SetScrollState(8, true);
//				this.SetDisplayRectLocation(-offset, this.DisplayRectangle.Y);
////				this.SyncScrollbars();
//			}
		}

		public BorderStyle BorderStyle
		{
			get { return borderStyle; }
			set
			{
				if ( borderStyle == value )
					return;

				borderStyle=value;
				UpdateStyles();
				RecreateHandle();
			}
		}

		private void ValidateLicense()
		{
//			Console.WriteLine("XEditNetCtrl: ValidateLicense");

			licenceChecked=true;
			licenceValid=false;
			licenceExpiryDate=DateTime.MinValue;

			XEditNetLicence license=(XEditNetLicence) LicenseManager.Validate(typeof(XEditNetCtrl),this);
			switch (license.Validity)
			{
				case LicenseState.Trial_Active:
					licenceValid=true;
					break;

				case LicenseState.Full:
					licenceValid=true;
					break;

				case LicenseState.Trial_Expired:
					licenceExpiryDate=license.ExpiryDate;
					break;

				case LicenseState.Invalid:
				case LicenseState.None:
					break;
			}

		}

		/// <summary>
		/// Gets an array containing all validation errors in the current document.
		/// </summary>
		public ValidationError[] Errors
		{
			get { 
				return validationManager.Errors; 
			}
		}

		/// <summary>
		/// Gets a value indicating if a DTD is associated with the current document.
		/// </summary>
		public bool HasDtd
		{
			get
			{
				return validationManager.HasElements;
			}
		}

		internal Stylesheet Stylesheet
		{
			set 
			{
				if ( value != null )
				{
					stylesheet=value;
					selectionManager=new SelectionManager(stylesheet);
				}
			}
			get 
			{
				return stylesheet;
			}
		}

		/// <summary>
		/// Inserts an element.
		/// </summary>
		/// <param name="e">The XmlElement to insert.</param>
		/// <remarks>
		/// <para>If the current selection is a balanced range, the new element will wrap the
		/// content of the range. Otherwise the new element is inserted at the caret and the caret
		/// is positioned within the new element.</para>
		/// </remarks>
		public void Insert(XmlElement e)
		{
			CreateUndoPoint();

			Selection sel=currentSelection;
			if ( currentSelection.IsRange && currentSelection.IsBalanced )
			{
				sel=selectionManager.Wrap(currentSelection, e);
			}
			else
			{
				selectionManager.Insert(sel.Start, e);
				ElementSelectionPoint esp=new ElementSelectionPoint(e, TagType.EndTag);
				sel=new Selection(esp);
			}

			if ( e.ParentNode is XmlElement )
				// invalidate the parent node we've been inserted into
				Invalidate((XmlElement) e.ParentNode);
			else
				// this is document node so reflow all, primarily to
				// ensure caret is displayed correctly
				Reflow();

			SetSelection(sel);
			UpdateCaretPosition(true);
			EnsureCaretVisible();

			CreateUndoPoint();
		}

		private void Change(XmlElement c, XmlElement e)
		{
			CreateUndoPoint();

			Selection sel = SelectionManager.Change(currentSelection, c, e);

			SetSelection(sel);
			UpdateCaretPosition(true);
			EnsureCaretVisible();
		}

		/// <seebase/>
		protected override bool IsInputKey(Keys keyData)
		{
			Keys key=(Keys) (Int16) keyData;
			if ( key == Keys.Tab )
				return false;

			// TODO: M: Think about the TAB key as this is the only one in doubt
			return true;
		}

		protected override bool IsInputChar(char charCode)
		{
			if ( charCode == '\t' )
				return base.IsInputChar(charCode);

			return true;
		}

		private Rectangle VisibleRectangle 
		{
			get { return ClientRectangle; }
		}

		private Rectangle LogicalRectangle
		{
			get
			{
				Rectangle rc=ClientRectangle;
				rc.Offset(OffsetX, OffsetY);
				return rc;
			}
//			{
//				Rectangle rc=VisibleRectangle;
//				rc.Offset(OffsetX, OffsetY);
//				return rc;
//			}
		}

		/// <summary>
		/// Gets or sets the current selection.
		/// </summary>
		/// <remarks>See "Selection" in the developers guide for more information 
		/// about selections in XEditNet.</remarks>
		public Selection Selection
		{
			get 
			{ 
				return currentSelection;
			}
			set 
			{
				SetSelection(value);
				if ( !currentSelection.IsRange )
					UpdateCaretPosition(false);
			}
		}

		private void SetSelection(Selection selection)
		{
			if ( layoutEngine == null )
				return;

			if ( currentSelection.Equals(selection) )
				return;

			Selection prevSel=currentSelection;

			if ( selection == null )
				currentSelection=new Selection();
			else
				currentSelection=selection;

			Rectangle rcOld=layoutEngine.SelectionBounds;
			if ( !rcOld.IsEmpty )
			{
				rcOld.Offset(AutoScrollPosition);
				Invalidate(rcOld, true);
			}

			SelectionPoint caretPos;

			if ( currentSelection.IsRange )
			{
				layoutEngine.Selection=currentSelection;

				Rectangle rcNew=layoutEngine.SelectionBounds;
				if ( !rcNew.IsEmpty )
				{
					rcNew.Offset(AutoScrollPosition);
					Invalidate(rcNew, true);
				}
				caretPos=currentSelection.End;
				caret.Visible=false;
			} 
			else
			{
				// this simply tells layout engine that nothing is highlighted
				layoutEngine.Selection=Selection.Empty;
				caretPos=currentSelection.Start;
				if ( caretPos != null && caretPos.IsElement )
					Invalidate((XmlElement) caretPos.Node, false);

				caret.Visible=true;
			}

			OnSelectionChanged(prevSel, currentSelection);
		}

		// TODO: L: rationalise - too many overloads with different intentions
		private void UpdateCaretPosition()
		{
			UpdateCaretPosition(false, CaretDirection.None);
		}

		private void UpdateCaretPosition(bool resetX)
		{
			UpdateCaretPosition(resetX, CaretDirection.None);
		}

		private void UpdateCaretPosition(bool resetX, CaretDirection d)
		{
			if ( currentSelection.IsEmpty )
				return;

			SelectionPoint sp=currentSelection.IsRange ? currentSelection.End : currentSelection.Start;
			UpdateCaretPosition(sp, resetX, d);
		}

		private void UpdateCaretPosition(Rectangle rc, bool resetX)
		{
			if ( !rc.IsEmpty )
			{
				SetCaretPosition(rc.Location, rc.Height);

				if ( resetX )
					linePosition=rc.Left;
			} else
				UpdateCaretPosition(resetX);
		}

		private void UpdateCaretPosition(SelectionPoint sp, bool resetX)
		{
			UpdateCaretPosition(sp, resetX, CaretDirection.None);
		}

		private void UpdateCaretPosition(SelectionPoint sp, bool resetX, CaretDirection d)
		{
			Rectangle rc=Rectangle.Empty;
			if ( layoutEngine != null )
			{
				using ( IGraphics gr=grFactory.CreateGraphics(this) )
					rc=layoutEngine.GetCaretPosition(gr, sp, d);
			}
			if ( !rc.IsEmpty )
			{
				SetCaretPosition(rc.Location, rc.Height);

				if ( resetX )
					linePosition=rc.Left;

				caret.Visible=!currentSelection.IsRange;
				return;
			} 

			// TODO: M: not sure why caret does not update when window resizing
			caret.Visible=false;
			Logger.Log("WARN: failed to get caret position");
		}

		/// <summary>
		/// Raises the SelectionChanged event.
		/// </summary>
		/// <param name="oldSel">The previous selection.</param>
		/// <param name="newSel">The new selection.</param>
		protected virtual void OnSelectionChanged(Selection oldSel, Selection newSel)
		{
			SelectionChangedEventArgs slea=new SelectionChangedEventArgs();
			slea.OldSelection=oldSel;
			slea.NewSelection=newSel;

			if ( SelectionChanged != null )
				SelectionChanged(this, slea);

		}

		private void Reflow()
		{
//			Console.WriteLine("XEditNetCtrl: Reflow");

			if ( doc == null || doc.DocumentElement == null )
				return;

			LayoutEngine le=new LayoutEngine(stylesheet);

			Size sz=new Size();

//			Console.WriteLine("Reflow: Create graphics from control");
			using ( IGraphics gr=grFactory.CreateGraphics(this) )
			{
				Rectangle rc=VisibleRectangle;
				rc.Height=0;

				DrawContext dc=new DrawContext(gr, new Point(-OffsetX, -OffsetY), rc, rc, validationManager.InvalidNodes, validationManager.DocumentType, currentSelection.Start);
				sz=le.Reflow(dc, doc.DocumentElement);
			}

			layoutEngine=le;

			if ( currentSelection.IsRange )
			{
				layoutEngine.Selection=currentSelection;
				Invalidate(true);
			}

			UpdateCaretPosition();
			ResetScrollbars(sz);
		}

		private bool ResetScrollbars(Size sz)
		{
			if ( sz.Width <= ClientRectangle.Width )
				sz.Width=0;

			if ( AutoScrollMinSize.Height == sz.Height && AutoScrollMinSize.Width == sz.Width )
				return false;

			AutoScrollMinSize=new Size(sz.Width, sz.Height);
			return true;
		}

		/// <summary>
		/// Gets the current XmlDocument attached to this control.
		/// </summary>
		public XmlDocument Document 
		{
			get 
			{
				return doc;
			}
		}

		/// <summary>
		/// Detaches the current XmlDocument, if any, from the control.
		/// </summary>
		/// <remarks>
		/// Use this method to remove any association between the XmlDocument attached to the control
		/// and the control. All events handlers are detached and so on. You might use this method before
		/// making extensive changes to the XmlDocument due to the overhead of undo/redo support and
		/// other change tracking performed by XEditNet.
		/// </remarks>
		public void Detach()
		{
			if ( doc == null )
				return;

			// TODO: M: this isn't working - when undo over new doc element
			caret.Visible=false;

			doc.NodeChanged-=new XmlNodeChangedEventHandler(NodeChanged);
			doc.NodeInserted-=new XmlNodeChangedEventHandler(NodeInserted);
			doc.NodeRemoved-=new XmlNodeChangedEventHandler(NodeRemoved);
			undoManager.Detach();
			validationManager.Detach();

			SetSelection(Selection.Empty);

			doc=null;
			docUri=null;
			layoutEngine=null;
			tooltip.Active=false;
			Invalidate(true);

			nodocWnd.Visible=true;
			nodocWnd.ShowStartMessage=true;

			return;
		}

		private void GetStylesheetFromPi()
		{
			XmlProcessingInstruction stylePi=doc.SelectSingleNode(
				"processing-instruction('xeditnet-style')") as XmlProcessingInstruction;

			if ( stylePi != null )
				stylesheetFile=FileUtils.FindFile(stylePi.Value, docUri);
		}

		public void SetStylesheet(string filename)
		{
			stylesheetFile=FileUtils.FindFile(filename, docUri);
		}

		private bool DoLicenseCheck(bool initMode)
		{
			if ( !licenceChecked )
			{
				if ( initMode )
					// this allows attach to happen before OnLoad, ensuring that
					// window exists before attempting to show a licence dialog
					return true;

				ValidateLicense();
			}

			while ( !licenceValid )
			{
				ActivationDialog dlg=new ActivationDialog();
				dlg.TrialExpiryDate=licenceExpiryDate;
				DialogResult ret=dlg.ShowDialog(this.ParentForm);
				if ( ret == DialogResult.OK )
					ValidateLicense();
				else
					return false;
			}
			return true;
		}

		/// <summary>
		/// Attach an XmlDocument to this control.
		/// </summary>
		/// <param name="doc">The XmlDocument to attach.</param>
		/// <param name="valid">Indicates whether the document is valid according to its DTD.</param>
		/// <remarks>
		/// <para>This method detaches any existing XmlDocument and 
		/// attaches the new document. If the valid parameter is set to <b>false</b>,
		/// the document is validated according to the DTD, if present, so that any invalid
		/// nodes are highlighted. This can take a little time for very large documents, so
		/// if possible only valid documents should be attached. However, this feature allows
		/// the loading of well-formed but invalid documents into the control.</para>
		/// </remarks>
		public void Attach(XmlDocument doc, bool valid)
		{
			if ( !DoLicenseCheck(true) )
				return;

			Detach();

			if ( Handle.Equals(IntPtr.Zero) )
				return;

			if ( doc == null )
				return;

			nodocWnd.Visible=false;

			this.doc=doc;
			if ( doc.BaseURI.Length > 0 )
                docUri=new Uri(doc.BaseURI);

			try 
			{
				GetStylesheetFromPi();
				FileReloadStylesheet();
				undoManager.Attach(doc);
				validationManager.Attach(doc, null);
				if ( !valid )
					validationManager.ValidateAll();
			}
			catch ( Exception )
			{
				// clean up
				Detach();
				throw;
			}

			doc.NodeChanged+=new XmlNodeChangedEventHandler(NodeChanged);
			doc.NodeInserted+=new XmlNodeChangedEventHandler(NodeInserted);
			doc.NodeRemoved+=new XmlNodeChangedEventHandler(NodeRemoved);

			tooltip.Active=true;

			layoutEngine=new LayoutEngine(stylesheet);
			Reflow();

			if ( doc.DocumentElement != null )
			{
				SelectionPoint sel=new ElementSelectionPoint(doc.DocumentElement, TagType.StartTag);
				sel=selectionManager.NextSelectionPoint(sel);
				SetSelection(new Selection(sel));
			}
			UpdateCaretPosition();

			Invalidate(true);
		}

		private void NodeInserted(object sender, XmlNodeChangedEventArgs e)
		{
			if ( disableEvents )
				return;

			XmlElement elem=e.NewParent as XmlElement;
			if ( elem == null )
			{
				elem=XmlUtil.GetParentNode(e.NewParent) as XmlElement;
				if ( elem == null )
					return;
			}

			// TODO: M: invalidate old parent - only really needed when drag-drop implemented
			Invalidate(elem);
		}

		private void NodeRemoved(object sender, XmlNodeChangedEventArgs e)
		{
			if ( disableEvents )
				return;

			switch ( e.OldParent.NodeType )
			{
				case XmlNodeType.DocumentFragment:
					break;

				case XmlNodeType.Element:
					Invalidate((XmlElement) e.OldParent);
					break;

				case XmlNodeType.Attribute:
					// parent of deleted node is attribute,
					// so go up to parent
					XmlElement elem=e.OldParent.ParentNode as XmlElement;
					Invalidate(elem);
					break;
			}
		}

		private void NodeChanged(object sender, XmlNodeChangedEventArgs e)
		{
			if ( disableEvents )
				return;

			switch ( e.Node.NodeType )
			{
				case XmlNodeType.Text:
				case XmlNodeType.Attribute:
					XmlElement elem=e.Node.ParentNode as XmlElement;
					Invalidate(elem);
					break;

				case XmlNodeType.CDATA:
				case XmlNodeType.Comment:
					elem=e.Node.ParentNode as XmlElement;
					Invalidate(elem);
					break;

			}
			// TODO: M: optimise - don't need to do this - deal with attributes correctly
			Invalidate();
		}

		internal HitTestInfo HitTest(Point pt, bool offset)
		{
			if ( layoutEngine == null )
				return null;

			if ( offset )
				pt.Offset(OffsetX, OffsetY);

			using ( IGraphics gr=grFactory.CreateGraphics(this) )
				return layoutEngine.GetHitTestInfo(gr, pt);
		}

		private void Invalidate(XmlElement elem)
		{
			Invalidate(elem, true);
		}

		private void InvalidateRegion(XmlElement elem)
		{
			if ( layoutEngine ==null )
				return;

			Rectangle rc=layoutEngine.GetBoundingRect(elem);
			rc.Offset(AutoScrollPosition); // changed
			Invalidate(rc);
		}

		private void Invalidate(XmlElement elem, bool forceReflow)
		{
//			Console.WriteLine("XEditNetCtrl: Invalidate");

			if ( layoutEngine == null || elem == null || elem.ParentNode == null )
				// element is not in document
				return;

			if ( !forceReflow )
			{
				InvalidateRegion(elem);
				return;
			}

			Rectangle rc=VisibleRectangle;
			rc.Height=0;

			Size sz=new Size();

			Rectangle invalidRect;

			using ( IGraphics gr=grFactory.CreateGraphics(this) )
			{
				DrawContext dc=new DrawContext(gr, new Point(-OffsetX, -OffsetY), rc, Rectangle.Empty, validationManager.InvalidNodes, validationManager.DocumentType, currentSelection.Start);
				sz=layoutEngine.Invalidate(dc, elem, stylesheet, out invalidRect);
			}

			if ( !invalidRect.IsEmpty )
			{
				invalidRect.Offset(AutoScrollPosition); // changed

				// TODO: M: don't quite understand this
				if ( ResetScrollbars(sz) )
					invalidRect=VisibleRectangle;

				Invalidate(invalidRect, true);
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if( components != null )
					components.Dispose();
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
			this.tooltip = new System.Windows.Forms.ToolTip(this.components);
			this.scrollTimer = new System.Timers.Timer();
			this.reflowTimer = new System.Windows.Forms.Timer(this.components);
			this.quickFixMenu = new System.Windows.Forms.ContextMenu();
			this.menuQuickFixDelete = new System.Windows.Forms.MenuItem();
			this.menuQuickFixWrap = new System.Windows.Forms.MenuItem();
			this.menuQuickFixChange = new System.Windows.Forms.MenuItem();
			((System.ComponentModel.ISupportInitialize)(this.scrollTimer)).BeginInit();
			// 
			// tooltip
			// 
			this.tooltip.AutomaticDelay = 0;
			// 
			// scrollTimer
			// 
			this.scrollTimer.SynchronizingObject = this;
			this.scrollTimer.Elapsed += new System.Timers.ElapsedEventHandler(this.ScrollTimerFire);
			// 
			// reflowTimer
			// 
			this.reflowTimer.Interval = 200;
			this.reflowTimer.Tick += new System.EventHandler(this.reflowTimer_Tick);
			// 
			// quickFixMenu
			// 
			this.quickFixMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																						 this.menuQuickFixChange,
																						 this.menuQuickFixWrap,
																						 this.menuQuickFixDelete});
			// 
			// menuQuickFixDelete
			// 
			this.menuQuickFixDelete.Index = 2;
			this.menuQuickFixDelete.Text = "&Delete";
			// 
			// menuQuickFixWrap
			// 
			this.menuQuickFixWrap.Index = 1;
			this.menuQuickFixWrap.Text = "&Surround With";
			// 
			// menuQuickFixChange
			// 
			this.menuQuickFixChange.Index = 0;
			this.menuQuickFixChange.Text = "&Change To";
			// 
			// XEditNetCtrl
			// 
			this.BackColor = System.Drawing.Color.White;
			this.Name = "XEditNetCtrl";
			this.Size = new System.Drawing.Size(376, 248);
			((System.ComponentModel.ISupportInitialize)(this.scrollTimer)).EndInit();

		}
		#endregion

		/// <seebase/>
		protected override void OnPaint(PaintEventArgs e)
		{
			if ( this.DesignMode )
				return;

			using ( Brush b=new SolidBrush(BackColor) )
			{
				e.Graphics.FillRectangle(b, e.ClipRectangle);
			}

			if ( doc == null )
				return;

			Rectangle main=VisibleRectangle;
			main.Offset(OffsetX, OffsetY);

			Rectangle clip=e.ClipRectangle;
			clip.Offset(OffsetX, OffsetY);

			using ( IGraphics gr=grFactory.CreateGraphics(e.Graphics) )
			{
//				Console.WriteLine("XEditNetCtrl: OnPaint {0}, {1}", clip, main);

				DrawContext ctx=new DrawContext(gr, new Point(-OffsetX, -OffsetY), main, clip, validationManager.InvalidNodes, validationManager.DocumentType, currentSelection.Start);

				// TODO: -: uncomment to enable constant reflow
				//			layoutEngine.Reflow(ctx, doc.DocumentElement);
				layoutEngine.Draw(ctx, doc.DocumentElement);
				Rectangle bounds=layoutEngine.BoundingRect;

				if ( layoutEngine.ReflowComplete )
					ResetScrollbars(bounds.Size);
			}
			base.OnPaint(e);
		}

//		protected new void SetScrollState(int bit, bool value)
//		{
//			base.SetScrollState(bit, value);
//			base.SetScrollState(4, true);
//		}

//		protected new bool VScroll
//		{
//			get { return true; }
//			set { base.VScroll=true; }
//		}

		/// <seebase/>
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);

			if ( Size.Width == 0 )
			{
				// minimise
				Console.WriteLine("Resize not actioned - width is 0");
				return;
			}

			if ( currentSize.Width != ClientRectangle.Width )
			{
				if ( caret != null )
					caret.Visible=false;

//				Logger.Log("Recreating layoutEngine");
				layoutEngine=new LayoutEngine(stylesheet);
				reflowTimer.Stop();
				reflowTimer.Start();
			}
//			else if ( layoutEngine != null )
//			{
//				ResetScrollbars(layoutEngine.BoundingRect.Size);
//			}

			currentSize=ClientRectangle.Size;
			if ( nodocWnd != null )
				nodocWnd.AutoSize();
		}

		// x
//		private void ScrollBar_Scroll(object sender, ScrollEventArgs e)
//		{
//			if ( e.Type == ScrollEventType.EndScroll )
//				return;
//
//			int offset=ScrollBarV.Value-e.NewValue;
//			if ( offset == 0 )
//				return;
//
//			if ( e.Type == ScrollEventType.ThumbTrack )
//			{
//				offsetY=e.NewValue;
//				ResetCaretPosition();
//				Invalidate(true);
//				return;
//			}
//
//			int smoothScrollDistance=SCROLLY_LARGE;
//			switch ( e.Type )
//			{
//				case ScrollEventType.LargeDecrement:
//					smoothScrollDistance=SCROLLY_LARGE;
//					break;
//				case ScrollEventType.LargeIncrement:
//					smoothScrollDistance=SCROLLY_LARGE;
//					break;
//				case ScrollEventType.SmallDecrement:
//					smoothScrollDistance=SCROLLY_SMALL;
//					break;
//				case ScrollEventType.SmallIncrement:
//					smoothScrollDistance=SCROLLY_SMALL;
//					break;
//
//				default:
//					Logger.Log("Unrecognised scroll type: {0}", e.Type);
//					break;
//			}
//
//			Rectangle rcOut=Rectangle.Empty;
//			Rectangle rcClip=VisibleRectangle;
//
//			int absOffsetY=Math.Abs(offset);
//			int increment=Math.Sign(offset) * smoothScrollDistance;
//
//			while ( absOffsetY >= smoothScrollDistance )
//			{
//				offsetY-=increment;
//				Win32Util.ScrollWindow(this, 0, increment, rcClip, ref rcOut, 0);
//				Invalidate(rcOut, true);
//				Update();
//
//				absOffsetY-=smoothScrollDistance;
//			}
//
//			offsetY=e.NewValue;
//
//			if ( absOffsetY > 0 )
//			{
//				increment=Math.Sign(offset) * absOffsetY;
//				Win32Util.ScrollWindow(this, 0, increment, rcClip, ref rcOut, 0);
//				Invalidate(rcOut, true);
//			}
//			ResetCaretPosition();
//		}

//		public XmlNode GetNodeUnderPoint(Point pt)
//		{
//			if ( !VisibleRectangle.Contains(pt) )
//				return null;
//
//			pt.Offset(offsetX, offsetY);
//			WaitForReflow();
//
//			if ( layoutEngine == null )
//				return null;
//
//			return layoutEngine.GetNodeUnderPoint(pt);
//		}

		/// <seebase/>
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			base.OnKeyPress(e);

			if ( Char.IsControl(e.KeyChar) )
				return;

			InsertChar(e.KeyChar);
		}

		protected void InsertChar(char c)
		{
			Selection sel=currentSelection.Normalise();
			if ( sel.IsEmpty || (sel.IsRange && !sel.IsBalanced) )
				// selection must be point or balanced range
				return;

			if ( sel.IsRange )
				sel=selectionManager.Backspace(sel);

			SelectionPoint sp=selectionManager.Insert(sel.Start, new string(new char[] {c}));
			SetSelection(new Selection(sp));
			UpdateCaretPosition(true);
			EnsureCaretVisible();
		}

		private SelectionPoint GetSelectionPoint(Point pt)
		{
			Rectangle rc=Rectangle.Empty;
			return GetSelectionPoint(pt, ref rc);
		}

		private SelectionPoint GetSelectionPoint(Point pt, ref Rectangle caret)
		{
			if ( layoutEngine == null )
				return null;

			if ( pt.Y < 0 )
				pt.Y=0;

			if ( pt.Y >= layoutEngine.BoundingRect.Bottom )
				pt.Y = layoutEngine.BoundingRect.Bottom - 1;

			SelectionPoint sp=null;

			HitTestInfo ht=HitTest(pt, false);
			if ( ht != null )
			{
				sp=ht.SelectionPoint;
				caret=ht.Caret;
				if ( ht.After )
					sp=selectionManager.NextSelectionPoint(sp);

				if ( !sp.IsValid )
					Logger.Log("WARN: Invalid selection point!");

				sp=selectionManager.Validate(sp);
			}
			return sp;
		}

		private bool MouseBeyondClickTolerance(int x, int y)
		{
			if ( mouseDownPoint.IsEmpty )
				return false;

			int dx=mouseDownPoint.X - x;
			int dy=mouseDownPoint.Y - y;

			double distance=Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));

			return distance > 3;
		}

		private void RemoveQuickFixIndicator()
		{
			quickFixIndicator.Hide();
		}

		/// <seebase/>
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if ( doc == null || layoutEngine == null )
				return;

			if ( mouseSelection || MouseBeyondClickTolerance(e.X, e.Y) )
			{
				mouseSelection=true;
				MouseMoveSelection();
				return;
			}

			Point pt=new Point(e.X, e.Y);
			if ( pt.Y > layoutEngine.BoundingRect.Bottom )
				pt.Y = layoutEngine.BoundingRect.Bottom-1;

			HitTestInfo ht=HitTest(pt, true);

			XmlNode n=ht == null ? null : ht.SelectionPoint.Node;
			if ( n == null )
			{
				Cursor=Cursors.Default;
				RemoveQuickFixIndicator();
				return;
			}

			ICollection errors=validationManager.GetErrorDetails(n);

			if ( errors.Count == 0 || (ht.SelectionPoint.IsTag && !ht.SelectionPoint.IsAtStart) )
			{
				tooltip.Active=false;
				RemoveQuickFixIndicator();
				SetCursorType(ht);
				return;
			}

			// TODO: M: clean up this interface
//			bool quickFix=validationManager.InvalidNodes.HasQuickFix(n);
			bool buttonMode=false;
//			if ( quickFix )
//			{
				Point p=ht.LineItemContext.Location;
				// TODO: M: factor this into helper method, ie. LayoutEngineToClient
				p.Offset(AutoScrollPosition.X, AutoScrollPosition.Y);

				buttonMode=quickFixIndicator.Update(n, p, pt);
//			}
//			else 
//				quickFixIndicator.Hide();
			
			if ( buttonMode )
			{
				Cursor=Cursors.Default;
				tooltip.Active=false;
				return;
			}

			SetCursorType(ht);

			string msg="";
			bool first=true;
			foreach ( ValidationError ve in errors )
			{
				if ( !first )
					msg+="\n";

				first=false;
				msg+=ve.Message;
			}

			tooltip.SetToolTip(this, msg);
			tooltip.Active=true;
		}

		/// <summary>
		/// Loads a document into the control from a file path or URL.
		/// </summary>
		/// <param name="uriString">The file path or URL to load.</param>
		/// <param name="allowWellFormed">Indicates whether to allow well-formed but invalid documents.</param>
		/// <remarks>
		/// <para>If allowWellFormed is true, a message box is displayed when an
		/// invalid document is read, asking if the user wants to attempt to load
		/// the document as well-formed.</para>
		/// </remarks>
		public void LoadDocument(string uriString, bool allowWellFormed)
		{
			bool isValid;
			XmlDocument doc=LoadDocument(uriString, allowWellFormed, out isValid);
			if ( doc == null )
				return;

			Attach(doc, isValid);
		}

		/// <summary>
		/// Loads a document from a file path or URL.
		/// </summary>
		/// <param name="uriString">The file path or URL to load.</param>
		/// <param name="allowWellFormed">Indicates whether to allow well-formed but invalid documents.</param>
		/// <param name="isValid">[out] Set to true if document is valid.</param>
		/// <returns>The XmlDocument that was loaded, or null if unsuccessful.</returns>
		/// <remarks>
		/// <para>If allowWellFormed is true, a message box is displayed when an
		/// invalid document is read, asking if the user wants to attempt to load
		/// the document as well-formed.</para>
		/// </remarks>
		public static XmlDocument LoadDocument(string uriString, bool allowWellFormed, out bool isValid)
		{
			XmlDocument doc=new XmlDocument();
			isValid=true;

			Uri uri=new Uri(uriString);
			XmlTextReader xtr=new XmlTextReader(uriString);
			XmlResolver xr=new CustomXmlResolver(uri);
			xtr.XmlResolver=xr;
			doc.XmlResolver=xr;
			try
			{
				XmlValidatingReader xvr=new XmlValidatingReader(xtr);
				xvr.EntityHandling=EntityHandling.ExpandCharEntities;
				doc.Load(xvr);
				return doc;
			}
			catch ( XmlSchemaException e )
			{
				isValid=false;
				string msg=string.Format("The document is not valid. Do you want to attempt to load the document as well-formed?\n{0}", e.Message);
				DialogResult res=MessageBox.Show(msg, "Open File", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
				if ( res != DialogResult.OK )
					throw;
			}
			catch ( FileNotFoundException e )
			{
				Uri errorFile=new Uri(e.FileName);
				if ( errorFile.Equals(uri) )
					throw;

				// TODO: M: refactor (repeat of above) - but is different
				isValid=false;
				string msg=string.Format("One or more files that this document depends on could not be found. Do you want to attempt to load the document as well-formed?\n{0}", e.Message);
				DialogResult res=MessageBox.Show(msg, "Open File", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
				if ( res != DialogResult.OK )
					throw;

				xr=null; // do not attempt to resolve entities
			}
			finally
			{
				xtr.Close();
			}
			
			try 
			{
				xtr=new XmlTextReader(uriString);
				xtr.XmlResolver=xr;
				doc.Load(xtr);
				return doc;
			}
			finally
			{
				xtr.Close();
			}
		}

		/// <seebase/>
		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);

			Cursor=Cursors.Default;
		}

		/// <summary>
		/// Creates an undo point.
		/// </summary>
		/// <remarks>Use this method if you are changing the XmlDocument document outside
		/// of the control but want to allow the user to undo to the point just before the
		/// change. The current caret is automatically saved with the undo point.</remarks>
		public void CreateUndoPoint()
		{
			undoManager.Mark(currentSelection);
		}

		private SelectionPoint GetSelectionPointUnderCursor()
		{
			Rectangle rc=Rectangle.Empty;
			return GetSelectionPointUnderCursor(ref rc);
		}

		private SelectionPoint GetSelectionPointUnderCursor(ref Rectangle caretPos)
		{
			return GetSelectionPoint(CursorPosition, ref caretPos);
		}

		private Point CursorPosition
		{
			get 
			{
				Point pt=PointToClient(Cursor.Position);
				pt.Offset(OffsetX, OffsetY);
				return pt;
			}
		}

		/// <seebase/>
		protected override void OnDoubleClick(EventArgs e)
		{
			base.OnDoubleClick(e);

			Rectangle caretPos=Rectangle.Empty;
			SelectionPoint sp=GetSelectionPointUnderCursor(ref caretPos);
			if ( sp == null )
				return;

			// TODO: M: this is a bit odd when markup is selected

			Selection sel=selectionManager.SelectWord(sp);
			SetSelection(sel);
			UpdateCaretPosition(true);
			EnsureCaretVisible();
		}

		/// <seebase/>
		protected override void OnClick(EventArgs e)
		{
			// TODO: M: think about whether this should be before or after
			base.OnClick(e);

			if ( mouseSelection )
				return;

			Point pt=Cursor.Position;
			pt=PointToClient(pt);
			if ( quickFixIndicator.Click(pt) )
			{
				ShowQuickFixMenu();
				return;
			}

			Rectangle caretPos=Rectangle.Empty;
			SelectionPoint sp=GetSelectionPointUnderCursor(ref caretPos);
			if ( sp == null )
				return;
			
			if ( !sp.IsValid )
				sp=selectionManager.NextSelectionPoint(sp);

			if ( newSelection == null || (ModifierKeys & Keys.Shift) != Keys.Shift )
			{
				newSelection=new Selection(sp);
			}
			else if ( newSelection.IsRange && (ModifierKeys & Keys.Shift) != Keys.Shift )
			{
				// TODO: M: this doesn't look right
				newSelection=null;
				return;
			}
			else
				newSelection=new Selection(newSelection.Start, sp);

			if ( !newSelection.IsRange )
				StartSelection(newSelection.Start);
			else
			{
				SetSelection(newSelection);
			}

			// this handles case when cursor is beyond end of line
			UpdateCaretPosition(caretPos, true);
		}

		private void StartSelection(SelectionPoint sp)
		{
			CreateUndoPoint();

			Selection sel=new Selection(selectionManager.Validate(sp), null);
			SetSelection(sel);
			UpdateCaretPosition(true);
			EnsureCaretVisible();
		}

		/// <seebase/>
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			// TODO: H: review this - is it right to grab all keys
			//			previously pneumonics for dialog controls were not being captured
//			return true;

			if ( commandMapper.Invoke(keyData) )
				return true;

			return base.ProcessCmdKey(ref msg, keyData);
		}

//		private void CenterCaret()
//		{
//			Point pt=caretLocation;
//			Logger.Log("Scrolling to center offsetY={0}, caretLocation.Y={1}, height={2}", offsetY,
//				caretLocation.Y, VisibleRectangle.Height);
//			int scrollAmount = offsetY - caretLocation.Y + VisibleRectangle.Height / 2;
//			Logger.Log("Scrolling to center cursor {0}", scrollAmount);
//			ScrollWindow(0, scrollAmount);
//		}
//

		private void EnsureCaretVisible()
		{
//			if ( currentSelection.IsRange )
//				// making a big selection shouldn't really scroll the window
//				return;

			Point pt=caretLocation;
			Rectangle rc=LogicalRectangle;
			int scrollX=0;
			int scrollY=0;

			if ( pt.Y+caret.Height > rc.Bottom )
			{
				int scrollAmount=pt.Y + caret.Height - rc.Bottom;
				scrollY=-scrollAmount;
			} 
			else if ( pt.Y < rc.Top )
			{
				int scrollAmount=rc.Top - pt.Y;
				scrollY=scrollAmount;
			}

			if ( pt.X+20 > rc.Right)
			{
				int scrollAmount=pt.X - rc.Right + 20;
				scrollX=-scrollAmount;
			} 
			else if ( pt.X < rc.Left )
			{
				int scrollAmount=rc.Left - pt.X;
				scrollX=scrollAmount;
			}

			if ( scrollX != 0 || scrollY != 0)
				ScrollWindow(scrollX, scrollY);
		}

		private void SetCaretPosition(Point pt, int height)
		{
			caretLocation=pt;
//			pt.X-=offsetX;
//			pt.Y-=offsetY;
			caret.Set(pt, height);
//			Console.WriteLine("Set caret position {0}", pt);
		}

//		private void SetCaretPosition(Point pt)
//		{
//			caret.Set(pt, caret.Height);
//		}

		private void ResetCaretPosition()
		{
			if ( ContainsFocus )
				SetCaretPosition(caretLocation, caret.Height);
		}

		private void ScrollWindow(int x, int y)
		{
			if ( AutoScrollMinSize.Height == 0 )
				return;

			bool visible=caret.Visible;
			if ( visible )
				caret.Visible=false;

			if ( OffsetY - y < 0 )
				y=OffsetY;

			int actualMax=AutoScrollMinSize.Height - ClientRectangle.Height + 1;
			if ( OffsetY - y > actualMax )
				y=OffsetY-actualMax;

			if ( OffsetX - x < 0 )
				x=OffsetX;

			actualMax=AutoScrollMinSize.Width - ClientRectangle.Width + 1;
			if ( OffsetX - x > actualMax )
				x=OffsetX-actualMax;

			AutoScrollPosition=new Point(OffsetX-x, OffsetY-y);

			ResetCaretPosition();

			if ( visible )
				caret.Visible=true;
		}

//		private static bool IsKeyPressed(Keys data, Keys key)
//		{
//			return (Int16) data == (Int16) key;
//		}

		/// <seebase/>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);

			Point pt=CursorPosition;

			SelectionPoint sp=GetSelectionPoint(pt);
			if ( sp == null )
				return;

			if ( (ModifierKeys & Keys.Shift) != Keys.Shift )
			{
				mouseDownPoint=new Point(e.X, e.Y);
				newSelection=new Selection(sp);
			}
		}

		private void MouseMoveSelection()
		{
			if ( doc == null || layoutEngine == null )
				return;

			tooltip.Active=false;

			Point pt=CursorPosition;
			if ( pt.Y > LogicalRectangle.Bottom && LogicalRectangle.Bottom < AutoScrollMinSize.Height )
			{
				scrollTimerIncrement=pt.Y - LogicalRectangle.Bottom;
				scrollTimer.Enabled=true;
			} 
			else if ( pt.Y < LogicalRectangle.Top )
			{
				scrollTimerIncrement=pt.Y-LogicalRectangle.Top;
				scrollTimer.Enabled=true;
			}
			else
			{
				scrollTimer.Enabled=false;
				SelectionPoint sp=GetSelectionPointUnderCursor();
				if ( sp == null )
					return;

				newSelection=new Selection(newSelection.Start, sp);
				SetSelection(newSelection);
				UpdateCaretPosition(true);
			}
		}

		/// <seebase/>
		protected override void OnGotFocus(EventArgs e)
		{
			UpdateCaretPosition();
			base.OnGotFocus(e);
		}

		/// <seebase/>
//		protected override void OnMouseWheel(MouseEventArgs e)
//		{
//			base.OnMouseWheel(e);
//
//			ScrollWindow(0, e.Delta);
//		}

		/// <seebase/>
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);

			scrollTimer.Enabled=false;

			mouseDownPoint=Point.Empty;
			mouseSelection=false;

			if ( currentSelection.IsEmpty )
				return;

			CreateUndoPoint();

			if ( currentSelection.Start.Equals(currentSelection.End) )
			{
				Selection sel=new Selection(selectionManager.Validate(currentSelection.Start), null);
				SetSelection(sel);
				UpdateCaretPosition(true);
			}
		}

		private void ScrollTimerFire(object sender, ElapsedEventArgs e)
		{
			int newOffsetY=OffsetY+scrollTimerIncrement*3;
			if ( newOffsetY < 0 )
				newOffsetY=0;

			if ( newOffsetY > AutoScrollMinSize.Height )
				newOffsetY=AutoScrollMinSize.Height;

			AutoScrollPosition=new Point(0, OffsetY);

			SelectionPoint sp=GetSelectionPoint(CursorPosition);
			// TODO: L: not sure what this is doing
			if ( sp == null )
			{
				Update();
				return;
			}

			newSelection=new Selection(newSelection.Start, sp);
			SetSelection(newSelection);
			UpdateCaretPosition(true);
		}

//		private SelectionPoint GetSelectionPoint(int dy)
//		{
//			Point pt=caretLocation;
//
//			pt.Y+=dy;
//			if ( linePosition >= 0 )
//				pt.X=linePosition;
//
//			return GetSelectionPoint(pt, false);
//		}
//
		/// Moves the caret forward one character.
		[CommandTarget(Group="Movement")]
		public void CursorCharRight()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			SetSelection(selectionManager.MoveCharRight(currentSelection));
			UpdateCaretPosition(true, CaretDirection.LTR);
			EnsureCaretVisible();
		}

		/// Moves the caret back one character.
		[CommandTarget(Group="Movement")]
		public void CursorCharLeft()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			SetSelection(selectionManager.MoveCharLeft(currentSelection));
			UpdateCaretPosition(true);
			EnsureCaretVisible();
		}

		/// <summary>
		/// Extends the current selection one character to the right.
		/// </summary>
		/// <remarks>Note that if the selection start appears before the selection
		/// end, this will actually contract the selection.</remarks>
		[CommandTarget(Group="Movement")]
		public void CursorExtendCharRight()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			SetSelection(selectionManager.ExtendCharRight(currentSelection));
			UpdateCaretPosition(true);
			EnsureCaretVisible();
		}

		/// <summary>
		/// Extends the current selection one character to the left.
		/// </summary>
		/// <remarks>Note that if the selection start appears before the selection
		/// end, this will actually expand the selection.</remarks>
		[CommandTarget(Group="Movement")]
		public void CursorExtendCharLeft()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			SetSelection(selectionManager.ExtendCharLeft(currentSelection));
			UpdateCaretPosition(true);
			EnsureCaretVisible();
		}

		/// Moves the caret forward one word.
		[CommandTarget(Group="Movement")]
		public void CursorWordRight()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			SetSelection(selectionManager.MoveWordRight(currentSelection));
			UpdateCaretPosition(true);
			EnsureCaretVisible();
		}

		/// Moves the caret back one word.
		[CommandTarget(Group="Movement")]
		public void CursorWordLeft()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			SetSelection(selectionManager.MoveWordLeft(currentSelection));
			UpdateCaretPosition(true);
			EnsureCaretVisible();
		}

		/// <summary>
		/// Extends the current selection one word to the right.
		/// </summary>
		/// <remarks>Note that if the selection start appears before the selection
		/// end, this will actually contact the selection.</remarks>
		[CommandTarget(Group="Movement")]
		public void CursorExtendWordRight()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			SetSelection(selectionManager.ExtendWordRight(currentSelection));
			UpdateCaretPosition(true);
			EnsureCaretVisible();
		}

		/// <summary>
		/// Extends the current selection one word to the left.
		/// </summary>
		/// <remarks>Note that if the selection start appears before the selection
		/// end, this will actually expand the selection.</remarks>
		[CommandTarget(Group="Movement")]
		public void CursorExtendWordLeft()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			SetSelection(selectionManager.ExtendWordLeft(currentSelection));
			UpdateCaretPosition(true);
			EnsureCaretVisible();
		}

		private SelectionPoint GetNextLineSelectionPoint(ref Rectangle rcCaret)
		{
			Point pt=caret.Location;
			HitTestInfo ht=HitTest(pt, false);

			// TODO: L: throws exception on cursor move before first reflow (think fixed)
			int dy=ht.LineItemContext.LineHeight-(ht.LineItemContext.LineBaseline-caret.Height);
			pt.Y+=dy;

			if ( linePosition > 0 )
				pt.X=linePosition;

			return GetSelectionPoint(pt, ref rcCaret);
		}

		private SelectionPoint GetPreviousLineSelectionPoint(ref Rectangle rcCaret)
		{
			// TODO: H: this is too simplistic, consider case of table cell and div with padding
			Point pt=caretLocation;
			HitTestInfo ht=HitTest(pt, false);
			pt.Y=ht.LineBounds.Top-1;

			if ( linePosition > 0 )
				pt.X=linePosition;

			return GetSelectionPoint(pt, ref rcCaret);
		}

		/// <summary>
		/// Moves the caret to the same x-position on the next line.
		/// </summary>
		/// <remarks>
		/// The control keeps track of the position from the left edge so that subsequent
		/// calls to this method will always use the target x-position. The x-position is
		/// reset when other movement methods are called or the mouse is clicked.
		/// </remarks>
		[CommandTarget(Group="Movement")]
		public void CursorLineDown()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			Rectangle rcCaret=Rectangle.Empty;
			SelectionPoint sp=GetNextLineSelectionPoint(ref rcCaret);

			if ( sp == null )
				return;

			sp=selectionManager.Validate(sp);
			SetSelection(new Selection(sp));

			UpdateCaretPosition(rcCaret, false);
			EnsureCaretVisible();
		}

		/// <summary>
		/// Extends the selection to the same x-position on the next line.
		/// </summary>
		/// <remarks>
		/// The control keeps track of the position from the left edge so that subsequent
		/// calls to this method will always use the target x-position. The x-position is
		/// reset when other movement methods are called or the mouse is clicked.
		/// </remarks>
		[CommandTarget(Group="Movement")]
		public void CursorExtendLineDown()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			Rectangle rcCaret=Rectangle.Empty;
			SelectionPoint sp=GetNextLineSelectionPoint(ref rcCaret);

			if ( sp == null )
				return;

			SetSelection(new Selection(currentSelection.Start, sp));
			UpdateCaretPosition(rcCaret, false);
			EnsureCaretVisible();
		}

		private SelectionPoint GetPageShiftedSelectionPoint(int dy)
		{
			Point pt=caret.Location;
			pt.Y+=dy;

			if ( linePosition > 0 )
				pt.X=linePosition;

			return GetSelectionPoint(pt);
		}

		/// <summary>
		/// Moves the caret to the same x-position on the next page.
		/// </summary>
		/// <remarks>
		/// The control keeps track of the position from the left edge so that subsequent
		/// calls to this method will always use the target x-position. The x-position is
		/// reset when other movement methods are called or the mouse is clicked.
		/// </remarks>
		[CommandTarget(Group="Movement")]
		public void CursorPageDown()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			// TODO: M: should get caret pos and use this to update, like line up/down
			CreateUndoPoint();
			SelectionPoint sp=GetPageShiftedSelectionPoint(VisibleRectangle.Height);
			SetSelection(new Selection(sp));
			UpdateCaretPosition(false);
			EnsureCaretVisible();
			Update();
		}

		/// <summary>
		/// Extends the selection to the same x-position on the next page.
		/// </summary>
		/// <remarks>
		/// The control keeps track of the position from the left edge so that subsequent
		/// calls to this method will always use the target x-position. The x-position is
		/// reset when other movement methods are called or the mouse is clicked.
		/// </remarks>
		[CommandTarget(Group="Movement")]
		public void CursorExtendPageDown()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			SelectionPoint sp=GetPageShiftedSelectionPoint(VisibleRectangle.Height);
			SetSelection(new Selection(currentSelection.Start, sp));
			UpdateCaretPosition(false);
			EnsureCaretVisible();
			Update();
		}

		/// <summary>
		/// Moves the caret to the same x-position on the previous line.
		/// </summary>
		/// <remarks>
		/// The control keeps track of the position from the left edge so that subsequent
		/// calls to this method will always use the target x-position. The x-position is
		/// reset when other movement methods are called or the mouse is clicked.
		/// </remarks>
		[CommandTarget(Group="Movement")]
		public void CursorLineUp()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			Rectangle rcCaret=Rectangle.Empty;
			SelectionPoint sp=GetPreviousLineSelectionPoint(ref rcCaret);

			if ( sp == null )
				sp=new ElementSelectionPoint(doc.DocumentElement, TagType.StartTag);

			sp=selectionManager.Validate(sp);
			SetSelection(new Selection(sp));

			UpdateCaretPosition(rcCaret, false);
			EnsureCaretVisible();
		}

		/// <summary>
		/// Extends the selection to the same x-position on the previous line.
		/// </summary>
		/// <remarks>
		/// The control keeps track of the position from the left edge so that subsequent
		/// calls to this method will always use the target x-position. The x-position is
		/// reset when other movement methods are called or the mouse is clicked.
		/// </remarks>
		[CommandTarget(Group="Movement")]
		public void CursorExtendLineUp()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			Rectangle rcCaret=Rectangle.Empty;
			SelectionPoint sp=GetPreviousLineSelectionPoint(ref rcCaret);

			if ( sp == null )
				sp=new ElementSelectionPoint(doc.DocumentElement, TagType.StartTag);

			sp=selectionManager.Validate(sp);
			SetSelection(new Selection(currentSelection.Start,  sp));

			UpdateCaretPosition(rcCaret, false);
			EnsureCaretVisible();
		}

		/// <summary>
		/// Moves the caret to the same x-position on the previous page.
		/// </summary>
		/// <remarks>
		/// The control keeps track of the position from the left edge so that subsequent
		/// calls to this method will always use the target x-position. The x-position is
		/// reset when other movement methods are called or the mouse is clicked.
		/// </remarks>
		[CommandTarget(Group="Movement")]
		public void CursorPageUp()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			SelectionPoint sp=GetPageShiftedSelectionPoint(-VisibleRectangle.Height);
			SetSelection(new Selection(sp));
			UpdateCaretPosition();
			EnsureCaretVisible();
			Update();
		}

		/// <summary>
		/// Extends the selection to the same x-position on the previous page.
		/// </summary>
		/// <remarks>
		/// The control keeps track of the position from the left edge so that subsequent
		/// calls to this method will always use the target x-position. The x-position is
		/// reset when other movement methods are called or the mouse is clicked.
		/// </remarks>
		[CommandTarget(Group="Movement")]
		public void CursorExtendPageUp()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			SelectionPoint sp=GetPageShiftedSelectionPoint(-VisibleRectangle.Height);
			SetSelection(new Selection(currentSelection.Start, sp));
			UpdateCaretPosition();
			EnsureCaretVisible();
			Update();
		}

		private SelectionPoint GetLineSelectionPoint(int x, ref Rectangle rcCaret)
		{
			Point pt=caret.Location;
			pt.X=x;

			SelectionPoint sp=GetSelectionPoint(pt, ref rcCaret);

			if ( sp == null )
				sp=new ElementSelectionPoint(doc.DocumentElement, TagType.EndTag);

			return selectionManager.Validate(sp);
		}

		/// <summary>
		/// Moves the caret to the end of the current line.
		/// </summary>
		[CommandTarget(Group="Movement")]
		public void CursorLineEnd()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			Rectangle rcCaret=Rectangle.Empty;
			SelectionPoint sp=GetLineSelectionPoint(VisibleRectangle.Right, ref rcCaret);
			SetSelection(new Selection(sp));
			UpdateCaretPosition(rcCaret, true);
			EnsureCaretVisible();
		}

		/// <summary>
		/// Extends the selection to the end of the current line.
		/// </summary>
		[CommandTarget(Group="Movement")]
		public void CursorExtendLineEnd()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			Rectangle rcCaret=Rectangle.Empty;
			SelectionPoint sp=GetLineSelectionPoint(VisibleRectangle.Right, ref rcCaret);
			SetSelection(new Selection(currentSelection.Start, sp));
			UpdateCaretPosition(rcCaret, true);
			EnsureCaretVisible();
		}

		/// <summary>
		/// Moves the caret to the beginning of the current line
		/// </summary>
		[CommandTarget(Group="Movement")]
		public void CursorLineBegin()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			Rectangle rcCaret=Rectangle.Empty;
			SelectionPoint sp=GetLineSelectionPoint(VisibleRectangle.Left, ref rcCaret);
			SetSelection(new Selection(sp));
			UpdateCaretPosition(rcCaret, true);
			EnsureCaretVisible();
		}

		/// <summary>
		/// Extends the selection to the beginning of the current line
		/// </summary>
		[CommandTarget(Group="Movement")]
		public void CursorExtendLineBegin()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			Rectangle rcCaret=Rectangle.Empty;
			SelectionPoint sp=GetLineSelectionPoint(VisibleRectangle.Left, ref rcCaret);
			SetSelection(new Selection(currentSelection.Start, sp));
			UpdateCaretPosition(rcCaret, true);
			EnsureCaretVisible();
		}

		/// <summary>
		/// Moves the caret to the beginning of the document
		/// </summary>
		/// <remarks>The first valid caret position is just after the document element start tag.</remarks>
		[CommandTarget(Group="Movement")]
		public void CursorDocumentBegin()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			SelectionPoint sp=new ElementSelectionPoint(doc.DocumentElement, TagType.StartTag);
			sp=selectionManager.Validate(sp);
			SetSelection(new Selection(sp));
			UpdateCaretPosition(true);
			EnsureCaretVisible();
		}

		/// <summary>
		/// Moves the caret to the end of the document
		/// </summary>
		/// <remarks>The last valid caret position is just before the document element end tag.</remarks>
		[CommandTarget(Group="Movement")]
		public void CursorDocumentEnd()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			SelectionPoint sp=new ElementSelectionPoint(doc.DocumentElement, TagType.EndTag);
			SetSelection(new Selection(sp));
			UpdateCaretPosition(true);
			EnsureCaretVisible();
		}

		/// Extends the selection to the beginning of the document
		[CommandTarget(Group="Movement")]
		public void CursorExtendDocumentBegin()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			SelectionPoint sp=new ElementSelectionPoint(doc.DocumentElement, TagType.StartTag);
			sp=selectionManager.Validate(sp);
			SetSelection(new Selection(currentSelection.Start, sp));
			UpdateCaretPosition(true);
			EnsureCaretVisible();
		}

		/// Extends the selection to the end of the document
		[CommandTarget(Group="Movement")]
		public void CursorExtendDocumentEnd()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			SelectionPoint sp=new ElementSelectionPoint(doc.DocumentElement, TagType.EndTag);
			SetSelection(new Selection(currentSelection.Start, sp));
			UpdateCaretPosition(true);
			EnsureCaretVisible();
		}

		/// <summary>
		/// Extends the selection upwards from the current position.
		/// </summary>
		/// <remarks>First the selection is extended to include the content of the current element.
		/// Then it is extended to contain the element start and end tags. The process then
		/// repeats until the entire document is selected (if repeated calls are made to this method).</remarks>
		[CommandTarget(Group="Movement")]
		public void EditExtendSelection()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			Selection sel=selectionManager.ExtendOut(currentSelection);
			if ( !sel.IsEmpty )
			{
				Selection=sel;
				UpdateCaretPosition();
				EnsureCaretVisible();
			}
		}

		/// <summary>
		/// Selects the whole document, excluding the start and end tags.
		/// </summary>
		[CommandTarget(Group="Movement")]
		public void EditSelectAll()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();
			Selection sel=selectionManager.SelectAll(currentSelection);
			if ( !sel.IsEmpty )
			{
				Selection=sel;
				UpdateCaretPosition();
				EnsureCaretVisible();
			}
		}

		[CommandTarget(Group="Editing")]
		public void EditNewline()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			if ( currentSelection.IsEmpty || currentSelection.IsRange )
				return;

			XmlNode context=SelectionManager.GetInsertionContext(currentSelection.Start);
			WhitespaceHandling wh=stylesheet.Classify(context);
			if ( wh == WhitespaceHandling.Default )
				EditSplitElement();
			else
				InsertChar('\n');
		}

		/// <summary>
		/// Splits the current element to give two elements with the same name.
		/// </summary>
		/// <remarks>
		/// <para>This method does not have any effect if the current selection is a range.</para>
		/// <para>Any element attributes are always retained in the first element.</para>
		/// <para>The caret is positioned inside the new element.</para>
		/// <para>By default, this method is called when the user presses the Enter key.</para>
		/// </remarks>
		[CommandTarget(Group="Editing")]
		public void EditSplitElement()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			if ( currentSelection.IsEmpty || currentSelection.IsRange )
				return;

			Selection sel=SelectionManager.Split(currentSelection.Start);
			if ( sel != null )
			{
				SetSelection(sel);
				UpdateCaretPosition(sel.Start, true);
				EnsureCaretVisible();
			}
		}

		/// <summary>
		/// Deletes to the left of the caret, or deletes a balanced range.
		/// </summary>
		/// <remarks>
		/// <para>This method has no effect if the selection is an unbalanced range.</para>
		/// <para>If called when the caret is to the right of an element start or end point, 
		/// the element will be removed, and any content merged with the parent element.</para>
		/// <para>If the selection is a balanced range, the content of the range is deleted.</para>
		/// </remarks>
		[CommandTarget(Group="Editing")]
		public void EditBackspace()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			Selection sel=selectionManager.Backspace(currentSelection);
			if ( sel != null )
			{
				SetSelection(sel);
				UpdateCaretPosition(sel.Start, true);
				EnsureCaretVisible();
			}
		}

		/// <summary>
		/// Deletes to the right of the caret, or deletes a balanced range.
		/// </summary>
		/// <remarks>
		/// <para>This method has no effect if the selection is an unbalanced range.</para>
		/// <para>If called when the caret is to the left of an element start or end point, 
		/// the element will be removed, and any content merged with the parent element.</para>
		/// <para>If the selection is a balanced range, the content of the range is deleted.</para>
		/// </remarks>
		[CommandTarget(Group="Editing")]
		public void EditDelete()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			Selection sel=selectionManager.Delete(currentSelection);
			if ( sel != null )
			{
				SetSelection(sel);
				UpdateCaretPosition(sel.Start, true);
				EnsureCaretVisible();
			}
		}

		/// <summary>
		/// Performs an undo up to the last undo point.
		/// </summary>
		[CommandTarget(Group="Editing")]
		public void EditUndo()
		{
			if ( undoManager.CanUndo )
			{
				Selection sel=(Selection) undoManager.Undo(currentSelection);
				if ( doc.DocumentElement == null )
				{
					// TODO: M: shouldn't really detach as it loses DTD info
					Detach();
					return;
				}
				if ( sel != null && !sel.IsEmpty )
				{
					Selection=sel;
					UpdateCaretPosition();
					EnsureCaretVisible();
				}
			}
		}

		/// <summary>
		/// Performs a redo up to the next undo point.
		/// </summary>
		[CommandTarget(Group="Editing")]
		public void EditRedo()
		{
			if ( undoManager.CanRedo )
			{
				Selection sel=(Selection) undoManager.Redo();
				if ( !sel.IsEmpty )
				{
					Selection=sel;
					UpdateCaretPosition();
					EnsureCaretVisible();
				}
			}
		}

		/// <summary>
		/// Cuts the current selection to the clipboard.
		/// </summary>
		/// <remarks>
		/// <para>This method has no effect if the selection is not a balanced range.</para>
		/// <para>See <link linkend='clipboard'>Clipboard Handling"</link> for more information about
		/// how XEditNet uses the clipboard.</para>
		/// </remarks>
		[CommandTarget(Group="Editing")]
		public void EditCut()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

//			UndoUpdate();
			CreateUndoPoint();

			Selection sel=selectionManager.Cut(currentSelection);
			if ( !sel.IsEmpty )
			{
				Selection=sel;
				UpdateCaretPosition();
				EnsureCaretVisible();
			}
		}

		/// <summary>
		/// Copies the current selection to the clipboard.
		/// </summary>
		/// <remarks>
		/// <para>This method has no effect if the selection is not a balanced range.</para>
		/// <para>See <link linkend='clipboard'>Clipboard Handling"</link> for more information about
		/// how XEditNet uses the clipboard.</para>
		/// </remarks>
		[CommandTarget(Group="Editing")]
		public void EditCopy()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			SelectionManager.Copy(currentSelection);
		}

		/// <summary>
		/// Displays the modify attributes dialogue.
		/// </summary>
		/// <remarks>Derived classes can override this method to display
		/// custom dialogues for specific element, for example an image browser.</remarks>
		[CommandTarget(Group="Editing")]
		public virtual void EditModifyAttribute()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			InterfaceActivationEventArgs ea=new InterfaceActivationEventArgs();
			OnChangeAttributesActivated(ea);
			if ( ea.Handled )
				return;

			AttributeChangePanel acp=new AttributeChangePanel();
			acp.EnableSelectionTracking=false;
			acp.Editor=this;

			ShowPopup(acp);
		}

//		private void InsertOrChangeElement(EventHandler handler, ICollection valid, ICollection all)
//		{
//			PopupElements ps=new PopupElements();
//			ps.ElementList.ItemSelected+=handler;
//
//			ps.ElementList.ValidItems=valid;
//			ps.ElementList.AllItems=all;
//
//			Point pt=caret.Location;
//			pt.Offset(OffsetX, OffsetY);
//			pt.Y+=caret.Height;
//			pt.X=Math.Min(pt.X, VisibleRectangle.Width-ps.Width);
//			pt.Y=Math.Min(pt.Y, VisibleRectangle.Height-ps.Height);
//			ps.StartPosition=FormStartPosition.Manual;
//			pt=PointToScreen(pt);
//			ps.Location=pt;
//
//			ps.Show();
//		}

		protected virtual void OnInsertElementActivated(InterfaceActivationEventArgs e)
		{
			if ( InsertElementActivated != null )
				InsertElementActivated(this, e);
		}

		protected virtual void OnChangeElementActivated(InterfaceActivationEventArgs e)
		{
			if ( ChangeElementActivated != null )
				ChangeElementActivated(this, e);
		}

		protected virtual void OnChangeAttributesActivated(InterfaceActivationEventArgs e)
		{
			if ( ChangeAttributesActivated != null )
				ChangeAttributesActivated(this, e);
		}


		/// <summary>
		/// Displays the insert element dialogue.
		/// </summary>
		[CommandTarget(Group="Editing")]
		public void EditInsertElement()
		{
			if ( !licenceValid )
				return;

			InterfaceActivationEventArgs e=new InterfaceActivationEventArgs();
			OnInsertElementActivated(e);
			if ( e.Handled )
				return;

			if ( currentSelection.IsRange && !currentSelection.IsBalanced )
			{
				MessageBox.Show("The selection must be balanced in order to insert an element", "Insert Element", MessageBoxButtons.OK,  MessageBoxIcon.Information);
				return;
			}

			ElementInsertPanel eip=new ElementInsertPanel();
			eip.EnableSelectionTracking=false;
			eip.Editor=this;

			ShowPopup(eip);
		}

		private void ShowPopup(Control eip)
		{
			PopupWindow pe=new PopupWindow(eip);
	
			Point pt=caret.Location;
			pt.Offset(OffsetX, OffsetY);
			pt.Y+=caret.Height;
			pt.X=Math.Min(pt.X, VisibleRectangle.Width-pe.Width);
			pt.Y=Math.Min(pt.Y, VisibleRectangle.Height-pe.Height);
			pe.StartPosition=FormStartPosition.Manual;
			pt=PointToScreen(pt);
			pe.Location=pt;
	
			pe.Show();
		}

		/// <summary>
		/// Displays the change element dialogue.
		/// </summary>
		[CommandTarget(Group="Editing")]
		public void EditChangeElement()
		{
			if ( doc == null )
				return;

			InterfaceActivationEventArgs e=new InterfaceActivationEventArgs();
			OnChangeElementActivated(e);
			if ( e.Handled )
				return;

			ElementChangePanel eip=new ElementChangePanel();
			eip.EnableSelectionTracking=false;
			eip.Editor=this;

			ShowPopup(eip);
		}

		/// <summary>
		/// Gets a value indicating if the current document was modified.
		/// </summary>
		/// <remarks>This property will return the correct value even if
		/// the document is modified outside the control (via API calls 
		/// to the underlying XmlDocument).</remarks>
		public bool IsModified
		{
			get { return undoManager.Modified; }
			set { undoManager.Modified=value; }
		}

//		private void HandleInsertElementConfirm(object sender, EventArgs e)
//		{
//			PopupWindow ps=(PopupWindow) sender;
//			XmlName name=ps.ElementList.SelectedItem;
//			if ( name == null )
//			{
//				if ( HasDtd )
//				{
//					MessageBox.Show(this, "Please select a valid element from the DTD", "Insert Element", MessageBoxButtons.OK, MessageBoxIcon.Information);
//					return;
//				}
//				name=ps.ElementList.Filter;
//				if ( name == null || name.QualifiedName.Length == 0 )
//				{
//					MessageBox.Show(this, "Please enter an element name or choose one from the list", "Insert Element", MessageBoxButtons.OK, MessageBoxIcon.Information);
//					return;
//				}
//			}
//			if ( doc == null )
//			{
//				doc=new XmlDocument();
//				Attach(doc, true);
//			} else
//				CreateUndoPoint();
//
//			XmlElement elem=doc.CreateElement(name.Prefix, name.LocalName, name.NamespaceURI);
//			Insert(elem);
//			CreateUndoPoint();
//		}
//
//		private void HandleChangeElementConfirm(object sender, EventArgs e)
//		{
//			PopupWindow ps=(PopupWindow) sender;
//			XmlName name=ps.ElementList.SelectedItem;
//			if ( name == null )
//			{
//				if ( HasDtd )
//				{
//					MessageBox.Show(this, "Please select a valid element from the DTD", "Change Element", MessageBoxButtons.OK, MessageBoxIcon.Information);
//					return;
//				}
//				name=ps.ElementList.Filter;
//				if ( name == null || name.QualifiedName.Length == 0 )
//				{
//					MessageBox.Show(this, "Please enter an element name or choose one from the list", "Change Element", MessageBoxButtons.OK, MessageBoxIcon.Information);
//					return;
//				}
//			}
//
//			XmlElement elem=doc.CreateElement(name.Prefix, name.LocalName, name.NamespaceURI);
//			Change(elem);
//		}

		public void Change(XmlElement elem)
		{
			CreateUndoPoint();
			XmlNode n;
			XmlElement p=SelectionManager.GetInsertionContext(currentSelection.Start, out n);
			Change(p, elem);
			CreateUndoPoint();
		}

		/// <summary>
		/// Pastes the contents of the clipboard.
		/// </summary>
		/// <remarks>
		/// <para>This method has no effect if the selection is an unbalanced range.</para>
		/// <para>If the selection is a balanced range, the content of the range is removed.</para>
		/// <para>If the clipboard contains XEditNet data, it is inserted at the caret.</para>
		/// <para>If the clipboard contains well-formed XML text it is inserted as element content at the caret.</para>
		/// <para>If the clipboard contains text it is inserted as text at the caret.</para>
		/// </remarks>
		[CommandTarget(Group="Editing")]
		public void EditPaste()
		{
			if ( doc == null || doc.DocumentElement == null )
				return;

			CreateUndoPoint();

			Selection sel=selectionManager.Paste(currentSelection);
			if ( !sel.IsEmpty )
			{
				SetSelection(sel);
				UpdateCaretPosition();
				EnsureCaretVisible();
			}
		}

		/// <summary>
		/// Reloads the current stylesheet.
		/// </summary>
		/// <remarks>This is useful during stylesheet development.</remarks>
		[CommandTarget(Group="View")]
		public void FileReloadStylesheet()
		{
			if ( stylesheetFile != null && stylesheetFile.Exists )
			{
				this.Stylesheet=Stylesheet.Load(stylesheetFile.FullName, doc.NameTable);
				Reflow();
			}

//			reflowTimer.Stop();
//			layoutEngine=new LayoutEngine(stylesheet);
//			reflowTimer.Start();

			Invalidate(true);
		}

		/// <summary>
		/// Shows element tags.
		/// </summary>
		[CommandTarget(Group="View")]
		public void ViewTagsOn()
		{
			stylesheet.TagMode=TagViewMode.Full;
			reflowTimer.Stop();
			layoutEngine=new LayoutEngine(stylesheet);
			reflowTimer.Start();
			Invalidate(true);
		}

		/// <summary>
		/// Hides element tags.
		/// </summary>
		[CommandTarget(Group="View")]
		public void ViewTagsOff()
		{
			stylesheet.TagMode=TagViewMode.None;
			reflowTimer.Stop();
			layoutEngine=new LayoutEngine(stylesheet);
			reflowTimer.Start();
			Invalidate(true);
		}


		#region IMessageFilter Members

		/// <seebase/>
		public bool PreFilterMessage(ref Message m)
		{
			if ( m.Msg != 0x102 )
				return false;

//			Logger.Log("Filtering message: {0}", m);
			return false;
		}

		#endregion

		private void reflowTimer_Tick(object sender, EventArgs e)
		{
			if( this.DesignMode )
				return;

			reflowTimer.Stop();
			reflowTimer.Interval=300;
			Reflow();
		}

		#region IUndoContextProvider Members

		/// <summary>
		/// Used internally by the XEditNet undo framework. Currently gets the current selection.
		/// </summary>
		public object ContextInfo
		{
			get { return currentSelection; }
		}

		internal ValidationManager ValidationManager
		{
			get { return validationManager; }
		}

		#endregion

		private void ShowQuickFixMenu()
		{
			XmlNode n=quickFixIndicator.Node;

			QuickFix[] fixes=validationManager.GetQuickFixes(n);

			this.quickFixMenu.MenuItems.Clear();

			foreach ( QuickFix qf in fixes )
			{
				string main=qf.MainText;
				string sub=qf.SubText;

				MenuItem parent=null;
				foreach ( MenuItem mi in quickFixMenu.MenuItems )
				{
					if ( !mi.Text.Equals(main) )
						continue;

					parent=mi;
				}

				if ( sub != null )
				{
					if ( parent == null )
					{
						parent=new MenuItem(main);
						quickFixMenu.MenuItems.Add(parent);
					}

					MenuItem subMenuItem=new QuickFixMenuItem(sub, qf);
					subMenuItem.Click+=new EventHandler(QuickFixClicked);
					parent.MenuItems.Add(subMenuItem);
				}
				else
				{
					if ( parent == null )
					{
						parent=new QuickFixMenuItem(main, qf);
						parent.Click+=new EventHandler(QuickFixClicked);
						quickFixMenu.MenuItems.Add(parent);
					}
				}
			}
			Point pt=new Point(quickFixIndicator.CurrentBounds.Left, quickFixIndicator.CurrentBounds.Bottom+2);
			quickFixMenu.Show(this, pt);
		}

		private void QuickFixClicked(object sender, EventArgs e)
		{
			QuickFixMenuItem qfmi=(QuickFixMenuItem) sender;
			QuickFix qf=qfmi.QuickFix;

			PerformQuickFix(qf);
		}

		private class QuickFixMenuItem : MenuItem
		{
			private QuickFix quickFix;

			public QuickFixMenuItem(string text, QuickFix qf) : base(text)
			{
				quickFix=qf;
			}

			public QuickFix QuickFix
			{
				get { return quickFix; }
			}
		}

		internal void PerformQuickFix(QuickFix qf)
		{
			// set the selection just before change so that undo puts us
			// somewhere sensible
			SetSelection(qf.PreSelection(selectionManager, currentSelection));

			Selection sel=qf.Perform(this.selectionManager);

			SetSelection(sel);
			UpdateCaretPosition(true);
			EnsureCaretVisible();
		}

		public CommandMapping[] Commands
		{
			get { return commandMapper.Commands; }
		}

		public void DispatchCommand(CommandMapping cmd)
		{
			switch ( cmd.Method )
			{
				case "EditUndo":
					EditUndo();
					break;
				case "EditRedo":
					EditRedo();
					break;
				case "EditCut":
					EditCut();
					break;
				case "EditCopy":
					EditCopy();
					break;
				case "EditPaste":
					EditPaste();
					break;
				case "EditDelete":
					EditDelete();
					break;
				case "EditSelectAll":
					EditSelectAll();
					break;
				case "EditExtendSelection":
					EditExtendSelection();
					break;
				case "EditChangeElement":
					EditChangeElement();
					break;
				case "EditInsertElement":
					EditInsertElement();
					break;
				case "EditModifyAttribute":
					EditModifyAttribute();
					break;
				case "FileReloadStylesheet":
					FileReloadStylesheet();
					break;
				case "ViewTagsOff":
					ViewTagsOff();
					break;
				case "ViewTagsOn":
					ViewTagsOn();
					break;
			}
		}

		public SelectionManager SelectionManager
		{
			// TODO: M: check this is safe
			get { return selectionManager; }
		}

		private void SetCursorType(HitTestInfo ht)
		{
			switch ( ht.Type )
			{
				case HitTestType.TableColumnResize:
					Cursor=Cursors.VSplit;
					break;

				default:
					Cursor=Cursors.IBeam;
					break;
			}
		}
	}

	public delegate void InterfaceActivationEventHandler(object sender, InterfaceActivationEventArgs e);

	public class InterfaceActivationEventArgs : EventArgs
	{
		public bool Handled=false;
	}
}
