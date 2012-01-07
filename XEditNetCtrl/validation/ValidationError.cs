using System;
using System.Collections;
using System.Xml;

using XEditNet.Dtd;

namespace XEditNet.Validation
{
	/// <summary>
	/// Specifies a type of validation error.
	/// </summary>
	public enum ValidationErrorType
	{
		/// Element is not defined.
		ElementNotDefined,
		/// Attribute value is invalid according to its type.
		InvalidAttributeValue,
		/// Element content model is incomplete due to missing required element.
		RequiredElementMissing,
		/// A required attribute is missing.
		RequiredAttributeMissing,
		/// The node is not allowed according to the parent content model.
		NodeNotAllowed,
		/// The element is not in the content model for the parent element.
		ElementNotInContentModel,
		/// The element is not allowed at this point (possibly missing required elem)
		ElementNotAllowedHere,
		/// The attribute is not defined for the parent element.
		AttributeNotDefined,
		/// An id type attribute occurs more than once.
		IdAttributeInUse,
		/// An idref type attribute refers to an id that does not exist.
		IdAttributeNotDefined
	}

	internal interface IValidationErrorFilter
	{
		XmlNode Node
		{
			get;
		}

		bool IsMatch(ValidationError ve);
	}

	internal class ContextErrorFilter : IValidationErrorFilter
	{
		private XmlNode node;
		public ContextErrorFilter(XmlNode n)
		{
			node=n;
		}

		public XmlNode Node
		{
			get	{ return node; }
		}

		public bool IsMatch(ValidationError ve)
		{
			switch ( ve.Type )
			{
				case ValidationErrorType.ElementNotAllowedHere:
				case ValidationErrorType.ElementNotInContentModel:
				case ValidationErrorType.NodeNotAllowed:
					return true;
			}
			return false;
		}
	}

	internal class ContentErrorFilter : IValidationErrorFilter
	{
		private XmlNode node;
		public ContentErrorFilter(XmlElement e)
		{
			node=e;
		}

		public XmlNode Node
		{
			get	{ return node; }
		}

		public bool IsMatch(ValidationError ve)
		{
			switch ( ve.Type )
			{
				case ValidationErrorType.AttributeNotDefined:
				case ValidationErrorType.InvalidAttributeValue:
				case ValidationErrorType.IdAttributeNotDefined:
				case ValidationErrorType.IdAttributeInUse:
				case ValidationErrorType.RequiredAttributeMissing:
				case ValidationErrorType.ElementNotDefined:
				case ValidationErrorType.NodeNotAllowed:
				case ValidationErrorType.ElementNotAllowedHere:
				case ValidationErrorType.ElementNotInContentModel:
					return false;
			}
			return true;
		}
	}

	internal class AttributeErrorFilter : IValidationErrorFilter
	{
		private XmlNode node;
		private XmlAttribute attr;
		private ValidationErrorType errorType;
		private bool errorSpecific=false;

		public AttributeErrorFilter(XmlElement e)
		{
			node=e;
		}

		public AttributeErrorFilter(XmlElement e, XmlAttribute a) : this(e)
		{
			attr=a;
		}

		public AttributeErrorFilter(XmlElement e, XmlAttribute a, ValidationErrorType type) : this(e, a)
		{
			errorType=type;
			errorSpecific=true;
		}

		public XmlNode Node
		{
			get	{ return node; }
		}

		public bool IsMatch(ValidationError ve)
		{
			ValidationErrorAttribute vea=ve as ValidationErrorAttribute;
			if ( vea == null )
				return false;

			if ( attr != null && !vea.QualifiedName.Equals(attr.Name) )
				return false;

			if ( errorSpecific )
				return vea.Type == errorType;

			return true;
		}
	}

	/// <summary>
	/// Represents a an error in validation according to the document's DTD.
	/// </summary>
	public class ValidationError
	{
		/// <summary>
		/// The type of validation error.
		/// </summary>
		public readonly ValidationErrorType Type;
		/// <summary>
		/// The XmlNode where the error occurs.
		/// </summary>
		public readonly XmlNode Node;

		internal ValidationError(XmlNode node, ValidationErrorType type)
		{
			Node=node;
			Type=type;
		}

		public override string ToString()
		{
			return Message;
		}

		/// <summary>
		/// Gets a string that represents the error in a user friendly way.
		/// </summary>
		public virtual string Message
		{
			get 
			{
				switch ( Type )
				{
					case ValidationErrorType.ElementNotDefined:
						return string.Format("Element '{0}' is not defined in the DTD/schema", Node.Name);

					case ValidationErrorType.NodeNotAllowed:
						switch ( Node.NodeType )
						{
							case XmlNodeType.Element:
								return string.Format("Element '{0}' is not allowed at this point in '{1}'", Node.Name, Node.ParentNode.Name);

							case XmlNodeType.Text:
								return string.Format("Text is not allowed at this point");

							default:
								throw new InvalidOperationException("Unexpected node type: "+Node.NodeType);
						}

					case ValidationErrorType.ElementNotAllowedHere:
						return string.Format("One or more required elements is missing before '{0}' in element '{1}'", Node.Name, Node.ParentNode.Name);

					case ValidationErrorType.ElementNotInContentModel:
						return string.Format("Element '{0}' is not allowed in element '{1}'", Node.Name, Node.ParentNode.Name);

					case ValidationErrorType.RequiredElementMissing:
						return string.Format("Element '{0}' is incomplete. One or more required elements is missing", Node.Name);

					default:
						throw new InvalidOperationException("Unexpected error type (should be handled by ValidationErrorAttribute)");
				}
			}
		}
	}

	internal class ValidationErrorAttribute : ValidationError
	{
		public readonly string QualifiedName;

		public ValidationErrorAttribute(XmlElement node, string name, ValidationErrorType type) : base(node, type)
		{
			QualifiedName=name;
		}

		public override string Message
		{
			get
			{
				XmlElement node=(XmlElement) Node;
				switch ( Type )
				{
					case ValidationErrorType.AttributeNotDefined:
						return string.Format("Attribute '{0}' for element '{1}' is not defined in the DTD/schema", QualifiedName, Node.Name);

					case ValidationErrorType.IdAttributeInUse:
						return string.Format("ID value '{0}', for attribute '{1}' in element '{2}' is already in use", node.GetAttribute(QualifiedName), QualifiedName, Node.Name);

					case ValidationErrorType.IdAttributeNotDefined:
						return string.Format("IDREF attribute '{0}' (in element '{1}') refers to an non-existant ID", QualifiedName, Node.Name);

					case ValidationErrorType.InvalidAttributeValue:
						return string.Format("Attribute '{0}' in element '{1}' has an invalid value", QualifiedName, Node.Name);

					case ValidationErrorType.RequiredAttributeMissing:
						return string.Format("Element '{0}' is missing required attribute '{1}'", Node.Name, QualifiedName);

					default:
						throw new InvalidOperationException("Unexpected error type (in ValidationErrorAttribute)");
				}
			}
		}

	}
}
