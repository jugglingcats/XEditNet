using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;
using XEditNet.Dtd;
using Attribute = XEditNet.Dtd.Attribute;
// TODO: M: need to handle better when validation error is removed then added straight back in

namespace XEditNet.Validation
{
	internal interface IValidationProvider
	{
		bool IsAttributeRequired(XmlElement element, string name);
		bool IsAttributeFixed(XmlElement element, string name);
		bool IsAttributeDefined(XmlElement element, string name);
		bool IsAttributeValid(XmlElement element, string name, XmlAttribute attribute);
		string[] GetDefinedAttributeNames(XmlElement element);
		string[] GetEnumValues(XmlElement element, string name);
		AttributeType GetAttributeType(XmlElement element, string name);
	}

	internal class ValidationManager : IValidationProvider, IValidationClient
	{
		private XmlDocument document;
		private DocumentType documentType;
		private InvalidNodeInfo nodeInfo=new InvalidNodeInfo();
		private IdTracker idTracker=new IdTracker();
		private ValidationEngine validationEngine;
		private QuickFixer quickFixer;

		public bool HasElements
		{
			get 
			{
				return documentType != null && documentType.ElementTypes.Length > 0; 
			}
		}

		public ValidationError[] Errors
		{
			get
			{
				return nodeInfo.AllErrors;
			}
		}

		public ElementListItem[] GetAllElements()
		{
			if ( documentType == null )
				return new ElementListItem[] {};
			
			return ValidationUtil.ToElementList(documentType.ElementTypes);
		}

		public void Detach()
		{
			if ( document == null )
				return;

			document.NodeChanging-=new XmlNodeChangedEventHandler(NodeChanging);
			document.NodeChanged-=new XmlNodeChangedEventHandler(NodeChanged);
			document.NodeInserted-=new XmlNodeChangedEventHandler(NodeInserted);
			document.NodeRemoved-=new XmlNodeChangedEventHandler(NodeRemoved);

			documentType=null;
			quickFixer=null;
			nodeInfo=new InvalidNodeInfo();
			idTracker=new IdTracker();
			validationEngine=null;
		}

		private XmlAttribute GetAttributeFromEvent(XmlNodeChangedEventArgs e)
		{
			if ( e.Node.NodeType == XmlNodeType.Attribute )
				// node attribute direct
				return (XmlAttribute) e.Node;
			else if ( e.NewParent != null && e.NewParent.NodeType == XmlNodeType.Attribute )
				// text under node attribute
				return (XmlAttribute) e.NewParent;

			// not attribute
			return null;
		}
		protected void NodeChanging(object sender, XmlNodeChangedEventArgs e)
		{
			// we're only interested in attribute nodes
			XmlAttribute attr=GetAttributeFromEvent(e);
			if ( attr == null || !XmlUtil.HasAncestor(attr.OwnerElement, attr.OwnerDocument.DocumentElement) )
				// attribute's parent is not part of the document
				return;

			RemoveIdOrIdRef(attr.OwnerElement, attr);
		}

		protected void NodeChanged(object sender, XmlNodeChangedEventArgs e)
		{
			// Fired when XmlNode.Value is changed
			// The only types that have non-null Value are:
			//	- Attribute (special case)
			//	- CDATASection (not significant for validation)
			//	- Comment (not significant for validation)
			//	- ProcessingInstruction (not significant for validation)
			//	- Text (not significant for validation when changed)
			//	- [Significant]Whitespace (not significant for validation)
			//	- XmlDeclaration (not significant for validation)

			Console.WriteLine("Node {0} changed", e.Node.Name);

			XmlAttribute attr=GetAttributeFromEvent(e);

			if ( attr == null || !XmlUtil.HasAncestor(attr.OwnerElement, attr.OwnerDocument.DocumentElement) )
				// attribute's parent is not part of the document
				return;

			ValidateAttributeAddOrChange(attr);
		}

