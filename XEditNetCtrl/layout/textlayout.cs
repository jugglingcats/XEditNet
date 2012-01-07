using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml;
using XEditNet.Location;
using XEditNet.Styles;

// TODO: H: there is a problem when content ends with \r\n or even double spaces
//			user can easily enter double spaces in editor or can paste the newlines
//			left cursor and backspace behaviour is not correct in these cases

namespace XEditNet.Layout
{
	internal interface ISplittable
	{
		ItemMetrics Split();

		int MinStartWidth
		{
			get;
		}
		int MinEndWidth
		{
			get;
		}

		bool CanSplit { get; }
	}

	internal class TextItemMetrics : ItemMetrics, ISplittable
	{
		private ArrayList segments=new ArrayList();

		private class Segment
		{
			int width=0;

			public int Width
			{
				get { return width; }
			}

			public Segment(int start, int len, int width)
			{
				this.width=width;
			}
		}

		public TextItemMetrics(ILineItem item) : base(item)
		{
		}

		public void AddSegment(int start, int len, int width)
		{
			segments.Add(new Segment(start, len, width));
		}

		public override bool CanSplit
		{
			get { return segments.Count > 1; }
		}

		public ItemMetrics Split()
		{
			// TODO: M: this can be optimised - we have all the info in the segments (I think)

			TextFlowLayout current=(TextFlowLayout) item;
			TextFlowLayout tmp=current.Split();
			// new item may contain spaces at start
			tmp.AdjustForStartOfLine();
			return new TextItemMetrics(tmp);
		}

		public int MinStartWidth
		{
			get { return ((Segment) segments[0]).Width; }
		}

		public override int MinimumWidth
		{
			get
			{
				int retVal=0;
				foreach ( Segment s in segments )
					retVal=Math.Max(s.Width, retVal);

				return retVal;
			}
		}

		public int MinEndWidth
		{
			get { return ((Segment) segments[segments.Count-1]).Width; }
		}

	}

	internal abstract class TextLayoutBase : LineItemBase, ITextLayout
	{
		protected XmlCharacterData textNode;
		protected int len;
		protected int start=0;
		protected Style style;

		public TextLayoutBase(IContainer parent, XmlCharacterData t) : base(parent)
		{
			width=-1;
			height=-1;
			ascent=-1;

			textNode=t;
			len=t.Value.Length;
		}

		public abstract bool Compose(DrawContext dc, Style c, int width, out ItemMetrics bi, bool suppressWhitespace, out ITextLayout next);
		public abstract void Reset();

		protected string ProcessText(string t)
		{
			return t.Replace("\r", " ").Replace('\n', ' ').Replace('\t', ' ');
		}

		protected string Text
		{
			get { return textNode.Value.Substring(start, len); }
		}

		public override int Compose(DrawContext dc, Style c, out ItemMetrics bi)
		{
			// TODO: L: this is never called - refactor virtual methods

			// this is the default Compose which assumes that the text
			// has been fragmented so we just need to layout what we have

			Debug.Assert(Width >= 0, "TextFragment not composed"); // must have already been composed

			bi=new ItemMetrics(this);
			return Width;
		}

		public override XmlNode Node 
		{
			get { return textNode; }
		}

		public override HitTestInfo GetHitTestInfo(IGraphics gr, int x, int y, Point pt)
		{
			// caller will have tested if point is within bounding rect
			// taking into account additional ascent/descent on the line

			Debug.Assert(style != null, "No class attached to TextFragment!!");

			string text=ProcessText(Text);

			gr.PushFont(gr.GetFontHandle(style.FontDesc));
			try 
			{
				BinaryChopper bc=new BinaryChopper(text);
				int w=0;
				while ( bc.CanMove )
				{
					// TODO: L: this could be optimised by calculating the shift
					// left and right in pixels rather than measuring from zero
					w=gr.MeasureText(bc.Text).Width;
					if ( pt.X < x+w )
						bc.TooLong();
					else
						bc.TooShort();
				}
				int cw=gr.MeasureText(text[bc.Position].ToString()).Width;
				bool after=(float) 1.0 * x + w - cw / 2 < pt.X;

				Debug.Assert(start+bc.Position < Node.Value.Length, "Invalid TextSelectionPoint!");
				SelectionPoint sp=new TextSelectionPoint(Node, start+bc.Position);

				Line l=(Line) Parent;
				LineItemContext ili=new LineItemContext(l.Height, l.Baseline, this, new Point(x,y));
				HitTestInfo ht=new HitTestInfo(sp, ili, after);
				return ht;
			}
			finally 
			{
				gr.PopFont();
			}
		}

