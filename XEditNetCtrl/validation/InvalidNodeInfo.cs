using System;
using System.Collections;
using System.Xml;

namespace XEditNet.Validation
{
	internal interface IValidationLookup
	{
		bool Contains(XmlNode n);
		int Count
		{
			get;
		}
		ValidationError[] AllErrors
		{
			get;
		}
//		bool HasQuickFix(XmlNode n);
//		QuickFix[] GetQuickFixes(XmlNode n);
	}

	internal class InvalidNodeInfo : IValidationLookup
	{
		private Hashtable invalidNodeMap=new Hashtable();

		public void AddValidationError(XmlNode n, ValidationError ve)
		{
			ArrayList a=(ArrayList) invalidNodeMap[n];
			if ( a == null )
			{
				a=new ArrayList();
				invalidNodeMap[n]=a;
			}
			a.Add(ve);
			Console.WriteLine("Added validation error {0}", ve.Message);
		}

		public bool Contains(XmlNode n)
		{
			return invalidNodeMap.Contains(n);
		}

		public int Count
		{
			get { return invalidNodeMap.Count; }
		}

		public ValidationError[] AllErrors
		{
			get 
			{
				ArrayList ret=new ArrayList();
				foreach ( ArrayList a in invalidNodeMap.Values )
					ret.AddRange(a);

				return (ValidationError[]) ret.ToArray(typeof(ValidationError));
			}
		}

//		public bool HasQuickFix(XmlNode n)
//		{
//			bool hasQuickFix=false;
//			foreach ( ValidationError ve in GetDetails(n) )
//			{
//				if ( ve.HasQuickFix )
//					hasQuickFix=true;
//			}
//			return hasQuickFix;
//		}
//
//		public QuickFix[] GetQuickFixes(XmlNode n)
//		{
//			ArrayList ret=new ArrayList();
//			foreach ( ValidationError ve in GetDetails(n) )
//				ret.AddRange(ve.QuickFixes);
//
//			return ret.ToArray(typeof(QuickFix)) as QuickFix[];
//		}

		public ValidationError[] GetDetails(XmlNode n)
		{
			ArrayList list=invalidNodeMap[n] as ArrayList;
			if ( list == null )
				return new ValidationError[] {};

			return (ValidationError[]) list.ToArray(typeof(ValidationError));
		}

		public void SetDetails(XmlNode n, ValidationError[] col)
		{
			if ( col.Length== 0 )
				invalidNodeMap.Remove(n);
			else
				invalidNodeMap[n]=new ArrayList(col);
		}

		public void Remove(XmlNode n)
		{
			invalidNodeMap.Remove(n);
		}
	}
}
