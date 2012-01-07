using System;
using System.Collections;
using System.Xml;
using XEditNet.Dtd;
using XEditNet.Location;
using Attribute = XEditNet.Dtd.Attribute;

// TODO: H: current quick fix will not change an element to make it valid if there are other
//			errors among siblings. Need a way to say if element were changed, would there be
//			an error in the same position (or from this point forward)


namespace XEditNet.Validation
{
	/// <summary>
	/// Summary description for QuickFix.
	/// </summary>
	internal abstract class QuickFix
	{
		protected XmlNode node;

		public static QuickFix[] EmptyList
		{
			get { return new QuickFix[] {}; }
		}

		public QuickFix(XmlNode n)
		{
			node=n;
		}

		public abstract string MainText
		{
			get;
		}

		public virtual string SubText
		{
			get { return null; }
		}

		public virtual Selection PreSelection(SelectionManager sm, Selection sel)
		{
			if ( !sel.IsEmpty && !sel.Start.Node.Equals(node) )
				return new Selection(SelectionManager.CreateSelectionPoint(node, false));

			return sel;
		}

		public abstract Selection Perform(SelectionManager sm);
	}

	internal class QuickFixDelete : QuickFix
	{
		public QuickFixDelete(XmlNode n) : base(n)
		{
		}

		public override string MainText
		{
			get
			{
				switch ( node.NodeType )
				{
					case XmlNodeType.Text:
						return "Delete Text";

					case XmlNodeType.Element:
						return "Delete "+node.Name;

					default:
						return "Delete";
				}
			}
		}

		public override Selection Perform(SelectionManager sm)
		{
			SelectionPoint sp=SelectionManager.CreateSelectionPoint(node, true);
			sp=sm.NextSelectionPoint(sp);

			node.ParentNode.RemoveChild(node);

			return new Selection(sp);
		}
	}

	internal abstract class QuickFixAttribute : QuickFix
	{
		protected string name;
		protected string newValue;

		public QuickFixAttribute(XmlNode n, string name, string s) : base(n)
		{
			this.name=name;
			this.newValue=s;
		}
		
		public override string SubText
		{
			get
			{
				return string.Format("{0}", newValue);
			}
		}

		public override Selection Perform(SelectionManager sm)
		{
			// TODO: M: think about selection returned
			((XmlElement) node).SetAttribute(name, newValue);
			return sm.CreateSelection(node);
		}
	}

	internal class QuickFixChangeAttribute : QuickFixAttribute
	{
		public QuickFixChangeAttribute(XmlNode n, string name, string id) : base(n, name, id)
		{
		}

		public override string MainText
		{
			get { return string.Format("Change Attribute '{0}' To", name); }
		}
	}

	internal class QuickFixAddAttribute : QuickFixAttribute
	{
		public QuickFixAddAttribute(XmlNode n, string name, string id) : base(n, name, id)
		{
		}

		public override string MainText
		{
			get { return string.Format("Set Attribute '{0}' To", name); }
		}
	}


	internal class QuickFixChange : QuickFix
	{
		private XmlName name;

		public QuickFixChange(XmlNode n, XmlName name) : base(n)
		{
			this.name=name;
		}

		public override string MainText
		{
			get
			{
				return "Change To";
			}
		}

		public override string SubText
		{
			get
			{
				return string.Format("{0}", name.QualifiedName);
			}
		}


		public override Selection Perform(SelectionManager sm)
		{
			return SelectionManager.Change(Selection.Empty, (XmlElement) node, 
				XmlUtil.CreateElement(name, node.OwnerDocument));
		}
	}

	internal class QuickFixInsert : QuickFix
	{
		private XmlName name;

		public QuickFixInsert(XmlNode n, XmlName name) : base(n)
		{
			this.name=name;
		}

		public override string MainText
		{
			get
			{
				return "Insert";
			}
		}

		public override string SubText
		{
			get
			{
				return string.Format("{0}", name.QualifiedName);
			}
		}


		public override Selection Perform(SelectionManager sm)
		{
			XmlElement e=XmlUtil.CreateElement(name, node.OwnerDocument);
			node.ParentNode.InsertBefore(e, node);
			SelectionPoint sp=SelectionManager.CreateSelectionPoint(e, true);

			return new Selection(sp);
		}
	}

	internal class QuickFixAppend : QuickFix
	{
		private XmlName name;

		public QuickFixAppend(XmlNode n, XmlName name) : base(n)
		{
			this.name=name;
		}

