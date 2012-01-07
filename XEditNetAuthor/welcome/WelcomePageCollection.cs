using System;
using System.Collections;
using XEditNetAuthor.Welcome;

namespace XEditNetAuthor.Welcome
{
	/// <summary>
	/// Summary description for WelcomePageCollection.
	/// </summary>
	public class WelcomePageCollection : CollectionBase
	{
		private WelcomeTabControl parent;
		/// <summary>
		/// Constructor requires the  WelcomeTabControl that owns this collection
		/// </summary>
		/// <param name="parent">WelcomeTabControl</param>
		public WelcomePageCollection(WelcomeTabControl parent) : base()
		{
			this.parent = parent;
		}

		/// <summary>
		/// Returns the WelcomeTabControl that owns this collection
		/// </summary>
		public WelcomeTabControl Parent
		{
			get 
			{
				return parent;
			}
		}

		/// <summary>
		/// Finds the Page in the collection
		/// </summary>
		public WelcomeTabPage this[ int index ]  
		{
			get  
			{
				return( (WelcomeTabPage) List[index] );
			}
			set  
			{
				List[index] = value;
			}
		}


		/// <summary>
		/// Adds a WelcomeTabPage into the Collection
		/// </summary>
		/// <param name="value">The page to add</param>
		/// <returns></returns>
		public int Add(WelcomeTabPage value )  
		{		
			int result = List.Add( value );
			return result;
		}


		/// <summary>
		/// Adds an array of pages into the collection. Used by the Studio Designer generated coed
		/// </summary>
		/// <param name="pages">Array of pages to add</param>
		public void AddRange(WelcomeTabPage[] pages)
		{
			// Use external to validate and add each entry
			foreach(WelcomeTabPage page in pages)
			{
				this.Add(page);
			}
		}

		/// <summary>
		/// Finds the position of the page in the colleciton
		/// </summary>
		/// <param name="value">Page to find position of</param>
		/// <returns>Index of Page in collection</returns>
		public int IndexOf( WelcomeTabPage value )  
		{
			return( List.IndexOf( value ) );
		}

		/// <summary>
		/// Adds a new page at a particular position in the Collection
		/// </summary>
		/// <param name="index">Position</param>
		/// <param name="value">Page to be added</param>
		public void Insert( int index, WelcomeTabPage value )  
		{
			List.Insert(index, value );
		}


		/// <summary>
		/// Removes the given page from the collection
		/// </summary>
		/// <param name="value">Page to remove</param>
		public void Remove( WelcomeTabPage value )  
		{
			//Remove the item
			List.Remove( value );
		}

		/// <summary>
		/// Detects if a given Page is in the Collection
		/// </summary>
		/// <param name="value">Page to find</param>
		/// <returns></returns>
		public bool Contains( WelcomeTabPage value )  
		{
			// If value is not of type Int16, this will return false.
			return( List.Contains( value ) );
		}

		/// <summary>
		/// Propgate when a external designer modifies the pages
		/// </summary>
		/// <param name="index"></param>
		/// <param name="value"></param>
		protected override void OnInsertComplete(int index, object value)
		{
			base.OnInsertComplete (index, value);
			//Showthe page added
//			parent.PageIndex = index;
		}
	
		/// <summary>
		/// Propogates when external designers remove items from page
		/// </summary>
		/// <param name="index"></param>
		/// <param name="value"></param>
		protected override void OnRemoveComplete(int index, object value)
		{
			base.OnRemoveComplete (index, value);
			//If the page that was added was the one that was visible
			if (parent.PageIndex == index)
			{
				//Can I show the one after
				if (index < InnerList.Count)
				{
					parent.PageIndex = index;
				}
				else
				{
					//Can I show the end one (if not -1 makes everythign disappear
					parent.PageIndex = InnerList.Count-1;
				}
			}
		}
	}
}
