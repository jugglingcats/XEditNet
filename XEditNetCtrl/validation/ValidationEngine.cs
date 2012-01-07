using System;
using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;
using XEditNet.Dtd;
using Attribute = XEditNet.Dtd.Attribute;

namespace XEditNet.Validation
{
	internal interface IValidationClient
	{
		void RemoveValidationErrors(XmlNode n);
		void StartValidation(XmlElement e);
		void ProcessError(XmlNode n, ValidationErrorType type);
		void StartChildValidation(XmlNode n);
	}

	/// <summary>
	/// Summary description for ValidationEngine.
	/// </summary>
	internal class ValidationEngine
	{
		private DocumentType documentType;
		private IValidationClient client;

		#region Regular Expressions
		private static readonly string extender=
			"\u00B7|\u02D0|\u02D1|\u0387|\u0640|\u0E46|\u0EC6|\u3005|[\u3031-\u3035]|[\u309D-\u309E]|[\u30FC-\u30FE]";

		private static readonly string digit=
			"[\u0030-\u0039]|[\u0660-\u0669]|[\u06F0-\u06F9]|[\u0966-\u096F]|[\u09E6-\u09EF]|[\u0A66-\u0A6F]|[\u0AE6-\u0AEF]|[\u0B66-\u0B6F]|[\u0BE7-\u0BEF]|[\u0C66-\u0C6F]|[\u0CE6-\u0CEF]|[\u0D66-\u0D6F]|[\u0E50-\u0E59]|"+
			"[\u0ED0-\u0ED9]|[\u0F20-\u0F29]";

		private static readonly string combiningChar=
			"[\u0300-\u0345]|[\u0360-\u0361]|[\u0483-\u0486]|[\u0591-\u05A1]|[\u05A3-\u05B9]|[\u05BB-\u05BD]|\u05BF|[\u05C1-\u05C2]|\u05C4|[\u064B-\u0652]|\u0670|[\u06D6-\u06DC]|[\u06DD-\u06DF]|[\u06E0-\u06E4]|[\u06E7-\u06E8]|"+
			"[\u06EA-\u06ED]|[\u0901-\u0903]|\u093C|[\u093E-\u094C]|\u094D|[\u0951-\u0954]|[\u0962-\u0963]|[\u0981-\u0983]|\u09BC|\u09BE|\u09BF|[\u09C0-\u09C4]|[\u09C7-\u09C8]|[\u09CB-\u09CD]|\u09D7|[\u09E2-\u09E3]|\u0A02|\u0A3C|"+
			"\u0A3E|\u0A3F|[\u0A40-\u0A42]|[\u0A47-\u0A48]|[\u0A4B-\u0A4D]|[\u0A70-\u0A71]|[\u0A81-\u0A83]|\u0ABC|[\u0ABE-\u0AC5]|[\u0AC7-\u0AC9]|[\u0ACB-\u0ACD]|[\u0B01-\u0B03]|\u0B3C|[\u0B3E-\u0B43]|[\u0B47-\u0B48]|[\u0B4B-\u0B4D]|"+
			"[\u0B56-\u0B57]|[\u0B82-\u0B83]|[\u0BBE-\u0BC2]|[\u0BC6-\u0BC8]|[\u0BCA-\u0BCD]|\u0BD7|[\u0C01-\u0C03]|[\u0C3E-\u0C44]|[\u0C46-\u0C48]|[\u0C4A-\u0C4D]|[\u0C55-\u0C56]|[\u0C82-\u0C83]|[\u0CBE-\u0CC4]|[\u0CC6-\u0CC8]|"+
			"[\u0CCA-\u0CCD]|[\u0CD5-\u0CD6]|[\u0D02-\u0D03]|[\u0D3E-\u0D43]|[\u0D46-\u0D48]|[\u0D4A-\u0D4D]|\u0D57|\u0E31|[\u0E34-\u0E3A]|[\u0E47-\u0E4E]|\u0EB1|[\u0EB4-\u0EB9]|[\u0EBB-\u0EBC]|[\u0EC8-\u0ECD]|[\u0F18-\u0F19]|\u0F35|"+
			"\u0F37|\u0F39|\u0F3E|\u0F3F|[\u0F71-\u0F84]|[\u0F86-\u0F8B]|[\u0F90-\u0F95]|\u0F97|[\u0F99-\u0FAD]|[\u0FB1-\u0FB7]|\u0FB9|[\u20D0-\u20DC]|\u20E1|[\u302A-\u302F]|\u3099|\u309A";

