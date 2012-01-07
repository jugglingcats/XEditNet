using System;

namespace XEditNet.Dtd
{
	internal class DuplicateNotationException : Exception
	{
		public DuplicateNotationException(string msg) : base(msg)
		{
		}
	}

	internal class ElementNotFoundException : Exception
	{
		public ElementNotFoundException(string msg) : base(msg)
		{
		}
	}

}
