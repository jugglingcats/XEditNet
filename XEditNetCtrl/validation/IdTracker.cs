using System;
using System.Collections;
using System.Xml;
using System.Diagnostics;

namespace XEditNet.Validation
{
	internal class IdTracker
	{
		private Hashtable idMap=new Hashtable();
		private Hashtable idRefMap=new Hashtable();

		public int IdCount(string id)
		{
			ICollection col=(ICollection) idMap[id];
			return col == null ? 0 : col.Count;
		}

		private AttributeBinding[] ToArray(ICollection col)
		{
			AttributeBinding[] ret=new AttributeBinding[col.Count];
			int n=0;
			foreach ( AttributeBinding ab in col )
				ret[n++]=ab;

			return ret;
		}

		private AttributeBinding[] ToArray(ArrayList col)
		{
			return (AttributeBinding[]) col.ToArray(typeof(AttributeBinding));
		}

		public AttributeBinding[] GetDuplicateIds()
		{
			ArrayList ret=new ArrayList();
			foreach ( Hashtable h in idMap.Values )
			{
				if ( h.Count > 1 )
					// keys are the attribute bindings for elements
					// with this id
					ret.AddRange(h.Keys);
			}

			return ToArray(ret);
		}

		public AttributeBinding[] GetUndefinedIdRefs()
		{
			ArrayList ret=new ArrayList();
			foreach ( DictionaryEntry de in idRefMap )
			{
				if ( !idMap.ContainsKey(de.Key) )
				{
					Hashtable bindings=(Hashtable) de.Value;
					ret.AddRange((ICollection) bindings.Keys);
				}
			}
			return ToArray(ret);
		}

		public AttributeBinding[] GetIdBindings(string id)
		{
			Hashtable elems=(Hashtable) idMap[id];
			if ( elems == null )
				return new AttributeBinding[] {};

			return ToArray(elems.Keys);
		}

		public AttributeBinding[] GetIdRefBindings(string id)
		{
			Hashtable elems=(Hashtable) idRefMap[id];
			if ( elems == null )
				return new AttributeBinding[] {};
			
			return ToArray(elems.Keys);
		}

		public void RemoveId(AttributeBinding ab)
		{
			Console.WriteLine("Removing attribute value {0}", ab.Value);
			Hashtable elems=(Hashtable) idMap[ab.Value];
			if ( elems != null )
			{
				elems.Remove(ab);
				if ( elems.Count == 0 )
					idMap.Remove(ab.Value);

				Console.WriteLine("Removed id value {0}, new count={1}", ab.Value, elems.Count);
			}
			else
				// TODO: L: this is really an error - indicates failure in tracking
				Console.WriteLine("Removed id value {0}, but didn't exist!", ab.Value);
		}

		public void RemoveIdRefs(AttributeBinding ab)
		{
			foreach ( string idref in ab.Value.Split(' ') )
			{
				Hashtable elems=(Hashtable) idRefMap[idref];
				if ( elems == null )
					// TODO: L: this should never happen
					continue;

				elems.Remove(ab);
				if ( elems.Count == 0 )
					idRefMap.Remove(idref);
			}
		}

		public void AddId(AttributeBinding ab)
		{
			Hashtable elems=(Hashtable) idMap[ab.Value];
			if ( elems == null )
			{
				elems=new Hashtable();
				idMap[ab.Value]=elems;
			} 

			elems[ab]=true;
		}

		public void AddIdRefs(AttributeBinding ab)
		{
			foreach ( string id in ab.Value.Split(' ') )
			{
				Hashtable elems=(Hashtable) idRefMap[id];
				if ( elems == null )
				{
					elems=new Hashtable();
					idRefMap[id]=elems;
				}
				elems[ab]=true;
			}
		}
	}
}
