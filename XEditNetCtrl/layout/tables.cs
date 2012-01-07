using System;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml;
using System.Drawing;
using System.Xml.Schema;
using XEditNet.Dtd;
using XEditNet.Layout;
using XEditNet.Styles;
using XEditNet.Location;

// TODO: C: insert of tr fails

namespace XEditNet.Layout.Tables
{
	internal class Table : BlockImpl
	{
//		private ArrayList rows;
		private ArrayList columns;
		private Hashtable grid;
		private bool valid;
		private int lMargin;
		private int rMargin;
		private bool columnsInitialised=false;
		private int availableWidth;

		public enum CellReflowState
		{
			None,
			Completed,
			Reset
		}

		internal CellInfo this[XmlElement e]
		{
			get
			{
				return (CellInfo) grid[e];
			}
		}

		public class ColumnInfo
		{
			public int Index;
			public bool Specified=false;
			public int MinimumWidth=0;
			public int DesiredWidth=0;
			public int CalculatedWidth=0;
			public double proportionalWidth=0;
//			public bool HasMinWidth=false;

			public ColumnInfo(int colNum)
			{
				this.Index=colNum;
			}

			public int Width
			{
				get { return Math.Max(MinimumWidth, CalculatedWidth); }
			}

			public void Update(TableCell cell)
			{
				MinimumWidth=Math.Max(MinimumWidth, cell.MinimumWidth);
				DesiredWidth=Math.Max(DesiredWidth, cell.DesiredWidth);
			}

			public bool IsEquivalentTo(ColumnInfo other)
			{
				return other.MinimumWidth == MinimumWidth && other.DesiredWidth == DesiredWidth;
			}

			public double ProportionalWidth
			{
				get { return proportionalWidth; }
				set
				{
					proportionalWidth=value;
					Specified=true;
				}
			}
		}

		internal class CellInfo
		{
			public CellReflowState State=CellReflowState.None;
			public TableCell Cell=null;
			public ColumnInfo Column;

			public CellInfo(TableCell cell, ColumnInfo col)
			{
				this.Cell=cell;
				this.Column=col;
			}
		}

		public Table(IContainer parent, XmlElement e, Style style) : base(parent, e, style)
		{
		}

		public override void Draw(DrawContext dc, int x, int y, Style s)
		{
			base.Draw(dc, x, y, s);
			Rectangle rc=GetBoundingRect(x, y);
			rc.Offset(dc.Origin);
			dc.Graphics.FGraphics.DrawRectangle(Pens.LightGray, rc);

//			rc.Width=MinimumWidth;
//			dc.Graphics.FGraphics.DrawRectangle(new Pen(Color.Red, 4), rc);
			//			ControlPaint.DrawFocusRectangle(dc.Graphics.FGraphics, rc);
		}

		public override int MinimumWidth
		{
			get
			{
				int w=0;
				w+=lMargin+rMargin;
				foreach ( ColumnInfo col in columns )
					w+=col.MinimumWidth;

				return w;
			}
		}

		private void ReflowInvalidCells(DrawContext dc)
		{
			foreach ( CellInfo ci in grid.Values )
			{
				// TODO: L: cell might not be reflowed (suspect to do with partial reflow)
//				Debug.Assert(ci.State != CellReflowState.None, "Cell was not reflowed!");
				if ( ci.State == CellReflowState.Reset )
				{
					BoundingContext bounds=new BoundingContext(GetBoundsForSelf(dc, false), ci.Column.Width);
					ci.Cell.Reflow(dc, bounds, false);
					// TODO: M: this causes unnecessary processing of size changes
					//			up the stack
					BlockHelper.ProcessSizeChange((IBlock) ci.Cell.Parent);
				}
			}
		}

		private void InitColumnWidths(DrawContext dc, BoundingContext bounds)
		{
//			BoundingContext bounds=GetBoundsForSelf(dc, false);
			bounds=bounds.Narrow(lMargin, rMargin+10);
			availableWidth=bounds.Width;
			double pw=1.0/columns.Count;
			foreach ( ColumnInfo c in columns )
			{
				c.Specified=false;
				c.ProportionalWidth=0;
				c.Specified=false;
				c.CalculatedWidth=(int) Math.Round(bounds.Width * pw);
				c.MinimumWidth=0;
				c.DesiredWidth=0;
			}
			if ( columnsInitialised )
			{
				foreach ( CellInfo ci in grid.Values )
				{
					if ( ci.State == CellReflowState.Completed )
						ci.State=CellReflowState.Reset;
				}
			}
			columnsInitialised=true;
		}

