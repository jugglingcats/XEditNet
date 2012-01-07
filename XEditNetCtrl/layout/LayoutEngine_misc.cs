using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using XEditNet.Dtd;
using XEditNet.Layout.Tables;
using XEditNet.Location;
using XEditNet.Styles;
using XEditNet.Validation;

namespace XEditNet.Layout
{
	internal class ItemMetrics
	{
		protected ILineItem item;

		public bool CanBreakBefore=false;
		public bool CanBreakAfter=false;
		public bool ForceBreakAfter=false;

		public ItemMetrics(ILineItem item)
		{
			this.item=item;
		}

		public virtual int MinimumWidth
		{
			get { return item.Width; }
		}

		public virtual bool CanSplit
		{
			get { return false; }
		}

		public ILineItem LineItem
		{
			get { return item; }
		}
	}

	internal enum HitTestType
	{
		Default,
		TableColumnResize
	}

	internal class HitTestInfo
	{
		public readonly SelectionPoint SelectionPoint;
		public readonly bool After;
		internal readonly LineItemContext LineItemContext;
		public readonly Rectangle Caret;
		public Rectangle LineBounds;
		public HitTestType Type=HitTestType.Default;

		internal HitTestInfo(SelectionPoint sp, LineItemContext ili, bool after) : this(sp, ili, after, Rectangle.Empty)
		{
		}

		internal HitTestInfo(SelectionPoint sp, LineItemContext ili, bool after, Rectangle caret)
		{
			SelectionPoint=sp;
			After=after;
			LineItemContext=ili;
			Caret=caret;
		}
	}

	internal struct LineItemContext
	{
		public readonly ILineItem LineItem;
		public readonly int LineHeight;
		public readonly int LineBaseline;
		public readonly Point Location;

		public LineItemContext(int lineHeight, int lineBaseline, ILineItem lineItem, Point location)
		{
			LineItem=lineItem;
			LineHeight=lineHeight;
			LineBaseline=lineBaseline;
			Location=location;
		}

		public int ItemHeight
		{
			get { return LineItem.Height; }
		}

		public int ItemAscent
		{
			get { return LineItem.Ascent; }
		}
	}

	internal class Logger
	{
		private Stream of;
		private StreamWriter sw;

		public Logger(string file)
		{
			of=new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read);
			sw=new StreamWriter(of);
		}

		public void Dispose()
		{
			if ( of != null )
				of.Close();
		}

		public static void Log(string format, params object[] args) 
		{
			Debug.WriteLine(string.Format(format, args));
		}

