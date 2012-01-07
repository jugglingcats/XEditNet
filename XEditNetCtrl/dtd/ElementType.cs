// This software is in the public static domain.
//
// The software is provided "as is", without warranty of any kind,
// express or implied, including but not limited to the warranties
// of merchantability, fitness for a particular purpose, and
// noninfringement. In no event shall the author(s) be liable for any
// claim, damages, or other liability, whether in an action of
// contract, tort, or otherwise, arising from, out of, or in connection
// with the software or the use or other dealings in the software.
//
// Parts of this software were originally developed in the Database
// and Distributed Systems Group at the Technical University of
// Darmstadt, Germany:
//
//    http://www.informatik.tu-darmstadt.de/DVS1/

// Version 2.0
// Changes from version 1.x:
// * Change package name

using System;
using System.Collections;
using System.Xml;

namespace XEditNet.Dtd
{

	/**
	 * Class representing an element type.
	 *
	 * @author Ronald Bourret
	 * @version 2.0
	 */

	internal enum ElementContentType
	{
		Unknown,
		Empty,
		Any,
		PCDATA,
		Mixed, // excludes PCDATA-only above
		Element
	}

	internal class ElementType
	{
		// ********************************************************************
		// Variables
		// ********************************************************************

		/** The XMLName of the element type. */
		public XmlName Name;

		/**
		 * The type of the content model.
		 *
		 * <p> This must be one of the CONTENT_* constants. The default
		 * is CONTENT_UNKNOWN.</p>
		 */
		public ElementContentType ContentType = ElementContentType.Unknown;

		/**
		 * A Group representing the content model.
		 *
		 * <p>Must be null if the content type is not CONTENT_ELEMENT or CONTENT_MIXED.
		 * In the latter case, it must be a choice group with no child Groups.</p>
		 */
		public ContentModel ContentModel = null;

		/**
		 * A Hashtable of Attributes.
		 *
		 * <p>Keyed by the attribute's XMLName. May be empty.</p>
		 */
		protected Hashtable attributes = new Hashtable();

		/**
		 * A Hashtable of child ElementTypes.
		 *
		 * <p>Keyed by the child's XMLName. May be empty.</p>
		 */
		protected Hashtable children = new Hashtable();

		/**
		 * A Hashtable of parent ElementTypes.
		 *
		 * <p>Keyed by the parent's XMLName. May be empty.</p>
		 */
		protected Hashtable parents = new Hashtable();

		// ********************************************************************
		// Constructors
		// ********************************************************************

		/** Construct a new ElementType. */
		public ElementType()
		{
		}

		/**
		 * Construct a new ElementType from its namespace URI, local name, and prefix.
		 *
		 * @param uri Namespace URI of the element type. May be null.
		 * @param localName Local name of the element type.
		 * @param prefix Namespace prefix of the element type. May be null.
		 */
		public ElementType(string uri, string localName, string prefix)
		{
			this.Name = new XmlName(prefix, localName, uri);
		}

		/**
		 * Construct a new ElementType from an XMLName.
		 *
		 * @param name XMLName of the element type.
		 */
		public ElementType(XmlName name)
		{
			this.Name = name;
		}

		public void AddAttribute(Attribute attribute)
		{
			if (!attributes.ContainsKey(attribute.Name))
			{
				attributes.Add(attribute.Name, attribute);
			}
		}

		public void AddParentElement(ElementType parent)
		{
			if ( parents[parent.Name] == null )
				parents.Add(parent.Name, parent);
		}
		public void AddChildElement(ElementType child)
		{
			if ( children[child.Name] == null )
				children.Add(child.Name, child);
		}

		public bool HasChildElement(XmlName name)
		{
			return children.ContainsKey(name);
		}

		public ElementType[] ChildElements
		{
			get 
			{
				ArrayList list=new ArrayList(children.Values);
				return list.ToArray(typeof(ElementType)) as ElementType[];
			}
		}

		public ICollection Attributes
		{
			get 
			{
				return attributes.Values;
			}
		}

		public string[] AttributeNames
		{
			get
			{
				string[] ret=new string[attributes.Count];
				int n=0;
				foreach ( Attribute attr in attributes.Values )
					ret[n++]=attr.Name.QualifiedName;

				return ret;
			}
		}

		public Dtd.Attribute GetAttribute(XmlName name)
		{
			return (Dtd.Attribute) attributes[name];
		}

		public Dtd.Attribute GetAttribute(string name)
		{
			return (Dtd.Attribute) attributes[new XmlName(name)];
		}

		public Dtd.Attribute GetAttribute(XmlAttribute a)
		{
			// we only pass the qualified name because uri would confuse
			// DTD validation
			XmlName xn=new XmlName(a);
			return GetAttribute(xn);
		}

		public bool IsRootElement
		{
			get { return parents.Count == 0; }
		}
	}
}