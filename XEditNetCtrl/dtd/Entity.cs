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
	 * Base class for entities.
	 *
	 * @author Ronald Bourret
	 * @version 2.0
	 */

	internal enum EntityType
	{
		Unknown,
		ParsedGeneral,
		Parameter,
		Unparsed
	}

	internal class Entity
	{
		// ********************************************************************
		// Variables
		// ********************************************************************

		/** The entity type. */
		public EntityType Type = EntityType.Unknown;

		/** The entity name. */
		public string Name = null;

		/** The system ID of the entity. May be null. */
		public string SystemId = null;

		/** The public ID of the entity. May be null. */
		public string PublicId = null;

		// ********************************************************************
		// Constructors
		// ********************************************************************
		/** Construct a new Entity. */
		public Entity()
		{
		}

		/**
		 * Construct a new Entity and set its name.
		 *
		 * @parameter name The entity's name.
		 */
		public Entity(string name)
		{
			this.Name = name;
		}
	}
}