		private void Update(int colNum, int min, int desired)
		{
			ColumnInfo column=(ColumnInfo) columns[colNum];
			column.MinimumWidth=min;
			column.DesiredWidth=desired;

			RebalanceColumns();
		}

		private void RebalanceColumns()
		{
			double sperc=0;
			int fmin=0;
			int tmin=0;
			int fdesired=0;

			double[] pwidths=new double[columns.Count];

			int n=0;
			foreach ( ColumnInfo columnInfo in columns )
			{
				if ( columnInfo.MinimumWidth == 0 )
					// not all columns calculated yet, so no point continuing
					return;

				tmin+=columnInfo.MinimumWidth;

				if ( columnInfo.Specified )
				{
					sperc+=columnInfo.ProportionalWidth;
					pwidths[n]=columnInfo.ProportionalWidth;
				}
				else
				{
					fmin+=columnInfo.MinimumWidth;
					fdesired+=columnInfo.DesiredWidth;
					pwidths[n]=-1;
				}
				n++;
			}

			double fperc=Math.Max(0, 1-sperc);
			n=0;
			foreach ( ColumnInfo columnInfo in columns )
			{
				if ( pwidths[n] < 0 )
					pwidths[n]=fperc * columnInfo.DesiredWidth / fdesired;

				n++;
			}

			int perfectWidth=availableWidth;
			int nonMinWidth=availableWidth-tmin;

			double tperc=1;

			n=0;
			foreach ( ColumnInfo columnInfo in columns )
			{
				int maxWidthColumn=nonMinWidth+columnInfo.MinimumWidth;
				double proportion=pwidths[n++];

				int calculatedWidth=(int) Math.Round(perfectWidth * proportion / tperc);
				calculatedWidth=Math.Min(calculatedWidth, maxWidthColumn);
				calculatedWidth=Math.Max(calculatedWidth, columnInfo.MinimumWidth);

				tperc-=proportion;
				perfectWidth-=calculatedWidth;
				nonMinWidth+=columnInfo.MinimumWidth;
				nonMinWidth-=calculatedWidth;

				if ( calculatedWidth != columnInfo.Width )
				{
					columnInfo.CalculatedWidth=calculatedWidth;
					ResetColumn(columnInfo.Index);
				}
			}
		}

		private void ResetColumn(int colNum)
		{
			foreach ( CellInfo ci in grid.Values )
			{
				if ( ci.Column.Index == colNum && ci.State == CellReflowState.Completed )
					ci.State=CellReflowState.Reset;
			}
		}

		public bool Update(TableCell cell)
		{
			CellInfo cellInfo=this[cell.ElementNode];
			cellInfo.State=CellReflowState.Completed;

			ColumnInfo existingCol=cellInfo.Column;
			int colNum=existingCol.Index;

			int min=0;
			int desired=0;
			foreach ( CellInfo info in grid.Values )
			{
				if ( info.Column.Index != colNum )
					continue;

				if ( info.State == CellReflowState.Completed || info.State == CellReflowState.Reset )
				{
					min=Math.Max(min, info.Cell.MinimumWidth);
					desired=Math.Max(desired, info.Cell.DesiredWidth);
				}
			}

			if ( min != existingCol.MinimumWidth || desired != existingCol.DesiredWidth )
			{
				Update(colNum, min, desired);
				// TODO: L: likely to reset the same column twice (in RebalanceColumns)
				ResetColumn(colNum);
				return true;
			}
			return false;
		}

		public XmlElement Invalidate(DrawContext dc, TableCell cell)
		{
			if ( Update(cell) )
			{
				int w=Width;
				int h=Height;

				ReflowInvalidCells(dc);

				RecalcBounds();
				if ( w != Width || h != Height )
				{
					if ( parent != null )
						return parent.ProcessSizeChange(this, dc);
				}
				return ElementNode;
			}

			// no change needed
			return null;
		}