		public override void GetCaretPosition(IGraphics gr, int x, int y, SelectionPoint sp, ref CaretPositionInfo cpi)
		{
			// TODO: M: this is very innefficient since it is called for all text nodes during search
			if ( !ContainsSelectionPoint(sp) )
			{
				cpi.UpdateLocation(x+Width, y, Height, CaretSetting.Fallback);
				return;
			}

			TextSelectionPoint tsp=(TextSelectionPoint) sp;

			CaretSetting mode=CaretSetting.Absolute;

			gr.PushFont(gr.GetFontHandle(style.FontDesc));
			try
			{
				int n=tsp.Index-start;
				if ( n < 0 )
				{
					// position is not in visible part of the string, it's
					// in whitespace that has been trimmed.
					cpi.UseSecondary=true;
					return;
				} else if ( tsp.Index == 0 )
				{
					// caret can be put somewhere else if necessary
					// TODO: L: this is a bit simplistic - will be wrong if text nodes are
					//			split and first node ends with space and ends a line (!)
					mode=CaretSetting.Accurate;
				}

				string text=Text;

				if ( n > text.Length )
					// TODO: L: not sure exactly when this happens
					n=text.Length;

				text=ProcessText(text.Substring(0, n));

				int w=gr.MeasureText(text).Width;
				cpi.UpdateLocation(x+w, y, Height, mode);
			}
			finally 
			{
				gr.PopFont();
			}
		}

		private class BinaryChopper
		{
			private Stack endPoints=new Stack();
			private string text;
			private int len;
			private int start;

			public BinaryChopper(string text)
			{
				this.text=text;
				this.len=text.Length;
				start=0;
				endPoints.Push(new IntPtr(len));
			}

			private int End 
			{
				get 
				{
					// TODO: H: this can throw exception - stack empty
					//			(pretty sure this only happens if something not rendered correctly)
					return ((IntPtr) endPoints.Peek()).ToInt32();
				}
			}

			public string Text
			{
				get { return text.Substring(0, End); }
			}

			public int Position
			{
				get { return End; }
			}

			public bool CanMove
			{
				get { return start != End; }
			}
			public void TooLong()
			{
				int end=(start + End) / 2;
				endPoints.Push(new IntPtr(end));
			}
			public void TooShort()
			{
				start=End;
				endPoints.Pop();
			}
		}

