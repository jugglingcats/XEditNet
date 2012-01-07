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

using System.Collections;

namespace XEditNet.Dtd
{
	/// <summary>
	/// Types of attribute.
	/// </summary>
	internal enum AttributeType
	{
		/// used internally during parsing
		Unknown,
		/// CDATA type attribute
		CDATA,
		/// ID type attribute
		ID,
		/// IDREF type attribute
		IDREF,
		/// IDREFS type attribute
		IDREFS,
		/// ENTITY type attribute
		ENTITY,
		/// ENTITIES type attribute
		ENTITIES,
		/// NMTOKEN type attribute
		NMTOKEN,
		/// NMTOKENS type attribute
		NMTOKENS,
		/// Enumerated attribute
		Enumerated,
		/// NOTATION type attribute
		NOTATION
	}

	/// <summary>
	/// Attribute state.
	/// </summary>
	internal enum AttributeState
	{
		Unknown,
		Required,
		Optional,
		Fixed,
		Default
	}

	/**
	 * Class representing an attribute.
	 * Represents an attribute definition in a DTD, not an instance of an attribute (cf. XmlAttribute)
	 */

	internal class Attribute
	{
		/** The XMLName of the attribute. */
		public XmlName Name = null;

		/** The attribute type. */
		public AttributeType Type = AttributeType.Unknown;

		/** Whether the attribute is required and has a default. */
		public AttributeState State = AttributeState.Unknown;

		/** The attribute's default value. May be null. */
		public string DefaultValue = null;

		/**\
		 * The legal values for attributes with a type of TYPE_ENUMERATED or
		 * TYPE_NOTATION. Otherwise null.
		 */
		private ArrayList enums = null;

		/** Construct a new Attribute. */
		public Attribute()
		{
		}

		/**
		 * Construct a new Attribute from its namespace URI, local name, and prefix.
		 *
		 * @param uri Namespace URI of the attribute. May be null.
		 * @param localName Local name of the attribute.
		 * @param prefix Namespace prefix of the attribute. May be null.
		 */
		public Attribute(string uri, string localName, string prefix)
		{
			this.Name = new XmlName(prefix, localName, uri);
		}

		/**
		 * Construct a new Attribute from an XMLName.
		 *
		 * @param name XMLName of the attribute.
		 */
		public Attribute(XmlName name)
		{
			this.Name = name;
		}

		public string[] Enums
		{
			get { return (string[]) enums.ToArray(typeof (string)); }
		}

		public void AddEnumeratedValue(string val)
		{
			if ( enums == null )
				enums=new ArrayList();

			enums.Add(val);
		}
	}
}