using System;
using System.Collections;

namespace XEditNet.Layout
{
	internal class LineItemEnumerator : DrawItemEnumerator
	{
		public LineItemEnumerator(IContainedItem start) : base(start, false)
		{
		}
	
		public override bool MoveNext()
		{
			while ( base.MoveNext() && !(current is ILineItem) );
	
			return current != null;
		}
	}
	
	internal class DrawItemEnumerator
	{
		protected IContainedItem current;
		private IContainedItem start;
		private bool limitToSubTree;
		private bool completed=false;
		private Stack indexes=new Stack();
		private int currentIndex;

		public DrawItemEnumerator(IContainedItem start) : this(start, true)
		{
		}
	
		// made this private since we are now using stack for indexes,
		// so hard to go outside of subtree
		protected DrawItemEnumerator(IContainedItem start, bool limitToSubTree)
		{
			this.limitToSubTree=limitToSubTree;
			this.start=start;
			this.current=null;

			IContainer c=(IContainer) start.Parent;
			int count=c.ChildCount;
			int n=0;
			while ( n< count )
			{
				if ( c[n].Equals(start) )
					break;

				n++;
			}
			currentIndex=n;
		}
	
		public IContainedItem Current 
		{
			get { return current; }
		}
	
		public virtual bool MoveNext()
		{
			if ( !completed && current == null )
			{
				current=start;
				return true;
			}
	
			IContainer c=current as IContainer;
			if ( c != null && c.ChildCount > 0 )
			{
				// depth first
				indexes.Push(currentIndex);
				current=(IContainedItem) c[0];
				currentIndex=0;
				return true;
			}
	
			IContainer p=current.Parent as IContainer;
			if ( p != null )
			{
				currentIndex++;
				if ( currentIndex < p.ChildCount )
				{
					current=(IContainedItem) p[currentIndex];
					return true;
				}
			}
	
			while ( p != null )
			{
				if ( limitToSubTree && start.Equals(p) )
				{
					// we're finished iterating this sub-tree
					current=null;
					completed=true;
					return false;
				}
	
				if ( indexes.Count == 0 )
				{
					currentIndex=GetIndex((IContainedItem) p);
					if ( currentIndex < 0 )
					{
						// parent is null
						current=null;
						completed=true;
						return false;
					}
				}
				else
					currentIndex=(Int32) indexes.Pop();

				if ( currentIndex >= 0 )
				{
					p=((IContainedItem) p).Parent as IContainer;
					currentIndex++;
				
					if ( currentIndex < p.ChildCount )
					{
						current=(IContainedItem) p[currentIndex];
						return true;
					}
				}
			}
	
			current=null;
			completed=true;
			return false;
		}

		private int GetIndex(IContainedItem c)
		{
			IContainer p=c.Parent as IContainer;
			if ( p == null )
				return -1;

			int count=p.ChildCount;
			int n=0;
			while ( n < count )
			{
				if ( p[n] == c )
					break;

				n++;
			}
			return n;
		}

		public void Reset()
		{
			current=start;
			completed=false;
		}
	}
}