		protected void NodeInserted(object sender, XmlNodeChangedEventArgs e)
		{
			XmlAttribute attr=GetAttributeFromEvent(e);
			if ( attr != null )
			{
				// we deal with attributes differently
				if ( attr.OwnerElement != null && XmlUtil.HasAncestor(attr.OwnerElement, attr.OwnerDocument.DocumentElement) )
					// owner element is part of doc, so validate
					ValidateAttributeAddOrChange(attr);

				return;
			}

			Console.WriteLine("Node {0} inserted", e.Node.Name);

			if ( !XmlUtil.HasAncestor(e.NewParent, e.NewParent.OwnerDocument.DocumentElement) )
			{
				Console.WriteLine("Insert of {0} under {1}, is not part of doc", e.Node.Name, e.NewParent.Name);
				// new parent is not part of doc
				return;
			}

			XmlElement p=e.NewParent as XmlElement;
			if ( p == null )
				// must be entity reference
				p=(XmlElement) XmlUtil.GetParentNode(e.NewParent);

			RemoveValidationErrors(new ContextErrorFilter(e.Node));
			Console.WriteLine("Validating parent node {0}", e.NewParent.Name);
			Validate(p);

			XmlElement elem=e.Node as XmlElement;
			if ( elem != null )
				RecursiveInsert(elem);

			if ( e.OldParent != null )
				Validate((XmlElement) e.OldParent);
		}

		private void Validate(XmlElement p)
		{
			validationEngine.Validate(p);
		}

		private void RecursiveInsert(XmlElement e)
		{
			Validate(e);
			ValidateAttributes(e);

			foreach ( XmlNode n in e.ChildNodes )
			{
				// TODO: L: any other node types relevant?
				XmlElement child=n as XmlElement;

				if ( child == null )
					continue;

				RecursiveInsert(child);
			}
		}

		protected void NodeRemoved(object sender, XmlNodeChangedEventArgs e)
		{
			XmlAttribute attr=GetAttributeFromEvent(e);
			if ( attr != null )
			{
				if ( e.OldParent != null && XmlUtil.HasAncestor(e.OldParent, attr.OwnerDocument.DocumentElement) )
				{
					ValidateRemovedAttribute((XmlElement) e.OldParent, attr);
					RemoveIdOrIdRef((XmlElement) e.OldParent, attr);
				}
				return;
			}

			switch ( e.Node.NodeType )
			{
				case XmlNodeType.Element:
				case XmlNodeType.EntityReference:
					// remove this node and any child nodes
					RecursiveRemove(e.Node);
					XmlElement p=e.OldParent as XmlElement;
					if ( p == null )
						// go up dealing with entity references
						p=XmlUtil.GetParentNode(e.Node) as XmlElement;

					if ( p != null )
						Validate(p);

					break;

				default:
					// any other cases?
					break;
			}
		}

		private void RecursiveRemove(XmlNode n)
		{
			// node is expected to be XmlElement or XmlEntityReference
			XmlElement e=n as XmlElement;
			if ( e != null )
			{
				RemoveValidationErrors(e);

				foreach ( XmlAttribute a in e.Attributes )
					// TODO: L: innefficient - does lookup for ElementType each time 
					RemoveIdOrIdRef(e, a);
			}

			foreach ( XmlNode c in n.ChildNodes )
			{
				if ( c.NodeType == XmlNodeType.Element || c.NodeType == XmlNodeType.EntityReference )
					RecursiveRemove(c);
			}
		}

		private void RemoveIdOrIdRef(XmlElement e, XmlAttribute a)
		{
			ElementType et=documentType.GetElementType(e);
			XmlName xn=new XmlName(a);
			Attribute attr=et.GetAttribute(xn);
			if ( attr == null )
				return;

			AttributeBinding ab;
			string val=a.Value;
			switch ( attr.Type )
			{
				case AttributeType.ID:
					// record the previous id because we'll need that later (NodeChanged event)
					ab=new AttributeBinding(e, attr.Name, val, a.Specified);
					idTracker.RemoveId(ab);
					ProcessRemovedId(val);
					break;

				case AttributeType.IDREF:
				case AttributeType.IDREFS:
					ab=new AttributeBinding(e, attr.Name, val, a.Specified);
					idTracker.RemoveIdRefs(ab);
					break;
			}
		}

