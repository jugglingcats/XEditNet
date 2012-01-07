using System;
using System.Xml;
using System.Xml.XPath;
using System.Text.RegularExpressions;
using System.Collections;
using System.Diagnostics;
using XEditNet.Dtd;

namespace XEditNet
{
	internal class XmlUtil
	{
		private static readonly Regex MULTIPLE_WHITESPACE=new Regex(@"\s\s+");

		public static XmlElement CopyElement(XmlElement e)
		{
			string prefix=e.Prefix;
			string localName=e.LocalName;
			string uri=e.NamespaceURI;

			return e.OwnerDocument.CreateElement(prefix, localName, uri);
		}

		public static void NormaliseTextContent(XmlText t)
		{
			// ok we want to replace newlines with spaces and then remove
			// repeated white-space
			t.Value=MULTIPLE_WHITESPACE.Replace(t.Value, " ");
		}

		public static bool IsNamespaceAttribute(XmlNode n, XmlNode p)
		{
			// TODO: L: this is crazy hack because CreateNavigator always creates a dummy node
			//			xmlns:xml="http://www.w3.org/XML/1998/namespace"
			return ( p != null && n.NodeType == XmlNodeType.Text && p.NodeType == XmlNodeType.Attribute 
				&& p.LocalName.Equals("xml") && p.NamespaceURI.Equals("http://www.w3.org/2000/xmlns/") 
				&& n.Value.Equals("http://www.w3.org/XML/1998/namespace") );
		}

		/// <summary>
		/// Gets the parent node even for attributes and entity references.
		/// </summary>
		/// <param name="node">An XmlNode.</param>
		/// <returns>An XmlElement or XmlDocument node that is the direct or indirect parent of the given node.</returns>
		public static XmlNode GetParentNode(XmlNode node)
		{
			if ( node == null )
				return null;

			XmlNodeType xnt = node.NodeType;
			if ( xnt == XmlNodeType.Attribute )
				return ((XmlAttribute)node).OwnerElement;

			XmlNode parent = node.ParentNode;
			while ( parent != null && parent.NodeType == XmlNodeType.EntityReference ) 
				parent = parent.ParentNode;

			return parent;
		}

		public static ICollection GetChildren(XmlElement e)
		{
			ArrayList ret=new ArrayList();

			// TODO: L: re-write without using an xpn
			XPathNavigator xpn=e.CreateNavigator();
			bool hasNode=xpn.MoveToFirstChild();
			while ( hasNode )
			{
				XmlNode n=((IHasXmlNode) xpn).GetNode();
				ret.Add(n);

				hasNode=xpn.MoveToNext();
			}
			return ret;
		}

		public static void Normalise(XmlNode n)
		{
			XmlText prev=null;
			ArrayList deletedNodes=new ArrayList();

			foreach ( XmlNode child in n.ChildNodes )
			{
				XmlText t=child as XmlText;
				if ( t == null )
				{
					prev=null;
					continue;
				}
				if ( prev != null )
				{
					prev.AppendData(t.Data);
					deletedNodes.Add(t);
				} 
				else
					prev=t;
			}

			foreach ( XmlNode c in deletedNodes )
				c.ParentNode.RemoveChild(c);
		}

		public static bool AppearsBefore(XmlNode a, XmlNode b)
		{
			if ( a.Equals(b) )
				throw new ArgumentException("AppearsBefore must be called with distinct nodes");

			XmlNode common=FindCommonAncestor(a, b);

			if ( common == null )
				throw new ArgumentException("Arguments do not share a common ancestor");

			Stack ap=GetLocationFromAncestor(a, common);
			Stack bp=GetLocationFromAncestor(b, common);

			while ( ((IntPtr) ap.Peek()).ToInt32() == ((IntPtr) bp.Peek()).ToInt32() )
			{
				ap.Pop();
				bp.Pop();

				// TODO: E: fails on entities
				if ( ap.Count == 0 || bp.Count == 0 )
					throw new ArgumentException("AppearsBefore must not be called when one node is nested inside another");
			}
			return ((IntPtr) ap.Peek()).ToInt32() < ((IntPtr) bp.Peek()).ToInt32();
		}

