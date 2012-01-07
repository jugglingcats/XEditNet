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
// Changes from version 1.0:
// * Changed getMixedContent to check for case <!ELEMENT A (#PCDATA)*>.
// * Changed getContentModel to check for case <!ELEMENT A ( #PCDATA | B )*>.
// Changes from version 1.01:
// * Change package name, class name
// * Update for 2.0 code

using System;
using System.IO;
using System.Net;
using System.Collections;
using System.Text;
using System.Xml;

namespace XEditNet.Dtd
{
/**
 * Parses an external DocumentType or the DocumentType in an XML document and creates a DocumentType object.
 *
 * <p>While DTDParser checks for most syntactic errors in the DocumentType, it does not
 * check for all of them. (For example, it does not check if entities are well-formed.)
 * Thus, results are undetermined if the DocumentType is not syntactically correct.</p>
 *
 * @author Ronald Bourret
 * @version 2.0
 */

	internal class InputSource
	{
		public Uri Uri;
		public string Text;

		public InputSource(Uri uri)
		{
			this.Uri=uri;
			this.Text=null;
		}

		public InputSource(string text)
		{
			this.Text=text;
			this.Uri=null;
		}

		public InputSource(Uri baseUri, String systemId)
		{
			this.Uri=new Uri(baseUri, systemId);
		}
	}

	internal class XMLMiddlewareException : Exception
	{
		public XMLMiddlewareException(string msg) : base(msg)
		{
		}
	}

	internal class DTDParser
	{
		// This class converts an external DocumentType or the DocumentType in an XML document
		// into a DocumentType object. It's not the most brilliant parser in the world,
		// nor is it the fastest, nor does it cover all the cases, but it should
		// be useable for many DTDs.
		//
		// This code generally assumes the DocumentType to be syntactically correct;
		// results are undetermined if it is not.

		// ********************************************************************
		// Constants
		// ********************************************************************

		// READER_READER simply means we don't care what type of Reader
		// it is -- it is all set up and ready to go.

		const int READER_READER = 0,
			READER_STRING = 1,
			READER_URL = 2;
		const int STATE_OUTSIDEDTD = 0,
			STATE_DTD = 1,
			STATE_ATTVALUE = 2,
			STATE_ENTITYVALUE = 3,
			STATE_COMMENT = 4,
			STATE_IGNORE = 5,
			STATE_PARAMENTITYVALUE = 6;
		const int BUFSIZE = 8096,
			LITBUFSIZE = 1024,
			NAMEBUFSIZE = 1024;

		// ********************************************************************
		// Variables
		// ********************************************************************

		DocumentType dtd;
		Hashtable    /* namespaceURIs, */
			predefinedEntities = new Hashtable(),
			declaredElementTypes = new Hashtable();
		TokenList    dtdTokens;
		TextReader reader;
		int          readerType, bufferPos, bufferLen, literalPos, namePos,
			entityState, line, column;
		Stack        readerStack;
		StringBuilder literalStr, nameStr;
		char[]       buffer,
			literalBuffer = new char[LITBUFSIZE],
			nameBuffer = new char[NAMEBUFSIZE];
		public bool         ignoreQuote, ignoreMarkup;
		Uri          readerURL;
		private XmlResolver xmlResolver=new XmlUrlResolver();

		// ********************************************************************
		// Constructors
		// ********************************************************************

		/** Create a new DTDParser. */
		public DTDParser()
		{
			dtdTokens = new TokenList(DTDConst.KEYWDS, DTDConst.KEYWD_TOKENS, DTDConst.KEYWD_TOKEN_UNKNOWN);
			initPredefinedEntities();
		}

		// ********************************************************************
		// Methods
		// ********************************************************************

		public DocumentType Parse(string systemId)
		{
			InputSource s=new InputSource(new Uri(systemId));
			return parseXMLDocument(s/*, null*/);
		}
		/**
		 * Parse the DocumentType in an XML document containing an internal subset,
		 * reference to an external subset, or both.
		 *
		 * @param src A SAX InputSource for the XML document.
		 * @param namespaceURIs A Hashtable of keyed by prefixes used in the DocumentType,
		 *    mapping these to namespace URIs. May be null.
		 * @return The DocumentType object.
		 * @exception XMLMiddlewareException Thrown if a DocumentType error is found.
		 * @exception EOFException Thrown if EOF is reached prematurely.
		 * @exception MalformedURLException Thrown if a system ID is malformed.
		 * @exception IOException Thrown if an I/O error occurs.
		 */
		protected DocumentType parseXMLDocument(InputSource src/*, Hashtable namespaceURIs*/)
		{
			initGlobals();
			//      this.namespaceURIs = namespaceURIs;
			openInputSource(src);
			parseDocument();
			postProcessDTD();
			return dtd;
		}

		/**
		 * Parse the DocumentType in an external subset.
		 *
		 * @param src A SAX InputSource for DocumentType (external subset).
		 * @param namespaceURIs A Hashtable of keyed by prefixes used in the DocumentType,
		 *    mapping these to namespace URIs. May be null.
		 * @return The DocumentType object.
		 * @exception XMLMiddlewareException Thrown if a DocumentType error is found.
		 * @exception EOFException Thrown if EOF is reached prematurely.
		 * @exception MalformedURLException Thrown if a system ID is malformed.
		 * @exception IOException Thrown if an I/O error occurs.
		 */

		public DocumentType parseExternalSubset(InputSource src)
		{
			return parseExternalSubset(src, false);
		}

		public DocumentType parseExternalSubset(InputSource src, bool init)
		{
			if ( init )
				initGlobals();
			//      this.namespaceURIs = namespaceURIs;
			openInputSource(src);
			parseExternalSubset(true);
			postProcessDTD();
			return dtd;
		}

		public DocumentType parseInternalSubset(InputSource src/*, Hashtable namespaceURIs*/)
		{
			return parseInternalSubset(src, /*namespaceURIs, */true);
		}

		public DocumentType parseInternalSubset(InputSource src, /* Hashtable namespaceURIs, */bool finalise)
		{
			initGlobals();
//			this.namespaceURIs = namespaceURIs;
			openInputSource(src);
			parseInternalSubset(true);
			if ( finalise )
				postProcessDTD();

			return dtd;
		}

		// ********************************************************************
		// Methods -- general parsing (!!! IN ALPHABETICAL ORDER !!!)
		// ********************************************************************

		void parseAttlistDecl()
		{
			// <!ATTLIST already parsed

			ElementType elementType;

			requireWhitespace();
			elementType = createElementType();

			while (!isChar('>'))
			{
				requireWhitespace();
				if (isChar('>')) break;
				getAttDef(elementType);
			}
		}

		void parseComment()
		{
			// '<!--' already parsed.
			int saveEntityState;

			saveEntityState = entityState;
			entityState = STATE_COMMENT;
			discardUntil("--");
			requireChar('>');
			entityState = saveEntityState;
		}

		bool parseConditional()
		{
			//      int     saveEntityState;
			bool condFound = true;

			if (isString("<!["))
			{
				discardWhitespace();
				if (isString("INCLUDE"))
				{
					parseInclude();
				}
				else if (isString("IGNORE"))
				{
					entityState = STATE_IGNORE;
					parseIgnoreSect();
					entityState = STATE_DTD;
				}
				else
				{
					throwXMLMiddlewareException("Invalid conditional section.");
				}
			}
			else
			{
				condFound = false;
			}

			return condFound;
		}

		void parseDocTypeDecl()
		{
			string root, systemID = null;

			if (!isString("<!DOCTYPE")) return;

			// Get the root element type.

			requireWhitespace();
			root = getName();
			if (root == null)
				throwXMLMiddlewareException("Invalid root element type name.");

			// Get the system ID of the external subset, if any.

			if (isWhitespace())
			{
				discardWhitespace();
				if (isString("SYSTEM"))
				{
					systemID = parseSystemLiteral();
					discardWhitespace();
				}
				else if (isString("PUBLIC"))
				{
					// Ignore the public ID and get the system ID.

					parsePublicID();
					systemID = parseSystemLiteral();
					discardWhitespace();
				}
			}

			// Get the internal subset, if any.

			if (isChar('['))
			{
				parseInternalSubset();
				requireChar(']');
			}

			// Get the external subset, if any.

			if (systemID != null)
			{
				pushCurrentReader();
				createURLReader(new Uri(readerURL, systemID));
				parseExternalSubset(false);
			}

			// Finish the document type declaration.

			discardWhitespace();
			requireChar('>');
		}

		void parseDocument()
		{
			if (isString("<?xml"))
			{
				parseXMLDecl();
			}
			parseMisc();
			parseDocTypeDecl();

			// Here is where the rest of the document would be parsed. However,
			// we only care about the DocumentType, so we simply stop.
		}

		void parseElementDecl()
		{
			ElementType elementType;

			// <!ELEMENT already parsed

			requireWhitespace();
			elementType = addElementType();
			requireWhitespace();
			getContentModel(elementType);
			discardWhitespace();
			requireChar('>');
		}

		void parseEncodingDecl()
		{
			// S 'encoding' already parsed.

			// BUG! We really need to do something with this -- drive transformations
			// or reject the document as unreadable...

			parseEquals();
			getEncName();
		}