		private void ValidateRemovedAttribute(XmlElement e, XmlAttribute a)
		{
			ElementType et=documentType.GetElementType(e);
			if ( et == null )
				return;

			Attribute attr=et.GetAttribute(a);
			if ( attr == null )
			{
				// attribute was invalid, so remove that error
				AttributeErrorFilter aef=new AttributeErrorFilter(e, a, ValidationErrorType.AttributeNotDefined);
				RemoveValidationErrors(aef);
				return;
			} 
			else
			{
				// remove any validation errors for this attribute
				AttributeErrorFilter aef=new AttributeErrorFilter(e, a);
				RemoveValidationErrors(aef);
			}

			if ( attr.State == AttributeState.Required )
				// removing a required attribute
				AddValidationError(e, new ValidationErrorAttribute(e, a.Name, ValidationErrorType.RequiredAttributeMissing));
		}

		private void AddIdOrIdRef(XmlElement e, XmlAttribute a, Attribute attr)
		{
			// any previous value will have been removed in NodeChanging event

			AttributeBinding ab;
			string val=a.Value;
			switch ( attr.Type )
			{
				case AttributeType.ID:
					ab=new AttributeBinding(e, attr.Name, val, a.Specified);
					idTracker.AddId(ab);
					if ( idTracker.IdCount(val) > 1 )
					{
						foreach ( AttributeBinding ab2 in idTracker.GetIdBindings(val) )
						{
							// TODO: L: inefficient, lots of removing then adding
							XmlAttribute a2=ab2.Element.GetAttributeNode(ab2.Name);
							AttributeErrorFilter aef=new AttributeErrorFilter(ab2.Element, a2, ValidationErrorType.IdAttributeInUse);
							RemoveValidationErrors(aef);
							ValidationErrorAttribute vea=new ValidationErrorAttribute(ab2.Element, a2.Name, ValidationErrorType.IdAttributeInUse);
							AddValidationError(ab2.Element, vea);
						}
					}

					foreach ( AttributeBinding ab2 in idTracker.GetIdRefBindings(val) )
					{
						XmlAttribute ar=ab.Element.GetAttributeNode(ab2.Name);
						AttributeErrorFilter aef=new AttributeErrorFilter(ab2.Element, ar, ValidationErrorType.IdAttributeNotDefined);
						RemoveValidationErrors(aef);
					}

					break;

				case AttributeType.IDREF:
				case AttributeType.IDREFS:
					ab=new AttributeBinding(e, attr.Name, val, a.Specified);
					idTracker.AddIdRefs(ab);
					// TODO: L: inefficient - we split twice, here and in AddIdRefs
					foreach ( string id in ab.Value.Split(' ') )
					if ( idTracker.IdCount(id) == 0 )
					{
						ValidationErrorAttribute vea=new ValidationErrorAttribute(e, a.Name, ValidationErrorType.IdAttributeNotDefined);
						AddValidationError(e, vea);
					}
					break;
			}
		}

		private void ProcessRemovedId(string id)
		{
			AttributeBinding[] bindings;

			if ( idTracker.IdCount(id) == 1 )
			{
				// remove any errors from existing nodes
				bindings=idTracker.GetIdBindings(id);
				foreach ( AttributeBinding ab in bindings )
				{
					XmlAttribute a=ab.Element.GetAttributeNode(ab.Name);
					AttributeErrorFilter aef=new AttributeErrorFilter(ab.Element, a, ValidationErrorType.IdAttributeInUse);
					RemoveValidationErrors(aef);
				}
				// nothing to do for idrefs, id node exists
				return;
			}

			bindings=idTracker.GetIdRefBindings(id);
			foreach ( AttributeBinding ab in bindings )
			{
				ValidationErrorAttribute vea=new ValidationErrorAttribute(ab.Element, ab.Name, ValidationErrorType.IdAttributeNotDefined);
				AddValidationError(ab.Element, vea);
			}
		}