		private static readonly string ideographic=
			"[\u4E00-\u9FA5] | \u3007 | [\u3021-\u3029]";

		private static readonly string baseChar=
			"[\u0041-\u005A]|[\u0061-\u007A]|[\u00C0-\u00D6]|[\u00D8-\u00F6]|[\u00F8-\u00FF]|[\u0100-\u0131]|[\u0134-\u013E]|[\u0141-\u0148]|[\u014A-\u017E]|[\u0180-\u01C3]|[\u01CD-\u01F0]|[\u01F4-\u01F5]|[\u01FA-\u0217]|"+
			"[\u0250-\u02A8]|[\u02BB-\u02C1]|\u0386|[\u0388-\u038A]|\u038C|[\u038E-\u03A1]|[\u03A3-\u03CE]|[\u03D0-\u03D6]|\u03DA|\u03DC|\u03DE|\u03E0|[\u03E2-\u03F3]|[\u0401-\u040C]|[\u040E-\u044F]|[\u0451-\u045C]|"+
			"[\u045E-\u0481]|[\u0490-\u04C4]|[\u04C7-\u04C8]|[\u04CB-\u04CC]|[\u04D0-\u04EB]|[\u04EE-\u04F5]|[\u04F8-\u04F9]|[\u0531-\u0556]|\u0559|[\u0561-\u0586]|[\u05D0-\u05EA]|[\u05F0-\u05F2]|[\u0621-\u063A]|[\u0641-\u064A]|"+
			"[\u0671-\u06B7]|[\u06BA-\u06BE]|[\u06C0-\u06CE]|[\u06D0-\u06D3]|\u06D5|[\u06E5-\u06E6]|[\u0905-\u0939]|\u093D|[\u0958-\u0961]|[\u0985-\u098C]|[\u098F-\u0990]|[\u0993-\u09A8]|[\u09AA-\u09B0]|\u09B2|[\u09B6-\u09B9]|"+
			"[\u09DC-\u09DD]|[\u09DF-\u09E1]|[\u09F0-\u09F1]|[\u0A05-\u0A0A]|[\u0A0F-\u0A10]|[\u0A13-\u0A28]|[\u0A2A-\u0A30]|[\u0A32-\u0A33]|[\u0A35-\u0A36]|[\u0A38-\u0A39]|[\u0A59-\u0A5C]|\u0A5E|[\u0A72-\u0A74]|[\u0A85-\u0A8B]|"+
			"\u0A8D|[\u0A8F-\u0A91]|[\u0A93-\u0AA8]|[\u0AAA-\u0AB0]|[\u0AB2-\u0AB3]|[\u0AB5-\u0AB9]|\u0ABD|\u0AE0|[\u0B05-\u0B0C]|[\u0B0F-\u0B10]|[\u0B13-\u0B28]|[\u0B2A-\u0B30]|[\u0B32-\u0B33]|[\u0B36-\u0B39]|\u0B3D|[\u0B5C-\u0B5D]|"+
			"[\u0B5F-\u0B61]|[\u0B85-\u0B8A]|[\u0B8E-\u0B90]|[\u0B92-\u0B95]|[\u0B99-\u0B9A]|\u0B9C|[\u0B9E-\u0B9F]|[\u0BA3-\u0BA4]|[\u0BA8-\u0BAA]|[\u0BAE-\u0BB5]|[\u0BB7-\u0BB9]|[\u0C05-\u0C0C]|[\u0C0E-\u0C10]|[\u0C12-\u0C28]|"+
			"[\u0C2A-\u0C33]|[\u0C35-\u0C39]|[\u0C60-\u0C61]|[\u0C85-\u0C8C]|[\u0C8E-\u0C90]|[\u0C92-\u0CA8]|[\u0CAA-\u0CB3]|[\u0CB5-\u0CB9]|\u0CDE|[\u0CE0-\u0CE1]|[\u0D05-\u0D0C]|[\u0D0E-\u0D10]|[\u0D12-\u0D28]|[\u0D2A-\u0D39]|"+
			"[\u0D60-\u0D61]|[\u0E01-\u0E2E]|\u0E30|[\u0E32-\u0E33]|[\u0E40-\u0E45]|[\u0E81-\u0E82]|\u0E84|[\u0E87-\u0E88]|\u0E8A|\u0E8D|[\u0E94-\u0E97]|[\u0E99-\u0E9F]|[\u0EA1-\u0EA3]|\u0EA5|\u0EA7|[\u0EAA-\u0EAB]|[\u0EAD-\u0EAE]|"+
			"\u0EB0|[\u0EB2-\u0EB3]|\u0EBD|[\u0EC0-\u0EC4]|[\u0F40-\u0F47]|[\u0F49-\u0F69]|[\u10A0-\u10C5]|[\u10D0-\u10F6]|\u1100|[\u1102-\u1103]|[\u1105-\u1107]|\u1109|[\u110B-\u110C]|[\u110E-\u1112]|\u113C|\u113E|\u1140|\u114C|"+
			"\u114E|\u1150|[\u1154-\u1155]|\u1159|[\u115F-\u1161]|\u1163|\u1165|\u1167|\u1169|[\u116D-\u116E]|[\u1172-\u1173]|\u1175|\u119E|\u11A8|\u11AB|[\u11AE-\u11AF]|[\u11B7-\u11B8]|\u11BA|[\u11BC-\u11C2]|\u11EB|\u11F0|\u11F9|"+
			"[\u1E00-\u1E9B]|[\u1EA0-\u1EF9]|[\u1F00-\u1F15]|[\u1F18-\u1F1D]|[\u1F20-\u1F45]|[\u1F48-\u1F4D]|[\u1F50-\u1F57]|\u1F59|\u1F5B|\u1F5D|[\u1F5F-\u1F7D]|[\u1F80-\u1FB4]|[\u1FB6-\u1FBC]|\u1FBE|[\u1FC2-\u1FC4]|[\u1FC6-\u1FCC]|"+
			"[\u1FD0-\u1FD3]|[\u1FD6-\u1FDB]|[\u1FE0-\u1FEC]|[\u1FF2-\u1FF4]|[\u1FF6-\u1FFC]|\u2126|[\u212A-\u212B]|\u212E|[\u2180-\u2182]|[\u3041-\u3094]|[\u30A1-\u30FA]|[\u3105-\u312C]|[\uAC00-\uD7A3]";