		void parseEntityDecl()
		{
			// <!ENTITY already parsed.

			Entity  entity;
			bool isPE = false;
			string  name, notation, val = null, systemID = null, publicID = null;

			requireWhitespace();
			if (isChar('%'))
			{
				isPE = true;
				requireWhitespace();
			}

			name = getName();
			requireWhitespace();

			if (isString("PUBLIC"))
			{
				publicID = parsePublicID();
				systemID = parseSystemLiteral();
			}
			else if (isString("SYSTEM"))
			{
				systemID = parseSystemLiteral();
			}
			else
			{
				val = getEntityValue(isPE);
			}

			if (isPE)
			{
				// Parameter entity

				entity = new ParameterEntity(name);
				entity.SystemId = systemID;
				entity.PublicId = publicID;
				((ParameterEntity)entity).Value = val;

				dtd.AddParameterEntity(name, entity);
			}
			else if (isString("NDATA"))
			{
				// Unparsed entity

				requireWhitespace();
				notation = getName();

				entity = new UnparsedEntity(name);
				entity.SystemId = systemID;
				entity.PublicId = publicID;
				((UnparsedEntity)entity).Notation = notation;

				dtd.AddUnparsedEntity(name, entity);
			}
			else
			{
				// Parsed general entity

				entity = new ParsedGeneralEntity(name);
				entity.SystemId = systemID;
				entity.PublicId = publicID;
				((ParsedGeneralEntity)entity).Value = val;

				dtd.AddParsedGeneralEntity(name, entity);
			}
			discardWhitespace();
			requireChar('>');
		}

		void parseEquals()
		{
			discardWhitespace();
			requireChar('=');
			discardWhitespace();
		}

		void parseExternalSubset(bool eofOK)
		{
			entityState = STATE_DTD;

			if (isString("<?xml"))
			{
				parseTextDecl();
			}

			parseExternalSubsetDecl(eofOK);

			entityState = STATE_OUTSIDEDTD;
		}

		void parseExternalSubsetDecl(bool eofOK)
		{
			bool declFound = true;

			// Get the markup declarations.
			//
			// Note that we check here for an EOF. That is because this is the only
			// place it can legally occur, and then only when processing an external
			// subset only, as opposed to processing an external subset referenced
			// in a DOCTYPE statement.

			while (declFound)
			{
				try
				{
					discardWhitespace();
				}
				catch (EndOfStreamException)
				{
					if (eofOK) 
						return;

					throw;
				}
				declFound = parseMarkupDecl();
				if (!declFound)
				{
					declFound = parseConditional();
				}
			}
		}

		bool parseIgnore()
		{
			// There are three possible outcomes to this function:
			// * We find a new subsection ("<![")
			// * We find an end ("]]>")
			// * We run out of characters
			//
			// The states are as follows:
			// 0 - Just toodlin' along...
			// 1 - < found
			// 2 - <! found
			// 3 - ] found
			// 4 - ]] found

			char c;
			int  state = 0;

			while (true)
			{
				c = nextChar();

				switch (state)
				{
					case 0: // Toodlin' along
						if (c == '<')
							state = 1;
						else if (c == ']')
							state = 3;
						break;

					case 1: // < found
						state = (c == '!') ? 2 : 0;
						break;

					case 2: // <! found
						if (c == '[') return false;
						state = 0;
						break;

					case 3: // ] found
						state = (c == ']') ? 4 : 0;
						break;

					case 4: // ]] found
						if (c == ']') return true;
						state = 0;
						break;
				}
			}
		}

		void parseIgnoreSect()
		{
			discardWhitespace();
			requireChar('[');
			parseIgnoreSectContents();
		}

		void parseIgnoreSectContents()
		{
			// ignoreSectContents is an annoying little production. The
			// problem is that it can occur sequentially as well as
			// nested within itself. We solve the problem by keeping
			// track of the number of open <![...]]> sections. When we
			// close the last section, we're done. Note that we enter
			// this function already inside a section ([...]]>).

			int open = 1;

			while (open > 0)
			{
				open = (parseIgnore()) ? open - 1 : open + 1;
			}
		}

		void parseInclude()
		{
			discardWhitespace();
			requireChar('[');
			parseExternalSubsetDecl(false);
			requireString("]]>");
		}

		void parseInternalSubset()
		{
			parseInternalSubset(false);
		}

		// this added by Alfie
		void parseMarkedSection()
		{
			discardWhitespace();
			string name=getName();
			discardWhitespace();
			requireChar('[');
			if ( name.Equals(DTDConst.KEYWD_MS_IGNORE) )
			{
				discardMarkedSection();
			}
			else if ( name.Equals(DTDConst.KEYWD_MS_INCLUDE) )
			{
				bool declFound = true;
				while (declFound)
				{
					discardWhitespace();
					if ( isString("]]") )
						break;

					declFound = parseMarkupDecl();
				}
				requireChar('>');
			}
			else
				throw new XMLMiddlewareException("Invalid marked section - must be INCLUDE or EXCLUDE");
		}

		void discardMarkedSection()
		{
			int currentState=entityState;
			entityState=STATE_IGNORE;

			while ( true )
			{
				char x=getChar();
				if ( x == '<' && isString("![") )
				{
					discardMarkedSection();
				}
				else if ( x == ']' && isChar(']') )
					break;
			}
			requireChar('>');
			entityState=currentState;
		}

		void parseInternalSubset(bool eofOK)
		{
			bool declFound = true;

			entityState = STATE_DTD;

			// Get the markup declarations

			while (declFound)
			{
				try 
				{
					discardWhitespace();
					declFound = parseMarkupDecl();
				}
				catch (EndOfStreamException)
				{
					if (eofOK) 
						return;

					throw;
				}
			}
			entityState = STATE_OUTSIDEDTD;
		}

		bool parseMarkupDecl()
		{
			string name;

			// This function returns true if it finds a markup declaration; false
			// if it doesn't.

			if (!isChar('<')) return false;

			if (isString("!--"))
			{
				parseComment();
			}
			else if ( isString("![") )
			{
				parseMarkedSection();
			}
			else if (isChar('!'))
			{
				name = getName();

				int token=dtdTokens.getToken(name);

				if ( token.Equals(DTDConst.KEYWD_TOKEN_ELEMENT) )
					parseElementDecl();
				else if ( token.Equals(DTDConst.KEYWD_TOKEN_ATTLIST) )
					parseAttlistDecl();
				else if ( token.Equals(DTDConst.KEYWD_TOKEN_ENTITY) )
					parseEntityDecl();
				else if ( token.Equals(DTDConst.KEYWD_TOKEN_NOTATION) )
					parseNotationDecl();
				else
					throwXMLMiddlewareException("Invalid markup declaration: <!" + name);
			}
			else if (isChar('?'))
			{
				parsePI();
			}
			else
			{
				return false;
			}
			return true;
		}

		void parseMisc()
		{
			bool miscFound = true;

			while (miscFound)
			{
				discardWhitespace();
				if (isString("<!--"))
				{
					parseComment();
				}
				else if (isString("<?"))
				{
					parsePI();
				}
				else
				{
					miscFound = false;
				}
			}
		}

		void parseNotationDecl()
		{
			// <!NOTATION already parsed.

			Notation notation;
			string      keywd;

			// Create a new Notation.

			notation = new Notation();

			requireWhitespace();
			notation.Name = getName();
			requireWhitespace();

			keywd = getName();

			switch (dtdTokens.getToken(keywd))
			{
				case DTDConst.KEYWD_TOKEN_SYSTEM:
					notation.SystemID = parseSystemLiteral();
					discardWhitespace();
					requireChar('>');
					break;

				case DTDConst.KEYWD_TOKEN_PUBLIC:
					notation.PublicID = parsePublicID();
					if (!isChar('>'))
					{
						requireWhitespace();
						if (!isChar('>'))
						{
							notation.SystemID = getSystemLiteral();
							discardWhitespace();
							requireChar('>');
						}
					}
					break;

				default:
					throwXMLMiddlewareException("Invalid keyword in notation declaration: " + keywd);
					break;
			}

			// Add the Notation to the DocumentType.

			dtd.AddNotation(notation);
		}

		void parsePI()
		{
			// '<?' already parsed.
			discardUntil("?>");
		}

		string parsePublicID()
		{
			// PUBLIC already parsed.

			requireWhitespace();
			return getPubidLiteral();
		}

		void parseStandalone()
		{
			// S 'standalone' already parsed.

			//      string yesno;

			parseEquals();
			getYesNo();
		}

		string parseSystemLiteral()
		{
			// SYSTEM already parsed.

			requireWhitespace();
			return getSystemLiteral();
		}

		void parseTextDecl()
		{
			// '<?xml' already parsed.

			requireWhitespace();

			// Parse the version, if any.

			if (isString("version"))
			{
				parseVersion();
				requireWhitespace();
			}

			// Parse the encoding declaration.

			requireString("encoding");
			parseEncodingDecl();

			// Finish up.

			discardWhitespace();
			requireString("?>");
		}

		void parseVersion()
		{
			// S 'version' already parsed.

			char quote;

			parseEquals();
			quote = getQuote();
			requireString("1.0");
			requireChar(quote);
		}

		void parseXMLDecl()
		{
			// Check if we've got an XML declaration or a PI that starts
			// with XML. '<?xml' already parsed.

			if (!isWhitespace())
			{
				parsePI();
				return;
			}

			// Parse the version number.

			discardWhitespace();
			requireString("version");
			parseVersion();

			// Check for encoding and standalone declarations.

			if (isWhitespace())
			{
				discardWhitespace();

				// Parse the encoding declaration (if any), then return to the
				// same post-required-whitespace position.

				if (isString("encoding"))
				{
					parseEncodingDecl();
					if (!isWhitespace())
					{
						requireString("?>");
						return;
					}
					discardWhitespace();
				}

				// Parse the standalone declaration (if any), then return to the
				// same post-whitespace position.

				if (isString("standalone"))
				{
					parseStandalone();
					discardWhitespace();
				}
			}

			// Close the XML declaration.

			requireString("?>");
		}

