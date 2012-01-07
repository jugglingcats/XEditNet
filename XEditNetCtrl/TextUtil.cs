using System;

namespace XEditNet
{
	internal class TextUtil
	{
		public static int GetStartSpaceCount(string text, bool preserveWs)
		{
			int ret=0;
			while ( ret < text.Length && IsWhiteSpace(text[ret]) )
				ret++;

			return ret;
		}

		public static int GetEndSpaceCount(string text)
		{
			int ret=0;
			int ptr=text.Length-1;
			while ( ptr >= 0 && IsWhiteSpace(text[ptr--]) )
				ret++;
		
			return ret;
		}
		
		public static bool IsWhiteSpace(char c)
		{
			return c == ' ' || c == '\n' || c == '\r' || c == '\t';
		}

		public static bool IsNewline(char c)
		{
			return c == '\n' || c == '\r';
		}

		public static char[] WhiteSpaceChars
		{
			get
			{
				return new char[] {' ', '\n', '\r', '\t'};
			}
		}
	}
}
