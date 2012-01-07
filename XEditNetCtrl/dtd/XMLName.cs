using System;
using System.Xml;

namespace XEditNet.Dtd
{
	/// <summary>
	/// Represents an XML name. Designed to mirror the functionality provided
	/// by the .NET XmlNode naming exposed via LocalName, Name, Prefix and NamespaceURI
	/// </summary>
	public class XmlName
	{
		private string prefix;
		private string localName;
		private string uri;
		private string uniqueName;

		/// <summary>
		/// Initialises a new instance of the XmlName class.
		/// </summary>
		/// <param name="prefix">The prefix. Should be null if the prefix is not present.</param>
		/// <param name="localName">The local name. Cannot be null.</param>
		/// <param name="uri">The namespace URI. Should be null if the namespace is not specified.</param>
		public XmlName(string prefix, string localName, string uri)
		{
			this.prefix=prefix == null ? string.Empty : prefix;
			this.localName=localName;
			this.uri=uri == null ? string.Empty : uri;

			if ( localName == null )
				throw new ArgumentException("Local name must not be null", "localName");

			CreateUniqueName();
		}

		/// <summary>
		/// Initialises a new instance of the XmlName class based on an XmlNode. The
		/// name details are copied from the specified node.
		/// </summary>
		/// <param name="n">The XmlNode to use as the basis for this XmlName.</param>
		public XmlName(XmlNode n)
		{
			this.prefix=n.Prefix;
			this.localName=n.LocalName;
			this.uri=string.Empty;

			CreateUniqueName();
		}

		/// <summary>
		/// Initialises a new instance of the XmlName class based on a qualified name.
		/// A qualified name can contain a prefix but when constructing an XmlName
		/// using this method the namespace URI is always empty.
		/// </summary>
		/// <param name="qualifiedName">The qualified name to decompose into prefix and local name.</param>
		public XmlName(string qualifiedName)
		{
			this.uri=string.Empty;
			string[] parts=qualifiedName.Split(':');
			if ( parts.Length == 1 )
			{
				prefix=string.Empty;
				localName=qualifiedName;
			}
			else if ( parts.Length == 2 )
			{
				prefix=parts[0];
				localName=parts[1];
		
				if ( prefix.Length == 0 )
					throw new ArgumentException("Prefix cannot be empty", "qualifiedName");
		
				if ( localName.Length == 0 )
					throw new ArgumentException("LocalName cannot be empty", "qualifiedName");
			}
			else
				throw new ArgumentException("Invalid XML name", "qualified");

			CreateUniqueName();
		}

		private void CreateUniqueName()
		{
			if ( prefix.Length == 0 && uri.Length == 0 )
				uniqueName=localName;
			else if ( prefix.Length == 0 )
				uniqueName="{"+uri+"}"+localName;
			else
				uniqueName="{"+prefix+":"+uri+"}"+localName;
		}

		/// <summary>
		/// Gets the qualified name. This is the name including the prefix if the prefix is not empty,
		/// or the local name otherwise.
		/// </summary>
		public string QualifiedName
		{
			get
			{
				if ( prefix.Length == 0 )
					return localName;

				return prefix+":"+localName;
			}
		}

		/// <summary>
		/// Gets the local name. This is the name without any prefix.
		/// </summary>
		public string LocalName
		{
			get
			{
				return localName;
			}
		}

		/// <summary>
		/// Gets the prefix. This is the prefix without trailing colon, eg. "ns", or an empty
		/// string if no prefix exists.
		/// </summary>
		public string Prefix
		{
			get
			{
				return prefix;
			}
		}

		/// <summary>
		/// Gets the namespace URI, or an empty string if no namespace exists.
		/// </summary>
		public string NamespaceURI
		{
			get
			{
				return uri;
			}
		}

		/// <seebase/>
		public override bool Equals(object obj)
		{
			XmlName other=obj as XmlName;
			if ( other == null )
				return false;

			return other.uniqueName.Equals(uniqueName);
		}

		/// <seebase/>
		public override int GetHashCode()
		{
			return uniqueName.GetHashCode();
		}

		/// <seebase/>
		public override string ToString()
		{
			return uniqueName;
		}
	}
}