		// ********************************************************************
		// Methods -- specialized parsing and DocumentType object building
		// ********************************************************************

		ElementType addElementType()
		{
			XmlName name;

			// Get the element type name and add it to the DocumentType. We store the name in a Hashtable
			// so we can later check if the name has already been declared. Note that we only
			// care about the hashtable key, not the hashtable element.

			name = getXMLName();
			if (declaredElementTypes.ContainsKey(name))
				throwXMLMiddlewareException("Duplicate element type declaration: " + name);
			declaredElementTypes.Add(name, name);
			return dtd.CreateElementType(name);
		}

		void getAttDef(ElementType elementType)
		{
			// S already parsed.

			Attribute attribute;

			attribute = getAttribute(elementType);
			requireWhitespace();
			getAttributeType(attribute);
			requireWhitespace();
			getAttributeRequired(attribute);
		}

		Attribute getAttribute(ElementType elementType)
		{
			XmlName name;
			Attribute attribute;

			// Get the attribute name and create a new Attribute.

			name = getXMLName();
			attribute = new Attribute(name);
      
			// If the element does not have an attribute with this name, add
			// it to the ElementType. Otherwise, ignore it.

			elementType.AddAttribute(attribute);

			// Return the new Attribute. Note that we do this even if it is
			// not added to the ElementType so that other code is simpler.

			return attribute;
		}

		void getAttributeDefault(Attribute attribute)
		{
			attribute.DefaultValue = getAttValue();
		}

		void getAttributeRequired(Attribute attribute)
		{
			string name;

			if (isChar('#'))
			{
				name = getName();
   
				switch (dtdTokens.getToken(name))
				{
					case DTDConst.KEYWD_TOKEN_REQUIRED:
						attribute.State = AttributeState.Required;
						break;
   
					case DTDConst.KEYWD_TOKEN_IMPLIED:
						attribute.State = AttributeState.Optional;
						break;
   
					case DTDConst.KEYWD_TOKEN_FIXED:
						attribute.State = AttributeState.Fixed;
						requireWhitespace();
						getAttributeDefault(attribute);
						break;
   
					default:
						throwXMLMiddlewareException("Invalid attribute default: " + name);
						break;
				}
			}
			else
			{
				attribute.State = AttributeState.Default;
				getAttributeDefault(attribute);
			}
		}

		void getAttributeType(Attribute attribute)
		{
			string name;

			if (isChar('('))
			{
				attribute.Type = AttributeType.Enumerated;
				getEnumeration(attribute, false);
				return;
			}

			name = getName();

			switch (dtdTokens.getToken(name))
			{
				case DTDConst.KEYWD_TOKEN_CDATA:
					attribute.Type = AttributeType.CDATA;
					break;

				case DTDConst.KEYWD_TOKEN_ID:
					attribute.Type = AttributeType.ID;
					break;

				case DTDConst.KEYWD_TOKEN_IDREF:
					attribute.Type = AttributeType.IDREF;
					break;

				case DTDConst.KEYWD_TOKEN_IDREFS:
					attribute.Type = AttributeType.IDREFS;
					break;

				case DTDConst.KEYWD_TOKEN_ENTITY:
					attribute.Type = AttributeType.ENTITY;
					break;

				case DTDConst.KEYWD_TOKEN_ENTITIES:
					attribute.Type = AttributeType.ENTITIES;
					break;

				case DTDConst.KEYWD_TOKEN_NMTOKEN:
					attribute.Type = AttributeType.NMTOKEN;
					break;

				case DTDConst.KEYWD_TOKEN_NMTOKENS:
					attribute.Type = AttributeType.NMTOKENS;
					break;

				case DTDConst.KEYWD_TOKEN_NOTATION:
					attribute.Type = AttributeType.NOTATION;
					requireWhitespace();
					requireChar('(');
					getEnumeration(attribute, true);
					break;

				default:
					throwXMLMiddlewareException("Invalid attribute type: " + name);
					break;
			}
		}

		void getContentModel(ElementType elementType)
		{
			// Get the content model.

			if (isChar('('))
			{
				// 5/18/00, Ronald Bourret
				// Added following call to discardWhitespace(). This is needed
				// for the case where space precedes the '#':
				//    <!ELEMENT A ( #PCDATA | B )*>

				discardWhitespace();
				if (isChar('#'))
				{
					getMixedContent(elementType);
				}
				else
				{
					getElementContent(elementType);
				}
			}
			else if (isString("EMPTY"))
			{
				elementType.ContentType = ElementContentType.Empty;
			}
			else if (isString("ANY"))
			{
				elementType.ContentType = ElementContentType.Any;
			}
			else
				throwXMLMiddlewareException("Invalid element type declaration.");
		}

		void getContentParticle(Group group, ElementType parent)
		{
			Group     childGroup;
			Reference rf;

			if (isChar('('))
			{
				childGroup = new Group();
				group.AddMember(childGroup);
				getGroup(childGroup, parent);
			}
			else
			{
				rf = getReference(group, parent, false);
				getFrequency(rf);
			}
		}

		void getElementContent(ElementType elementType)
		{
			elementType.ContentModel = new ContentModel();
			elementType.ContentType = ElementContentType.Element;
			getGroup(elementType.ContentModel, elementType);
			elementType.ContentModel.FinaliseGroup();
		}

		ElementType createElementType()
		{
			// Get the element type name and get the ElementType from the DocumentType.
			return dtd.CreateElementType(getXMLName());
		}

		void getEnumeratedValue(Attribute attribute, bool useNames, Hashtable enums)
		{
			string name;

			discardWhitespace();
			name = useNames ? getName() : getNmtoken();
			if (enums.ContainsKey(name))
				throwXMLMiddlewareException("Enumerated values must be unique: " + name);
			attribute.AddEnumeratedValue(name);
			discardWhitespace();
		}

		void getEnumeration(Attribute attribute, bool useNames)
		{
			//      string    name;
			Hashtable enums = new Hashtable();

			// Get the first enumerated value.
			getEnumeratedValue(attribute, useNames, enums);

			// Get the remaining values, if any.
			while (!isChar(')'))
			{
				requireChar('|');
				getEnumeratedValue(attribute, useNames, enums);
			}
		}

		void getFrequency(Particle particle)
		{
			if (isChar('?'))
			{
				particle.IsRequired = false;
				particle.IsRepeatable = false;
			}
			else if (isChar('+'))
			{
				particle.IsRequired = true;
				particle.IsRepeatable = true;
			}
			else if (isChar('*'))
			{
				particle.IsRequired = false;
				particle.IsRepeatable = true;
			}
			else
			{
				particle.IsRequired = true;
				particle.IsRepeatable = false;
			}
		}

		void getGroup(Group group, ElementType parent)
		{
			// This gets a choice or sequence.

			bool moreCPs = true;

			while (moreCPs)
			{
				discardWhitespace();
				getContentParticle(group, parent);
				discardWhitespace();
				if (isChar('|'))
				{
					if (group.Type == ParticleType.Unknown)
					{
						group.Type = ParticleType.Choice;
					}
					else if (group.Type == ParticleType.Sequence)
					{
						throwXMLMiddlewareException("Invalid mixture of ',' and '|' in content model.");
					}
				}
				else if (isChar(','))
				{
					if (group.Type == ParticleType.Unknown)
					{
						group.Type = ParticleType.Sequence;
					}
					else if (group.Type == ParticleType.Choice)
					{
						throwXMLMiddlewareException("Invalid mixture of ',' and '|' in content model.");
					}
				}
				else if (isChar(')'))
				{
					moreCPs = false;
					getFrequency(group);

					// If there is a single content particle in the group,
					// we simply call it a sequence.

					if (group.Type == ParticleType.Unknown)
					{
						group.Type = ParticleType.Sequence;
					}
				}
			}
			group.FinaliseGroup();
		}

		void getMixedContent(ElementType parent)
		{
			bool moreNames = true;

			discardWhitespace();
			requireString("PCDATA");
			discardWhitespace();
			if (isChar('|'))
			{
				// Content model is mixed: (#PCDATA | A | B)*

				parent.ContentType = ElementContentType.Mixed;

				// Add a choice Group for the content model.

				parent.ContentModel = new ContentModel();
				parent.ContentModel.Type = ParticleType.Choice;
				parent.ContentModel.IsRequired = false;
				parent.ContentModel.IsRepeatable = true;

				// Process the element type names. There must be at least one,
				// or we would have fallen into the else clause below.

				while (moreNames)
				{
					discardWhitespace();
					getReference(parent.ContentModel, parent, true);
					discardWhitespace();
					moreNames = isChar('|');
				}

				// Close the content model.

				requireString(")*");
			}
			else
			{
				// Content model is PCDATA-only: (#PCDATA)

				parent.ContentType = ElementContentType.PCDATA;
				requireChar(')');

				// 5/17/00, Ronald Bourret
				// Check if there is an asterisk after the closing parenthesis.
				// This covers the following case:
				//    <!ELEMENT A (#PCDATA)*>

				isChar('*');
			}
		}

		XmlName getXMLName()
		{
			// Get the element type name and construct an XMLName from it.

			string qualifiedName;

			qualifiedName = getName();
			return new XmlName(qualifiedName);

			//      return XMLName.Create(qualifiedName, namespaceURIs, useDefaultNamespace);
		}

		Reference getReference(Group group, ElementType parent, bool mixed)
		{
			//      XMLName     name;
			ElementType child;
			Reference   rf;

			// Create an ElementType for the referenced child.

			child = createElementType();

			// Add the child to the parent and vice versa. If we are processing
			// mixed content, then each child must be unique in the parent.

			if ( mixed )
			{
				if (parent.HasChildElement(child.Name))
					throwXMLMiddlewareException("The element type " + child.Name + " appeared more than once in the declaration of mixed content for the element type " + parent.Name + ".");
			}

			parent.AddChildElement(child);
			child.AddParentElement(parent);

			// Create a Reference for the child, add it to the group, and return it.

			rf = new Reference(child);
			group.AddMember(rf);
			return rf;
		}