		private static readonly string space="(#x20|#x9|#xD|#xA)+";
		private static readonly string letter=baseChar+"|"+ideographic;
		private static readonly string nameChar=letter+"|"+digit+"|"+@"\."+"|-|_|:|"+combiningChar+"|"+extender;
		private static readonly string name="("+letter+"|_|:)("+nameChar+")*";
		private static readonly string names=name+"("+space+name+")*";
		private static readonly string nmtoken="("+nameChar+")+";
		private static readonly string nmtokens=nmtoken+"("+space+nmtoken+")*";

		public static readonly Regex RegexName=new Regex("^"+name+"$");
		public static readonly Regex RegexNames=new Regex("^"+names+"$");
		public static readonly Regex RegexNmtoken=new Regex("^"+nmtoken+"$");
		public static readonly Regex RegexNmtokens=new Regex("^"+nmtokens+"$");

		#endregion

		public ValidationEngine(DocumentType dtd, IValidationClient client)
		{
			this.documentType=dtd;
			this.client=client;
		}

		public void Validate(XmlElement e)
		{
			if ( documentType == null )
				return;

			client.StartValidation(e);

			ElementType et=documentType.GetElementType(e);
			if ( et == null )
			{
				client.ProcessError(e, ValidationErrorType.ElementNotDefined);
				return;
			}

//			Console.WriteLine("Validating children of {0}", e.Name);
			Validate(e, et, ToArrayList(e.ChildNodes));
		}