		protected override void ReflowStart(DrawContext dc)
		{
			base.ReflowStart(dc);

			valid=true;
//			rows=new ArrayList();
			columns=new ArrayList();
			grid=new Hashtable();

			foreach ( XmlNode n1 in elementNode.ChildNodes )
			{
				XmlElement element=n1 as XmlElement;
				if ( element == null )
				{
//					valid=false;
					continue;
				}

				Style s=style.Stylesheet.GetStyle(dc.Graphics, style, element, dc.DocumentType.GetElementType(element));
				if ( s is TableRowStyle )
				{
					InitialiseRow(element, s, dc);
				}
				else if ( s is TableRowGroupStyle )
				{
					InitialiseRowGroup(element, s, dc);
				}
//				else
//					valid=false;
			}
			return;
		}

		protected override void ReflowEnd(DrawContext dc)
		{
			base.ReflowEnd(dc);
			if ( !valid )
				return;

			ReflowInvalidCells(dc);
		}


		private void InitialiseRowGroup(XmlElement groupElement, Style style, DrawContext dc)
		{
			foreach ( XmlNode node in groupElement.ChildNodes )
			{
				XmlElement element=node as XmlElement;
				if ( element == null )
				{
					valid=false;					
					continue;
				}

				Style s=style.Stylesheet.GetStyle(dc.Graphics, style, element, dc.DocumentType.GetElementType(element));
				if ( s is TableRowStyle )
					InitialiseRow(element, s, dc);
				else
					valid=false;
			}
		}

		private void InitialiseRow(XmlElement rowElement, Style s, DrawContext dc)
		{
			TableRow row=new TableRow(this, rowElement, s);
//			rows.Add(row);
			int colNum=0;
			foreach ( XmlNode n2 in rowElement.ChildNodes )
			{
				XmlElement cellElement=n2 as XmlElement;
				if ( cellElement == null )
				{
					valid=false;
					continue;
				}

				s=style.Stylesheet.GetStyle(dc.Graphics, style, cellElement, dc.DocumentType.GetElementType(cellElement));
				if ( s is TableCellStyle )
				{
					if ( colNum >= columns.Count )
						columns.Add(new ColumnInfo(colNum));

					ColumnInfo colInfo=(ColumnInfo) columns[colNum];
					if ( !cellElement.GetAttribute("width").Equals("") )
						colInfo.ProportionalWidth=double.Parse(cellElement.GetAttribute("width"));

					TableCell cell=new TableCell(row, cellElement, s);
					row.AddCell(cell);
					CellInfo ci=new CellInfo(cell, colInfo);
					grid.Add(cellElement, ci);
					colNum++;
				}
				else
					valid=false;
			}
		}

		public int ColumnCount
		{
			// TODO: L: check for null
			get { return columns.Count; }
		}

		public void InitRow(DrawContext dc, Tag startTag, Tag endTag, BoundingContext bounds)
		{
			bool invalidate=false;
			if ( lMargin < startTag.Width )
			{
				lMargin=startTag.Width;
				invalidate=true;
			}
			if ( rMargin < startTag.Width )
			{
				rMargin=startTag.Width;
				invalidate=true;
			}

			if ( invalidate || !columnsInitialised )
			{
				InitColumnWidths(dc, bounds);
			}
		}

		public bool IsValid
		{
			get { return valid; }
		}

//		public Column GetColumnForCell(TableCell cell)
//		{
//			CellInfo ci=(CellInfo) grid[cell.ElementNode];
//			if ( ci == null )
//				throw new ArgumentException("Cell is not in table grid");
//
//			return columns[ci.Column];
//		}
//
//		public void SetActualColumnWidth(int index, int width, bool minWidth)
//		{
//			if ( columns[index].HasMinWidth )
//			{
//				int curWidth=columns[index].ActualWidth;
//				Debug.Assert(curWidth > 0, "Expected non-zero min column width!");
//
//				if ( minWidth )
//					columns[index].ActualWidth=Math.Max(width, curWidth);
//			}
//			else
//			{
//				columns[index].ActualWidth=width;
//				columns[index].HasMinWidth=minWidth;
//			}
//		}

//		public int GetColumnWidth(int index)
//		{
//			return columns[index].ActualWidth;
//		}
//
//		public Column GetColumn(int index)
//		{
//			return columns[index];
//		}

//		public void ReflowRows(DrawContext dc, BoundingContext bounds, TableRow row)
//		{
//			throw new NotImplementedException();
//		}
	}