		// ********************************************************************
		// Methods -- utility
		// ********************************************************************

		void initGlobals()
		{
			dtd = new DocumentType();
			entityState = STATE_OUTSIDEDTD;
			readerStack = new Stack();
			initReaderGlobals();
			declaredElementTypes.Clear();
		}

		void initPredefinedEntities()
		{
			ParsedGeneralEntity entity;

			entity = new ParsedGeneralEntity("lt");
			entity.Value = "<";
			predefinedEntities.Add(entity.Name, entity);

			entity = new ParsedGeneralEntity("gt");
			entity.Value = ">";
			predefinedEntities.Add(entity.Name, entity);

			entity = new ParsedGeneralEntity("amp");
			entity.Value = "&";
			predefinedEntities.Add(entity.Name, entity);

			entity = new ParsedGeneralEntity("apos");
			entity.Value = "'";
			predefinedEntities.Add(entity.Name, entity);

			entity = new ParsedGeneralEntity("quot");
			entity.Value = "\"";
			predefinedEntities.Add(entity.Name, entity);
		}

		void postProcessDTD()
		{
			if (dtd != null)
			{
				updateANYParents();
				checkElementTypeReferences();
				checkNotationReferences();
			}
		}

		private void updateANYParents()
		{
			// A common problem when building a DocumentType object is that element types
			// with a content model of ANY do not correctly list parents and children.
			// This method traverses the list of ElementTypes and, for each element
			// type with a content model of ANY, adds all other types as children and
			// this type as a parent.

			foreach ( ElementType parent in dtd.ElementTypes )
			{
				if (parent.ContentType == ElementContentType.Any)
				{
					foreach ( ElementType child in dtd.ElementTypes )
					{
						parent.AddChildElement(child);
						child.AddParentElement(parent);
					}
				}
			}
		}

		private void checkElementTypeReferences()
		{
			// TODO: M: think about this some more - most validators don't barf on undefined elements

			// Make sure that all referenced element types are defined.

			//	   foreach ( ElementType parent in dtd.ElementTypes )
			//      {
			//		   foreach ( ElementType child in parent.ChildElements )
			//		   {
			//			   if (!declaredElementTypes.ContainsKey(child.Name))
			//				   throw new ElementNotFoundException("Element type '" + child.Name.UniversalName + "' is referenced in element type '" + parent.Name.UniversalName + "' but is never defined.");
			//		   }
			//      }
		}

		private void checkNotationReferences()
		{
			// Checks that all notations referred to Attributes have been defined.

			foreach ( ElementType elementType in dtd.ElementTypes )
			{
				foreach ( Attribute attribute in elementType.Attributes )
				{
					if (attribute.Type == AttributeType.NOTATION)
					{
						foreach ( string notation in attribute.Enums )
						{
							if ( !dtd.HasNotation(notation) )
								throw new XMLMiddlewareException("Notation " + notation + " not defined. Used by the " + attribute.Name + " attribute of the " + elementType.Name + " element type.");
						}
					}
				}
			}

			foreach ( UnparsedEntity entity in dtd.UnparsedEntities )
			{
				if ( !dtd.HasNotation(entity.Notation) )
					throw new XMLMiddlewareException("Notation " + entity.Notation + " not defined. Used by the " + entity.Name + " unparsed entity.");
			}
		}

		void throwXMLMiddlewareException(string s)
		{
			throw new XMLMiddlewareException(s + "\nLine: " + line + " Column: " + column);
		}

		// ********************************************************************
		// Methods -- checking
		// 
		// NOTE: These methods are all designed on the notion that they start
		// checking whatever it is they are checking at the *next* character.
		// Therefore, methods that stop only by hitting something else (such
		// as isWhitespace()) must restore the last character read. This
		// probably isn't the Parsing 101 way to do things, but it provides
		// a consistent model that is easy to understand.
		// ********************************************************************

		bool isWhitespace()
		{
			// Checks if the next character is whitespace. If not, the
			// position is restored.

			if (isWhitespace(nextChar())) return true;
			restore();
			return false;
		}

		void requireWhitespace()
		{
			// Checks that the next character is whitespace and discards
			// that characters and any following whitespace characters.

			if (!isWhitespace())
				throwXMLMiddlewareException("Whitespace required.");
			discardWhitespace();
		}

		void discardWhitespace()
		{
			// Discards a sequence of whitespace.
			while (isWhitespace());
		}

		void discardUntil(string s)
		{
			// Discards a sequence of characters, stopping only after the
			// first occurrence of s is found.

			char[] chars = s.ToCharArray();
			char   c;
			int    pos = 0;

			while (pos < chars.Length)
			{
				c = nextChar();
				pos = (c == chars[pos]) ? pos + 1 : 0;
			}
		}

		bool isString(string s)
		{
			// Checks if the next sequence of characters matches s. If not,
			// the position is restored.

			char[] chars = s.ToCharArray();
			char   c;
			//      int    pos = 0;

			for (int i = 0; i < chars.Length; i++)
			{
				if ((c = nextChar()) != chars[i])
				{
					// Change the last character in the array to the current
					// character. Everything else up to this point has matched,
					// so there is no reason to change any earlier characters.

					chars[i] = c;
					restore(new string(chars, 0, i + 1));
					return false;
				}
			}
			return true;
		}

		bool isChar(char c)
		{
			// Checks if the next character matches c. If not, the position
			// is restored.

			if (nextChar() == c) return true;
			restore();
			return false;
		}

		void requireString(string s)
		{
			// Checks that the next sequence of characters matches s.

			if (!isString(s))
				throwXMLMiddlewareException("string required: " + s);
		}

		void requireChar(char c)
		{
			// Checks that the next character matches c.

			if (!isChar(c))
				throwXMLMiddlewareException("Character required: " + c);
		}

		// ********************************************************************
		// Methods -- production matching
		//
		// These methods return something that matches a low-level production
		// such as AttValue or Nmtoken. Like the checking methods, all assume
		// that you start checking with the *next* character and all leave
		// the pointer at the end of whatever it is you are checking, such as
		// the last character in a Nmtoken.
		//
		// NOTE: Most of these productions check for enclosing quotes, even when
		// these quotes are part of a higher-level production.
		// ********************************************************************

		string getAttValue()
		{
			// Gets something that matches the AttValue production. May be empty.

			char quote, c;

			// Set things up.

			entityState = STATE_ATTVALUE;
			quote = getQuote();
			resetLiteralBuffer();

			// Process the characters. Remember that quotes can be ignored if they 
			// are included as part of a parameter entity (Included as Literal) and
			// that markup (&, <) can be ignored if included as a character reference
			// (Included). See section 4.4.

			c = nextChar();
			while ((c != quote) || ignoreQuote)
			{
				if ((c == '<') || (c == '&'))
				{
					if (!ignoreMarkup)
						throwXMLMiddlewareException("Markup character '" + c + "' not allowed in default attribute value.");
				}
				appendLiteralBuffer(c);
				c = nextChar();
			}

			// Reset the state and return the value.

			entityState = STATE_DTD;
			return getLiteralBuffer();
		}

		string getEncName()
		{
			char quote, c;

			// Set things up.
			quote = getQuote();
			resetLiteralBuffer();

			// Process the first character.
			c = nextChar();
			if (!isLatinLetter(c))
				throwXMLMiddlewareException("Invalid starting character in encoding name: " + c);

			// Process the remaining characters
			while ((c = nextChar()) != quote)
			{
				if (!isLatinLetter(c) && !isLatinDigit(c) &&
					(c != '.') && (c != '_') && (c != '-'))
					throwXMLMiddlewareException("Invalid character in encoding name: " + c);
				appendLiteralBuffer(c);
			}

			// Return the literal.
			return getLiteralBuffer();
		}

		string getEntityValue(bool isPE)
		{
			// Gets something that matches the EntityValue production. May be empty.
			// Gets something that matches the AttValue production. May be empty.

			char quote, c;

			// Set things up.

			entityState = isPE ? STATE_PARAMENTITYVALUE : STATE_ENTITYVALUE;
			quote = getQuote();
			resetLiteralBuffer();

			// Process the characters. Remember that quotes can be ignored if they 
			// are included as part of a parameter entity (Included as Literal) and
			// that markup (%, <) can be ignored if included as a character reference
			// (Included). See section 4.4.

			c = nextChar();
			while ((c != quote) || ignoreQuote)
			{
				// commented by alfie - we don't really care what's in the entity dcl
				//         if ((c == '<') || (c == '%'))
				//         {
				//            if (!ignoreMarkup)
				//               throwXMLMiddlewareException("Markup character '" + c + "' not allowed in entity value.");
				//         }
				appendLiteralBuffer(c);
				c = nextChar();
			}

			// Reset the state and return the value.

			entityState = STATE_DTD;
			return getLiteralBuffer();
		}

		string getName()
		{
			// Gets something that matches the Name production. Must be non-empty.

			char c;

			// Set things up.

			resetLiteralBuffer();

			// Get the first character.

			c = nextChar();
			if (!isLetter(c) && (c != '_') && (c != ':'))
				throwXMLMiddlewareException("Invalid name start character: " + c);

			// Get characters until you hit a non-NameChar, then restore the last
			// character read.

			while (isNameChar(c))
			{
				appendLiteralBuffer(c);
				c = nextChar();
			}
			restore();

			// Return the buffered characters.

			return getLiteralBuffer();
		}

