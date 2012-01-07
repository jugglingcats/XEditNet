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
using System.Diagnostics;

namespace XEditNet.Dtd
{
	/**
	 * A content particle that is either a choice group or a sequence group.
	 *
	 * @author Ronald Bourret
	 * @version 2.0
	 */

	internal class Group : Particle
	{
		// ********************************************************************
		// Variables
		// ********************************************************************

		/**
		* A Vector containing the members of the group.
		*
		* <p>This contains Particles (either Groups or References).</p>
		*/
		private ArrayList members = new ArrayList(); // Contains Particles
		private Particle previous=null;

		private IList GetValidElements(Particle p)
		{
			if ( p.Type == ParticleType.ElementTypeRef )
				return new object[] { new ElementTypeRef((Reference) p) };
			else
				return ((Group) p).GetValidElements();

		}

		public IList GetValidNextElements(Particle p)
		{
			return GetValidNextElements(p, false);
		}

		public IList GetValidNextElements(Particle p, bool forceNext)
		{
			ArrayList ret=new ArrayList();

			if ( p.IsRepeatable )
			{
				ICollection x=GetValidElements(p);
				UpdateRequiredStatus(x, false);
				ret.AddRange(x);
			}

			// if a choice then is already exhausted
			if ( Type == ParticleType.Sequence )
			{
				bool firstLoop=true;
				bool anyRequired=false;
				if ( !p.IsRepeatable || forceNext )
					p=p.Next;
				else
					firstLoop=false;

				while ( p != null  )
				{
					ICollection x=GetValidElements(p);

					anyRequired=firstLoop ? ContainsRequiredItems(x) : false;
					UpdateRequiredStatus(x, anyRequired);
					ret.AddRange(x);

					if ( anyRequired )
						break;

					p=p.Next;
				}
				if ( !anyRequired )
				{
					// got to end of sequence, nothing required, can repeat,
					// so check add elements allowed at start of sequence
					// TODO: L: this can lead to duplicates -- not if model is deterministic
					if ( IsRepeatable )
						ret.AddRange(GetValidElements(false));
				}
			}
			else if ( Type == ParticleType.Choice && IsRepeatable )
				// TODO: L: does this validate that any are selected?
				ret.AddRange(GetValidElements(false));

			// TODO: L: this might add duplicates - but have changed now to move next
			if ( Group != null && this.Next != null )
				ret.AddRange( Group.GetValidNextElements(this, true) );

			return ret;
		}

		protected IList GetValidElements()
		{
			return GetValidElements(true);
		}

		protected IList GetValidElements(bool firstLoop)
		{
			ArrayList ret=new ArrayList();
			foreach ( Particle p in members )
			{
				ICollection x=GetValidElements(p);

				bool anyRequired=firstLoop ? ContainsRequiredItems(x) : false;
				UpdateRequiredStatus(x, anyRequired);
				ret.AddRange(x);

				if ( anyRequired && this.Type == ParticleType.Sequence )
					break;
			}
			return ret;
		}

		private bool ContainsRequiredItems(ICollection col)
		{
			foreach ( ElementTypeRef r in col )
			{
				if ( r.IsRequired )
					return true;
			}
			return false;
		}

		private void UpdateRequiredStatus(ICollection col, bool isRequired)
		{
			foreach ( ElementTypeRef r in col )
			{
				if ( !this.IsRequired || !isRequired )
					r.IsRequired=false;

				if ( this.Type == ParticleType.Choice )
					r.IsChoice=true;
			}
		}

		public void FinaliseGroup()
		{
			if ( Type != ParticleType.Choice )
				return;

			// if a choice contains optional items then
			// effectively the whole choice is optional
			foreach ( Particle p in Members )
			{
				ICollection col=GetValidElements(p);
				if ( !ContainsRequiredItems(col) )
				{
					IsRequired=false;
					return;
				}
			}
		}

		// ********************************************************************
		// Constructors
		// ********************************************************************

		/** Construct a new Group. */
		public Group()
		{
		}

		public void AddMember(Particle p)
		{
			if ( previous != null )
				previous.Next=p;

			previous=p;
			p.Group=this;
			members.Add(p);
		}

		public IList Members
		{
			get { return members; }
		}
	}
}