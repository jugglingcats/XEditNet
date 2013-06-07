using System;
using System.Drawing;
using System.Xml;
using XEditNet.Location;
using XEditNet.Styles;

namespace XEditNet.Layout
{
	/// <summary>
	/// <para><b>Internal to XEditNet</b> - not intended for public use at this time.
	/// Classes implementing this interface can be laid out on lines by the XEditNet
	/// layout engine. Examples are tags and runs of text.</para>
	/// </summary>
	internal interface ILineItem : ISelectable
	{
		/// <summary>
		/// Sets the current selection. This method is only called by the
		/// layout engine when it knows a selection encompasses this line item.
		/// </summary>
		Selection Selection
		{
			set;
		}

		/// <summary>
		/// Gets a boolean indicating if this line item can be split in order
		/// to line wrap. Tags cannot be split but text runs often can.
		/// </summary>
//		bool CanSplit
//		{
//			get;	
//		}

		/// <summary>
		/// Requests that the line item split itself.
		/// </summary>
		/// <returns>The new line item that was produced.</returns>
//		ILineItem Split();
		/// <summary>
		/// Called when the layout engine places a line item at the start
		/// of a line. The text line item uses this to suppress white-space
		/// when starting a line.
		/// </summary>
//		void AdjustForStartOfLine();
		/// <summary>
		/// Draws the item.
		/// </summary>
		/// <param name="dc">The device context to use.</param>
		/// <param name="pos">The horizontal location.</param>
		/// <param name="y">The vertical location.</param>
		/// <param name="baseline">The baseline of the current line. 
		///		The line item should align its baseline to this value.</param>
		/// <param name="height">The height of the current line.</param>
		/// <param name="c">The style to use.</param>
		void Draw(DrawContext dc, int pos, int y, int baseline, int height, Style c);
		/// <summary>
		/// Determines if the given selection point is within the line item. For example,
		/// a TextSelectionPoint holds a specific character position, and a text node
		/// can be split over a number of line items with different character ranges.
		/// </summary>
		/// <param name="sp">The selection point to test.</param>
		/// <returns>True if the selection point is contained in the line item.</returns>
		bool ContainsSelectionPoint(SelectionPoint sp);
		/// <summary>
		/// Composes (measures) the line item. This is called during reflow in order to
		/// lay out line items in lines and blocks.
		/// </summary>
		/// <param name="dc">The device context to use.</param>
		/// <param name="c">The style to use.</param>
		/// <returns>The composed width of the line item.</returns>
		int Compose(DrawContext dc, Style c, out ItemMetrics bi);

		/// <summary>
		/// Gets the ascent (the distance above the baseline to the top of the line item). This
		/// is used to lay out line items on the overall line's baseline.
		/// </summary>
		int Ascent { get; }

		/// <summary>
		/// Gets the underlying XmlNode for this line item.
		/// </summary>
		XmlNode Node { get; }
		/// <summary>
		/// Returns a selection point for this line item. If the end selection point is requested
		/// the line item returns the last selection point possible within the line item. For
		/// example, the text line item will return the location immediately before the last character
		/// in the text range.
		/// </summary>
		/// <param name="end">Indicates the caller wants the last valid selection point.</param>
		/// <returns>A new selection point.</returns>
		SelectionPoint GetSelectionPoint(bool end);
	}

	internal interface ITextLayout : ILineItem
	{
		bool Compose(DrawContext dc, Style style, int i, out ItemMetrics bi, bool whitespace, out ITextLayout next);
		void Reset();
	}

	/// <summary>
	/// Represents an object that can contain other IContainedItem objects.
	/// </summary>
	internal interface IContainer : IContainedItem
	{
		/// <summary>
		/// Gets the number of children in this container.
		/// </summary>
		int ChildCount
		{
			get;
		}

		/// <summary>
		/// Indexed property for items in the container
		/// </summary>
		IContainedItem this[int index]
		{
			get;
        }

		IContainedItem LastChild
		{
			get;
		}

		/// <summary>
		/// Method called by child items when their size changes as a result of reflow.
		/// Typically the container will recalculate its own bounds after receiving this call,
		/// but it may force some incremental reflow. Also, it may call its parent
		/// ProcessSizeChange up the hierarchy if necessary.
		/// </summary>
		/// <param name="originator">The child issuing the notification.</param>
		/// <returns>The element, represented by a block that ultimately had its
		/// bounds changed, used for invalidating the correct layout area.</returns>
		XmlElement ProcessSizeChange(IContainedItem originator, DrawContext dc);
		XmlElement ProcessMetricsChange(IContainedItem originator, DrawContext dc);

