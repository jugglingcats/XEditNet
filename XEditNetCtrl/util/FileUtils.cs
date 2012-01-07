using System;
using System.IO;
using System.Windows.Forms;

namespace XEditNet.Util
{
	/// <summary>
	/// Summary description for FileUtils.
	/// </summary>
	public class FileUtils
	{
		private static readonly string dir=Environment.CurrentDirectory;

		public static FileInfo FindFile(string name)
		{
			// check relative to working dir, then assembly
			string fullPath=dir+"/"+name;

			FileInfo fi=new FileInfo(fullPath);
			if ( fi.Exists )
				return fi;

			Uri baseUri=new Uri(typeof(FileUtils).Assembly.CodeBase);
			Uri newUri=new Uri(baseUri, name);
			fi=new FileInfo(newUri.AbsolutePath);
			if ( fi.Exists )
				return fi;

			return null;
		}

		public static FileInfo FindFile(string name, Uri baseUri)
		{
			if ( baseUri == null )
				return FindFile(name);

			Uri uri=new Uri(baseUri, name);
			FileInfo fi=new FileInfo(uri.LocalPath);

			if ( !fi.Exists )
				return FindFile(name);

			return fi;
		}
	}
}