		public void Write(string format, params object[] args)
		{
			sw.WriteLine(format, args);
		}
	}

	internal class InlineImpl : INestedReflowObject
	{
		private IContainer parent;
		private XmlElement element;
		private Style style;

		public InlineImpl(IContainer parent, XmlElement e, Style s)
		{
			this.parent=parent;
			this.element=e;
			this.style=s;
		}

		public void Reflow(IReflowHost host, DrawContext dc, BoundingContext bounds, bool incremental)
		{
			if ( style.Empty )
			{
				// this is empty tag case
				EmptyTag et=new EmptyTag(host.CurrentLine, element);
				host.ReflowMarkup(et, dc, style, bounds);
				return;
			}
			
			MarkupItem tag=new StartTag(host.CurrentLine, element);
			host.ReflowMarkup(tag, dc, style, bounds);			
			
			host.ReflowChildren(element, dc, style, bounds);
			
			tag=new EndTag(host.CurrentLine, element);
			host.ReflowMarkup(tag, dc, style, bounds);
		}
	}

	internal class BoundingContext
	{
		private int left;
		private int right;
		private int offset;

		public BoundingContext(Rectangle rc)
		{
			this.left=rc.Left;
			this.right=rc.Right;
			this.offset=0;
		}

		public BoundingContext(BoundingContext b, int width)
		{
			this.left=b.left;
			this.right=b.left+width;
			this.offset=0;
		}

		private BoundingContext(int left, int right, int offset)
		{
			this.left=left;
			this.right=right;
			this.offset=offset;
		}

		public int Width
		{
			get { return right-left; }
		}

		public BoundingContext Narrow(int left, int right)
		{
			return new BoundingContext(this.left+left, this.right-right, 0);
		}

		public void Place(int i)
		{
			offset+=i;
		}

		public bool CanPlace(int width)
		{
			return left+offset+width <= right;
		}

		public void Reset()
		{
			offset=0;
		}

		public int Remaining
		{
			get { return right-left-offset; }
		}

		public override string ToString()
		{
			return string.Format("({0}, {1}, {2})", left, right, offset);
		}
	}

	internal class Dimensions
	{
		public int Width=0;
		public int Height=0;
		public int MinWidth=0;
		public int MinHeight=0;
	}

	internal class DrawContext 
	{
		public readonly IGraphics Graphics;
		public Rectangle BoundingRectangle;
		public Rectangle ClippingRectangle;
		public Point Origin;
		public IValidationLookup InvalidInfo;
		internal DocumentType DocumentType;
		public bool TagsOff=true;
		public SelectionPoint SelectionPoint;

		internal DrawContext(IGraphics gr, Point origin, Rectangle bounds, Rectangle clip, IValidationLookup invalidNodes, DocumentType dtd, SelectionPoint sp)
		{
			this.Graphics=gr;
			this.BoundingRectangle=bounds;
			this.ClippingRectangle=clip;
			this.Origin=origin;
			this.InvalidInfo=invalidNodes;
			this.DocumentType=dtd;
			this.SelectionPoint=sp;
		}
	}

	internal abstract class DrawItemBase
	{
		protected IContainer parent;
		protected int height;
		protected int width;

		public IContainer Parent
		{
			get { return parent; }
			set { parent=value; }
		}

		public int Height
		{
			get { return height; }
//			set { height=value; }
		}
		public int Width
		{
			get { return width; }
//			set { width=value; }
		}

		public DrawItemBase(IContainer parent)
		{
			this.parent=parent;
		}

		public static Color InvertColor(Color c)
		{
			return Color.FromArgb(0xFFFFFF-c.ToArgb());
		}

		public Rectangle GetHorizontalRect(int x, int y)
		{
			// returns a very wide rectangle with the top and bottom of the
			// bounding rect but 0 and max for the left and right
			Rectangle rc=GetBoundingRect(x, y);
			rc.X=0;
			rc.Width=Int32.MaxValue;
			return rc;
		}

		public virtual Rectangle GetBoundingRect(int x, int y)
		{
			return new Rectangle(x, y, Width, Height);
		}
	}

	internal abstract class Region : DrawItemBase, IRegion
	{
		public Region(IContainer parent) : base(parent)
		{
		}

		public abstract void Draw(DrawContext dc, int x, int y, Style s);
		public abstract XmlNode GetNodeUnderPoint(int x, int y, Point pt);
		public abstract HitTestInfo GetHitTestInfo(IGraphics gr, int x, int y, Point pt);
		public abstract void GetCaretPosition(IGraphics gr, int x, int y, SelectionPoint sp, ref CaretPositionInfo cpi);
		public abstract ILineItem FindSelectionPoint(SelectionPoint sp);

		public virtual BoundingContext GetBoundsForChild(DrawContext dc, IContainedItem child)
		{
			if ( parent == null )
				return new BoundingContext(dc.BoundingRectangle);

			return parent.GetBoundsForChild(dc, this);
		}

		public virtual XmlElement ProcessMetricsChange(IContainedItem originator, DrawContext dc)
		{
			if ( parent == null )
				return null;

			// in the default scenario we don't want to process anything, but we need
			// to keep calling up the chain to see if an ancestor item wants to do
			// something about a change in min or desired width
			return parent.ProcessMetricsChange(this, dc);
		}
	}

	internal class LineItemQueue
	{
		private ArrayList items=new ArrayList();
		private int splitIndex=-1;

		public void Add(ItemMetrics itemMetrics)
		{
			if ( itemMetrics.CanSplit )
				splitIndex=items.Count;

			items.Add(itemMetrics);
		}

		public void Clear()
		{
			items.Clear();
			splitIndex=-1;
		}

		public ICollection LineItems
		{
			get
			{
				ArrayList a=new ArrayList();
				foreach ( ItemMetrics im in items )
					a.Add(im.LineItem);

				return a.ToArray(typeof(ILineItem)) as ILineItem[];
			}
		}

		public bool Empty
		{
			get { return items.Count == 0; }
		}

		public int Count
		{
			get { return items.Count; }
		}

		public int CalcWidth
		{
			get
			{
				int x=0;
				foreach ( ItemMetrics im in items )
					x+=im.LineItem.Width;

				return x;
			}
		}

		public bool CanSplit
		{
			get
			{
				return splitIndex >= 0;
			}
		}

		public LineItemQueue Split()
		{
			int n=splitIndex;
			ISplittable itemMetrics=(ISplittable) items[n];
//			ILineItem prev=itemMetrics.LineItem;
//			Debug.Assert(itemMetrics.CanSplit);

			ItemMetrics newItem=itemMetrics.Split();

			LineItemQueue liq=new LineItemQueue();
			for ( int i=0; i< n+1; i++ )
			{
				object o=items[i];
				liq.items.Add(o);
			}
			items.RemoveRange(0, n+1);
//			ItemMetrics newItem=new ItemMetrics(tmp);
			items.Insert(0, newItem);

			splitIndex=-1;
			return liq;
		}

		public int MinimumWidth
		{
			get
			{
				int retVal=0;
				int tmpVal=0;
				foreach ( ItemMetrics qi in items )
				{
					ISplittable s=qi as ISplittable;

					retVal=Math.Max(retVal, qi.MinimumWidth);

					if ( qi.CanBreakBefore )
					{
						retVal=Math.Max(retVal, tmpVal);
						if ( qi.CanBreakAfter )
							tmpVal=0;
						else
						{
							if ( s != null )
								tmpVal=s.MinEndWidth;
							else
								tmpVal=qi.LineItem.Width;
						}
						continue;
					}
					// can't break before
					if ( s != null && s.CanSplit )
					{
						tmpVal+=s.MinStartWidth;
						retVal=Math.Max(retVal, tmpVal);

						tmpVal=qi.CanBreakAfter ? 0 : s.MinEndWidth;
					}
					else
						tmpVal+=qi.LineItem.Width;
				}

				retVal=Math.Max(retVal, tmpVal);
				return retVal;
			}
		}
	}

	internal class BlockImpl : Region, IBlock, IReflowHost
	{
		private Hashtable childBlocks;
		private Line currentLine;
		private LineItemQueue queue=new LineItemQueue();
		private Stack styleStack=new Stack();
		private bool reflowCompleted;

		protected Style style;
		protected ArrayList Lines=new ArrayList();

		public XmlElement elementNode;
		private int minimumWidth;
		private int desiredWidth;

		public bool IsSingleLine
		{
			get
			{
				IRegion prev=null;
				foreach ( IRegion r in Lines )
				{
					if ( r is Line && prev is Line )
						return false;
					
					IBlock b=r as IBlock;
					if ( b != null && !b.IsSingleLine )
						return false;

					prev=r;
				}
				// TODO: M: this is too simplistic - consider case of
				//			para nested in table cell - is single line but won't report it
				return true;
			}
		}
		public IContainer CurrentLine
		{
			get { return currentLine; }
		}
		public XmlElement ElementNode
		{
			get { return elementNode; }
			set { elementNode=value; }
		}

		public Style Style
		{
			get { return style; }
			set { style=value; }
		}

		public BlockImpl(IContainer parent, XmlElement e, Style style) : base(parent)
		{
			this.style=style;
			styleStack.Push(style);

			elementNode=e;
			Lines.Clear();
			childBlocks=new Hashtable();
//			needReflow=true;
		}

		public XmlElement ProcessSizeChange(IContainedItem originator, DrawContext dc)
		{
			int w=Width;
			int h=Height;
			RecalcBounds();
			if ( w != Width || h != Height )
			{
				// TODO: L: This is questionable because the width won't change when text
				//			is added to a line and doesn't affect line breaking, however
				//			this can affect min/desired width and so affect column balance
				//			in a table, but recalc of these values is performance hit.
				//			Does seem to be working fine.
				if ( parent != null )
					return parent.ProcessSizeChange(this, dc);
			}
			return elementNode;
		}

		public override Rectangle GetBoundingRect(int x, int y)
		{
			Rectangle rc=base.GetBoundingRect(x+style.Left, y);
			return rc;
		}

		private void Commit(Line l, LineItemQueue q)
		{
			int m=q.MinimumWidth;
			if ( m > minimumWidth )
				minimumWidth=m;

			int w=q.CalcWidth;
			desiredWidth+=w;

			l.AddRange(q.LineItems);
			q.Clear();
		}

		public int ChildCount
		{
			get { return Lines.Count; }
		}

		public IContainedItem this[int index]
		{
			get { return (IContainedItem) Lines[index]; }
		}

		public IContainedItem FirstChild
		{
			get { return Lines.Count > 0 ? (IContainedItem) Lines[0] : null; }
		}

		public IContainedItem LastChild
		{
			get { return Lines.Count > 0 ? (IContainedItem) Lines[Lines.Count-1] : null; }
		}

		public IBlock FindBlock(XmlElement e)
		{
			if ( e.Equals(ElementNode) )
				return this;

			IBlock block=(IBlock) childBlocks[e];
			if ( block != null )
				return block;

			if ( !XmlUtil.HasAncestor(e, ElementNode) )
				return null;

			foreach ( IBlock block2 in childBlocks.Values )
			{
				IBlock ret=block2.FindBlock(e);
				if ( ret != null )
					return ret;
			}

			// we are ancestor but not found in any child blocks
			// TODO: L: error?
			return this;
		}

		public override ILineItem FindSelectionPoint(SelectionPoint sp)
		{
			foreach ( IRegion r in Lines )
			{
				ILineItem ret=r.FindSelectionPoint(sp);
				if ( ret != null )
					return ret;
			}
			return null;
		}

		public override BoundingContext GetBoundsForChild(DrawContext dc, IContainedItem child)
		{
			return GetBoundsForSelf(dc, true);
		}

		protected BoundingContext GetBoundsForSelf(DrawContext dc, bool adjust)
		{
			BoundingContext ret;
			
			if ( parent == null )
				ret=new BoundingContext(dc.BoundingRectangle);
			else
				ret=parent.GetBoundsForChild(dc, this);

			if ( adjust )
				ret=ret.Narrow(style.Left, style.Right);

			return ret;
		}

		public override void Draw(DrawContext dc, int x, int y, Style c) 
		{
//			if ( needReflow )
//				Reflow(dc, GetBoundsForSelf(dc, false), false);

			if ( y + Height < dc.ClippingRectangle.Top )
				// we're not visible
				return;

			dc.Graphics.PushFont(dc.Graphics.GetFontHandle(c.FontDesc));

			int h=y;
			foreach ( IRegion r in Lines ) 
			{
				r.Draw(dc, x+style.Left, h, c);
				h+=r.Height;
				if ( dc.ClippingRectangle.Bottom > 0 && h > dc.ClippingRectangle.Bottom )
					break;
			}

			dc.Graphics.PopFont();
		}

//		private class BlockState
//		{
//			public IBlock Block;
//			public bool Used;
//
//			public BlockState(IBlock block)
//			{
//				Block=block;
//				Used=true;
//			}
//		}

		protected virtual void ReflowStart(DrawContext dc)
		{
			Lines.Clear();
			currentLine=new Line(this);

//			Width=0;
//			Height=0;

			minimumWidth=0;
			desiredWidth=0;

			reflowCompleted=true;

			childBlocks.Clear();
		}

		protected virtual void ReflowEnd(DrawContext dc) 
		{
			Commit(currentLine, queue);
			if ( !currentLine.IsEmpty )
				Lines.Add(currentLine);

			Line firstLine=(Line) Lines[0];
			firstLine.AddTop(style.Top+style.Top);

			Line lastLine=(Line) Lines[Lines.Count-1];
			lastLine.AddBottom(style.Bottom+style.Bottom);

//			RecalcBounds();
//			needReflow=false;
		}

		public virtual void RecalcBounds()
		{
			width=0;
			height=0;
			foreach ( IRegion r in Lines )
			{
				int itemWidth=r.Width;
				if ( r is IBlock )
					itemWidth+=((IBlock) r).Style.Left;

				if ( itemWidth > Width )
					width=itemWidth;

				height+=r.Height;
			}
		}

		public virtual int MinimumWidth
		{
			get
			{
				// TODO: L: this can be false when heavily nested tables are reflowed
				//			suspect it's just to do with cancelling reflow
//				Debug.Assert(reflowCompleted, "MinimumWidth called before reflow completed!");

				int retVal = minimumWidth;

				foreach (object o in Lines)
				{
					if (!(o is IBlock))
						continue;

					int subWidth = ((IBlock) o).MinimumWidth;
					if (subWidth > retVal)
						retVal = subWidth;
				}
				return retVal;
			}
		}

		public virtual int DesiredWidth
		{
			get
			{
				// TODO: L: this can be false when heavily nested tables are reflowed
				//			suspect it's just to do with cancelling reflow
//				Debug.Assert(reflowCompleted, "DesiredWidth called before reflow completed!");

				int retVal=desiredWidth;

				foreach ( object o in Lines )
				{
					if ( !(o is IBlock) )
						continue;

					int subWidth=((IBlock) o).DesiredWidth;
					if ( subWidth > retVal )
						retVal=subWidth;
				}
				return retVal;
			}
		}

		public bool ReflowCompleted
		{
			get { return reflowCompleted; }
		}

		public void Newline()
		{
			Newline(true);
		}

		private void Newline(bool commit) 
		{
			if ( currentLine.IsEmpty && (queue.Empty || !commit) )
				// nothing to do
				return;

			if ( commit )
				Commit(currentLine, queue);

			Lines.Add(currentLine);
			height+=currentLine.Height;
			currentLine=new Line(this);
		}

		public virtual XmlElement Invalidate(DrawContext dc)
		{
			// Need to consider special case when this is a row inside a rowgroup
			// or table and the table is currently invalid. In this case we might be
			// adding or removing child nodes of the row which cause the table to be
			// valid again, in which case we need to reflow the entire table.
			// TODO: L: could be optimised by working out if the table is now valid
			Table tbl=ParentTableForRow();
			if ( tbl != null && !tbl.IsValid )
				return tbl.Invalidate(dc);

			int w=Width;
			int h=Height;
			// TODO: M: performance?
			int m=MinimumWidth;
			int d=DesiredWidth;

			Reflow(dc, GetBoundsForSelf(dc, false), false);

			XmlElement min=elementNode;
			XmlElement max=null;

			// TODO: L: think about whether width is significant (don't think so)
			if ( /* w != Width || */ h != Height )
			{
				if ( parent != null )
					min=parent.ProcessSizeChange(this, dc);
			}
			if ( m != MinimumWidth || d != DesiredWidth )
			{
				if ( parent != null )
					max=parent.ProcessMetricsChange(this, dc);
			}

			if ( XmlUtil.HasAncestor(min, max) )
				return max;

			return min;
		}

		private Table ParentTableForRow()
		{
			Table tbl=parent as Table;
			if ( tbl != null )
				return tbl;

			if ( parent == null )
				return null;

			// row in row group case
			return parent.Parent as Table;
		}

		public virtual void Reflow(DrawContext dc, BoundingContext bounds, bool incremental)
		{
			ReflowStart(dc);
			bounds=bounds.Narrow(style.Left, style.Right);

			MarkupItem tag=new StartTag(currentLine, ElementNode);

			ItemMetrics bi;
			tag.Compose(dc, style, out bi);
			bounds.Place(tag.Width);
			queue.Add(bi);

			Style activeStyle=(Style) styleStack.Peek();
			ReflowChildren(ElementNode, dc, activeStyle, bounds);

			tag=new EndTag(currentLine, ElementNode);
			tag.Compose(dc, style, out bi);

			if ( !bounds.CanPlace(tag.Width) )
				FlushLine(bi, bounds);
			else
				queue.Add(bi);

			ReflowEnd(dc);
			RecalcBounds();
		}

		public virtual void ReflowChildren(XmlNode e, DrawContext dc, Style activeStyle, BoundingContext bounds)
		{
			bool whitespaceActive=false;

			dc.Graphics.PushFont(dc.Graphics.GetFontHandle(activeStyle.FontDesc));

			reflowCompleted=true;
			foreach ( XmlNode n in e.ChildNodes ) 
			{
				switch ( n.NodeType )
				{
					case XmlNodeType.Element:
						ReflowElement((XmlElement) n, dc, bounds, activeStyle, whitespaceActive);
						whitespaceActive=false;
						break;
			
					case XmlNodeType.SignificantWhitespace:
					case XmlNodeType.Text:
						ReflowText((XmlCharacterData) n, dc, bounds, activeStyle, ref whitespaceActive);
						break;

					case XmlNodeType.Comment:
						ReflowComment(n, dc, bounds, ref whitespaceActive);
						whitespaceActive=false;
						break;

					case XmlNodeType.EntityReference:
						ReflowEntityRef(n, dc, bounds);
						whitespaceActive=false;
						break;

					case XmlNodeType.CDATA:
						ReflowCDataSection(n, dc, bounds, ref whitespaceActive);
						whitespaceActive=true;
						break;

					default:
						Debug.Assert(false, "Unrecognised node type, "+n.NodeType);
						break;
				}

				// this never happens
				if ( dc.ClippingRectangle.Bottom > 0 && Height > dc.ClippingRectangle.Bottom )
				{
					//					Logger.Log("Aborting reflow for {0}, lower bound={1}, clipping={2}",
					//						this.ElementNode.Name, Height, dc.ClippingRectangle);
					reflowCompleted=false;
					break;
				}
			}

			dc.Graphics.PopFont();
		}

		// TODO: M: why is whitespaceActive not used
		private void ReflowElement(XmlElement e, DrawContext dc, BoundingContext bounds, Style activeStyle, bool whitespaceActive)
		{
			Style childStyle=style.Stylesheet.GetStyle(dc.Graphics, activeStyle, e, dc.DocumentType.GetElementType(e));
			IReflowObject ro=childStyle.CreateReflowObject(this, e);
			IBlock block=ro as IBlock;

			if ( block != null )
			{
				ReflowBlock(dc, bounds, block);
			}
			else
			{
				((INestedReflowObject) ro).Reflow(this, dc, bounds, false);
			}
		}

		protected void ReflowBlock(DrawContext dc, BoundingContext bounds, IBlock block)
		{
			Newline(true);

			IBlock existing=GetExistingBlock(block.ElementNode);
			if ( existing != null )
				block=existing;

			childBlocks[block.ElementNode]=block;
			
//			BlockState bs=(BlockState) childBlocks[block.ElementNode];
//			if ( bs == null )
//			{
//				bs=new BlockState(block);
//				childBlocks[block.ElementNode]=bs;
//			} 
//			else
//			{
//				bs.Used=true;
//				throw new InvalidOperationException("Block should always be null during reflow");
//			}
	
			Lines.Add(block);
			currentLine=new Line(this);
	
			block.Reflow(dc, bounds, false);
			reflowCompleted=block.ReflowCompleted;
			height+=block.Height;
			bounds.Reset();
		}

		private void ReflowEntityRef(XmlNode n, DrawContext dc, BoundingContext bounds)
		{
			EntityRef er=new EntityRef(this, n);
			ReflowMarkup(er, dc, style, bounds);
		}

		private void ReflowCDataSection(XmlNode n, DrawContext dc, BoundingContext bounds, ref bool whitespaceActive)
		{
			MarkupItem tag=new CDataStartTag(currentLine, n);
			ReflowMarkup(tag, dc, style, bounds);			

			// TODO: M: change to cdata specific style - literal layout
			Style s=style.Stylesheet.GetCommentStyle(dc.Graphics);

			dc.Graphics.PushFont(dc.Graphics.GetFontHandle(s.FontDesc));
			ReflowText((XmlCDataSection) n, dc, bounds, s, ref whitespaceActive);
			dc.Graphics.PopFont();

			tag=new CDataEndTag(currentLine, n);
			ReflowMarkup(tag, dc, style, bounds);
		}
		
		private void ReflowComment(XmlNode n, DrawContext dc, BoundingContext bounds, ref bool whitespaceActive)
		{
			MarkupItem tag=new CommentStartTag(currentLine, n);
			ReflowMarkup(tag, dc, style, bounds);			

			Style s=style.Stylesheet.GetCommentStyle(dc.Graphics);

			dc.Graphics.PushFont(dc.Graphics.GetFontHandle(s.FontDesc));
			ReflowText((XmlComment) n, dc, bounds, s, ref whitespaceActive);
			dc.Graphics.PopFont();

			tag=new CommentEndTag(currentLine, n);
			ReflowMarkup(tag, dc, style, bounds);
		}

		public void ReflowMarkup(ILineItem tag, DrawContext dc, Style activeStyle, BoundingContext bounds)
		{
			// TODO: M: consider way for line item to indicate line break?
			ItemMetrics bi; // =new BreakInfo();
			tag.Compose(dc, activeStyle, out bi);
			if ( !bounds.CanPlace(tag.Width) )
				FlushLine(bi, bounds);
			else
			{
				bounds.Place(tag.Width);
				queue.Add(bi);
			}

//			if ( bi.CanBreakAfter )
//				Commit(currentLine, queue);
		}

		private void FlushLine(ItemMetrics bi, BoundingContext bounds)
		{
			bool itemAdded;
			FlushLine(bi, bounds, true, out itemAdded);
		}

		private void FlushLine(ItemMetrics bi, BoundingContext bounds, bool forceItem, out bool itemAdded)
		{
			// TODO: M: simplify - definitely possible

			itemAdded=true;

			// can't fit the current item so need to
			// track back and see if there is a split
			// point available

			// TODO: M: could optimise - track whether
			// any item placed on queue can be split as
			// items are added to the queue (new queue class?)
//			int n=queue.Count;
//			bool split=false;
//			while ( n-- > 0 )
//			{
				if ( queue.CanSplit )
//				ILineItem prev=(ILineItem) queue[n];
//				if ( prev.CanSplit )
				{
					LineItemQueue liq=queue.Split();
					Commit(currentLine, liq);

					// set flag
//					split=true;
//					break;
//				} 
//			}
//			if ( split )
//			{
				// line was split so paint the current line
				if ( !currentLine.IsEmpty )
					Newline(false);

				if ( forceItem )
					queue.Add(bi);
				else
					itemAdded=false;

				// start new line and compute offset position from left
				bounds.Reset();
				bounds.Place(queue.CalcWidth);
			}
			else if ( queue.Count == 0 )
			{
				// couldn't split and nothing is in the queue
				// This means that everything on the line is
				// committed - which means that a break is allowed
				// at this point
				if ( !currentLine.IsEmpty )
				{
					Newline(false);

					int x=0;
					if ( forceItem )
					{
						queue.Add(bi);
						x=bi.LineItem.Width;
					} 
					else
						itemAdded=false;

					bounds.Reset();
					bounds.Place(x);
					return;
				}

				// queue and line were empty so we must force
				// whatever could fit onto the queue regardless
				bounds.Reset();
				bounds.Place(bi.LineItem.Width);
				queue.Add(bi);
				if ( bi.CanBreakAfter )
					Commit(currentLine, queue);
			}
			else if ( !currentLine.IsEmpty )
			{
				// couldn't split and there is content on the line
				// already. This means that the line needs to be
				// completed (everything on it has already been committed)

				Newline(false);
				queue.Add(bi);
				bounds.Reset();
				bounds.Place(queue.CalcWidth);

				if ( bi.CanBreakAfter )
					Commit(currentLine, queue);
			}
			else if ( bi.CanBreakBefore )
			{
				Newline(true);

				if ( forceItem )
				{
					queue.Add(bi);
					bounds.Place(bi.LineItem.Width);
				} 
				else
					itemAdded=false;
			}
			else
			{
				// must go on this line
				queue.Add(bi);
				bounds.Place(bi.LineItem.Width);
				if ( bi.CanBreakAfter )
				{
					Newline(true);
					bounds.Reset();
				}
			}
		}

		public void ReflowText(XmlCharacterData t, DrawContext dc, BoundingContext bounds, Style s, ref bool whitespaceActive)
		{
			ITextLayout tr;
			ITextLayout next;

			if ( style.Pre )
				tr=new TextLiteralLayout(currentLine, t);
			else
				tr=new TextFlowLayout(currentLine, t);

			while ( tr != null )
			{
				ItemMetrics itemMetrics;

				bool suppressWhitespace=(currentLine.IsEmpty && queue.Count == 0) || whitespaceActive;

				bool fit=tr.Compose(dc, s, bounds.Remaining, out itemMetrics, suppressWhitespace, out next);
				whitespaceActive=itemMetrics.CanBreakAfter;
				
				if ( fit )
				{
					// 2nd conditional is just for optimisation
					// (line will be committed below if CanBreakAfter is active)
					if ( itemMetrics.CanBreakBefore && !itemMetrics.CanBreakAfter )
						// can commit the current queue because this
						// item can always start a new line (ie. queue
						// will now start with this item)
						Commit(currentLine, queue);

					// add new item to the queue (may not be able to
					// break after)
					queue.Add(itemMetrics);

					if ( itemMetrics.CanBreakAfter )
						// we know this item will appear on this line
						Commit(currentLine, queue);

					// increment position on the line
					bounds.Place(tr.Width);
				}
				else if ( itemMetrics.CanBreakBefore )
				{
					Newline(true);
					tr.Reset();
					next=tr;
					bounds.Reset();
					continue;
				}
				else 
				{
					bool wasAdded;
					FlushLine(itemMetrics, bounds, false, out wasAdded);
					if ( !wasAdded )
					{
						// compose operation may have split the current
						// text element at wrong point, so reset to include
						// complete text and go around again (recompose)
						tr.Reset();
						next=tr;
						continue;
					}
				}

				if ( itemMetrics.ForceBreakAfter )
				{
					Newline(true);
					bounds.Reset();
				}
				tr=next;
			}
		}

		public Rectangle GetBoundingRect(int x, int y, XmlNode n)
		{
			if ( ElementNode.Equals(n) )
				return GetBoundingRect(x, y);

			int ypos=y;
			foreach ( IRegion r in Lines )
			{
				IBlock b=r as IBlock;
				if ( b == null )
				{
					ypos+=r.Height;
					continue;
				}

				Rectangle rc=b.GetBoundingRect(x+style.Left, ypos, n);
				if ( !rc.IsEmpty )
					return rc;

				ypos+=r.Height;
			}
			// not found in any child blocks, so if we're a parent of
			// the node, then this is the containing block for the node
			// so return this bounding rect
			if ( XmlUtil.GetAncestors(n).Contains(ElementNode) )
				return GetBoundingRect(x, y);

			return Rectangle.Empty;
		}

		public override XmlNode GetNodeUnderPoint(int x, int y, Point pt)
		{
			if ( !GetHorizontalRect(x, y).Contains(pt) )
				return null;

			foreach ( Region r in Lines )
			{
				XmlNode ret=r.GetNodeUnderPoint(x+style.Left, y, pt);
				if ( ret != null )
					return ret;

				y+=r.Height;
			}
			// we got here so have excluded all children - it must be this node
			return ElementNode;
		}

		public override HitTestInfo GetHitTestInfo(IGraphics gr, int x, int y, Point pt)
		{
			if ( !GetHorizontalRect(x, y).Contains(pt) )
				return null;

			foreach ( IRegion r in Lines )
			{
				HitTestInfo ret=r.GetHitTestInfo(gr, x+style.Left, y, pt);
				if ( ret != null )
					return ret;

				y+=r.Height;
			}
			// TODO: M: this is throwing error for some tables (may be fixed)
			throw new InvalidOperationException("Point is contained in block but could not find in any sub-region");
		}

		public override void GetCaretPosition(IGraphics gr, int x, int y, SelectionPoint sp, ref CaretPositionInfo cpi)
		{
			foreach ( IRegion r in Lines )
			{
				r.GetCaretPosition(gr, x+style.Left, y, sp, ref cpi);
				if ( cpi.IsFinal )
					return;

				y+=r.Height;
			}
		}

		protected virtual IBlock GetExistingBlock(XmlElement node)
		{
			// override to maintain blocks manually
			return null;
		}
	}

	internal class Line : Region, ILineItemContainer
	{
		private ArrayList lineItems=new ArrayList();
		private int baseline=0;

		public Line(IContainer parent) : base(parent)
		{
		}

		public XmlElement ProcessSizeChange(IContainedItem originator, DrawContext dc)
		{
			return parent.ProcessSizeChange(originator, dc);
		}

		public void AddTop(int val)
		{
			height+=val;
			baseline+=val;
		}

		public void AddBottom(int val)
		{
			height+=val;
		}

		public int Baseline
		{
			get { return baseline; }
		}

		public override void Draw(DrawContext dc, int x, int y, Style c) 
		{
			Rectangle rc=GetBoundingRect(x, y);

			if ( !dc.ClippingRectangle.IntersectsWith(rc) )
				return;

			int pos=x;
			foreach ( ILineItem li in lineItems ) 
			{
				li.Draw(dc, pos, y, baseline, Height, c);
				pos+=li.Width;
			}
		}

		public override ILineItem FindSelectionPoint(SelectionPoint sp)
		{
			foreach ( ILineItem li in lineItems )
			{
				if ( li.ContainsSelectionPoint(sp) )
					return li;
			}
			return null;
		}

		public int ChildCount
		{
			get { return lineItems.Count; }
		}

		public IContainedItem this[int index]
		{
			get { return (IContainedItem) lineItems[index]; }
		}

		public IContainedItem FirstChild
		{
			get { return lineItems.Count > 0 ? (IContainedItem) lineItems[0] : null; }
		}

		public IContainedItem LastChild
		{
			get { return lineItems.Count > 0 ? (IContainedItem) lineItems[lineItems.Count-1] : null; }
		}

		public bool IsEmpty 
		{
			get { return lineItems.Count == 0; }
		}

		public void Add(ILineItem l) 
		{
			l.Parent=this;
			lineItems.Add(l);

			width+=l.Width;
			if ( l.Height > Height )
				height=l.Height;

			if ( l.Ascent > baseline )
				baseline=l.Ascent;
		}

		public void AddRange(ICollection c)
		{
			foreach ( ILineItem l in c )
				Add(l);
		}

		public override string ToString() 
		{
			StringBuilder sb=new StringBuilder();
			foreach ( ILineItem l in lineItems )
			{
				sb.Append(l.ToString());
			}
			return sb.ToString();
		}

		public override XmlNode GetNodeUnderPoint(int x, int y, Point pt)
		{
			if ( !GetHorizontalRect(x, y).Contains(pt) )
				return null;

			int pos=x;
			foreach ( ILineItem li in lineItems ) 
			{
				Rectangle rc=li.GetBoundingRect(pos, y);
				rc.Height+=Height-li.Height;

				if ( rc.Contains(pt) )
					return li.Node;

				pos+=li.Width;
			}

			// we're here so the point must be off to the left or right of the
			// actual space taken up by the line
			int index=0;
			if ( pt.X > x )
				index=lineItems.Count-1;

			return ((ILineItem) lineItems[index]).Node;
		}

		public override HitTestInfo GetHitTestInfo(IGraphics gr, int x, int y, Point pt)
		{
			Rectangle bounds=GetHorizontalRect(x, y);
			if ( !bounds.Contains(pt) )
				return null;

			int pos=x;
			Point lastLocation=Point.Empty;
			foreach ( ILineItem li in lineItems ) 
			{
				// we test bounding rect as we know about additional ascent/descent
				Rectangle rc=li.GetBoundingRect(pos, y);
				lastLocation=rc.Location;

				rc.Height+=Height-li.Height;

				if ( rc.Contains(pt) )
				{
					HitTestInfo ret=li.GetHitTestInfo(gr, pos, y, pt);
					if ( ret != null )
					{
						ret.LineBounds=bounds;
						return ret;
					}
				}

				pos+=li.Width;
			}

			// we're here so the point must be off to the left or right of the
			// actual space taken up by the line
			int index=0;
			if ( pt.X > x )
				index=lineItems.Count-1;

			ILineItem endItem=(ILineItem) lineItems[index];
			SelectionPoint sp=endItem.GetSelectionPoint(pt.X > x);
			Rectangle rcCaret=new Rectangle(0, 0, 1, endItem.Height);
			rcCaret.Offset(pt.X > x ? pos : x, y+baseline-endItem.Ascent);
			LineItemContext ili=new LineItemContext(Height, baseline, endItem, lastLocation);
			HitTestInfo ht=new HitTestInfo(sp, ili, pt.X > x, rcCaret);
			ht.LineBounds=bounds;
			return ht;
		}

		public override void GetCaretPosition(IGraphics gr, int x, int y, SelectionPoint sp, ref CaretPositionInfo cpi)
		{
			int pos=x;
			foreach ( ILineItem li in lineItems ) 
			{
				li.GetCaretPosition(gr, pos, y, sp, ref cpi);
				cpi.AdjustForLine(li.Height,  baseline, li.Ascent);
				if ( cpi.IsFinal )
					return;

				pos+=li.Width;
			}
		}
	}

	internal abstract class LineItemBase : DrawItemBase, ISelectable, ILineItem
	{
		protected Selection selection=null;

		public abstract int Compose(DrawContext dc, Style c, out ItemMetrics bi);
		public abstract void Draw(DrawContext dc, int x, int y, int baseline, int height, Style s);
		public abstract HitTestInfo GetHitTestInfo(IGraphics gr, int x, int y, Point pt);
		public abstract void GetCaretPosition(IGraphics gr, int x, int y, SelectionPoint sp, ref CaretPositionInfo cpi);
		public abstract SelectionPoint GetSelectionPoint(bool atEnd);

		public abstract bool ContainsSelectionPointInternal(SelectionPoint sp);

		protected int ascent=0;

		public LineItemBase(IContainer parent) : base(parent)
		{
		}

		public int Ascent
		{
			get { return ascent; }
		}

		public Selection Selection
		{
			set { selection=value == null ? value : value.Normalise(); }
		}

//		virtual public bool CanSplit
//		{
//			get { return false; }
//		}

//		virtual public ILineItem Split()
//		{
//			// This must be overridden by derived class if it overrides
//			// CanSplit
//			return null;
//		}

//		virtual public void AdjustForStartOfLine()
//		{
//		}

		protected void DrawErrorIndicator(DrawContext dc, int x, int y, bool selected)
		{
			DrawErrorIndicator(dc, x, x, Width, y, selected);
		}

		protected void DrawErrorIndicator(DrawContext dc, int origin, int start, int width, int y, bool selected)
		{
			int pos=0;
			int dy=1;
			int ypos=dc.Origin.Y+y+2; // +Height-2;
			Color col=selected ? Color.White : Color.Red;

			if ( origin != start )
			{
				Rectangle rc=new Rectangle(start, y, width, y);
				rc.Inflate(0, 3);
				rc.Offset(dc.Origin.X, dc.Origin.Y);
				dc.Graphics.FGraphics.IntersectClip(rc);
			}

			int w2=width+start-origin;
			while ( pos < w2 )
			{
				dc.Graphics.FGraphics.DrawLine(new Pen(col), dc.Origin.X+origin+pos, ypos-dy, dc.Origin.X+origin+pos+2, ypos+dy);
				dy=-dy;
				pos+=2;
			}
		}

		public abstract XmlNode Node
		{
			get;
		}

		public bool ContainsSelectionPoint(SelectionPoint sp)
		{
			if ( !sp.Node.Equals(Node) )
				return false;

			return ContainsSelectionPointInternal(sp);
		}

		protected void DrawText(DrawContext dc, Rectangle rc, int baseline, string text, Color col, Color bkCol, bool selected)
		{
			int startHighlight=-1;
			int endHighlight=-1;

			if ( selected )
			{
				startHighlight=0;
				endHighlight=text.Length;
			}
			DrawText(dc, rc, baseline, text, col, bkCol, startHighlight, endHighlight);
		}

		protected void DrawText(DrawContext dc, Rectangle rc, int baseline, string text, Color col, Color bkCol, int startHighlight, int endHighlight) 
		{
//			Debug.Assert(Width >= 0, "LineItem was not composed before attempting to draw"); // must have been composed

			if ( Width < 0 )
				Logger.Log("WARNING: Zero width for {0} (in {1})", text, this.Node.ParentNode.Name);

			if ( text.Length == 0 )
				return;

			// Need to manually offset the drawing according to translation
			// applied in GDI+

			Point origin=dc.Origin;
			rc.X+=origin.X;
			rc.Y+=origin.Y;
//			Rectangle rc=dc.ClippingRectangle;
//			rc.Offset(origin);

			dc.Graphics.DrawText(rc, baseline-ascent, text, col, bkCol, startHighlight, endHighlight);
		}
	}

	internal abstract class Tag : MarkupItem
	{
		public Tag(IContainer parent, XmlNode n) : base(parent, n)
		{
		}

		public abstract TagType Type
		{
			get;
		}

		public override int Compose(DrawContext dc, Style c, out ItemMetrics bi)
		{
			if ( c.Stylesheet.TagMode == TagViewMode.None )
			{
				width=0;
				height=4;
				bi=new ItemMetrics(this);
				return Width;
			} 

			return base.Compose(dc, c, out bi);
		}

		public override void Draw(DrawContext dc, int x, int y, int baseline, int height, Style c) 
		{
			if ( c.Stylesheet.TagMode == TagViewMode.None )
				return;

			base.Draw(dc, x, y, baseline, height, c);

			//			ValidationError vt=(ValidationError) dc.InvalidNodes[node];
			//			if ( vt == null )
			//				return;
			//
			//			if ( vt.Type == ValidationErrorType.RequiredElementMissing && Type == TagType.EndTag )
			//				DrawErrorIndicator(dc, x, y+baseline, Selection != null);
			//			else if ( Type == TagType.StartTag )
			//				DrawErrorIndicator(dc, x, y+baseline, Selection != null);
		}

		public override SelectionPoint GetSelectionPoint(bool atEnd)
		{
			return new ElementSelectionPoint(Node, Type);
		}

		public override HitTestInfo GetHitTestInfo(IGraphics gr, int x, int y, Point pt)
		{
			bool after=(float) 1.0 * x + Width/2 < pt.X;
			SelectionPoint sp=new ElementSelectionPoint(Node, Type);

			ILineItemContainer container=(ILineItemContainer) this.Parent;
			Rectangle rcCaret=Rectangle.Empty;
			if ( after )
				rcCaret=new Rectangle(x+Width, y+container.Baseline-ascent, 1, Height);

			LineItemContext ili=new LineItemContext(container.Height, container.Baseline, this, new Point(x,y));
			return new HitTestInfo(sp, ili, after, rcCaret);
		}

		public override bool ContainsSelectionPointInternal(SelectionPoint sp)
		{
			MarkupSelectionPoint msp=sp as MarkupSelectionPoint;
			if ( msp == null )
				// this is node where the tag is the same node as the value (CDATA)
				return false;

			//			Debug.Assert(msp != null, "Invalid selection point!");

			return msp.Type == Type;
		}

		protected override int GetPolygonWidth(SelectionPoint sel)
		{
			return 1;
		}
	}

	internal class CDataStartTag : StartTag
	{
		public CDataStartTag(IContainer parent, XmlNode n) : base(parent, n)
		{
		}

		protected override string DisplayTextName
		{
			get	{ return "[CDATA["; }
		}
	}

	internal class CDataEndTag : EndTag
	{
		public CDataEndTag(IContainer parent, XmlNode n) : base(parent, n)
		{
		}

		protected override string DisplayTextName
		{
			get	{ return "]]"; }
		}
	}

	internal class CommentStartTag : StartTag
	{
		public CommentStartTag(IContainer parent, XmlNode n) : base(parent, n)
		{
		}

		protected override string DisplayTextName
		{
			get	{ return "!--"; }
		}
	}

	internal class CommentEndTag : EndTag
	{
		public CommentEndTag(IContainer parent, XmlNode n) : base(parent, n)
		{
		}

		protected override string DisplayTextName
		{
			get	{ return "--"; }
		}
	}

	internal class StartTag : Tag
	{
		public StartTag(IContainer parent, XmlNode n) : base(parent, n)
		{
		}

		public override TagType Type
		{
			get { return TagType.StartTag; }
		}

		protected override int TagNameShift
		{
			get { return -2; }
		}

		protected override Point[] GetPolygon(Rectangle rc)
		{
			int midpoint=rc.Height / 2;

			//			rc.Offset(2, 0);
			Point pt1=rc.Location;
			Point pt2=new Point(rc.Right-midpoint, rc.Top);
			Point pt3=new Point(rc.Right, rc.Top+midpoint);
			Point pt4=new Point(rc.Right-midpoint, rc.Bottom);
			Point pt5=new Point(rc.Left, rc.Bottom);

			return new Point[] {pt1, pt2, pt3, pt4, pt5};
		}
	}

	internal class EndTag : Tag
	{
		public EndTag(IContainer parent, XmlNode n) : base(parent, n)
		{
		}

		protected override int TagNameShift
		{
			get { return 2; }
		}

		public override TagType Type
		{
			get { return TagType.EndTag; }
		}

		protected override Point[] GetPolygon(Rectangle rc)
		{
			int midpoint=rc.Height / 2;

			//			rc.Offset(-2, 0);
			Point pt1=new Point(rc.Left, rc.Top+midpoint);
			Point pt2=new Point(rc.Left+midpoint, rc.Top);
			Point pt3=new Point(rc.Right, rc.Top);
			Point pt4=new Point(rc.Right, rc.Bottom);
			Point pt5=new Point(rc.Left+midpoint, rc.Bottom);

			return new Point[] {pt1, pt2, pt3, pt4, pt5};
		}
	}


	internal class CommentTag : SimpleTag
	{
		public CommentTag(IContainer parent, XmlNode e) : base(parent, e)
		{
		}

		protected override string DisplayTextName
		{
			get { return node.Value; }
		}

		protected override Color TagNameColor
		{
			get { return Color.Gray; }
		}
	}

	internal abstract class SimpleTag : MarkupItem
	{
		public SimpleTag(IContainer parent, XmlNode n) : base(parent, n)
		{
		}

		protected override Point[] GetPolygon(Rectangle rc)
		{
			Point pt1=new Point(rc.Left+2, rc.Top);
			Point pt2=new Point(rc.Right-2, rc.Top);
			Point pt3=new Point(rc.Right, rc.Top+2);
			Point pt4=new Point(rc.Right, rc.Bottom-2);
			Point pt5=new Point(rc.Right-2, rc.Bottom);
			Point pt6=new Point(rc.Left+2, rc.Bottom);
			Point pt7=new Point(rc.Left, rc.Bottom-2);
			Point pt8=new Point(rc.Left, rc.Top+2);
			return new Point[] {pt1, pt2, pt3, pt4, pt5, pt6, pt7, pt8};
		}

		protected override int GetPolygonWidth(SelectionPoint sp)
		{
			return 1;
		}

		public override SelectionPoint GetSelectionPoint(bool atEnd)
		{
			return new MarkupSelectionPoint(Node);
		}

		public override HitTestInfo GetHitTestInfo(IGraphics gr, int x, int y, Point pt)
		{
			bool after=(float) 1.0 * x + Width/2 < pt.X;
			MarkupSelectionPoint gsp=new MarkupSelectionPoint(Node);

			Line l=(Line) Parent;
			Rectangle rcCaret=Rectangle.Empty;
			if ( after )
				rcCaret=new Rectangle(x+Width, y+l.Baseline-ascent, 1, Height);

			LineItemContext ili=new LineItemContext(l.Height, l.Baseline, this, new Point(x,y));
			return new HitTestInfo(gsp, ili, after, rcCaret);
		}

		public override bool ContainsSelectionPointInternal(SelectionPoint sp)
		{
			return sp.Node.Equals(Node);
		}
	}

	internal class EntityRef : SimpleTag
	{
		public EntityRef(IContainer parent, XmlNode n) : base(parent, n)
		{
		}

		protected override string DisplayTextName
		{
			get { return "&"+node.Name+";"; }
		}
	}

	internal class EmptyTag : SimpleTag
	{
		public EmptyTag(IContainer parent, XmlNode n) : base(parent, n)
		{
		}

		protected override string DisplayTextName
		{
			get { return node.Name; }
		}

		protected override int GetPolygonWidth(SelectionPoint sel)
		{
			if ( sel.Node.Equals(this.Node) )
			{
				MarkupSelectionPoint msp=(MarkupSelectionPoint) sel;
				return msp.Type == TagType.EndTag ? 2 : 1;
			}
			return 1;
		}

		// TODO: M: not sure when / why this is used (not just in this class but generally)
		public override SelectionPoint GetSelectionPoint(bool atEnd)
		{
//			throw new NotSupportedException();
			return new ElementSelectionPoint(Node, TagType.EndTag);
		}

		public override HitTestInfo GetHitTestInfo(IGraphics gr, int x, int y, Point pt)
		{
			ElementSelectionPoint esp=new ElementSelectionPoint(Node, TagType.EndTag);

			Line l=(Line) Parent;
			Rectangle rcCaret=Rectangle.Empty;

			LineItemContext ili=new LineItemContext(l.Height, l.Baseline, this, new Point(x,y));
			return new HitTestInfo(esp, ili, false, rcCaret);
		}

	}

	internal abstract class MarkupItem : LineItemBase
	{
		protected XmlNode node;
		private const int expandFactor=0;
		private const int internalPadding=6;
		private const int externalPadding=2;

		public MarkupItem(IContainer parent, XmlNode node) : base(parent)
		{
			this.node=node;
			width=-1;
		}

		protected abstract Point[] GetPolygon(Rectangle rc);
		protected abstract int GetPolygonWidth(SelectionPoint sel);

		protected virtual int TagNameShift
		{
			get { return 0; }
		}

		public override int Compose(DrawContext dc, Style c, out ItemMetrics bi) 
		{
			bi=new ItemMetrics(this);

			if ( Width >= 0 )
				// already composed
				return Width;

			int itemWidth=0;

			object tagFont=dc.Graphics.GetFontHandle(c.Stylesheet.TagFont);
			dc.Graphics.PushFont(tagFont);
			Size sz=dc.Graphics.MeasureText(DisplayTextName);
			dc.Graphics.PopFont();

			itemWidth+=sz.Width;
			itemWidth+=internalPadding*2;
			itemWidth+=externalPadding*2;
			this.width=itemWidth;
			height=c.FontHeight;
			ascent=c.FontAscent;

			return Width;
		}

		public override void Draw(DrawContext dc, int x, int y, int baseline, int height, Style c) 
		{
			Rectangle rc=GetBoundingRect(x, y);
			rc.Height+=height-Height;
			if ( !dc.ClippingRectangle.IntersectsWith(rc) )
				return;

			bool invalid=dc.InvalidInfo.Contains(this.Node);

			Color markupColor=invalid ? Color.Red : Color.LightSteelBlue;
			Color tagNameColor=TagNameColor;
			Color bk=Color.White;

			object tagFont=dc.Graphics.GetFontHandle(c.Stylesheet.TagFont);
			dc.Graphics.PushFont(tagFont);

			baseline-=expandFactor;

			if ( selection != null )
			{
				Rectangle rcReverseOut=new Rectangle(x, y, Width, height);
				rcReverseOut.Offset(dc.Origin);
				dc.Graphics.FGraphics.FillRectangle(new SolidBrush(GraphicsBase.InverseOf(bk)), rcReverseOut);
			}

			int ascent=dc.Graphics.GetFontAscent();
			int hAdjust=this.ascent-ascent;

			int pos=x + internalPadding+externalPadding+TagNameShift;
			Size sz=dc.Graphics.MeasureText(DisplayTextName);

			Rectangle rcText=new Rectangle(pos, y, sz.Width, height);
			DrawText(dc, rcText, baseline+hAdjust, DisplayTextName, tagNameColor, bk, selection != null);

			rc.Height=sz.Height+expandFactor;

			rc.Inflate(-externalPadding, 1);
			rc.Offset(0, baseline-ascent-expandFactor/2);
			rc.Offset(dc.Origin);

			Point[] points=GetPolygon(rc);
			int pw=invalid ? 2 : GetPolygonWidth(dc.SelectionPoint);

			dc.Graphics.FGraphics.DrawPolygon(new Pen(markupColor, pw), points);

			dc.Graphics.PopFont();
		}

		protected virtual string DisplayTextName
		{
			get { return node.Name; }
		}

		protected virtual Color TagNameColor
		{
			get { return Color.DarkRed; }
		}

		public override XmlNode Node 
		{
			get { return node; }
		}

		public override void GetCaretPosition(IGraphics gr, int x, int y, SelectionPoint sp, ref CaretPositionInfo cpi)
		{
			if ( ContainsSelectionPoint(sp) )
				cpi.UpdateLocation(x, y, Height, CaretSetting.Accurate);
			else
				cpi.UpdateLocation(x+Width, y, Height, CaretSetting.Fallback);
		}
	}
}