		private ArrayList ToArrayList(XmlNodeList nodes)
		{
			ArrayList ret=new ArrayList(nodes.Count);
			foreach ( XmlNode n in nodes )
				// TODO: E: entities
				ret.Add(n);

			return ret;
		}

		public void Validate(XmlElement e, ElementType et, ICollection list)
		{
			switch ( et.ContentType )
			{
				case ElementContentType.Any:
					ValidateAnyModel();
					break;

				case ElementContentType.Element:
					ValidateElementModel(e, et, list);
					break;

				case ElementContentType.Empty:
					ValidateEmptyModel(list);
					break;
			
				case ElementContentType.Mixed:
					ValidateMixedModel(et, list);
					break;

				case ElementContentType.PCDATA:
					ValidateTextModel(list);
					break;
			}
		}

		private void ValidateAnyModel()
		{
			// nothing to do
			// validation of all inserted nodes will
			// check that child nodes are defined in DTD
		}

		private void ValidateElementModel(XmlElement e, ElementType et, ICollection children)
		{
			ICollection col=et.ContentModel.GetValidFirstElements();

			bool elementMissing=false;
			foreach ( XmlNode n in children )
			{
				client.StartChildValidation(n);

				if ( n.NodeType == XmlNodeType.Text )
				{
					client.ProcessError(n, ValidationErrorType.NodeNotAllowed);
				} 
				else if ( n.NodeType == XmlNodeType.Element )
				{
					ElementType et2=documentType.GetElementType((XmlElement) n);
					if ( et2 == null )
						// not in DTD, will be picked up later
						continue;

					// TODO: E: entities!
					if ( !et.HasChildElement(new XmlName(n)) )
					{
						// this element can never be allowed here
						client.ProcessError(n, ValidationErrorType.ElementNotInContentModel);
						continue;						
					}

					if ( elementMissing )
						continue;

					ElementTypeRef sr=FindInCollection(col, (XmlElement) n);
					if ( sr == null )
					{
						foreach ( ElementTypeRef etr in col )
						{
							if ( etr.IsRequired )
							{
								elementMissing=true;						
								client.ProcessError(n, ValidationErrorType.ElementNotAllowedHere);
								break;
							}
						}
						if ( elementMissing )
							continue;

						client.ProcessError(n, ValidationErrorType.NodeNotAllowed);
					}
					else
						col=sr.OriginalReference.GetValidNextElements();
				}
			}
			if ( !elementMissing )
			{
				foreach ( ElementTypeRef sr in col )
				{
					if ( sr.IsRequired )
					{
						// TODO: M: can provide some more info to the error here
						client.ProcessError(e, ValidationErrorType.RequiredElementMissing);
						break;
					}
				}
			}
		}

		private void ValidateEmptyModel(ICollection list)
		{
			foreach ( XmlNode n in list )
			{
				// TODO: M: there are other node types, eg. entity that can cause probs
				if ( n.NodeType == XmlNodeType.Element ||
					n.NodeType == XmlNodeType.Text ||
					n.NodeType == XmlNodeType.Whitespace ||
					n.NodeType == XmlNodeType.SignificantWhitespace)
				{
					// TODO: ?: need to StartChildValidation
					client.ProcessError(n, ValidationErrorType.NodeNotAllowed);
				}
			}
		}

		private void ValidateMixedModel(ElementType et, ICollection list)
		{
			foreach ( XmlNode n in list )
			{
				client.StartChildValidation(n);

				if ( n.NodeType != XmlNodeType.Element )
					// TODO: H: entities!
					continue;
//				{
//					RemoveValidationErrors(n);
//					continue;
//				}

				if ( !et.HasChildElement(new XmlName(n.Name)) )
					client.ProcessError(n, ValidationErrorType.ElementNotInContentModel);
			}
		}

		private void ValidateTextModel(ICollection list)
		{
			foreach ( XmlNode n in list )
			{
				// TODO: H: entity references can cause issues
				if ( n.NodeType == XmlNodeType.Element )
					client.ProcessError(n, ValidationErrorType.NodeNotAllowed);
			}
		}