		string getNmtoken()
		{
			// Gets something that matches the Nmtoken production. Must be non-empty.

			char c;

			// Set things up.

			resetLiteralBuffer();

			// Get the first character.

			c = nextChar();
			if (!isNameChar(c))
				throwXMLMiddlewareException("Invalid Nmtoken start character: " + c);

			// Get characters until you hit a non-NameChar, then restore the last
			// character read.

			while (isNameChar(c))
			{
				appendLiteralBuffer(c);
				c = nextChar();
			}
			restore();

			// Return the buffered characters.

			return getLiteralBuffer();
		}

		string getPubidLiteral()
		{
			// Gets something that matches the PubidLiteral production. May be empty.

			char quote, c;

			quote = getQuote();
			resetLiteralBuffer();

			while ((c = nextChar()) != quote)
			{
				if (!isPubidChar(c))
					throwXMLMiddlewareException("Invalid character in public identifier: " + c);
				appendLiteralBuffer(c);
			}
			return getLiteralBuffer();
		}

		char getQuote()
		{
			char quote;

			quote = nextChar();
			if ((quote != '\'') && (quote != '"'))
				throwXMLMiddlewareException("Quote character required.");
			return quote;
		}

		string getSystemLiteral()
		{
			// Gets something that matches the SystemLiteral production. May be empty.

			char quote, c;

			quote = getQuote();
			resetLiteralBuffer();

			while ((c = nextChar()) != quote)
			{
				appendLiteralBuffer(c);
			}
			return getLiteralBuffer();
		}

		string getYesNo()
		{
			char    quote;
			bool no = true;

			quote = getQuote();
			if (!isString("no"))
			{
				requireString("yes");
				no = false;
			}
			requireChar(quote);
			return ((no) ? "no" : "yes");
		}

		void resetLiteralBuffer()
		{
			literalPos = -1;
			literalStr = null;
		}

		void appendLiteralBuffer(char c)
		{
			literalPos++;
			if (literalPos >= LITBUFSIZE)
			{
				if (literalStr == null)
				{
					literalStr = new StringBuilder();
				}
				literalStr.Append(literalBuffer);
				literalPos = 0;
			}
			literalBuffer[literalPos] = c;
		}

		string getLiteralBuffer()
		{
			if (literalStr == null)
			{
				return new string(literalBuffer, 0, literalPos + 1);
			}
			else
			{
				literalStr.Append(literalBuffer, 0, literalPos + 1);
				return literalStr.ToString();
			}
		}

		// ********************************************************************
		// Methods -- various character tests.
		//
		// Note that these tests do not read characters. They also aren't going
		// to win any speed contests, but they do work...
		// ********************************************************************

		bool isWhitespace(char c)
		{
			// Checks if the specified character is whitespace.

			switch (c)
			{
				case ' ':   // Space
				case '\r':   // Carriage return
				case '\n':   // Line feed
				case '\t':   // Tab
					return true;
            
				default:
					return false;
			}
		}

		bool isLatinLetter(char c)
		{
			return (((c >= 'A') && (c <= 'Z')) || ((c >= 'a') && (c <= 'z')));
		}

		bool isLatinDigit(char c)
		{
			return ((c >= '0') && (c <= '9'));
		}

		bool isPubidChar(char c)
		{
			switch (c)
			{
				case '-':
				case '\'':
				case '(':
				case ')':
				case '+':
				case ',':
				case '.':
				case '/':
				case ':':
				case '=':
				case '?':
				case ';':
				case '!':
				case '*':
				case '#':
				case '@':
				case '$':
				case '_':
				case '%':
				case ' ':
				case '\r':
				case '\n':
					return true;

				default:
					return (isLatinLetter(c) || isLatinDigit(c));
			}
		}

		bool isNameChar(char c)
		{
			if (isLatinLetter(c)) return true;
			if (isLatinDigit(c)) return true;
			if ((c == '.') || (c == '-') || (c == '_') || (c == ':')) return true;
			if (isLetter(c)) return true;
			if (isDigit(c)) return true;
			if (isCombiningChar(c)) return true;
			if (isExtender(c)) return true;
			return false;
		}

