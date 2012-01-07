using System;

namespace XEditNet
{
	internal class XEditNetCommandException : Exception
	{
		public XEditNetCommandException(string s, Exception e) : base(s, e)
		{
		}
	}

	internal class XEditNetLayoutException : Exception
	{
		public XEditNetLayoutException(string s, Exception e) : base(s, e)
		{
		}
	}
}