		public void Attach(XmlDocument doc, Uri baseUri)
		{
			Detach();
			this.document=doc;
			if ( doc.DocumentType == null )
				return;

			DTDParser dtp=new DTDParser();

			bool hasInternalSubset=doc.DocumentType.InternalSubset != null;
			bool internalSubsetOnly=doc.DocumentType.SystemId == null;

			if ( baseUri == null && !doc.BaseURI.Equals("") )
				baseUri=new Uri(doc.BaseURI);

			// TODO: H: shouldn't this use the doc's resolver?
			CustomXmlResolver cxr=new CustomXmlResolver(baseUri);
			dtp.XmlResolver=cxr;

			if ( hasInternalSubset )
			{
				InputSource ii=new InputSource(doc.DocumentType.InternalSubset);
				documentType=dtp.parseInternalSubset(ii, internalSubsetOnly);
			}

			if ( !internalSubsetOnly )
			{
				Uri doctypeUri=cxr.ResolveUri(baseUri, doc.DocumentType.SystemId);
				InputSource i=new InputSource(doctypeUri);
				if ( hasInternalSubset )
					documentType=dtp.parseExternalSubset(i);
				else
					documentType=dtp.parseExternalSubset(i, true);
			}

			validationEngine=new ValidationEngine(documentType, this);
			quickFixer=new QuickFixer(documentType);

			GetAllIdAndIdRefs();
			ValidateAllIdAndIdRefs();

			document.NodeChanging+=new XmlNodeChangedEventHandler(NodeChanging);
			document.NodeChanged+=new XmlNodeChangedEventHandler(NodeChanged);
			document.NodeInserted+=new XmlNodeChangedEventHandler(NodeInserted);
			document.NodeRemoved+=new XmlNodeChangedEventHandler(NodeRemoved);
		}

		public void ValidateAll()
		{
			if ( document.DocumentElement != null )
				ValidateAll(document.DocumentElement);
		}

		// TODO: X: move?
		public void ValidateAll(XmlElement e)
		{
			Validate(e);
			ValidateChildren(e.ChildNodes);
		}

		// TODO: X: move?
		public void ValidateChildren(XmlNodeList l)
		{
			foreach ( XmlNode n in l )
			{
				switch ( n.NodeType )
				{
					case XmlNodeType.Element:
						ValidateAll((XmlElement) n);
						break;
					case XmlNodeType.EntityReference:
						ValidateChildren(n.ChildNodes);
						break;
				}
			}
		}

		private void GetAllIdAndIdRefs()
		{
			if ( documentType == null )
				return;

			foreach ( XmlElement e in document.SelectNodes("//*") )
			{
				ElementType et=documentType.GetElementType(e);
				if ( et == null )
					continue;

				foreach ( Attribute a in et.Attributes )
				{
					XmlAttribute attr=e.Attributes[a.Name.QualifiedName];
					if ( attr == null )
						continue;

					if ( !validationEngine.ValidateAttribute(a, e, attr) )
						// don't bother storing info on invalid attributes
						continue;

					AttributeBinding ab;
					string val=attr.Value;
					switch ( a.Type )
					{
						case AttributeType.ID:
							ab=new AttributeBinding(e, a.Name, val, attr.Specified);
							idTracker.AddId(ab);
							break;

						case AttributeType.IDREF:
						case AttributeType.IDREFS:
							ab=new AttributeBinding(e, a.Name, val, attr.Specified);
							idTracker.AddIdRefs(ab);
							break;
					}
				}
			}
		}