		BoundingContext GetBoundsForChild(DrawContext dc, IContainedItem child);
	}

	internal interface ILineItemContainer : IContainer
	{
		int Baseline
		{
			get;
		}
	}

	/// <summary>
	/// The base interface for all items that are created and manipulated by the layout
	/// engine. Represents a rectangular region of the screen that may be contained
	/// in another region.
	/// </summary>
	internal interface IContainedItem
	{
		/// Gets the width of the item.
		int Width
		{
			get;
		}
		/// Gets the height of the item.
		int Height
		{
			get;
		}
		/// Gets the container for this item. May be null.
		IContainer Parent
		{
			get;
			set;
		}

		/// Gets the bounds for this item given a specific location.
		Rectangle GetBoundingRect(int x, int y);
	}

	/// <summary>
	/// Represents items that can handle selection and hit test calls.
	/// </summary>
	internal interface ISelectable : IContainedItem
	{
		/// <summary>
		/// Returns the position of the caret given a selection point. The coordinates passed in the
		/// x and y parameters determine the location of this item relative to the origin of the
		/// layout area.
		/// </summary>
		/// <param name="gr">The current device context.</param>
		/// <param name="x">The x-coordinate of the top-left corner of this draw item.</param>
		/// <param name="y">The y-coordinate of the top-left corner of this draw item.</param>
		/// <param name="sp">The current selection point.</param>
		/// <param name="info">The caret position info to be updated.</param>
		/// <returns>A zero width rectangle with the coordinates and height of the caret.</returns>
		void GetCaretPosition(IGraphics gr, int x, int y, SelectionPoint sp, ref CaretPositionInfo info);
		/// <summary>
		/// Returns hit test info for the given point.
		/// </summary>
		/// <param name="gr">The current device context.</param>
		/// <param name="x">The x-coordinate of the top-left corner of this draw item.</param>
		/// <param name="y">The y-coordinate of the top-left corner of this draw item.</param>
		/// <param name="pt">The point to test.</param>
		/// <returns>A new HitTest object.</returns>
		HitTestInfo GetHitTestInfo(IGraphics gr, int x, int y, Point pt);
	}

	internal enum CaretDirection
	{
		None,
		LTR,
		RTL
	}

	internal enum CaretSetting
	{
		Fallback,
		Accurate,
		Absolute
	}


	internal class CaretPositionInfo
	{
		private bool useSecondary=false;
		private CaretLocation primary=null;
		private CaretLocation secondary=null;

		private class CaretLocation
		{
			private bool adjusted=false;
			private int x;
			private int y;
			private int height;

			public int X
			{
				get { return x; }
			}

			public int Y
			{
				get { return y; }
			}

			public int Height
			{
				get { return height; }
			}

			public CaretLocation(int x, int y, int h)
			{
				this.x=x;
				this.y=y;
				this.height=h;
			}

			public void AdjustForLine(int height, int baseline, int ascent)
			{
				if ( adjusted )
					return;

				this.height=height;
				this.y += baseline-ascent;

				adjusted=true;
			}
		}

		public bool IsFinal
		{
			get { return primary != null || (useSecondary && secondary != null); }
		}

		public bool UseSecondary
		{
			set { useSecondary=value; }
		}

		public void UpdateLocation(int x, int y, int height, CaretSetting mode)
		{
			if ( IsFinal )
				throw new InvalidOperationException("CaretPosition is already finalised");

			CaretLocation loc=new CaretLocation(x, y, height);
			switch ( mode )
			{
				case CaretSetting.Absolute:
					primary=loc;
					secondary=null;
					break;

				case CaretSetting.Accurate:
					primary=loc;
					break;

				case CaretSetting.Fallback:
					secondary=loc;
					break;
			}
		}

		public int X
		{
			get
			{
				CheckIsFinal();
				// TODO: M: throwing null pointer after undo of element insert
				//			(around another element on new document - flip to other wnd then back)
				//			can't reproduce
				if ( Location == null )
					return 0;

				return Location.X;
			}
		}