		public override void Draw(DrawContext dc, int x, int y, int baseline, int height, Style c) 
		{
			Debug.Assert(Height >= 0, "TextFragment is not composed!");

			Rectangle rc=GetBoundingRect(x, y);
			rc.Height+=height-Height;
			if ( !dc.ClippingRectangle.IntersectsWith(rc) )
				return;

			string text=ProcessText(Text); // .Replace('\r', ' ').Replace('\t', ' ').Replace('\n', ' ');
			int startIndex=0;
			int endIndex=0;

			Color bkCol=c.Stylesheet.HighlightColor;

			bool inError=dc.InvalidInfo.Contains(Node);

			dc.Graphics.PushFont(dc.Graphics.GetFontHandle(style.FontDesc));
//			dc.Graphics.PushFont(style.FontHandle);
			try 
			{
				if ( selection != null )
				{
					SelectionPoint startSel=selection.Start;
					SelectionPoint endSel=selection.End;

					// by default everything selected
					endIndex=text.Length;

					if ( ContainsSelectionPoint(startSel) )
					{
						TextSelectionPoint sel=(TextSelectionPoint) startSel;
						startIndex=sel.Index-start;
						if ( startIndex < 0 )
							startIndex=0;
					}
					if ( ContainsSelectionPoint(endSel) )
					{
						TextSelectionPoint sel=(TextSelectionPoint) endSel;
						endIndex=sel.Index-start;
						if ( endIndex < 0 )
							endIndex=text.Length;
					}
				}

				int dx=dc.Graphics.MeasureText(text).Width;
				Rectangle rcText=new Rectangle(x, y, dx, height);
				DrawText(dc, rcText, baseline, text, bkCol, Color.White, startIndex, endIndex);

				if ( inError )
				{
					DrawErrorIndicator(dc, x, x, dx, y+baseline, false);
				}
			}
			finally 
			{
				dc.Graphics.PopFont();
			}
		}
	}

	internal class TextLiteralLayout : TextLayoutBase
	{
		public TextLiteralLayout(IContainer parent, XmlCharacterData t) : base(parent, t)
		{
		}

		public TextLiteralLayout(IContainer parent, XmlCharacterData t, int start) : this(parent, t)
		{
			TrimStart(start);
			Debug.Assert(len > 0, "TextFragment must be non-empty!");
		}

		private void TrimStart(int count)
		{
			start+=count;
			len-=count;
		}

		public override bool Compose(DrawContext dc, Style c, int width, out ItemMetrics bi, bool suppressWhitespace, out ITextLayout next)
		{
			this.style=c;
			bi=new ItemMetrics(this);

			next=null;

			// idea here is just to scan for next line break
			// TODO: M: may need to deal with double spaces, tabs, etc
			string text=Text;
			int ptr=text.IndexOf('\n');
			int measureLen=len;
			if ( ptr >= 0 )
			{
				bi.ForceBreakAfter=true;
				len=ptr+1;
				measureLen=len-1;

				if ( len < text.Length )
					next=GetNextFragment();
			}
			if ( measureLen > 0 && Text[measureLen-1]=='\r' )
				measureLen--;

			Size sz=dc.Graphics.MeasureText(Text.Substring(0, measureLen));
			height=dc.Graphics.GetFontHeight();
			this.width=sz.Width;
			ascent=c.FontAscent;

			return true;
		}

		private TextLiteralLayout GetNextFragment()
		{
			Debug.Assert(start+len < textNode.Value.Length, "Invalid use of GetNextFragment");
			return new TextLiteralLayout(parent, textNode, start+len);
		}

		public override void Reset()
		{
			// TODO: L: don't know why this would ever be called
			len=textNode.Value.Length-start;
			Debug.Assert(len > 0, "TextFragment must be non-empty!");
		}

		public override SelectionPoint GetSelectionPoint(bool atEnd)
		{
			if ( !atEnd )
				return new TextSelectionPoint(Node, start);

			int index=start+len-1;
			TextSelectionPoint tsp=new TextSelectionPoint(Node, index);
			if ( tsp.Index > 0 )
				// we do this as this is only called when cursor is at end of line,
				// and we want to position the cursor in front of the line-end
				tsp=new TextSelectionPoint(Node, tsp.Index-1);

			Console.WriteLine("Returning {0}", tsp);
			return tsp;
		}
		
		public override bool ContainsSelectionPointInternal(SelectionPoint sp)
		{
			TextSelectionPoint tsp=sp as TextSelectionPoint;
			if ( tsp == null )
				return false;

			if ( tsp.Node.NodeType == XmlNodeType.SignificantWhitespace )
			{
				return true;
			}

			return (tsp.Index >= start && tsp.Index < start+len);
		}
	}