		bool isLetter(char c)
		{
			// Checks for letters (BaseChar | Ideographic)

			switch(c >> 8)
			{
				case 0x00:
					if ((c >= 0x0041) && (c <= 0x005A)) return true;
					if ((c >= 0x0061) && (c <= 0x007A)) return true;
					if ((c >= 0x00C0) && (c <= 0x00D6)) return true;
					if ((c >= 0x00D8) && (c <= 0x00F6)) return true;
					if ((c >= 0x00F8) && (c <= 0x00FF)) return true;

					return false;

				case 0x01:
					if ((c >= 0x0100) && (c <= 0x0131)) return true;
					if ((c >= 0x0134) && (c <= 0x013E)) return true;
					if ((c >= 0x0141) && (c <= 0x0148)) return true;
					if ((c >= 0x014A) && (c <= 0x017E)) return true;
					if ((c >= 0x0180) && (c <= 0x01C3)) return true;
					if ((c >= 0x01CD) && (c <= 0x01F0)) return true;
					if ((c >= 0x01F4) && (c <= 0x01F5)) return true;
					if ((c >= 0x01FA) && (c <= 0x01FF)) return true;

					return false;

				case 0x02:
					if ((c >= 0x0200) && (c <= 0x0217)) return true;
					if ((c >= 0x0250) && (c <= 0x02A8)) return true;
					if ((c >= 0x02BB) && (c <= 0x02C1)) return true;

					return false;

				case 0x03:
					if ((c >= 0x0388) && (c <= 0x038A)) return true;
					if ((c >= 0x038E) && (c <= 0x03A1)) return true;
					if ((c >= 0x03A3) && (c <= 0x03CE)) return true;
					if ((c >= 0x03D0) && (c <= 0x03D6)) return true;
					if ((c >= 0x03E2) && (c <= 0x03F3)) return true;

					if ((c == 0x0386)  || (c == 0x038C)  || (c == 0x03DA)  ||
						(c == 0x03DC)  || (c == 0x03DE)  || (c == 0x03E0)) return true;

					return false;

				case 0x04:
					if ((c >= 0x0401) && (c <= 0x040C)) return true;
					if ((c >= 0x040E) && (c <= 0x044F)) return true;
					if ((c >= 0x0451) && (c <= 0x045C)) return true;
					if ((c >= 0x045E) && (c <= 0x0481)) return true;
					if ((c >= 0x0490) && (c <= 0x04C4)) return true;
					if ((c >= 0x04C7) && (c <= 0x04C8)) return true;
					if ((c >= 0x04CB) && (c <= 0x04CC)) return true;
					if ((c >= 0x04D0) && (c <= 0x04EB)) return true;
					if ((c >= 0x04EE) && (c <= 0x04F5)) return true;
					if ((c >= 0x04F8) && (c <= 0x04F9)) return true;

					return false;

				case 0x05:
					if ((c >= 0x0531) && (c <= 0x0556)) return true;
					if ((c >= 0x0561) && (c <= 0x0586)) return true;
					if ((c >= 0x05D0) && (c <= 0x05EA)) return true;
					if ((c >= 0x05F0) && (c <= 0x05F2)) return true;

					if (c == 0x0559) return true;

					return false;

				case 0x06:
					if ((c >= 0x0621) && (c <= 0x063A)) return true;
					if ((c >= 0x0641) && (c <= 0x064A)) return true;
					if ((c >= 0x0671) && (c <= 0x06B7)) return true;
					if ((c >= 0x06BA) && (c <= 0x06BE)) return true;
					if ((c >= 0x06C0) && (c <= 0x06CE)) return true;
					if ((c >= 0x06D0) && (c <= 0x06D3)) return true;
					if ((c >= 0x06E5) && (c <= 0x06E6)) return true;

					if (c == 0x06D5) return true;

					return false;

				case 0x09:
					if ((c >= 0x0905) && (c <= 0x0939)) return true;
					if ((c >= 0x0958) && (c <= 0x0961)) return true;
					if ((c >= 0x0985) && (c <= 0x098C)) return true;
					if ((c >= 0x098F) && (c <= 0x0990)) return true;
					if ((c >= 0x0993) && (c <= 0x09A8)) return true;
					if ((c >= 0x09AA) && (c <= 0x09B0)) return true;
					if ((c >= 0x09B6) && (c <= 0x09B9)) return true;
					if ((c >= 0x09DC) && (c <= 0x09DD)) return true;
					if ((c >= 0x09DF) && (c <= 0x09E1)) return true;
					if ((c >= 0x09F0) && (c <= 0x09F1)) return true;

					if ((c == 0x093D)  || (c == 0x09B2)) return true;

					return false;

				case 0x0A:
					if ((c >= 0x0A05) && (c <= 0x0A0A)) return true;
					if ((c >= 0x0A0F) && (c <= 0x0A10)) return true;
					if ((c >= 0x0A13) && (c <= 0x0A28)) return true;
					if ((c >= 0x0A2A) && (c <= 0x0A30)) return true;
					if ((c >= 0x0A32) && (c <= 0x0A33)) return true;
					if ((c >= 0x0A35) && (c <= 0x0A36)) return true;
					if ((c >= 0x0A38) && (c <= 0x0A39)) return true;
					if ((c >= 0x0A59) && (c <= 0x0A5C)) return true;
					if ((c >= 0x0A72) && (c <= 0x0A74)) return true;
					if ((c >= 0x0A85) && (c <= 0x0A8B)) return true;
					if ((c >= 0x0A8F) && (c <= 0x0A91)) return true;
					if ((c >= 0x0A93) && (c <= 0x0AA8)) return true;
					if ((c >= 0x0AAA) && (c <= 0x0AB0)) return true;
					if ((c >= 0x0AB2) && (c <= 0x0AB3)) return true;
					if ((c >= 0x0AB5) && (c <= 0x0AB9)) return true;

					if ((c == 0x0A5E)  || (c == 0x0A8D)  || (c == 0x0ABD)  ||
						(c == 0x0AE0)) return true;

					return false;

				case 0x0B:
					if ((c >= 0x0B05) && (c <= 0x0B0C)) return true;
					if ((c >= 0x0B0F) && (c <= 0x0B10)) return true;
					if ((c >= 0x0B13) && (c <= 0x0B28)) return true;
					if ((c >= 0x0B2A) && (c <= 0x0B30)) return true;
					if ((c >= 0x0B32) && (c <= 0x0B33)) return true;
					if ((c >= 0x0B36) && (c <= 0x0B39)) return true;
					if ((c >= 0x0B5C) && (c <= 0x0B5D)) return true;
					if ((c >= 0x0B5F) && (c <= 0x0B61)) return true;
					if ((c >= 0x0B85) && (c <= 0x0B8A)) return true;
					if ((c >= 0x0B8E) && (c <= 0x0B90)) return true;
					if ((c >= 0x0B92) && (c <= 0x0B95)) return true;
					if ((c >= 0x0B99) && (c <= 0x0B9A)) return true;
					if ((c >= 0x0B9E) && (c <= 0x0B9F)) return true;
					if ((c >= 0x0BA3) && (c <= 0x0BA4)) return true;
					if ((c >= 0x0BA8) && (c <= 0x0BAA)) return true;
					if ((c >= 0x0BAE) && (c <= 0x0BB5)) return true;
					if ((c >= 0x0BB7) && (c <= 0x0BB9)) return true;

					if ((c == 0x0B3D)  || (c == 0x0B9C)) return true;

					return false;

				case 0x0C:
					if ((c >= 0x0C05) && (c <= 0x0C0C)) return true;
					if ((c >= 0x0C0E) && (c <= 0x0C10)) return true;
					if ((c >= 0x0C12) && (c <= 0x0C28)) return true;
					if ((c >= 0x0C2A) && (c <= 0x0C33)) return true;
					if ((c >= 0x0C35) && (c <= 0x0C39)) return true;
					if ((c >= 0x0C60) && (c <= 0x0C61)) return true;
					if ((c >= 0x0C85) && (c <= 0x0C8C)) return true;
					if ((c >= 0x0C8E) && (c <= 0x0C90)) return true;
					if ((c >= 0x0C92) && (c <= 0x0CA8)) return true;
					if ((c >= 0x0CAA) && (c <= 0x0CB3)) return true;
					if ((c >= 0x0CB5) && (c <= 0x0CB9)) return true;
					if ((c >= 0x0CE0) && (c <= 0x0CE1)) return true;

					if (c == 0x0CDE) return true;

					return false;

				case 0x0D:
					if ((c >= 0x0D05) && (c <= 0x0D0C)) return true;
					if ((c >= 0x0D0E) && (c <= 0x0D10)) return true;
					if ((c >= 0x0D12) && (c <= 0x0D28)) return true;
					if ((c >= 0x0D2A) && (c <= 0x0D39)) return true;
					if ((c >= 0x0D60) && (c <= 0x0D61)) return true;

					return false;

				case 0x0E:
					if ((c >= 0x0E01) && (c <= 0x0E2E)) return true;
					if ((c >= 0x0E32) && (c <= 0x0E33)) return true;
					if ((c >= 0x0E40) && (c <= 0x0E45)) return true;
					if ((c >= 0x0E81) && (c <= 0x0E82)) return true;
					if ((c >= 0x0E87) && (c <= 0x0E88)) return true;
					if ((c >= 0x0E94) && (c <= 0x0E97)) return true;
					if ((c >= 0x0E99) && (c <= 0x0E9F)) return true;
					if ((c >= 0x0EA1) && (c <= 0x0EA3)) return true;
					if ((c >= 0x0EAA) && (c <= 0x0EAB)) return true;
					if ((c >= 0x0EAD) && (c <= 0x0EAE)) return true;
					if ((c >= 0x0EB2) && (c <= 0x0EB3)) return true;
					if ((c >= 0x0EC0) && (c <= 0x0EC4)) return true;

					if ((c == 0x0E30)  || (c == 0x0E84)  || (c == 0x0E8A)  ||
						(c == 0x0E8D)  || (c == 0x0EA5)  || (c == 0x0EA7)  ||
						(c == 0x0EB0)  || (c == 0x0EBD)) return true;

					return false;

				case 0x0F:
					if ((c >= 0x0F40) && (c <= 0x0F47)) return true;
					if ((c >= 0x0F49) && (c <= 0x0F69)) return true;

					return false;

				case 0x10:
					if ((c >= 0x10A0) && (c <= 0x10C5)) return true;
					if ((c >= 0x10D0) && (c <= 0x10F6)) return true;

					return false;

				case 0x11:
					if ((c >= 0x1102) && (c <= 0x1103)) return true;
					if ((c >= 0x1105) && (c <= 0x1107)) return true;
					if ((c >= 0x110B) && (c <= 0x110C)) return true;
					if ((c >= 0x110E) && (c <= 0x1112)) return true;
					if ((c >= 0x1154) && (c <= 0x1155)) return true;
					if ((c >= 0x115F) && (c <= 0x1161)) return true;
					if ((c >= 0x116D) && (c <= 0x116E)) return true;
					if ((c >= 0x1172) && (c <= 0x1173)) return true;
					if ((c >= 0x11AE) && (c <= 0x11AF)) return true;
					if ((c >= 0x11B7) && (c <= 0x11B8)) return true;
					if ((c >= 0x11BC) && (c <= 0x11C2)) return true;

					if ((c == 0x1100)  || (c == 0x1109)  || (c == 0x113C)  ||
						(c == 0x113E)  || (c == 0x1140)  || (c == 0x114C)  ||
						(c == 0x114E)  || (c == 0x1150)  || (c == 0x1159)  ||
						(c == 0x1163)  || (c == 0x1165)  || (c == 0x1167)  ||
						(c == 0x1169)  || (c == 0x1175)  || (c == 0x119E)  ||
						(c == 0x11A8)  || (c == 0x11AB)  || (c == 0x11BA)  ||
						(c == 0x11EB)  || (c == 0x11F0)  || (c == 0x11F9)) return true;

					return false;

				case 0x1E:
					if ((c >= 0x1E00) && (c <= 0x1E9B)) return true;
					if ((c >= 0x1EA0) && (c <= 0x1EF9)) return true;

					return false;

				case 0x1F:
					if ((c >= 0x1F00) && (c <= 0x1F15)) return true;
					if ((c >= 0x1F18) && (c <= 0x1F1D)) return true;
					if ((c >= 0x1F20) && (c <= 0x1F45)) return true;
					if ((c >= 0x1F48) && (c <= 0x1F4D)) return true;
					if ((c >= 0x1F50) && (c <= 0x1F57)) return true;
					if ((c >= 0x1F5F) && (c <= 0x1F7D)) return true;
					if ((c >= 0x1F80) && (c <= 0x1FB4)) return true;
					if ((c >= 0x1FB6) && (c <= 0x1FBC)) return true;
					if ((c >= 0x1FC2) && (c <= 0x1FC4)) return true;
					if ((c >= 0x1FC6) && (c <= 0x1FCC)) return true;
					if ((c >= 0x1FD0) && (c <= 0x1FD3)) return true;
					if ((c >= 0x1FD6) && (c <= 0x1FDB)) return true;
					if ((c >= 0x1FE0) && (c <= 0x1FEC)) return true;
					if ((c >= 0x1FF2) && (c <= 0x1FF4)) return true;
					if ((c >= 0x1FF6) && (c <= 0x1FFC)) return true;

					if ((c == 0x1F59)  || (c == 0x1F5B)  || (c == 0x1F5D)  ||
						(c == 0x1FBE)) return true;

					return false;

				case 0x21:
					if ((c >= 0x212A) && (c <= 0x212B)) return true;
					if ((c >= 0x2180) && (c <= 0x2182)) return true;

					if ((c == 0x2126)  || (c == 0x212E)) return true;

					return false;

				case 0x20:
					if ((c >= 0x3041) && (c <= 0x3094)) return true;
					if ((c >= 0x30A1) && (c <= 0x30FA)) return true;
					if ((c >= 0x3021) && (c <= 0x3029)) return true;

					if (c == 0x3007) return true;

					return false;

				case 0x31:
					if ((c >= 0x3105) && (c <= 0x312C)) return true;

					return false;

				default:
					if ((c >= 0xAC00) && (c <= 0xD7A3)) return true;
					if ((c >= 0x4E00) && (c <= 0x9FA5)) return true;

					return false;
			}
		}

		bool isDigit(char c)
		{
			// Checks for digits. Note that the Java Character.isDigit() function
			// includes the values 0xFF10 - 0xFF19, which are not considered digits
			// according to the XML spec. Therefore, we need to check if these are
			// the reason Character.isDigit() returned true.

			if (!char.IsDigit(c)) return false;
			return (c > 0xF29);
		}