		public int Y
		{
			get
			{
				CheckIsFinal();

				if ( Location == null )
					return 0;

				return Location.Y;
			}
		}

		public int Height
		{
			get
			{
				CheckIsFinal();

				if ( Location == null )
					return 10;

				return Location.Height;
			}
		}

		private void CheckIsFinal()
		{
			if ( !IsFinal )
				Logger.Log("WARN: Caret info not finalised!");
		}

		private CaretLocation Location
		{
			get
			{
				return (useSecondary && secondary != null) ? secondary : primary;
			}
		}

		public void AdjustForLine(int height, int baseline, int ascent)
		{
			if ( primary != null )
				primary.AdjustForLine(height, baseline, ascent);

			if ( secondary != null )
				secondary.AdjustForLine(height, baseline, ascent);
		}

		public Rectangle ToRectangle()
		{
			return new Rectangle(X, Y, 1, Height);
		}
	}

	internal interface IRegion : ISelectable
	{
		void Draw(DrawContext dc, int x, int y, Style c);
		XmlNode GetNodeUnderPoint(int x, int y, Point pt);
		ILineItem FindSelectionPoint(SelectionPoint sp);
	}

	/// <summary>
	/// Represents the object that is currently in reflow. Used by custom items
	/// to get the current reflow context and make use of the generic reflow functionality
	/// in the layout engine.
	/// </summary>
	internal interface IReflowHost
	{
		/// <summary>
		/// The current line beyind reflowed. Can change as items are added using other
		/// methods in this interface.
		/// </summary>
		IContainer CurrentLine
		{
			get;
		}
		/// <summary>
		/// Request reflow of a line item. The layout engine will layout the line item
		/// which make cause CurrentLine to change.
		/// </summary>
		/// <param name="markupItem">The item to reflow.</param>
		/// <param name="dc">The current draw context.</param>
		/// <param name="style">The current style.</param>
		/// <param name="bounds">The bounding context.</param>
		void ReflowMarkup(ILineItem markupItem, DrawContext dc, Style style, BoundingContext bounds);
		/// <summary>
		/// Request a reflow of children of a particular node (typically the node corresponding
		/// to the item currently in reflow). This will reflow the children of the given node
		/// using the standard layout engine methods.
		/// </summary>
		/// <param name="node">The node to reflow.</param>
		/// <param name="dc">The current draw context.</param>
		/// <param name="style">The current style.</param>
		/// <param name="bounds">The bounding context.</param>
		void ReflowChildren(XmlNode node, DrawContext dc, Style style, BoundingContext bounds);
	}

	/// <summary>
	/// Represents objects that support reflow. Used for custom reflow implementations.
	/// </summary>
	internal interface IReflowObject
	{
	}

	internal interface INestedReflowObject : IReflowObject
	{
		/// <summary>
		/// Request by the layout engine for this object to reflow.
		/// </summary>
		/// <param name="host">The reflow host.</param>
		/// <param name="dc">The current draw context.</param>
		/// <param name="bounds">The bounding context.</param>
		/// <param name="incremental">Indicates whether this reflow is incremental.</param>
		void Reflow(IReflowHost host, DrawContext dc, BoundingContext bounds, bool incremental);
	}

	internal interface ICustomReflowObject : IReflowObject
	{
		IContainer Parent
		{
			set;
		}
		XmlElement ElementNode
		{
			set;
		}
		Style Style
		{
			set;
		}
	}

	internal interface IBlock : IReflowObject, IRegion, IContainer
	{
		IBlock FindBlock(XmlElement e);
		XmlElement Invalidate(DrawContext dc);
		// TODO: L: this could be refactored into LayoutEngine
		Rectangle GetBoundingRect(int x, int y, XmlNode n);
		void RecalcBounds();

		int MinimumWidth
		{
			get;
		}

		int DesiredWidth
		{
			get;
		}

		/// <summary>
		/// Request by the layout engine for this object to reflow.
		/// </summary>
		/// <param name="dc">The current draw context.</param>
		/// <param name="bounds">The bounding context.</param>
		/// <param name="incremental">Indicates whether this reflow is incremental.</param>
		void Reflow(DrawContext dc, BoundingContext bounds, bool incremental);

		bool IsSingleLine
		{
			get;
		}

		bool ReflowCompleted
		{
			get;
		}

		XmlElement ElementNode
		{
			get;
		}

		Style Style
		{
			get;
		}
	}
}