	internal class TableRow : Region, IBlock, IEnumerable, ILineItemContainer
	{
		protected XmlElement elementNode;
		protected Style style;
		protected ArrayList cells=new ArrayList();
//		private bool needReflow=true;
		private bool reflowCompleted=false;
		private StartTag startTag;
		private EndTag endTag;
//		private int availCellWidth;

		public TableRow(IContainer parent, XmlElement e, Style style) : base(parent)
		{
			this.elementNode=e;
			this.style=style;
		}

		// TODO: M: this is same as method GetBounds(sig) in BlockImpl
		private BoundingContext GetBoundsForSelf(DrawContext dc, bool adjust)
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


		public XmlElement ElementNode
		{
			get { return elementNode; }
		}

		public int ChildCount
		{
			get 
			{
				return cells.Count+2;
			}
		}

		public int Baseline
		{
			get
			{
				// the baseline is calculated
				// so that the start/end tags sit at the top of the
				// rectangle
				return startTag.Ascent;
			}
		}

		public IContainedItem this[int index]
		{
			get {
				if ( index == 0 )
					return startTag;
				else if ( index > cells.Count )
					return endTag;

				return (IContainedItem) cells[index-1];
			}
		}

		public IContainedItem FirstChild
		{
			get { return cells.Count > 0 ? (IContainedItem) cells[0] : null; }
		}

		public IContainedItem LastChild
		{
			get { return cells.Count > 0 ? (IContainedItem) cells[cells.Count-1] : null; }
		}

		public override BoundingContext GetBoundsForChild(DrawContext dc, IContainedItem child)
		{
			BoundingContext bounds=parent.GetBoundsForChild(dc, this);

			TableCell cell=child as TableCell;
			if ( cell == null )
				return bounds;

			Table.ColumnInfo col=ParentTable[cell.ElementNode].Column;

			return new BoundingContext(bounds, col.Width);
		}

		private Table ParentTable
		{
			// TODO: H: error checking
			get
			{
				IContainer p=parent;
				while ( p != null && !(p is Table) )
					p=p.Parent;

				return (Table) p;
			}
		}

		public XmlElement ProcessSizeChange(IContainedItem originator, DrawContext dc)
		{
			// TODO: L: don't think this will ever do anything
			return BlockHelper.ProcessSizeChange(this);
			// if a cell changes size then we may need to invalidate the whole table
//			XmlElement min=BlockHelper.ProcessSizeChange(this);
//
//			TableCell cell=(TableCell) originator;
//			XmlElement max=ParentTable.Invalidate(dc, cell);
//			if ( max == null )
//				max=cell.ElementNode;
//
//			if ( XmlUtil.HasAncestor(min, max) )
//				return max;
//
//			return min;
		}

		public override XmlElement ProcessMetricsChange(IContainedItem originator, DrawContext dc)
		{
			TableCell cell=(TableCell) originator;

			XmlElement min=ParentTable.Invalidate(dc, cell);
			XmlElement max=parent.ProcessMetricsChange(this, dc);
			return max == null ? min : max;
		}

//		public bool NeedReflow
//		{
//			get { return needReflow; }
//		}

		public bool ReflowCompleted
		{
			get { return reflowCompleted; }
		}

		public Style Style
		{
			get { return style; }
		}

		public IBlock FindBlock(XmlElement e)
		{
			if ( e.Equals(elementNode) )
				return this;

			return BlockHelper.FindBlock(this, e, cells);
		}

		public override ILineItem FindSelectionPoint(SelectionPoint sp)
		{
			if ( startTag.ContainsSelectionPoint(sp) )
				return startTag;
			else if ( endTag.ContainsSelectionPoint(sp) )
				return endTag;

			foreach ( TableCell c in cells )
			{
				ILineItem ret=c.FindSelectionPoint(sp);
				if ( ret != null )
					return ret;
			}
			return null;
		}

		public override XmlNode GetNodeUnderPoint(int x, int y, Point pt)
		{
			if ( !GetHorizontalRect(x, y).Contains(pt) )
				return null;

			int pos=x+startTag.Width;
			foreach ( TableCell c in cells )
			{
				XmlNode ret=c.GetNodeUnderPoint(pos, y, pt);
				if ( ret != null )
					return ret;

				pos+=ParentTable[c.ElementNode].Column.Width;
			}
			// we got here so have excluded all children - it must be this node
			return ElementNode;
		}