		internal bool ValidateAttribute(Attribute a, XmlElement e, XmlAttribute val)
		{
			if ( !val.Specified )
				// if not specified then either required | (defaulted | implied)
				return a.State != AttributeState.Required;

			switch ( a.Type )
			{
				case AttributeType.CDATA:
					// any value is ok
					return true;

				case AttributeType.Enumerated:
					return ValidateEnumeratedAttribute(a, val);

				case AttributeType.ID:
					return ValidateIdAttribute(val);

				case AttributeType.IDREF:
					return ValidateIdRefAttribute(val, false);

				case AttributeType.IDREFS:
					return ValidateIdRefAttribute(val, true);

				case AttributeType.NMTOKEN:
					return ValidateNmtokenAttribute(val, false);

				case AttributeType.NMTOKENS:
					return ValidateNmtokenAttribute(val, true);

				default:
					// TODO: M: support other types (entity and notation)
					return true;
			}
		}

		private bool ValidateEnumeratedAttribute(Attribute a, XmlAttribute val)
		{
			foreach ( string s in a.Enums )
			{
				if ( s.Equals(val.Value) )
					return true;
			}
			return false;
		}

		private bool ValidateIdAttribute(XmlAttribute val)
		{
			if ( !RegexName.IsMatch(val.Value) )
				return false;

			return true;
		}

		private bool ValidateIdRefAttribute(XmlAttribute val, bool allowMultiple)
		{
			if ( !allowMultiple )
				return RegexName.IsMatch(val.Value);
			else
				return RegexNames.IsMatch(val.Value);
		}

		private bool ValidateNmtokenAttribute(XmlAttribute val, bool allowMultiple)
		{
			if ( !allowMultiple )
				return RegexNmtoken.IsMatch(val.Value);
			else
				return RegexNmtokens.IsMatch(val.Value);
		}

		private ElementTypeRef FindInCollection(ICollection col, XmlElement e)
		{
			foreach ( ElementTypeRef sr in col )
			{
				XmlName xn=new XmlName(e);
				if ( sr.Name.Equals(xn) )
					return sr;
			}
			return null;
		}

		private ElementTypeRef FindInCollection(ICollection col, XmlName name)
		{
			foreach ( ElementTypeRef sr in col )
			{
				if ( sr.Name.Equals(name) )
					return sr;
			}
			return null;
		}

		internal bool IsValid(ElementType et, ICollection list)
		{
			switch ( et.ContentType )
			{
				case ElementContentType.Any:
					return true;

				case ElementContentType.Element:
					return IsValidElementContent(et, list);

				case ElementContentType.Empty:
					// TODO: H: too simplistic
					return list.Count == 0;

				case ElementContentType.Mixed:
					return IsValidMixedContent(et, list);

				case ElementContentType.PCDATA:
					return IsValidTextContent(et, list);

				default:
					// can never happen due to enum
					throw new InvalidOperationException("Unrecognised ElementContentType enum");
			}
		}

		private bool IsValidTextContent(ElementType et, ICollection list)
		{
			foreach ( XmlNode n in list )
			{
				XmlElement e=n as XmlElement;
				// TODO: E: entities
				if ( e != null )
					return false;
			}
			return true;
		}

		private bool IsValidMixedContent(ElementType et, ICollection list)
		{
			// list is assumed to contain all children (including indirect children via entity reference)
			foreach ( XmlNode n in list )
			{
				XmlElement e=n as XmlElement;
				if ( e == null )
					continue;

				bool valid=false;
				foreach ( ElementType ct in et.ChildElements )
				{
					if ( ct.Name.QualifiedName.Equals(n.Name) )
					{
						valid=true;
						break;
					}
				}
				if ( !valid )
					return false;
			}
			return true;
		}

		private bool IsValidElementContent(ElementType et, ICollection list)
		{
			// list is assumed to contain all children (including indirect children via entity reference)
			ICollection col=et.ContentModel.GetValidFirstElements();
				
			foreach ( XmlNode n in list )
			{
				if ( XmlUtil.IsTextContent(n) )
					return false;

				XmlElement e=n as XmlElement;
				if ( e == null )
					// not significant, eg. comment
					continue;

				ElementTypeRef sr=FindInCollection(col, new XmlName(e));
				if ( sr == null )
					return false;

				col=sr.OriginalReference.GetValidNextElements();
			}
			// TODO: think about required elements (remaining col)
			return true;
		}

