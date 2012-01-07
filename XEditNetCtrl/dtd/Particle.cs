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

	/**
	 * Class representing a content particle in a content model.
	 *
	 * <p>This is the base class for Group and Reference.</p>
	 *
	 * @author Ronald Bourret
	 * @version 2.0
	 */

	internal enum ParticleType
	{
		Unknown,
		ElementTypeRef,
		Choice,
		Sequence
	}

	internal abstract class Particle
	{
		/** Content particle type. */
		public ParticleType Type = ParticleType.Unknown;
		public Group Group;
		public Particle Next=null;

		/**
		 * Whether the particle is required.
		 *
		 * <p>By default, this is true. The following table shows how isRequired
		 * and isRepeatable map to the *, +, and ? qualifiers:</p>
		 *
		 * <pre>
		 *
		 *                         isRequired
		 *                   ------------------------
		 *    isRepeatable  |   true    |    false
		 *    --------------|-----------|------------
		 *            true  |     +     |      *
		 *    --------------|-----------|------------
		 *           false  |     --    |      ?
		 *
		 * </pre>
		 *
		 * <p>Note that the defaults of isRequired and isRepeatable map to
		 * the required/not repeatable (i.e. no operator) case.</p>
		 */
		public bool IsRequired = true;

		/** Whether the particle may be repeated.
		 *
		 * <p>By default, this is false.</p>
		 */
		public bool IsRepeatable = false;

		// ********************************************************************
		// Constructors
		// ********************************************************************

		/** Construct a new Particle. */
		public Particle()
		{
		}

		public override string ToString()
		{
			return string.Format("{0}: Required {1}, Repeatable {2}, Type {3}",
				base.ToString(), IsRequired, IsRepeatable, Type);
		}
	}
}