		bool isCombiningChar(char c)
		{
			// Checks for combining characters.

			switch (c >> 8)
			{
				case 0x03:
					if ((c >= 0x0300) && (c <= 0x0345)) return true;
					if ((c >= 0x0360) && (c <= 0x0361)) return true;

					return false;

				case 0x04:
					if ((c >= 0x0483) && (c <= 0x0486)) return true;

					return false;

				case 0x05:
					if ((c >= 0x0591) && (c <= 0x05A1)) return true;
					if ((c >= 0x05A3) && (c <= 0x05B9)) return true;
					if ((c >= 0x05BB) && (c <= 0x05BD)) return true;
					if ((c >= 0x05C1) && (c <= 0x05C2)) return true;

					if ((c == 0x05BF) || (c == 0x05C4)) return true;

					return false;

				case 0x06:
					if ((c >= 0x064B) && (c <= 0x0652)) return true;
					if ((c >= 0x06D6) && (c <= 0x06DC)) return true;
					if ((c >= 0x06DD) && (c <= 0x06DF)) return true;
					if ((c >= 0x06E0) && (c <= 0x06E4)) return true;
					if ((c >= 0x06E7) && (c <= 0x06E8)) return true;
					if ((c >= 0x06EA) && (c <= 0x06ED)) return true;

					if (c == 0x0670) return true;

					return false;

				case 0x09:
					if ((c >= 0x0901) && (c <= 0x0903)) return true;
					if ((c >= 0x093E) && (c <= 0x094C)) return true;
					if ((c >= 0x0951) && (c <= 0x0954)) return true;
					if ((c >= 0x0962) && (c <= 0x0963)) return true;
					if ((c >= 0x0981) && (c <= 0x0983)) return true;
					if ((c >= 0x09C0) && (c <= 0x09C4)) return true;
					if ((c >= 0x09C7) && (c <= 0x09C8)) return true;
					if ((c >= 0x09CB) && (c <= 0x09CD)) return true;
					if ((c >= 0x09E2) && (c <= 0x09E3)) return true;

					if ((c == 0x093C) || (c == 0x094D) || (c == 0x09BC) ||
						(c == 0x09BE) || (c == 0x09BF) || (c == 0x09D7)) return true;

					return false;

				case 0x0A:
					if ((c >= 0x0A40) && (c <= 0x0A42)) return true;
					if ((c >= 0x0A47) && (c <= 0x0A48)) return true;
					if ((c >= 0x0A4B) && (c <= 0x0A4D)) return true;
					if ((c >= 0x0A70) && (c <= 0x0A71)) return true;
					if ((c >= 0x0A81) && (c <= 0x0A83)) return true;
					if ((c >= 0x0ABE) && (c <= 0x0AC5)) return true;
					if ((c >= 0x0AC7) && (c <= 0x0AC9)) return true;
					if ((c >= 0x0ACB) && (c <= 0x0ACD)) return true;

					if ((c == 0x0A02) || (c == 0x0A3C) || (c == 0x0A3E) ||
						(c == 0x0A3F) || (c == 0x0ABC)) return true;

					return false;

				case 0x0B:
					if ((c >= 0x0B01) && (c <= 0x0B03)) return true;
					if ((c >= 0x0B3E) && (c <= 0x0B43)) return true;
					if ((c >= 0x0B47) && (c <= 0x0B48)) return true;
					if ((c >= 0x0B4B) && (c <= 0x0B4D)) return true;
					if ((c >= 0x0B56) && (c <= 0x0B57)) return true;
					if ((c >= 0x0B82) && (c <= 0x0B83)) return true;
					if ((c >= 0x0BBE) && (c <= 0x0BC2)) return true;
					if ((c >= 0x0BC6) && (c <= 0x0BC8)) return true;
					if ((c >= 0x0BCA) && (c <= 0x0BCD)) return true;

					if ((c == 0x0B3C) || (c == 0x0BD7)) return true;

					return false;

				case 0x0C:
					if ((c >= 0x0C01) && (c <= 0x0C03)) return true;
					if ((c >= 0x0C3E) && (c <= 0x0C44)) return true;
					if ((c >= 0x0C46) && (c <= 0x0C48)) return true;
					if ((c >= 0x0C4A) && (c <= 0x0C4D)) return true;
					if ((c >= 0x0C55) && (c <= 0x0C56)) return true;
					if ((c >= 0x0C82) && (c <= 0x0C83)) return true;
					if ((c >= 0x0CBE) && (c <= 0x0CC4)) return true;
					if ((c >= 0x0CC6) && (c <= 0x0CC8)) return true;
					if ((c >= 0x0CCA) && (c <= 0x0CCD)) return true;
					if ((c >= 0x0CD5) && (c <= 0x0CD6)) return true;
					return false;

				case 0x0D:
					if ((c >= 0x0D02) && (c <= 0x0D03)) return true;
					if ((c >= 0x0D3E) && (c <= 0x0D43)) return true;
					if ((c >= 0x0D46) && (c <= 0x0D48)) return true;
					if ((c >= 0x0D4A) && (c <= 0x0D4D)) return true;

					if (c == 0x0D57) return true;

					return false;

				case 0x0E:
					if ((c >= 0x0E34) && (c <= 0x0E3A)) return true;
					if ((c >= 0x0E47) && (c <= 0x0E4E)) return true;
					if ((c >= 0x0EB4) && (c <= 0x0EB9)) return true;
					if ((c == 0x0EBB) && (c <= 0x0EBC)) return true;
					if ((c >= 0x0EC8) && (c <= 0x0ECD)) return true;

					if ((c == 0x0E31) || (c == 0x0EB1)) return true;

					return false;

				case 0x0F:
					if ((c >= 0x0F18) && (c <= 0x0F19)) return true;
					if ((c >= 0x0F71) && (c <= 0x0F84)) return true;
					if ((c >= 0x0F86) && (c <= 0x0F8B)) return true;
					if ((c >= 0x0F90) && (c <= 0x0F95)) return true;
					if ((c >= 0x0F99) && (c <= 0x0FAD)) return true;
					if ((c >= 0x0FB1) && (c <= 0x0FB7)) return true;

					if ((c == 0x0F35) || (c == 0x0F37) || (c == 0x0F39) ||
						(c == 0x0F3E) || (c == 0x0F3F) || (c == 0x0F97) ||
						(c == 0x0FB9)) return true;

					return false;

				case 0x20:
					if ((c >= 0x20D0) && (c <= 0x20DC)) return true;

					if (c == 0x20E1) return true;

					return false;

				case 0x30:
					if ((c >= 0x302A) && (c <= 0x302F)) return true;

					if ((c == 0x3099) || (c == 0x309A)) return true;

					return false;

				default:
					return false;
			}
		}

		bool isExtender(char c)
		{
			// Checks for extenders.

			switch (c)
			{
				case (char) 0x00B7:
				case (char) 0x02D0:
				case (char) 0x02D1:
				case (char) 0x0387:
				case (char) 0x0640:
				case (char) 0x0E46:
				case (char) 0x0EC6:
				case (char) 0x3005:
					return true;

				default:
					if ((c >= 0x3031) && (c <= 0x3035)) return true;
					if ((c >= 0x309D) && (c <= 0x309E)) return true;
					if ((c >= 0x30FC) && (c <= 0x30FE)) return true;
					return false;
			}
		}

		// ********************************************************************
		// Methods -- Entity handling
		// ********************************************************************

		char nextChar()
		{
			// This method gets a character and then deals with the Joy of Entities.

			char c;

			c = getChar();

			switch (c)
			{
				case '&':
					c = processAmpersand();
					break;

				case '%':
					c = processPercent();
					break;

				default:
					break;
			}

			if (c == '\n')
			{
				line++;
				column = 1;
			}
			else
			{
				column++;
			}

			return c;
		}

		char processAmpersand()
		{
			switch (entityState)
			{
				case STATE_DTD:
					throwXMLMiddlewareException("Invalid general entity reference or character reference.");
					return (char) 0;

				case STATE_ATTVALUE:
					if (getChar() == '#')
					{
						getCharRef();
					}
					else
					{
						restore();
						getGeneralEntityRef();
					}
					return nextChar();

				case STATE_ENTITYVALUE:
					if (getChar() == '#')
					{
						getCharRef();
						return nextChar();
					}
					else
					{
						restore();
						return '&';
					}

				case STATE_OUTSIDEDTD:
				case STATE_COMMENT:
				case STATE_IGNORE:
					return '&';

				default:
					throw new InvalidOperationException("Internal error: invalid entity state: " + entityState);
			}
		}

		char processPercent()
		{
			char c;

			switch (entityState)
			{
				case STATE_DTD:
					// Check if we are processing a parameter entity declaration
					// rather than a parameter entity reference.
					c = getChar();
					restore();
					if (isWhitespace(c)) return '%';
					getParameterEntityRef();
					return nextChar();

				case STATE_ATTVALUE:
					return '%';

				case STATE_PARAMENTITYVALUE:
					getParameterEntityRef();
					return nextChar();

				case STATE_ENTITYVALUE:
				case STATE_OUTSIDEDTD:
				case STATE_COMMENT:
				case STATE_IGNORE:
					return '%';

				default:
					throw new InvalidOperationException("Internal error: invalid entity state: " + entityState);
			}
		}

		void getCharRef()
		{
			// &# already parsed.

			bool hex = false;
			char    c;
			char[]  chars = new char[1];
			int     value = 0;

			// Check if we have a hexadecimal character reference.

			c = getChar();
			if (c == 'x')
			{
				hex = true;
				c = getChar();
			}

			// Parse the character reference and get the value.

			while (c != ';')
			{
				if (hex)
				{
					c = char.ToUpper(c);
					if ((c < '0') || (c > 'F') || ((c > '9') && (c < 'A')))
						throwXMLMiddlewareException("Invalid character in character reference: " + c);
					value *= 16;
					value += (c < 'A') ? c - '0' : c - 'A' + 10;
				}
				else
				{
					if ((c < '0') || (c > '9'))
						throwXMLMiddlewareException("Invalid character in character reference: " + c);
					value *= 10;
					value += c - '0';
				}
				c = getChar();
			}
			if (value > char.MaxValue)
				throwXMLMiddlewareException("Invalid character reference: " + value);

			// Push the current Reader and its associated information on the stack,
			// then create a new Reader for the corresponding character.

			pushCurrentReader();

			chars[0] = (char)value;
			createStringReader(new string(chars));
			ignoreQuote = true;
			ignoreMarkup = true;
		}

