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

namespace XEditNet.Dtd
{

	/**
	 * Class representing an unparsed entity.
	 *
	 * @author Ronald Bourret
	 * @version 2.0
	 */

	internal class UnparsedEntity : Entity
	{
		// ********************************************************************
		// Variables
		// ********************************************************************

		/** The notation used by the entity. */
		public string Notation = null;

		// ********************************************************************
		// Constructors
		// ********************************************************************

		/** Construct a new UnparsedEntity. */
		public UnparsedEntity()
		{
			this.Type = EntityType.Unparsed;
		}


		/**
		 * Construct a new UnparsedEntity and set its name.
		 *
		 * @param name The entity's name.
		 */
		public UnparsedEntity(string name) : base(name)
		{
			this.Type = EntityType.Unparsed;
		}
	}
}