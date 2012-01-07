using System;
using System.Xml;
using System.Collections;

namespace XEditNet.Layout
{
	internal class BlockHelper
	{
		public static IBlock FindBlock(IBlock block, XmlElement findElement, ICollection childRegions)
		{
			if ( !XmlUtil.HasAncestor(findElement, block.ElementNode) )
				return null;

			foreach ( object o in childRegions )
			{
				IBlock b = o as IBlock;
				if ( b == null )
					continue;

				IBlock ret=b.FindBlock(findElement);
				if ( ret != null )
					return ret;
			}

			// we are ancestor but not found in any child blocks
			return block;
		}

		public static XmlElement ProcessSizeChange(IBlock child)
		{
			int w=child.Width;
			int h=child.Height;

			child.RecalcBounds();

			if ( w != child.Width || h != child.Height )
			{
				IBlock p=(IBlock) child.Parent;
				if ( p != null )
					return BlockHelper.ProcessSizeChange(p);
			}
			return child.ElementNode;
		}
	}
}