		private void ValidateAllIdAndIdRefs()
		{
			foreach ( AttributeBinding ab in idTracker.GetDuplicateIds() )
			{
				ValidationErrorAttribute vea=new ValidationErrorAttribute(ab.Element, ab.Name, ValidationErrorType.IdAttributeInUse);
				AddValidationError(ab.Element, vea);
			}

			foreach ( AttributeBinding ab in idTracker.GetUndefinedIdRefs() )
			{
				ValidationErrorAttribute vea=new ValidationErrorAttribute(ab.Element, ab.Name, ValidationErrorType.IdAttributeNotDefined);
				AddValidationError(ab.Element, vea);
			}
		}

		internal IValidationLookup InvalidNodes
		{
			get 
			{
				return nodeInfo;
			}
		}

		public ValidationError[] GetErrorDetails(XmlNode n)
		{
			return nodeInfo.GetDetails(n);
		}

		internal DocumentType DocumentType
		{
			get { return documentType == null ? new DocumentType() : documentType; }
		}

		private ICollection ConvertToSimpleReferences(ICollection col)
		{
			ArrayList ret=new ArrayList();
			foreach ( ElementType t in col )
			{
				Reference r=new Reference(t);
				ret.Add(new ElementTypeRef(r));
			}
			return ret;
		}

		// TODO: ?: remove?
		public IList GetAttributeInfo(XmlElement elem)
		{
			ArrayList ret=new ArrayList();

			if ( documentType == null )
				return ret;

			ElementType et=documentType.GetElementType(elem);
			if ( et == null )
				return ret;

			foreach ( Attribute a in et.Attributes )
			{
				string val=a.DefaultValue;
				bool specified=false;
				if ( elem.HasAttribute(a.Name.QualifiedName) )
				{
					val=elem.GetAttribute(a.Name.QualifiedName);
					specified=true;
				}
				AttributeBinding ab=new AttributeBinding(elem, a.Name, val, specified);
				ret.Add(ab);
			}
			return ret;
		}

//		private IList FilterValidNext(IList col, XmlElement n)
//		{
//			ArrayList ret=new ArrayList();
//			foreach ( SimpleReference sr in col )
//			{
//				if ( ret.Contains(sr) )
//					continue;
//
//				if ( n != null && 
//						FindInCollection(sr.OriginalReference.GetValidNextElements(), n) != null )
//					ret.Add(sr);
//			}
//			return ret;
//		}

//		private IList GetValidFirstCaseElements(ElementType et, XmlElement e)
//		{
//			ArrayList ret=new ArrayList();
//			IList col=et.ContentModel.GetValidFirstElements();
//			foreach ( SimpleReference sr in col )
//			{
//				if ( ret.Contains(sr) )
//					continue;
//
//				if ( FindInCollection(sr.OriginalReference.GetValidNextElements(), e) != null )
//					ret.Add(sr);
//			}
//			return ret;
//		}

//		private SimpleReference FindReference(ElementType et, XmlElement parent, XmlElement n)
//		{
//			IList col=et.ContentModel.GetValidFirstElements();
//			SimpleReference r=null;
//			foreach ( XmlNode c in parent.ChildNodes )
//			{
//				if ( c.NodeType != XmlNodeType.Element )
//					continue;
//
//				XmlElement child=(XmlElement) c;
//
//				if ( child.Equals(n) )
//					break;
//
//				r=FindInCollection(col, child);
//				if ( r == null )
//					return null;
//
//				col=r.OriginalReference.GetValidNextElements();
//				if ( col.Count == 0 )
//					return null;
//
//				r=(SimpleReference) col[0];
//			}
//			return r;
//		}

