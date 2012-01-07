using System;
using System.IO;
using System.Xml;

namespace XEditNet
{
	internal class CustomXmlResolver : XmlUrlResolver
	{
		Uri baseUri=null;

		public CustomXmlResolver()
		{
		}

		public CustomXmlResolver(Uri baseUri)
		{
			this.baseUri=baseUri;
		}

		public CustomXmlResolver(XmlDocument doc)
		{
			if ( doc.BaseURI != null && doc.BaseURI.Length > 0 )
				this.baseUri=new Uri(doc.BaseURI);
		}

		public override System.Uri ResolveUri(Uri baseUri, string relativeUri)
		{
			if ( baseUri == null )
				baseUri=this.baseUri;

			Uri uri=base.ResolveUri(baseUri, relativeUri);
			if ( !uri.IsFile )
			{
				String[] segs=uri.Segments;
				int n=segs.Length - 1;
				string attempt="";

				while ( n > 0 )
				{
					attempt=segs[n--]+attempt;
					Uri tryUri=new Uri(baseUri, attempt);
					if ( tryUri.IsFile && File.Exists(tryUri.AbsolutePath) )
						return tryUri;
				}
			}
			return base.ResolveUri(baseUri,relativeUri);
		}	
	}
}
