using System;
using System.Collections;

namespace XEditNet
{
	internal class PerfLog
	{
		private static Stack marks=new Stack();

		public static void Mark()
		{
#if DEBUG
			marks.Push(DateTime.Now);
#endif
		}

		public static void Write(string format, params object[] args)
		{
#if DEBUG
			string msg=string.Format(format, args);
			Console.WriteLine("{0} - {1}", msg, DateTime.Now - (DateTime) marks.Pop());
#endif
		}
	}
}
