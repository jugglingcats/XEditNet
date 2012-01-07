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
using System.Xml.Serialization;

namespace XEditNet.Dtd
{
	/**
	 * Class representing a reference to an ElementType.
	 *
	 * <p>Reference is used in the members of a Group.</p>
	 *
	 * @author Ronald Bourret
	 * @version 2.0
	 */

	internal class Reference : Particle
	{
		// ********************************************************************
		// Variables
		// ********************************************************************

		/** The referred-to element type. */
		public ElementType ElementType = null;

		// ********************************************************************
		// Constructors
		// ********************************************************************

		/** Construct a new Reference. */
		public Reference()
		{
			this.Type = ParticleType.ElementTypeRef;
		}

		public IList GetValidNextElements()
		{
			return Group.GetValidNextElements(this);
		}
			
		/**
		 * Construct a new Reference and set the element type.
		 *
		 * @param elementType The referenced element type.
		 */
		public Reference(ElementType elementType)
		{
			this.Type = ParticleType.ElementTypeRef;
			this.ElementType = elementType;
		}

		public override string ToString()
		{
			return string.Format("{0}, Reference {1}", base.ToString(), 
				ElementType.Name);
		}

	}

	internal class ElementTypeRef
	{
		public XmlName Name;
		[XmlAttribute]
		public bool IsRequired;
		[XmlAttribute]
		public bool IsChoice;
		[XmlIgnore]
		public Reference OriginalReference;

		public ElementTypeRef()
		{
		}

		public ElementTypeRef(Reference r)
		{
			OriginalReference=r;
			IsRequired=r.IsRequired;
			IsChoice=r.Group.Type == ParticleType.Choice;
			Name=r.ElementType.Name;
		}

		public override string ToString()
		{
			return Name + (IsRequired ? "" : "?") + (IsChoice ? "#" : "");
		}

		public override bool Equals(object obj)
		{
			if ( !(obj is ElementTypeRef) )
				return false;

			ElementTypeRef other=(ElementTypeRef) obj;
			if ( !Name.Equals(other.Name) )
				return false;

			return (IsRequired == other.IsRequired) && (IsChoice == other.IsChoice);
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode() + IsRequired.GetHashCode() + IsChoice.GetHashCode();
		}
 
	}
}