	internal class TextFlowLayout : TextLayoutBase
	{
		private int ignoreStart=0;
		private int ignoreEnd=0;
		private bool canSplit=false;
		private int splitLeftWidth=0;
		private int splitRightWidth=0;
		private int splitIndex=0;
		// TODO: L: optimise - get from lookup by font
		private int spaceWidth=0;
		private static readonly char[] whitespaceChars=
			new char[] {' ', '\n', '\r', '\t'};
																  
		public TextFlowLayout(IContainer parent, XmlCharacterData t) : base(parent, t)
		{
		}

		public override void Reset()
		{
			canSplit=false;
			len=textNode.Value.Length-start;
			ignoreStart=0;
			ignoreEnd=0;
			Debug.Assert(len > 0, "TextFragment must be non-empty!");
		}

		protected TextFlowLayout(IContainer parent, XmlCharacterData t, int start) : this(parent, t)
		{
			// TODO: L: this is confusing since TrimStart changes ignoreStart
			TrimStart(start);
			ignoreStart=0;
			Debug.Assert(len > 0, "TextFragment must be non-empty!");
		}

		public override SelectionPoint GetSelectionPoint(bool atEnd)
		{
			return new TextSelectionPoint(Node, atEnd ? start+len-1 : start);
		}

		public override bool ContainsSelectionPointInternal(SelectionPoint sp)
		{
			TextSelectionPoint tsp=sp as TextSelectionPoint;
			if ( tsp == null )
				return false;

			if ( tsp.Node.NodeType == XmlNodeType.SignificantWhitespace )
			{
				return true;
			}

			return (tsp.Index >= start-ignoreStart && tsp.Index < start+len+ignoreEnd);
		}

		private int GetStartSpaceCount()
		{
			int ret=0;
			int ptr=start;
			while ( ptr < len && TextUtil.IsWhiteSpace(textNode.Value[ptr++]) )
				ret++;

			return ret;
		}

		private void TrimStart(int count)
		{
			start+=count;
			ignoreStart+=count;
			len-=count;
			//			Debug.Assert(len > 0, "TextFragment must be non-empty!");
		}

		private void TrimEnd(int count)
		{
			len-=count;
			ignoreEnd+=count;
			Debug.Assert(len > 0, "TextFragment must be non-empty!");
		}

		private bool IncludeNextWord(out int newLen, out int endSpaceCount)
		{
			newLen=len;
			endSpaceCount=0;
//			newline=false;

			Debug.Assert(start+len < textNode.Value.Length, "No more text to scan!");

			string text=textNode.Value;
			int total_len=text.Length;
			int ptr=start+len;

			ptr=text.IndexOfAny(whitespaceChars, ptr);
			if ( ptr != -1 )
			{
				int ptrsave=ptr;
				while ( ptr != total_len && TextUtil.IsWhiteSpace(text[ptr]) )
				{
					// TODO: L: optimise
//					if ( TextUtil.IsNewline(text[ptr]) )
//						newline=true;

					ptr++;
				}

				endSpaceCount=ptr-ptrsave;
			} 
			else
				ptr=total_len;

			newLen=ptr-start;
			return ptr != total_len;
		}

