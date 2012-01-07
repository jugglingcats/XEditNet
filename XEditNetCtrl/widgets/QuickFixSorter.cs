using System;
using System.Collections;
using XEditNet.Validation;

namespace XEditNet.Widgets
{
	/// <summary>
	/// Summary description for QuickFixSorter.
	/// </summary>
	internal class QuickFixSorter
	{
		private Hashtable topGroups=new Hashtable();
		public ArrayList TopItems=new ArrayList();

		public QuickFixSorter(ICollection fixes, int maxItems)
		{
			Sort(fixes, maxItems);
		}

		private void Sort(ICollection fixes, int maxItems)
		{
			topGroups.Clear();
			
			foreach ( QuickFix qf in fixes )
			{
				QuickFixGroup qfg=(QuickFixGroup) topGroups[qf.MainText];
				if ( qfg == null )
				{
					qfg=new QuickFixGroup();
					qfg.Name=qf.MainText;
					topGroups[qfg.Name]=qfg;
				}
				qfg.Items.Add(qf);
			}

			TopItems.Clear();

			foreach ( QuickFixGroup qfg in topGroups.Values )
			{
				if ( qfg.Items.Count == 1 )
				{
					TopItems.Add(qfg.Items[0]);
					continue;					
				}

				if ( qfg.Items.Count > maxItems )
				{
					qfg.Items.Sort(new Sorter());
					QuickFixGroup newGroup=null;
					ArrayList origItems=qfg.Items;
					qfg.Items=new ArrayList();

					int count=0;
					foreach ( QuickFix qf in origItems )
					{
						if ( newGroup == null )
						{
							newGroup=new QuickFixGroup();
							newGroup.Name=qf.SubText;
							count=0;
						}
						newGroup.Items.Add(qf);
						count++;
						if ( count > maxItems )
						{
							newGroup.Name+=" - "+qf.SubText;
							qfg.Items.Add(newGroup);
							newGroup=null;
						}
					}
					if ( newGroup != null )
					{
						QuickFix qf=(QuickFix) origItems[origItems.Count-1];
                        newGroup.Name+=" - "+qf.SubText;
						qfg.Items.Add(newGroup);
					}
				}
				TopItems.Add(qfg);
			}
		}

		private class Sorter : IComparer
		{
			public int Compare(object x, object y)
			{
				QuickFix a=x as QuickFix;
				QuickFix b=y as QuickFix;

				if ( a == null || b == null )
					return 0;

				return a.SubText.CompareTo(b.SubText);
			}
		}
	}

	internal class QuickFixGroup
	{
		public string Name;
		public ArrayList Items=new ArrayList();
	}
}