		public override string MainText
		{
			get
			{
				return "Append";
			}
		}

		public override string SubText
		{
			get
			{
				return string.Format("{0}", name.QualifiedName);
			}
		}


		public override Selection Perform(SelectionManager sm)
		{
			XmlElement e=XmlUtil.CreateElement(name, node.OwnerDocument);
			node.AppendChild(e);
			SelectionPoint sp=SelectionManager.CreateSelectionPoint(e, true);

			return new Selection(sp);
		}
	}


	internal class QuickFixStrip : QuickFix
	{
		public QuickFixStrip(XmlElement n) : base(n)
		{
		}

		public override string MainText
		{
			get
			{
				return "Strip Element "+node.Name;
			}
		}

		public override Selection Perform(SelectionManager sm)
		{
			SelectionPoint sp=SelectionManager.CreateSelectionPoint(node, false);
			sp=sm.NextSelectionPoint(sp);

			foreach ( XmlNode n in node.ChildNodes )
				node.ParentNode.InsertBefore(n, node);

			node.ParentNode.RemoveChild(node);

			return new Selection(sp);
		}
	}

	internal class QuickFixValidator : IValidationClient
	{
		private bool valid=true;
		private DocumentType documentType;

		public QuickFixValidator(DocumentType dtd)
		{
			documentType=dtd;	
		}

		public void RemoveValidationErrors(XmlNode n)
		{
		}

		public void StartValidation(XmlElement e)
		{
		}

		public void ProcessError(XmlNode n, ValidationErrorType type)
		{
			valid=false;
		}

		public void StartChildValidation(XmlNode n)
		{
		}

		public bool IsValidSequence(XmlElement e, ArrayList items)
		{
			ValidationEngine ve=new ValidationEngine(documentType, this);
			ElementType et=documentType.GetElementType(e);
			if ( et == null )
				return true;

			valid=true;
			ve.Validate(e, et, items);

			return valid;
		}
	}

	internal class QuickFixer
	{
		private DocumentType documentType;

		public QuickFixer(DocumentType documentType)
		{
			this.documentType=documentType;
		}

		public QuickFix[] GetFixes(ValidationError ve)
		{
			XmlNode n=ve.Node;
			ArrayList fixes=new ArrayList();

			// just in case we need it
			ValidationErrorAttribute vea=ve as ValidationErrorAttribute;

			switch ( ve.Type )
			{
				case ValidationErrorType.ElementNotDefined:
					fixes.Add(new QuickFixDelete(n));
					fixes.AddRange(GetStripFixes(n));
					break;

				case ValidationErrorType.NodeNotAllowed:
					fixes.Add(new QuickFixDelete(n));
					fixes.AddRange(GetChangeFixes(n as XmlElement));
					fixes.AddRange(GetStripFixes(n));
					break;

				case ValidationErrorType.ElementNotAllowedHere:
					fixes.Add(new QuickFixDelete(n));
					fixes.AddRange(GetStripFixes(n));
					fixes.AddRange(GetChangeFixes(n as XmlElement));
					fixes.AddRange(GetInsertFixes(n as XmlElement));
					break;

				case ValidationErrorType.ElementNotInContentModel:
					fixes.Add(new QuickFixDelete(n));
					fixes.AddRange(GetStripFixes(n));
					fixes.AddRange(GetChangeFixes(n as XmlElement));
					break;

				case ValidationErrorType.RequiredElementMissing:
					fixes.AddRange(GetAppendFixes(n as XmlElement));
					break;

				case ValidationErrorType.IdAttributeInUse:
					fixes.AddRange(GetDuplicateIdFixes((XmlElement) vea.Node, vea.QualifiedName));
					break;

				case ValidationErrorType.IdAttributeNotDefined:
					break;

				case ValidationErrorType.InvalidAttributeValue:
					fixes.AddRange(GetAttributeValueFixes((XmlElement) vea.Node, vea.QualifiedName));
					break;

				case ValidationErrorType.RequiredAttributeMissing:
					fixes.AddRange(GetGenerateIdFixes((XmlElement) vea.Node, vea.QualifiedName));
					break;
			}
			return fixes.ToArray(typeof(QuickFix)) as QuickFix[];
		}

		private QuickFix[] GetDuplicateIdFixes(XmlElement element, string name)
		{
			Guid guid=Guid.NewGuid();
			string id="ID-"+guid.ToString();

			ArrayList ret=new ArrayList();
			ret.Add(new QuickFixChangeAttribute(element, name, id));

			return ret.ToArray(typeof(QuickFix)) as QuickFix[];
		}