		public override bool Compose(DrawContext dc, Style c, int availWidth, out ItemMetrics im, bool suppressWhitespace, out ITextLayout next)
		{
			TextItemMetrics metrics=new TextItemMetrics(this);
			im=metrics;

			this.style=c;
			next=null;
			// TODO: L: optimise
			Size sz=dc.Graphics.MeasureText(" ");
			spaceWidth=sz.Width;
			height=sz.Height;
			width=0;
			// get the font ascent
			ascent=c.FontAscent;

			if ( textNode.NodeType == XmlNodeType.SignificantWhitespace )
			{
				// special case, simply set to single space width and return
				width=sz.Width;
				len=textNode.Value.Length;
				metrics.CanBreakBefore=true;
				metrics.CanBreakAfter=true;
				return true;
			}

			int startSpaceCount=GetStartSpaceCount();
			if ( startSpaceCount > 0 && suppressWhitespace )
			{
				TrimStart(startSpaceCount);
				startSpaceCount=0;
			}

			if ( startSpaceCount > 1 )
			{
				TrimStart(startSpaceCount - 1);
				startSpaceCount=1;
			}

			len=startSpaceCount;
			if ( start+len == textNode.Value.Length )
			{
				// this is pure whitespace
				width=sz.Width;
				return true;
			}

			metrics.CanBreakBefore=startSpaceCount > 0;

			bool hasMoreContent=true;
			bool firstWord=true;
			bool fit=false;
			int endSpaceCount=0;

			int finalWidth=startSpaceCount * spaceWidth;
			int splitLen=0;
			int splitWidth=0;
			bool needNewline=false;
//			bool newline=false;

			while ( hasMoreContent )
			{
				int testLen;
				int tesc;
				hasMoreContent=IncludeNextWord(out testLen, out tesc);

				string segString=textNode.Value.Substring(start+len, testLen-len-tesc);
				string testString=ProcessText(segString);
				int segWidth=dc.Graphics.MeasureText(testString).Width;
				int testWidth=finalWidth+segWidth;

//				string testString=ProcessText(textNode.Value.Substring(start, testLen-tesc));
//				int testWidth=dc.Graphics.MeasureText(testString).Width;

				fit=testWidth < availWidth;

				if ( fit || firstWord )
				{
					// either it fits or it has to fit because this is the first word
					metrics.AddSegment(start+len, testLen-len, segWidth);

					splitLen=len;
					len=testLen;
					splitWidth=finalWidth;
					endSpaceCount=tesc;
					finalWidth=testWidth + (endSpaceCount > 0 ? spaceWidth : 0);

					if ( !fit && firstWord )
						needNewline=true;

					if ( !fit || endSpaceCount > 1 )
						break;
				} 
				else
					break;

				firstWord=false;
			}

			// this is handled by custom class
//			if ( style.Pre && newline )
//				bi.ForceBreakAfter=true;

			Debug.Assert(len > 0, "Must have laid out something!");

			// at this point we have these possibilities:
			//		everything fit - fit == true, nothing to do, return true
			//		not everything fit - fit == false, get next, return false
			//			and cannot split - oldLen=-1, return false
			//			and can split - oldLen > 0, ConfigureSplitInfo, return true

			metrics.CanBreakAfter=endSpaceCount > 0;
			width=finalWidth;

			if ( splitLen > 0 )
			{
				canSplit=true;
				splitIndex=splitLen;
				splitLeftWidth=splitWidth;
				splitRightWidth=finalWidth-splitWidth;
			} 

			if ( start+len < textNode.Value.Length )
				next=GetNextFragment();

			if ( endSpaceCount > 1 )
			{
				TrimEnd(endSpaceCount - 1);
				endSpaceCount=1;
			}

			return !needNewline;
		}

		private TextFlowLayout GetNextFragment()
		{
			Debug.Assert(start+len < textNode.Value.Length, "Invalid use of GetNextFragment");
			return new TextFlowLayout(parent, textNode, start+len);
		}

		public void AdjustForStartOfLine()
		{
			int startSpaceCount=GetStartSpaceCount();
			if ( startSpaceCount > 0 )
			{
				TrimStart(startSpaceCount);
				width-=spaceWidth*startSpaceCount;
			}
		}

		public bool CanSplit
		{
			get { return canSplit; }
		}

		public TextFlowLayout Split()
		{
			len=splitIndex;
			TextFlowLayout tr=new TextFlowLayout(this.Parent, textNode, start+len);
			this.width=splitLeftWidth;
			tr.width=splitRightWidth;
			tr.height=this.Height;
			tr.ascent=this.ascent;
			tr.style=this.style;
			return tr;
		}

		public override string ToString() 
		{
			return string.Format("[{0}]", Text);
		}
	}
}