		internal ElementTypeRef IsValidSequence(ElementType et, ICollection s, int index)
		{
			ICollection col=et.ContentModel.GetValidFirstElements();
				
			int count=0;
			ElementTypeRef ret=null;
			foreach ( XmlName name in s )
			{
				count++;

				if ( !et.HasChildElement(name) )
					// ignore elements that simply aren't in the model
					// TODO: H: count can get out
					continue;

				ElementTypeRef sr=FindInCollection(col, name);
				if ( count == index+1 )
				{
					ret=sr;
				}

				if ( sr == null )
				{
					// element name is not allowed here
					if ( ret != null && count < index+1 )
						return ret;

					return null;
				}

				// TODO: M: optimise - this is called at end of s even though not going to loop again
				col=sr.OriginalReference.GetValidNextElements();
			}
			// TODO: M: think about required elements at end - but probably
			//			not needed here because don't want to stop someone
			//			adding an element even if more are allowed
			return ret;
		}

		public ElementListItem[] GetValidElements(XmlElement parent, XmlNode n, bool replace)
		{
			ArrayList ret=new ArrayList();

			if ( documentType == null || parent == null )
				return new ElementListItem[] {};

			ElementType et=documentType.GetElementType(parent);
			if ( et == null )
				return new ElementListItem[] {};

			switch ( et.ContentType )
			{
				case ElementContentType.Any:
					return ValidationUtil.ToElementList(documentType.ElementTypes);

				case ElementContentType.Element:
					return FindValidElements(et, parent, n, replace);

				case ElementContentType.Empty:
					// cannot have children
					break;
			
				case ElementContentType.Mixed:
					return ValidationUtil.ToElementList(et.ChildElements);

				case ElementContentType.PCDATA:
					// cannot have element children
					break;
			}

			return new ElementListItem[] {};
		}

		private ElementListItem[] FindValidElements(ElementType et, XmlElement parent, XmlNode n, bool replace)
		{
			int index=-1;

			ArrayList children=new ArrayList();
			foreach ( XmlNode c in parent.ChildNodes )
			{
				if ( c.Equals(n) )
					index=children.Count;

				if ( c.NodeType != XmlNodeType.Element )
					continue;

				XmlName xn=new XmlName(c);
				children.Add(xn);
			}
			if ( index < 0 )
			{
				Debug.Assert(!replace, "This can't happen if in replace mode");
				children.Add(null);
				index=children.Count-1;
			}
			else
			{
				if ( !replace )
					children.Insert(index, null);
			}

			ArrayList ret=new ArrayList();
			foreach ( ElementType t in et.ChildElements )
			{
				children[index]=t.Name;

				ElementTypeRef sr=IsValidSequence(et, children, index);
				if ( sr != null )
					ret.Add(new ElementListItem(sr.Name, sr.IsRequired, sr.IsChoice));
			}
			return ret.ToArray(typeof(ElementListItem)) as ElementListItem[];
		}

		public ElementListItem[] GetMissingElements(XmlElement e)
		{
			ElementType et=documentType.GetElementType(e);

			ICollection col=et.ContentModel.GetValidFirstElements();
				
			foreach ( XmlNode n in e.ChildNodes )
			{
				if ( XmlUtil.IsTextContent(n) )
					// not interested for this method
					continue;

				XmlElement e2=n as XmlElement;
				if ( e2 == null )
					// not significant, eg. comment
					// TODO: E: entities!
					continue;

				ElementTypeRef sr=FindInCollection(col, new XmlName(e2));
				if ( sr == null )
					// just keep on going until we get to the end
					continue;

				col=sr.OriginalReference.GetValidNextElements();
			}
			ArrayList ret=new ArrayList();
			foreach ( ElementTypeRef etr in col )
			{
				if ( etr.IsRequired )
					ret.Add(new ElementListItem(etr.Name, etr.IsRequired, etr.IsChoice));
			}
			return ret.ToArray(typeof(ElementListItem)) as ElementListItem[];
		}
	}
}