		private QuickFix[] GetGenerateIdFixes(XmlElement element, string name)
		{
			Guid guid=Guid.NewGuid();
			string id="ID-"+guid.ToString();

			ArrayList ret=new ArrayList();
			ret.Add(new QuickFixAddAttribute(element, name, id));

			return ret.ToArray(typeof(QuickFix)) as QuickFix[];
		}

		private QuickFix[] GetAttributeValueFixes(XmlElement element, string name)
		{
			ElementType et=documentType.GetElementType(element);
			if ( et == null )
				return QuickFix.EmptyList;

			ArrayList ret=new ArrayList();

			Attribute attr=et.GetAttribute(name);
			// TODO: M: think about fixing up others, eg. NMTOKEN
			switch ( attr.Type )
			{
				case AttributeType.ID:
					// TODO: M: think about re-parsing rather than gen new
					Guid guid=Guid.NewGuid();
					string id="ID-"+guid.ToString();
					ret.Add(new QuickFixChangeAttribute(element, name, id));
					break;
			}

			return ret.ToArray(typeof(QuickFix)) as QuickFix[];
		}

		private QuickFix[] GetChangeFixes(XmlElement e)
		{
			if ( e == null )
				return new QuickFix[] {};

			XmlElement parent=e.ParentNode as XmlElement;
			// TODO: E: entities!
			if ( parent == null )
				return new QuickFix[] {};

			// TODO: M: refactor - shouldn't need to pass null here
			//			(some of the ve methods have no callback - new class?)
			ValidationEngine ve=new ValidationEngine(documentType, null);
			// get candidate replacements
			ElementListItem[] possibles=ve.GetValidElements(parent, e, true);

			ArrayList children=new ArrayList();
			foreach ( XmlNode n in e.ChildNodes )
				children.Add(n);

			ArrayList ret=new ArrayList();
			foreach ( ElementListItem eli in possibles )
			{
				// TODO: M: error checking (should never be null though)
				ElementType et=documentType[eli.Name];
				if ( ve.IsValid(et, children) )
				{
					QuickFixChange qfc=new QuickFixChange(e, eli.Name);
					ret.Add(qfc);
				}
			}
			return ret.ToArray(typeof(QuickFix)) as QuickFix[];
		}

		private QuickFix[] GetInsertFixes(XmlElement e)
		{
			if ( e == null )
				return new QuickFix[] {};

			XmlElement parent=e.ParentNode as XmlElement;
			// TODO: E: entities!
			if ( parent == null )
				return new QuickFix[] {};

			ValidationEngine ve=new ValidationEngine(documentType, null);
			// get candidate replacements
			ElementListItem[] possibles=ve.GetValidElements(parent, e, false);

			ArrayList ret=new ArrayList();
			foreach ( ElementListItem eli in possibles )
			{
				if ( eli.IsRequired )
				{
					QuickFixInsert qfi=new QuickFixInsert(e, eli.Name);
					ret.Add(qfi);
				}
			}
			return ret.ToArray(typeof(QuickFix)) as QuickFix[];
		}


		private ICollection GetStripFixes(XmlNode n)
		{
			ArrayList list=new ArrayList();

			if ( n.HasChildNodes )
			{
				XmlElement e=(XmlElement) n;
				ArrayList items=new ArrayList();
				foreach ( XmlNode child in e.ParentNode.ChildNodes )
				{
					if ( child.Equals(n) )
					{
						foreach ( XmlNode child2 in e.ChildNodes )
							items.Add(child2);

						continue;
					}
					items.Add(child);
				}

				QuickFixValidator sv=new QuickFixValidator(documentType);
				if ( sv.IsValidSequence(e.ParentNode as XmlElement, items) )
					list.Add(new QuickFixStrip((XmlElement) n));
			}
			return list;
		}

		private ICollection GetAppendFixes(XmlElement e)
		{
			ValidationEngine ve=new ValidationEngine(documentType, null);
			ElementListItem[] possibles=ve.GetMissingElements(e);

			ArrayList list=new ArrayList();
			foreach ( ElementListItem eli in possibles )
			{
				if ( eli.IsRequired )
				{
					QuickFixAppend qfi=new QuickFixAppend(e, eli.Name);
					list.Add(qfi);
				}
			}
			return list;
		}
	}
}
