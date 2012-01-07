using System;
using XEditNet.Dtd;

namespace XEditNet.Validation
{
	/// <summary>
	/// Summary description for ValidationUtil.
	/// </summary>
	internal class ValidationUtil
	{
		public static ElementListItem[] ToElementList(ElementTypeRef[] src)
		{
			if ( src == null )
				return new ElementListItem[] {};

			ElementListItem[] ret=new ElementListItem[src.Length];
			int n=0;
			foreach ( ElementTypeRef sr in src )
				ret[n++]=new ElementListItem(sr.Name, sr.IsRequired, sr.IsChoice);

			return ret;
		}

		public static ElementListItem[] ToElementList(ElementType[] src)
		{
			if ( src == null )
				return new ElementListItem[] {};

			ElementListItem[] ret=new ElementListItem[src.Length];
			int n=0;
			foreach ( ElementType et in src )
				ret[n++]=new ElementListItem(et.Name, false);

			return ret;
		}
	}
}