		public Rectangle GetBoundingRect(int x, int y, XmlNode n)
		{
			if ( ElementNode.Equals(n) )
				return GetBoundingRect(x, y);

			int xpos=x;
			foreach ( TableCell c in cells )
			{
				Rectangle rc=c.GetBoundingRect(xpos, y, n);
				if ( !rc.IsEmpty )
					return rc;

				xpos+=c.Width;
			}
			// not found in any child cells, so if we're a parent of
			// the node, then this is the containing block for the node
			// so return this bounding rect
			if ( XmlUtil.GetAncestors(n).Contains(elementNode) )
				return GetBoundingRect(x, y);

			return Rectangle.Empty;
		}

		public override HitTestInfo GetHitTestInfo(IGraphics gr, int x, int y, Point pt)
		{
			if ( !GetHorizontalRect(x, y).Contains(pt) )
			{
				return null;
			}

			// is the X pos of the cursor in same vertical rect as start tag
			if ( startTag.GetBoundingRect(x, y).Contains(new Point(pt.X, y)) )
				return startTag.GetHitTestInfo(gr, x, y, pt);

			Point lastLocation=Point.Empty;
			int pos=x+startTag.Width;
			foreach ( TableCell cell in cells )
			{
				int width=ParentTable[cell.ElementNode].Column.Width;

				// here we override default behaviour and check if region contains
				// point, because don't want a line to grab the point and think the
				// cursor is beyond the end of the line

				Rectangle rcCell=cell.GetBoundingRect(pos, y);
				lastLocation=rcCell.Location;
				// ensure cell is full-height
				Rectangle rcTest=rcCell;
				rcTest.Height=Height;
				rcTest.Width=width;

				// check to see if the cursor is in the grab area of the cell (right edge)
				Rectangle rcGrabTest=rcTest;
				rcGrabTest.Width=6;
				rcGrabTest.X+=width-3;
				if ( rcGrabTest.Contains(pt) )
				{
					HitTestInfo hti=endTag.GetHitTestInfo(gr, pos, y, pt);
					hti.Type=HitTestType.TableColumnResize;
					return hti;
				}

				// we need to test each cell here otherwise first cell will grab
				// everything across horizontal region
				if ( rcTest.Contains(pt) )
				{
					if ( pt.Y >= rcCell.Bottom )
						pt.Y=rcCell.Bottom-1;

					HitTestInfo ret=cell.GetHitTestInfo(gr, pos, y, pt);
					if ( ret != null )
						return ret;
				}

				pos+=width;
			}

			// it must be in the end tag
			return endTag.GetHitTestInfo(gr, pos, y, pt);
		}

		public override void GetCaretPosition(IGraphics gr, int x, int y, SelectionPoint sp, ref CaretPositionInfo cpi)
		{
			startTag.GetCaretPosition(gr, x, y, sp, ref cpi);
			if ( cpi.IsFinal )
				return;

			int pos=x+startTag.Width;
			foreach ( TableCell cell in cells ) 
			{
				cell.GetCaretPosition(gr, pos, y, sp, ref cpi);
				if ( cpi.IsFinal )
					return;

				int cellWidth=ParentTable[cell.ElementNode].Column.Width;
				pos+=cellWidth;
			}

			endTag.GetCaretPosition(gr, pos, y, sp, ref cpi);
			if ( cpi.IsFinal )
				return;
		}

		public XmlElement Invalidate(DrawContext dc)
		{
			return ParentTable.Invalidate(dc);
//			Reflow(dc, GetBoundsForSelf(dc, false), true);
//			return BlockHelper.ProcessSizeChange(this);
		}

		public void Reflow(DrawContext dc, BoundingContext bounds, bool incremental)
		{
			reflowCompleted=true;

			// TODO: L: do something with item metrics?
			ItemMetrics im;
			startTag=new StartTag(this, elementNode);
			startTag.Compose(dc, style, out im);

			endTag=new EndTag(this, elementNode);
			endTag.Compose(dc, style, out im);

			ParentTable.InitRow(dc, startTag, endTag, bounds);

			cells.Clear();
			foreach ( XmlNode n in elementNode.ChildNodes )
			{
				XmlElement e=n as XmlElement;
				if ( e == null )
					continue;

				Table.CellInfo ci=ParentTable[e];
				TableCell c=ci.Cell;
				cells.Add(c);
				BoundingContext newBounds=new BoundingContext(bounds, ci.Column.Width);
				c.Parent=this;
				c.Reflow(dc, newBounds, false);
				ParentTable.Update(c);
			}

			RecalcBounds();
		}

