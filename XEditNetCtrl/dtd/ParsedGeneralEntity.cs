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
	 * Class representing a parsed general entity.
	 *
	 * @author Ronald Bourret
	 * @version 2.0
	 */

	internal class ParsedGeneralEntity : Entity
	{
		// ********************************************************************
		// Variables
		// ********************************************************************

		/**
		* The value of the parsed general entity.
		*
		* <p>This variable serves as a flag that the entity is an internal entity.
		* It must be null if the systemID or publicID variables inherited from
		* Entity are non-null.</p>
		*/
		public string Value = null;

		// ********************************************************************
		// Constructors
		// ********************************************************************

		/** Construct a ParsedGeneralEntity. */
		public ParsedGeneralEntity()
		{
			this.Type = EntityType.ParsedGeneral;
		}

		/**
		 * Construct a ParsedGeneralEntity and set its name.
		 *
		 * @param name The entity's name.
		 */
		public ParsedGeneralEntity(string name) : base(name)
		{
			this.Type = EntityType.ParsedGeneral;
		}
	}
}