		private void ValidateAttributeAddOrChange(XmlAttribute a)
		{
			XmlElement e=a.OwnerElement;

			if ( documentType == null || e == null )
				return;

			if ( e.ParentNode == null )
				// node is not in document yet
				return;

			ElementType et=documentType.GetElementType(e);
			if ( et == null )
				return;

			AttributeErrorFilter aef=new AttributeErrorFilter(e, a);
			RemoveValidationErrors(aef);

			Attribute attr=et.GetAttribute(a);
			if ( attr == null )
			{
				ValidationError ve=new ValidationErrorAttribute(e, a.Name, ValidationErrorType.AttributeNotDefined);
				AddValidationError(e, ve);
				return;
			}

			// it's only at this point we can even tell if it's ID/IDREF
			AddIdOrIdRef(e, a, attr);

			if ( !validationEngine.ValidateAttribute(attr, e, a) )
			{
				ValidationError ve=new ValidationErrorAttribute(e, a.Name, ValidationErrorType.InvalidAttributeValue);
				AddValidationError(e, ve);
			}
		}

		private void ValidateAttributes(XmlElement e)
		{
			AttributeErrorFilter aef=new AttributeErrorFilter(e);
			RemoveValidationErrors(aef);

			if ( documentType == null )
				return;

			ElementType et=documentType.GetElementType(e);
			if ( et == null )
				return;

			ArrayList defined=new ArrayList(et.Attributes);
			ArrayList foundList=new ArrayList();

			foreach ( Attribute attr in defined )
			{
				bool found=false;
				foreach ( XmlAttribute a in e.Attributes )
				{
					if ( attr.Name.QualifiedName.Equals(a.Name) )
					{
						found=true;
						if ( !validationEngine.ValidateAttribute(attr, e, a) )
							AddValidationError(e, new ValidationErrorAttribute(e, a.Name, ValidationErrorType.InvalidAttributeValue));

						AddIdOrIdRef(e, a, attr);
						break;
					}
				}
				if ( !found && attr.State == AttributeState.Required )
					AddValidationError(e, new ValidationErrorAttribute(e, attr.Name.QualifiedName, ValidationErrorType.RequiredAttributeMissing));
				else
					foundList.Add(attr.Name.QualifiedName);
			}

			foreach ( XmlAttribute a in e.Attributes )
			{
				if ( !foundList.Contains(a.Name) )
					AddValidationError(e, new ValidationErrorAttribute(e, a.Name, ValidationErrorType.AttributeNotDefined));
			}
		}

		private void AddValidationError(XmlNode n, ValidationError ve)
		{
			nodeInfo.AddValidationError(n, ve);
		}

		private ValidationError AddValidationError(XmlNode n, ValidationErrorType type)
		{
			ValidationError ve=new ValidationError(n, type);
			AddValidationError(n, ve);
			return ve;
		}

		private void RemoveValidationErrors(IValidationErrorFilter filter)
		{
			ArrayList newList=new ArrayList();
			ICollection col=nodeInfo.GetDetails(filter.Node);
			foreach ( ValidationError ve in col )
			{
				if ( !filter.IsMatch(ve) )
					newList.Add(ve);
				else
					Console.WriteLine("Removing validation error {0}", ve.Message);
			}
			nodeInfo.SetDetails(filter.Node, (ValidationError[]) newList.ToArray(typeof(ValidationError)));
		}

		public void RemoveValidationErrors(XmlNode n)
		{
			nodeInfo.Remove(n);
		}

		public void StartValidation(XmlElement e)
		{
			RemoveValidationErrors(new ContentErrorFilter(e));
		}

		public void ProcessError(XmlNode n, ValidationErrorType type)
		{
			ValidationError ve=AddValidationError(n, type);
		}

		public void StartChildValidation(XmlNode n)
		{
			RemoveValidationErrors(new ContextErrorFilter(n));
		}

		private Attribute LookupAttribute(XmlElement e, string name)
		{
			if ( documentType != null )
			{
				ElementType et=documentType.GetElementType(e);
				if ( et != null )
					return et.GetAttribute(name);
			}
			return null;
		}

		public bool IsAttributeRequired(XmlElement element, string name)
		{
			Attribute attr=LookupAttribute(element, name);
			return attr != null && attr.State == AttributeState.Required;
		}