		public bool IsSingleLine
		{
			// TODO: L: review this - is it really not a single line?
			get { return false; }
		}

		public override void Draw(DrawContext dc, int x, int y, Style c) 
		{
			if ( y + Height < dc.ClippingRectangle.Top )
				// we're not visible
				return;

//			int count=this.ElementNode.ChildNodes.Count;

			dc.Graphics.PushFont(dc.Graphics.GetFontHandle(c.FontDesc));

			startTag.Draw(dc, x, y, startTag.Ascent, startTag.Height, style);

			// TODO: H: invalid stylesheet can cause errors, ie. if cell/row defined without
			//			parent table

			// lines are actually our cells
			int pos=x+startTag.Width;
			foreach ( TableCell cell in cells ) 
			{
				cell.Draw(dc, pos, y, c);

				int cellWidth=ParentTable[cell.ElementNode].Column.Width;
//				int minWidth=ParentTable[cell.ElementNode].Column.MinimumWidth;

				Rectangle rc=new Rectangle(pos, y, cellWidth, Height);
				rc.Offset(dc.Origin);
				dc.Graphics.FGraphics.DrawRectangle(Pens.LightGray, rc);

//				rc.Width=minWidth;
//				dc.Graphics.FGraphics.DrawRectangle(new Pen(Color.Blue, 2), rc);

				pos+=cellWidth;
			}

			endTag.Draw(dc, pos, y, endTag.Ascent, endTag.Height, style);

//			Rectangle rc2=GetBoundingRect(x, y);
//			rc2.Offset(dc.Origin);
//			rc2.Width=MinimumWidth;
//			dc.Graphics.FGraphics.DrawRectangle(new Pen(Color.Cyan, 3), rc2);

//			dc.Graphics.FGraphics.DrawLine(Pens.LightGray, 
//				rc.Location, new Point(rc.Right, rc.Top));
//			dc.Graphics.FGraphics.DrawLine(Pens.LightGray, 
//				new Point(rc.Left, rc.Bottom), new Point(rc.Right, rc.Bottom));

			dc.Graphics.PopFont();
		}

		public void RecalcBounds()
		{
			height=0;
			width=0;

			width+=startTag.Width+endTag.Width;
			height=startTag.Height;
			foreach ( TableCell cell in cells )
			{
				width+=ParentTable[cell.ElementNode].Column.Width;
				if ( cell.Height > Height )
					height=cell.Height;
			}
		}

		public int MinimumWidth
		{
			get 
			{
				int w=0;
				w+=startTag.Width;
				w+=endTag.Width;

				foreach ( TableCell cell in cells )
					w+=cell.MinimumWidth;

				return w;
			}
		}

		public int DesiredWidth
		{
			get 
			{
				int w=0;
				w+=startTag.Width;
				w+=endTag.Width;

				foreach ( TableCell cell in cells )
					w+=cell.DesiredWidth;

				return w;
			}
		}

		private class RowItemEnumerator : IEnumerator
		{
			private int pos=-2;
			private TableRow row;

			public RowItemEnumerator(TableRow row)
			{
				this.row=row;
			}

			public object Current
			{
				get
				{
					if ( pos == -2 )
						throw new InvalidOperationException("RowItemEnumerator still in start state - call MoveNext");

					if ( pos == -1 ) 
						return row.startTag;
					else if ( pos < row.cells.Count )
						return row.cells[pos];
					else
						return row.endTag;
				}
			}

			public bool MoveNext()
			{
				if ( pos++ >= row.cells.Count )
					return false;

				return true;
			}

			public void Reset()
			{
				pos=-2;
			}
		}

		public IEnumerator GetEnumerator()
		{
			return new RowItemEnumerator(this);
		}

		public void AddCell(TableCell cell)
		{
			cells.Add(cell);
		}
	}

	internal class TableCell : BlockImpl
	{
		public TableCell(IContainer parent, XmlElement e, Style style) : base(parent, e, style)
		{
		}

//		public override XmlElement Invalidate(DrawContext dc)
//		{
//			// any change to a cell can affect parent
//
//			// TODO: M: check if column is specified as this can affect need to invalidate all
//			Reflow(dc, GetBoundsForSelf(dc, false), false);
//			return parent.ProcessSizeChange(this, dc);
//		}

	}
}