		void getGeneralEntityRef()
		{
			// & already parsed.
			//
			// WARNING! This is not a generic function. It assumes it is called only
			// from inside an attribute value.

			char                c;
			//      int                 size;
			string              entityName;
			ParsedGeneralEntity entity;

			// Get the general entity name.

			resetNameBuffer();
			while ((c = getChar()) != ';')
			{
				appendNameBuffer(c);
			}
			entityName = getNameBuffer();

			// Push the current Reader and its associated information on the stack.

			pushCurrentReader();

			// Get the general entity and set up the Reader information.

			entity = dtd.GetParsedGeneralEntity(entityName);
			if (entity == null)
			{
				entity = (ParsedGeneralEntity) predefinedEntities[entityName];
				if (entity == null)
					throwXMLMiddlewareException("Reference to undefined parsed general entity: " + entityName);
			}

			if (entity.Value == null)
				throwXMLMiddlewareException("Reference to external parsed general entity in attribute value: " + entityName);

			createStringReader(entity.Value);
			ignoreQuote = true;
			ignoreMarkup = false;
		}

		void getParameterEntityRef()
		{
			// % already parsed.
			//
			// WARNING! This is not a generic function. It assumes it is called only
			// from inside the DocumentType or an entity value.

			char            c;
			string          entityName;
			ParameterEntity entity;

			// Get the parameter entity name.

			resetNameBuffer();
			while ((c = getChar()) != ';')
			{
				appendNameBuffer(c);
			}
			entityName = getNameBuffer();

			// Push the current Reader and its associated information on the stack.

			pushCurrentReader();

			// Get the parameter entity.

			entity = dtd.GetParameterEntity(entityName);
			if (entity == null)
				throwXMLMiddlewareException("Reference to undefined parameter entity: " + entityName);

			// Set up the Reader information. Notice that we need to include spaces
			// before and after the entity value. Also notice that we ignore quotes
			// if the parameter entity occurs inside an entity value.

			pushStringReader(" ", false, false);
			if (entity.Value != null)
			{
				pushStringReader(entity.Value, (entityState == STATE_PARAMENTITYVALUE), false);
			}
			else
			{
				pushURLReader(entity.SystemId, (entityState == STATE_PARAMENTITYVALUE), false);
			}

			createStringReader(" ");
			ignoreQuote = false;
			ignoreMarkup = false;
		}

		void createStringReader(string s)
		{
			int size;

			// Note that readerURL is unchanged because the string is in the
			// context of its parent Reader.

			reader = new StringReader(s);
			readerType = READER_READER;
			size = (s.Length > BUFSIZE) ? BUFSIZE : s.Length;
			buffer = new char[size];
			bufferPos = BUFSIZE + 1;
			bufferLen = -1;
			line = 1;
			column = 1;
		}

		public void createURLReader(Uri url)
		{
			if ( url.IsFile )
				reader = new StreamReader(url.LocalPath);
			else
			{
				HttpWebRequest hwr=(HttpWebRequest) WebRequest.Create(url.AbsoluteUri);
				hwr.KeepAlive=false;
				HttpWebResponse hwresp=(HttpWebResponse) hwr.GetResponse();
				reader = new StreamReader(hwresp.GetResponseStream());
			}

			readerType = READER_READER;
			readerURL = url;
			buffer = new char[BUFSIZE];
			bufferPos = BUFSIZE + 1;
			bufferLen = 0;
			line = 1;
			column = 1;
		}

		void pushCurrentReader()
		{
			readerStack.Push(new ReaderInfo(reader, buffer, readerURL, null, readerType, bufferPos, bufferLen, line, column, ignoreQuote, ignoreMarkup));
		}

		void pushStringReader(string s, bool ignoreQuote, bool ignoreMarkup)
		{
			readerStack.Push(new ReaderInfo(null, null, null, s, READER_STRING, 0, 0, 1, 1, ignoreQuote, ignoreMarkup));
		}

		void pushURLReader(string urlString, bool ignoreQuote, bool ignoreMarkup)
		{
			Uri uri=xmlResolver.ResolveUri(readerURL, urlString);
			readerStack.Push(new ReaderInfo(null, null, uri, null, READER_URL, 0, 0, 1, 1, ignoreQuote, ignoreMarkup));
		}

		void popReader()
		{
			ReaderInfo readerInfo;

			// TODO: M: added by alfie - not 100% sure this is correct
			reader.Close();

			if ( readerStack.Count == 0 )
				throw new EndOfStreamException("End of file reached while parsing.");

			readerInfo = (ReaderInfo)readerStack.Pop();
			switch (readerInfo.type)
			{
				case READER_READER:
					// All this means is that we've already created the Reader
					// and the associated buffers.
					reader = readerInfo.reader;
					readerType = readerInfo.type;
					readerURL = readerInfo.url;
					buffer = readerInfo.buffer;
					bufferPos = readerInfo.bufferPos;
					bufferLen = readerInfo.bufferLen;
					line = readerInfo.line;
					column = readerInfo.column;
					break;

				case READER_STRING:
					// We need to create a Reader over a string.
					createStringReader(readerInfo.str);
					break;

				case READER_URL:
					// We need to create a Reader over a URL.
					createURLReader(readerInfo.url);
					break;
			}

			ignoreQuote = readerInfo.ignoreQuote;
			ignoreMarkup = readerInfo.ignoreMarkup;
		}

		void resetNameBuffer()
		{
			namePos = -1;
			nameStr = null;
		}

		void appendNameBuffer(char c)
		{
			namePos++;
			if (namePos >= LITBUFSIZE)
			{
				if (nameStr == null)
				{
					nameStr = new StringBuilder();
				}
				nameStr.Append(nameBuffer);
				namePos = 0;
			}
			nameBuffer[namePos] = c;
		}

		string getNameBuffer()
		{
			if (nameStr == null)
			{
				return new string(nameBuffer, 0, namePos + 1);
			}
			else
			{
				nameStr.Append(nameBuffer, 0, namePos + 1);
				return nameStr.ToString();
			}
		}

		// ********************************************************************
		// Methods -- I/O
		// ********************************************************************

		void initReaderGlobals()
		{
			reader = null;
			readerURL = null;
			line = 1;
			column = 1;
			ignoreQuote = false;
			ignoreMarkup = false;
		}

		char getChar()
		{
			// This function just gets the next character in the buffer. Entity
			// processing is done at a higher level (nextChar). Note that nextChar()
			// may change the value of reader.

			//      char c;

			if (bufferPos >= bufferLen)
			{
				bufferLen = reader.Read(buffer, 0, buffer.Length);
				if (bufferLen <= 0)
				{
					// If we've hit the end of the Reader, pop the Reader off the
					// stack and get the first character in the next Reader.

					popReader();
					return getChar();
				}
				else
				{
					bufferPos = 0;
				}
			}

			// Uncomment the following line for debugging. Note that what is
			// printed is everything that goes past this function. Some characters
			// go past multiple times because of being tested and restored, as
			// with isWhitespace() or isString(string).

			// System.out.print(buffer[bufferPos]);

			// Return the character.

			return buffer[bufferPos++];
		}

		void restore()
		{
			// To restore a single character, all we need to do is decrement the
			// buffer position. It isn't really important what the character is. This
			// is obviously true in the middle of the buffer. At the bottom end, it
			// means that a new buffer full of data has just been read and that we
			// have just returned the first character; thus, the bufferPos can never
			// be 0 in this case. At the top end, it means we have just returned the
			// last character; thus, we can safely restore it.
			//
			// WARNING! This function assumes that it is never called more than once
			// in succession. If it is, it can run off the lower end of the buffer.

			bufferPos--;
		}

		void restore(string s)
		{
			pushCurrentReader();
			createStringReader(s);
		}

		void openInputSource(InputSource src)
		{
			//      TextReader      srcReader;
			//      Stream srcStream;

			if ( src.Uri != null )
			{
				createURLReader(src.Uri);
				return;
			}
			createTextReader(src.Text);
		}

		private void createTextReader(string text)
		{
			reader=new StringReader(text);
			readerType = READER_READER;
			buffer=new char[BUFSIZE];
			bufferPos=buffer.Length-1;
			bufferLen=0;
		}
		// ********************************************************************
		// Inner class -- Reader information
		// ********************************************************************

		class ReaderInfo
		{
			public TextReader  reader;
			public char[]  buffer;
			public Uri     url;
			public string  str;
			public int     type,
				bufferPos,
				bufferLen,
				line,
				column;
			public bool ignoreQuote,
				ignoreMarkup;

			public ReaderInfo(TextReader reader,
				char[] buffer,
				Uri url,
				string str,
				int type,
				int bufferPos,
				int bufferLen,
				int line,
				int column,
				bool ignoreQuote,
				bool ignoreMarkup)
			{
				this.reader = reader;
				this.buffer = buffer;
				this.url = url;
				this.str = str;
				this.type = type;
				this.bufferPos = bufferPos;
				this.bufferLen = bufferLen;
				this.line = line;
				this.column = column;
				this.ignoreQuote = ignoreQuote;
				this.ignoreMarkup = ignoreMarkup;
			}
		}

		public XmlResolver XmlResolver
		{
			set { xmlResolver=value; }
		}
	}
}