		public static bool HasAncestor(XmlNode a, XmlNode b)
		{
			if ( b == null )
				return false;

			if ( a.Equals(b) )
				return true;

			XmlNode parent=GetParentNode(a);
			while ( parent != null && !parent.Equals(b) )
				parent=GetParentNode(parent);

			return (parent != null && parent.Equals(b));
		}

		public static Stack GetLocationFromAncestor(XmlNode n, XmlNode ancestor)
		{
			Stack ret=new Stack();
			while ( n != null && n != ancestor )
			{
				ret.Push(new IntPtr(GetIndex(n)));
				n=n.ParentNode;
			}
			if ( n == null )
				throw new ArgumentException("Node is not a descendent of given ancestor");

			return ret;
		}

		public static int GetIndex(XmlNode node)
		{
			int count=0;
			XmlNode p=node.ParentNode.FirstChild;
			while ( p != null && !p.Equals(node) )
			{
				p=p.NextSibling;
				count++;
			}
			Debug.Assert(count >= 0, "Child is not contained in parent's child nodes!!");
			return count;

//			int count=0;
//			XmlNode prev=node.PreviousSibling;
//			while ( prev != null )
//			{
//				count++;
//				prev=prev.PreviousSibling;
//			}
//			return count;

//			XmlNodeList l=node.ParentNode.ChildNodes;
//			int count=l.Count;
//			int n=0;
//			while ( n < count )
//			{
//				if ( l[n].Equals(node) )
//					return n;
//
//				n++;
//			}
//			Debug.Assert(false, "Child is not contained in parent's child nodes!!");
//			return 0;
		}

		public static XmlNode FindCommonAncestor(XmlNode a, XmlNode b)
		{
			XmlNode p1=a.ParentNode;
			while ( p1 != null )
			{
				XmlNode p2=b.ParentNode;
				while ( p2 != null )
				{
					if ( p2.Equals(p1) )
						return p1;

					p2=p2.ParentNode;
				}
				p1=p1.ParentNode;
			}
			return null;
		}

//		public static XmlNode FindCommonAncestor(XmlNode a, XmlNode b)
//		{
//			ICollection aa=GetAncestors(a);
//			ICollection ba=GetAncestors(b);
//
//			foreach ( XmlNode aAncestor in aa )
//			{
//				foreach ( XmlNode bAncestor in ba )
//				{
//					if ( aAncestor.Equals(bAncestor) )
//					{
//						return aAncestor;
//					}
//				}
//			}
//			return null;
//		}

		public static IList GetAncestors(XmlNode n)
		{
			ArrayList ret=new ArrayList();
			while ( n != null && n.NodeType != XmlNodeType.Document )
			{
				ret.Add(n);
				n=n.ParentNode;
			}
			return ret;
		}

		public static bool IsTextContent(XmlNode n)
		{
			switch ( n.NodeType )
			{
				case XmlNodeType.CDATA:
					return true;
				case XmlNodeType.SignificantWhitespace:
					return true;
				case XmlNodeType.Text:
					return true;
			}
			return false;
		}

		public static XmlElement CreateElement(XmlName name, XmlDocument doc)
		{
			string nsURI=name.NamespaceURI;
			if ( name.NamespaceURI.Length == 0 && name.Prefix.Length == 0 && doc.DocumentElement != null )
			{
				string prefix=doc.DocumentElement.Prefix;
				
				if ( prefix == null || prefix.Length == 0 )
				{
					nsURI=doc.DocumentElement.NamespaceURI;
					if ( nsURI == null )
						nsURI=string.Empty;
				}
			}
			return doc.CreateElement(name.Prefix, name.LocalName, nsURI);
		}
	}
}
