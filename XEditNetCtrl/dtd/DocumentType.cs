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
// * Now uses XMLWriter
// * Split off serialization methods.
// * Moved post-production methods to DTDParser.

using System;
using System.Collections;
using System.Xml;
using System.Xml.Serialization;

namespace XEditNet.Dtd
{

	/**
	 * Class representing a DTD.
	 *
	 * <p>DTD and the classes it points to are designed to be read-only. While you
	 * can use them to create your own model of a DTD, you do so at your own risk.
	 * This is because DTD and the classes it points to use public class variables
	 * to hold information. The lack of mutator (set) methods means it is easy to
	 * construct an invalid DTD.</p>
	 *
	 * @author Ronald Bourret
	 * @version 2.0
	 */

	internal class DocumentType
	{
		// ********************************************************************
		// Public variables
		// ********************************************************************

		/**
		 * A Hashtable of ElementTypes defined in the DTD.
		 *
		 * <p>Keyed by the element type's XMLName.</p>
		 */
		protected Hashtable elementTypes = new Hashtable();

		/**
		 * A Hashtable of Notations defined in the DTD.
		 *
		 * <p>Keyed by the notation's name.</p>
		 */
		protected Hashtable notations = new Hashtable();

		/**
		 * A Hashtable of ParameterEntities defined in the DTD.
		 *
		 * <p>Keyed by the entity's name.</p>
		 *
		 * <p><b>WARNING!</b> The ParameterEntity objects in this Hashtable are
		 * used during parsing. After parsing, this Hashtable and the ParameterEntity
		 * objects it contains are not guaranteed to contain useful information. In
		 * particular, ParameterEntity objects cannot be used to reconstruct a DTD and
		 * are not reference from where they were used, such as in content models.
		 * This is because DTD and its related classes are designed to be used by
		 * applications that want to explore the "logical" structure of a DTD --
		 * that is, its element types, attributes, and notations -- rather than its
		 * physical structure.</p>
		 */
		protected Hashtable parameterEntities = new Hashtable();

		/**
		 * A Hashtable of ParsedGeneralEntities defined in the DTD.
		 *
		 * <p>Keyed by the entity's name.</p>
		 */
		protected Hashtable parsedGeneralEntities = new Hashtable();

		/**
		 * A Hashtable of UnparsedEntities defined in the DTD.
		 *
		 * <p>Keyed by the entity's name.</p>
		 */
		protected Hashtable unparsedEntities = new Hashtable();

		// ********************************************************************
		// Constructors
		// ********************************************************************

		/** Construct a new DTD. */
		public DocumentType()
		{
		}

		// ********************************************************************
		// Public Methods -- creating element types
		// ********************************************************************

		public void SaveXml(string filename)
		{
			XmlSerializer xsr=new XmlSerializer(typeof(DocumentType));
			XmlTextWriter xtw=new XmlTextWriter(filename, System.Text.Encoding.UTF8);
			xsr.Serialize(xtw, this);
			xtw.Close();
		}

		/**
		 * Create an ElementType by XMLName.
		 *
		 * <p>If the ElementType already exists, it is returned. Otherwise, a new
		 * ElementType is created.</p>
		 *
		 * @param name The XMLName of the element type.
		 * @return The ElementType.
		 */
		public ElementType CreateElementType(XmlName name)
		{
			// Get an existing ElementType or add a new one if it doesn't exist.
			//
			// This method exists because we frequently need to refer to an 
			// ElementType object before it is formally created. For example, if
			// element type A is defined before element type B, and the content model
			// of element type A contains element type B, we need to add the
			// ElementType object for B (as a reference in the content model of A)
			// before it is formally defined and created.

			ElementType elementType;

			elementType = (ElementType)elementTypes[name];
			if (elementType == null)
			{
				elementType = new ElementType(name);
				elementTypes[name]=elementType;
			}

			return elementType;
		}

		/**
		 * Create an ElementType by URI, local name, and prefix.
		 *
		 * <p>If the ElementType already exists, it is returned. Otherwise, a new
		 * ElementType is created.</p>
		 *
		 * @param uri The namespace URI. May be null.
		 * @param localName The local name of the element type.
		 * @param prefix The namespace prefix. May be null.
		 * @return The ElementType.
		 */
		public ElementType CreateElementType(string uri, string localName, string prefix)
		{
			return CreateElementType(new XmlName(prefix, localName, uri));
		}

		public void AddParameterEntity(string name, Entity entity)
		{
			if (!parameterEntities.ContainsKey(name))
			{
				// If a parameter entity isn't already defined, use the
				// current definition.

				parameterEntities.Add(name, entity);
			}
		}

		public void AddUnparsedEntity(string name, Entity entity)
		{
			if (!unparsedEntities.ContainsKey(name) &&
				!parsedGeneralEntities.ContainsKey(name))
			{
				// If an unparsed entity isn't already defined, use the
				// current definition. Remember that unparsed entities
				// and parsed general entities share the same namespace.

				unparsedEntities.Add(name, entity);
			}
		}
		public void AddParsedGeneralEntity(string name, Entity entity)
		{
			if (!unparsedEntities.ContainsKey(name) &&
				!parsedGeneralEntities.ContainsKey(name))
			{
				// If parsed general entity isn't already defined, use the
				// current definition. Remember that unparsed entities
				// and parsed general entities share the same namespace.

				parsedGeneralEntities.Add(name, entity);
			}
		}

		public void AddNotation(Notation notation)
		{
			if (notations.ContainsKey(notation.Name))
				throw new DuplicateNotationException("Duplicate notation declaration: " + notation.Name);
			notations.Add(notation.Name, notation);
		}

		public bool HasNotation(string name)
		{
			return notations.ContainsKey(name);
		}

		public ICollection UnparsedEntities
		{
			get 
			{
				return unparsedEntities.Values;
			}
		}

		public ParsedGeneralEntity GetParsedGeneralEntity(string name)
		{
			return (ParsedGeneralEntity) parsedGeneralEntities[name];
		}

		public ParameterEntity GetParameterEntity(string name)
		{
			return (ParameterEntity) parameterEntities[name];
		}

		public ElementType GetElementType(XmlElement e)
		{
			// we don't care what the namespace of the element is since
			// none of the elements declared in the DTD will have namespaces,
			// only prefixes

			XmlName xn=new XmlName(e);
			return (ElementType) elementTypes[xn];
		}

		public ElementType this[XmlName name]
		{
			get 
			{
				return (ElementType) elementTypes[name];			
			}
		}

		public ElementType[] ElementTypes
		{
			get 
			{
				ArrayList ret=new ArrayList(elementTypes.Values);
				return ret.ToArray(typeof(ElementType)) as ElementType[];
			}
		}
	}
}