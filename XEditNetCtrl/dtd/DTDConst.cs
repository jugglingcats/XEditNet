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
	 * DTD constants.
	 *
	 * <p>These are used by DTDParser and DTDSerializer.</p>
	 *
	 * @author Ronald Bourret
	 * @version 2.0
	 */

	internal class DTDConst
	{
		// *********************************************************************
		// DTD Strings
		// *********************************************************************

		public const string KEYWD_ANY      = "ANY";
		public const string KEYWD_ATTLIST  = "ATTLIST";
		public const string KEYWD_CDATA    = "CDATA";
		public const string KEYWD_ELEMENT  = "ELEMENT";
		public const string KEYWD_EMPTY    = "EMPTY";
		public const string KEYWD_ENTITY   = "ENTITY";
		public const string KEYWD_ENTITIES = "ENTITIES";
		public const string KEYWD_FIXED    = "FIXED";
		public const string KEYWD_ID       = "ID";
		public const string KEYWD_IDREF    = "IDREF";
		public const string KEYWD_IDREFS   = "IDREFS";
		public const string KEYWD_IMPLIED  = "IMPLIED";
		public const string KEYWD_NDATA    = "NDATA";
		public const string KEYWD_NMTOKEN  = "NMTOKEN";
		public const string KEYWD_NMTOKENS = "NMTOKENS";
		public const string KEYWD_NOTATION = "NOTATION";
		public const string KEYWD_PCDATA   = "PCDATA";
		public const string KEYWD_PUBLIC   = "PUBLIC";
		public const string KEYWD_REQUIRED = "REQUIRED";
		public const string KEYWD_SYSTEM   = "SYSTEM";
   
		public static readonly string[] KEYWDS = 
	{
		KEYWD_ANY,
		KEYWD_ATTLIST,
		KEYWD_CDATA,
		KEYWD_ELEMENT,
		KEYWD_EMPTY,
		KEYWD_ENTITY,
		KEYWD_ENTITIES,
		KEYWD_FIXED,
		KEYWD_ID,
		KEYWD_IDREF,
		KEYWD_IDREFS,
		KEYWD_IMPLIED,
		KEYWD_NDATA,
		KEYWD_NMTOKEN,
		KEYWD_NMTOKENS,
		KEYWD_NOTATION,
		KEYWD_PCDATA,
		KEYWD_PUBLIC,
		KEYWD_REQUIRED,
		KEYWD_SYSTEM
		};
   
	// *********************************************************************
	// DTD Tokens
	// *********************************************************************

	public const int KEYWD_TOKEN_UNKNOWN  = 0;
	public const int KEYWD_TOKEN_ANY      = 1;
	public const int KEYWD_TOKEN_ATTLIST  = 2;
	public const int KEYWD_TOKEN_CDATA    = 3;
	public const int KEYWD_TOKEN_ELEMENT  = 4;
	public const int KEYWD_TOKEN_EMPTY    = 5;
	public const int KEYWD_TOKEN_ENTITY   = 6;
	public const int KEYWD_TOKEN_ENTITIES = 7;
	public const int KEYWD_TOKEN_FIXED    = 8;
	public const int KEYWD_TOKEN_ID       = 9;
	public const int KEYWD_TOKEN_IDREF    = 10;
	public const int KEYWD_TOKEN_IDREFS   = 11;
	public const int KEYWD_TOKEN_IMPLIED  = 12;
	public const int KEYWD_TOKEN_NDATA    = 13;
	public const int KEYWD_TOKEN_NMTOKEN  = 14;
	public const int KEYWD_TOKEN_NMTOKENS = 15;
	public const int KEYWD_TOKEN_NOTATION = 16;
	public const int KEYWD_TOKEN_PCDATA   = 17;
	public const int KEYWD_TOKEN_PUBLIC   = 18;
	public const int KEYWD_TOKEN_REQUIRED = 19;
	public const int KEYWD_TOKEN_SYSTEM   = 20;
   
	public static readonly int[] KEYWD_TOKENS = 
{
	KEYWD_TOKEN_ANY,
	KEYWD_TOKEN_ATTLIST,
	KEYWD_TOKEN_CDATA,
	KEYWD_TOKEN_ELEMENT,
	KEYWD_TOKEN_EMPTY,
	KEYWD_TOKEN_ENTITY,
	KEYWD_TOKEN_ENTITIES,
	KEYWD_TOKEN_FIXED,
	KEYWD_TOKEN_ID,
	KEYWD_TOKEN_IDREF,
	KEYWD_TOKEN_IDREFS,
	KEYWD_TOKEN_IMPLIED,
	KEYWD_TOKEN_NDATA,
	KEYWD_TOKEN_NMTOKEN,
	KEYWD_TOKEN_NMTOKENS,
	KEYWD_TOKEN_NOTATION,
	KEYWD_TOKEN_PCDATA,
	KEYWD_TOKEN_PUBLIC,
	KEYWD_TOKEN_REQUIRED,
	KEYWD_TOKEN_SYSTEM
};

		public const string KEYWD_MS_IGNORE  = "IGNORE";
		public const string KEYWD_MS_INCLUDE      = "INCLUDE";

}

}