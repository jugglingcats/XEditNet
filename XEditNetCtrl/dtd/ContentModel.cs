using System;
using System.Collections;

namespace XEditNet.Dtd
{
	internal class ContentModel : Group
	{
		public ContentModel()
		{
		}

		public IList GetValidFirstElements()
		{
			return GetValidElements();
		}
	}
}
