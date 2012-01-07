using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Xml;
using XEditNet.Location;
using XEditNet.Styles;

namespace XEditNet.Layout
{
	internal class LayoutEngine
	{
		private IBlock rootBlock=null;
		public Stylesheet Stylesheet;
		public ArrayList selectedItems=new ArrayList();
		private Selection currentSelection=new Selection();
		//		private DrawContext dc;

		public LayoutEngine(Stylesheet stylesheet)
		{
			this.Stylesheet=stylesheet;
		}

		public bool ReflowComplete
		{
			get { return rootBlock.ReflowCompleted; }
		}

		public void Draw(DrawContext dc, XmlElement e)
		{
			Style c=Stylesheet.GetStyle(dc.Graphics, e, dc.DocumentType.GetElementType(e));

			if ( rootBlock == null )
			{
				rootBlock=c.CreateReflowObject(null, e) as IBlock;
				rootBlock.Reflow(dc, new BoundingContext(dc.BoundingRectangle), false);
			}

			if ( rootBlock == null )
				throw new InvalidOperationException("Root element is not a block element");

//			PerfLog.Mark();
			rootBlock.Draw(dc, 0, 0, c);
//			PerfLog.Write("Layout engine redraw complete for rect bounds {0} / clip {1}", dc.BoundingRectangle, dc.ClippingRectangle);
		}

		public Size Reflow(DrawContext dc, XmlElement e)
		{
			PerfLog.Mark();

			Style s=Stylesheet.GetStyle(dc.Graphics, e, dc.DocumentType.GetElementType(e));
			rootBlock=s.CreateReflowObject(null, e) as IBlock;
			if ( rootBlock == null )
				// TODO: M: exception handling
				throw new ArgumentException("Invalid stylesheet / document. Root element must be a block.");

			BoundingContext bounds=new BoundingContext(dc.BoundingRectangle);

			Console.WriteLine("Root block reflow {0}", bounds);
			rootBlock.Reflow(dc, bounds, false);

			PerfLog.Write("Reflow complete for '{0}'", e.Name);
			return new Size(rootBlock.Width, rootBlock.Height);
		}

		public Size Invalidate(DrawContext dc, XmlElement e, Stylesheet s, out Rectangle invalidRect)
		{
			invalidRect=Rectangle.Empty;

			if ( rootBlock == null )
				Reflow(dc, e);

			IBlock b=rootBlock.FindBlock(e);
			// block can be null if just removed from doc, for example
			if ( b != null )
			{
				XmlNode n=b.Invalidate(dc);
				invalidRect=GetBoundingRect(n);
			}
			// TODO: L: we return the overall size here just so that the scrollbars can be
			// reset if needed - but still not quite sure why we need two returns
			return new Size(rootBlock.Width, rootBlock.Height);
		}

		public HitTestInfo GetHitTestInfo(IGraphics gr, Point pt)
		{
			if ( rootBlock == null )
				return null;

			return rootBlock.GetHitTestInfo(gr, 0, 0, pt);
		}

		public Rectangle GetCaretPosition(IGraphics gr, SelectionPoint sp, CaretDirection d)
		{
			if ( rootBlock == null )
				return Rectangle.Empty;

			CaretPositionInfo cpi=new CaretPositionInfo();
			rootBlock.GetCaretPosition(gr, 0, 0, sp, ref cpi);
			if ( d == CaretDirection.LTR )
				cpi.UseSecondary=true;

			return cpi.ToRectangle();
		}

		public Rectangle GetBoundingRect(XmlNode n)
		{
			if ( rootBlock == null )
				return Rectangle.Empty;

			return rootBlock.GetBoundingRect(0, 0, n);
		}

		public Rectangle SelectionBounds
		{
			get
			{
				if ( currentSelection.IsEmpty )
					return Rectangle.Empty;

				XmlNode common=XmlUtil.FindCommonAncestor(currentSelection.Start.Node, currentSelection.End.Node);
				if ( common != null && common.NodeType == XmlNodeType.Document )
					common=((XmlDocument) common).DocumentElement;

				Rectangle rc=GetBoundingRect(common);

				return rc;
			}
		}

//		public SelectionPoint GetNextSelectionPoint(SelectionPoint sp)
//		{
//			ILineItem item=rootBlock.FindSelectionPoint(sp);
//			if ( item == null )
//				return null;
//
//			SelectionPoint next=item.GetNextSelectionPoint(sp);
//			if ( next == null )
//			{
//				LineItemEnumerator lie=new LineItemEnumerator(item);
//				lie.MoveNext();
//				lie.MoveNext();
//				item=(ILineItem) lie.Current;
//				if ( item != null )
//					next=item.GetSelectionPoint(false);
//			}
//			return next;
//		}
//
		public Selection Selection
		{
			get { return currentSelection; }
			set
			{
				currentSelection = value == null ? new Selection() : value;

				if ( selectedItems != null )
				{
					foreach (ILineItem li in selectedItems)
						li.Selection=null;
				}

				if ( currentSelection.IsEmpty )
					return;

				Debug.Assert(currentSelection.IsRange, "Selection passed to layout engine must be a range");

				Selection sel=currentSelection.Normalise();

				ILineItem startItem=rootBlock.FindSelectionPoint(sel.Start);
				Debug.Assert(startItem != null, "Failed to find line item for start of selection!");

				ILineItem endItem=rootBlock.FindSelectionPoint(sel.End);
				if ( endItem == null )
				{
					Logger.Log("ERROR! Failed to find line item for {0}", sel.End);
					return;
				}

				bool endTag=false;
				if ( sel.End.IsTag )
				{
					MarkupSelectionPoint msp=(MarkupSelectionPoint) sel.End;
					endTag=msp.Type == TagType.EndTag;
				}

				LineItemEnumerator lie=new LineItemEnumerator(startItem);
				while ( !endItem.Equals(lie.Current) && lie.MoveNext() )
				{
					if ( endItem.Equals(lie.Current) && endItem is MarkupItem )
					{
						// this is to handle selection covering start of 
						// an empty tag, to show it as selected
						if ( !endTag || endItem is Tag )
							break;
					}

					((ILineItem) lie.Current).Selection=currentSelection;
					selectedItems.Add(lie.Current);
				}
				if ( lie.Current == null )
					Logger.Log("Reach end of iterator without finding end item");
			}
		}

		public Rectangle BoundingRect
		{
			get
			{
				if ( rootBlock == null )
					return Rectangle.Empty;

				return rootBlock.GetBoundingRect(0, 0);
			}
		}
	}
}