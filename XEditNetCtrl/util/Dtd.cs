using System;
using System.Windows.Forms;
using XEditNet.Dtd;

namespace XEditNet.Util
{
	/// <summary>
	/// Summary description for DtdUtil.
	/// </summary>

	public struct ElementInfo
	{
		public string LocalName;
		public bool IsRootElement;
	}

	public class DtdInfo
	{
		public static ElementInfo[] GetAllElements(Uri dtdUri)
		{
			DTDParser p=new DTDParser();
			InputSource ins=new InputSource(dtdUri, dtdUri.AbsoluteUri);
			DocumentType t=p.parseExternalSubset(ins, true);
			ElementInfo[] ret=new ElementInfo[t.ElementTypes.Length];
			int n=0;
			foreach ( ElementType et in t.ElementTypes )
			{
				ret[n].LocalName=et.Name.LocalName;
				ret[n].IsRootElement=et.IsRootElement;
				n++;
			}

			return ret;
		}
	}
}