		public bool IsAttributeFixed(XmlElement element, string name)
		{
			Attribute attr=LookupAttribute(element, name);
			return attr != null && attr.State == AttributeState.Fixed;
		}

		public bool IsAttributeDefined(XmlElement element, string name)
		{
			Attribute attr=LookupAttribute(element, name);
			return attr != null;
		}

		public bool IsAttributeValid(XmlElement element, string name, XmlAttribute actual)
		{
			if ( !HasElements )
				// there is no DTD so return valid
				return true;

			Attribute attr=LookupAttribute(element, name);
			if ( attr == null && actual != null )
				return false;

			if ( actual == null && attr != null )
				return attr.State != AttributeState.Required;

			// TODO: L: don't think this can happen
			if ( actual == null && attr == null )
				return false;

			return validationEngine.ValidateAttribute(attr, element, actual);
		}

		public string[] GetDefinedAttributeNames(XmlElement element)
		{
			if ( documentType == null )
				return new string[] {};

			ElementType et=documentType.GetElementType(element);
			if ( et == null )
				return new string[] {};

			return et.AttributeNames;
		}

		public string[] GetEnumValues(XmlElement element, string name)
		{
			Attribute attr=LookupAttribute(element, name);
			if ( attr == null )
				throw new ArgumentException("Attribute is not defined");

			if ( attr.Type != AttributeType.Enumerated )
				throw new ArgumentException("Attribute is not enumeration");

			return attr.Enums;
		}

		public AttributeType GetAttributeType(XmlElement element, string name)
		{
			Attribute attr=LookupAttribute(element, name);
			if ( attr == null )
				throw new ArgumentException("Attribute is not defined");

			return attr.Type;
		}

		public QuickFix[] GetQuickFixes(ValidationError ve)
		{
			return quickFixer.GetFixes(ve);
		}

		public QuickFix[] GetQuickFixes(XmlNode n)
		{
			ArrayList fixes=new ArrayList();
			foreach ( ValidationError ve in nodeInfo.GetDetails(n) )
				fixes.AddRange(quickFixer.GetFixes(ve));

			return fixes.ToArray(typeof(QuickFix)) as QuickFix[];
		}

		public ElementListItem[] GetValidElements(XmlElement parent, XmlNode child, bool b)
		{
			if ( validationEngine == null )
				return new ElementListItem[] {};

			return validationEngine.GetValidElements(parent, child, b);
		}

	}
	internal class AttributeBinding
	{
		private XmlElement element;
		private XmlName name;
		private string val;
		private bool specified;

		public AttributeBinding(XmlElement element, XmlName name, string val, bool specified)
		{
			this.element=element;
			this.name=name;
			this.val=val;
			this.specified=specified;
		}

		public override bool Equals(object obj)
		{
			AttributeBinding ab=obj as AttributeBinding;
			if ( ab == null )
				return false;

			// for hashtable purposes we don't care if the values are different
			return this.element.Equals(ab.element) && this.Name.Equals(ab.Name);
		}

		public override int GetHashCode()
		{
			return element.GetHashCode() + name.GetHashCode();
		}


		public XmlElement Element
		{
			get { return this.element; }
		}

		public string Name
		{
			get { return name.QualifiedName; }
		}

		[Browsable(false)]
		public string LocalName
		{
			get { return name.LocalName; }
		}

		public string Value
		{
			get { return val; }
			set 
			{
				this.val=value;
				element.SetAttribute(name.QualifiedName, value);
			}
		}

		[Browsable(false)]
		public bool Specified
		{
			get { return specified; }
		}
	}

	public class ElementListItem
	{
		public XmlName Name;
		public bool IsRequired;
		public bool IsChoice;

		public ElementListItem(XmlName name, bool required, bool choice)
		{
			this.Name=name;
			this.IsRequired=required;
			this.IsChoice=choice;
		}

		public ElementListItem(XmlName name, bool required) : this(name, required, false)
		{
		}

		public ElementListItem(XmlName name) : this(name, false, false)
		{
		}
	}
}
