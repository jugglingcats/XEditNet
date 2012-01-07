using System;
using System.Xml;
using System.Xml.Serialization;
using System.Reflection;
using System.Collections;

namespace XEditNet.Xml.Serialization
{
	class SimpleXmlSerializer
	{
		private Type type;

		public SimpleXmlSerializer(Type t)
		{
			type=t;
		}

		public object Deserialize(XmlReader r)
		{
			XmlDocument doc=new XmlDocument();
			doc.Load(r);

			return Deserialize(doc.DocumentElement, type);
		}

		private object Deserialize(XmlElement e, Type t)
		{
			string name=e.Name;

			object o=t.Assembly.CreateInstance(t.FullName);
			if ( o == null )
				throw new InvalidOperationException("Failed to instantiate "+ t.Name);

			if ( o is Enum )
				throw new ArgumentException("Cannot deserialize an enum directly");

			Deserialize(e, o);
			return o;
		}

		private object Deserialize(string val, Type t)
		{
			if ( t.IsEnum )
				return Enum.Parse(t, val);

			if ( t.Equals(typeof(string)) )
				return val;

			if ( t.Equals(typeof(int)) )
				return int.Parse(val);

			if ( t.Equals(typeof(bool)) )
				return bool.Parse(val);

			throw new InvalidOperationException("Cannot deserialize string to type "+t.FullName);
		}

		private object GetValue(MemberInfo mi, object o)
		{
			if ( mi is FieldInfo )
				return ((FieldInfo) mi).GetValue(o);

			if ( mi is PropertyInfo )
				return ((PropertyInfo) mi).GetValue(o, null);

			throw new ArgumentException("Cannot only get values from fields or properties");
		}

		private void SetValue(MemberInfo mi, object o, object val)
		{
			if ( mi is FieldInfo )
				((FieldInfo) mi).SetValue(o, val);
			else if ( mi is PropertyInfo )
				((PropertyInfo) mi).SetValue(o, val, null);
			else
				throw new ArgumentException("Cannot only set values on fields or properties", mi.Name+"("+mi.GetType().ToString()+")");
		}

		private Type GetType(MemberInfo mi)
		{
			if ( mi is FieldInfo )
				return ((FieldInfo) mi).FieldType;

			if ( mi is PropertyInfo )
				return ((PropertyInfo) mi).PropertyType;

			throw new ArgumentException("Cannot only get type from fields or properties");
		}

		private void DeserializeListMember(XmlElement elem, object o, MemberInfo mi, Type memberType)
		{
			if ( !mi.IsDefined(typeof(XmlElementAttribute), true) )
				// can't deserialize elements into list without XmlElementAttribute
				return;

			object[] attrs=mi.GetCustomAttributes(typeof(XmlElementAttribute), true);
			foreach ( XmlElementAttribute xea in attrs )
			{
				if ( xea.ElementName.Equals(elem.Name) )
				{
					IList list=(IList) GetValue(mi, o);
					if ( list == null )
					{
						// TODO: might not be an array list - get from type info
						list=new ArrayList();
						SetValue(mi, o, list);
					}
					object c=this.Deserialize(elem, xea.Type);
					list.Add(c);
					return;
				}
			}
		}

		private void DeserializeSimpleMember(XmlElement elem, object o, MemberInfo mi, Type memberType)
		{
			if ( !mi.IsDefined(typeof(XmlElementAttribute), true) )
			{
				// serialize directly if name matches
				if ( !mi.Name.Equals(elem.Name) )
					return;

				object c=this.Deserialize(elem, memberType);
				SetValue(mi, o, c);

				return;
			}

			object[] attrs=mi.GetCustomAttributes(typeof(XmlElementAttribute), true);
			foreach ( XmlElementAttribute xea in attrs )
			{
				if ( xea.ElementName.Equals(elem.Name) )
				{
					object c=this.Deserialize(elem, xea.Type);
					SetValue(mi, o, c);
					return;
				}
			}
		}

		private void DeserializeSimpleMember(XmlAttribute attr, object o, MemberInfo mi, Type memberType)
		{
			if ( !mi.IsDefined(typeof(XmlAttributeAttribute), true) )
				// have to explicitly declare attributes
				return;

			object c;
			foreach ( XmlAttributeAttribute xaa in mi.GetCustomAttributes(typeof(XmlAttributeAttribute), true) )
			{
				if ( xaa.AttributeName.Equals(attr.Name) )
				{
					c=Deserialize(attr.Value, xaa.Type);
					SetValue(mi, o, c);
					return;
				}
			}

			// serialize directly if name matches
			if ( !mi.Name.Equals(attr.Name) )
				return;

			c=Deserialize(attr.Value, memberType);
			SetValue(mi, o, c);
		}

		private void Deserialize(XmlElement current, object o)
		{
			ArrayList members=new ArrayList();
			members.AddRange(o.GetType().GetFields());
			members.AddRange(o.GetType().GetProperties());

			foreach ( XmlAttribute a in current.Attributes )
			{
				foreach ( MemberInfo mi in members )
				{
					Type t=GetType(mi);
					DeserializeSimpleMember(a, o, mi, t);
				}
			}

			foreach ( XmlNode n in current.ChildNodes )
			{
				if ( n.NodeType != XmlNodeType.Element )
					// ignore text content
					continue;

				XmlElement elem=(XmlElement) n;
				foreach ( MemberInfo mi in members )
				{
					if ( mi.IsDefined(typeof(XmlIgnoreAttribute), true) )
						continue;

					Type t=GetType(mi);
					if ( IsList(t) )
						DeserializeListMember(elem, o, mi, t);
					else
						DeserializeSimpleMember(elem, o, mi, t);
				}
			}
		}

		public void Serialize(XmlWriter w, object o)
		{
			Serialize(w, o, null);
		}
		
		public void Serialize(XmlWriter w, object o, string name)
		{
			if ( o == null )
				return;

			if ( o is Enum )
				throw new ArgumentException("Cannot serialize an enum directly");

			string elemName=name == null ? o.GetType().Name : name;

			w.WriteStartElement(elemName);

			SerializeProperties(w, o, true);
			SerializeFields(w, o, true);
			SerializeProperties(w, o, false);
			SerializeFields(w, o, false);

			w.WriteEndElement();
		}

		private void SerializeProperties(XmlWriter w, object o, bool attributes)
		{
			MemberInfo[] members=o.GetType().GetProperties();
			foreach ( PropertyInfo pi in members )
			{
				if ( !pi.CanWrite )
					// no point serializing stuff we can't write
					continue;

				if ( pi.IsDefined(typeof(XmlIgnoreAttribute), true) )
					continue;

				if ( attributes != pi.IsDefined(typeof(XmlAttributeAttribute), true) )
					continue;

				bool isList=IsList(pi.PropertyType);
				if ( isList && attributes )
					throw new InvalidOperationException("Cannot serialize a list as attributes");

				if ( !isList )
					SerializeSimpleMember(w, o, pi.Name, pi.PropertyType, BindingFlags.GetProperty, attributes);
				else
					SerializeListMember(w, o, pi.Name, BindingFlags.GetProperty);
			}
		}

		private void SerializeFields(XmlWriter w, object o, bool attributes)
		{
			MemberInfo[] members=o.GetType().GetFields();
			foreach ( FieldInfo fi in members )
			{
				if ( fi.IsStatic )
					continue;

				if ( fi.IsDefined(typeof(XmlIgnoreAttribute), true) )
					continue;

				if ( attributes != fi.IsDefined(typeof(XmlAttributeAttribute), true) )
					continue;

				bool isList=IsList(fi.FieldType);

				if ( isList )
				{
					if ( attributes )
						throw new InvalidOperationException("Cannot serialize a list as attributes");

					SerializeListMember(w, o, fi.Name, BindingFlags.GetField);
				}
				else
					SerializeSimpleMember(w, o, fi.Name, fi.FieldType, BindingFlags.GetField, attributes);
			}
		}

		private void SerializeSimpleMember(XmlWriter w, object o, string name, Type type, BindingFlags bf, bool asAttribute)
		{
			object val=o.GetType().InvokeMember(name, bf, null, o, null);
			if ( val == null )
				return;

			// check the type
			if ( val is Enum || 
				type.Equals(typeof(string)) || type.Equals(typeof(int)) || 
				type.Equals(typeof(bool)) )
			{
				if ( asAttribute )
					w.WriteAttributeString(name, val.ToString());
				else
				{
					w.WriteStartElement(name);
					w.WriteString(val.ToString());
					w.WriteEndElement();
				}
			} 
			else
			{
				if ( asAttribute )
					throw new InvalidOperationException("Cannot serialize a complex type as an attribute");

				Serialize(w, val, name);
			}
		}

		private void SerializeListMember(XmlWriter w, object o, string name, BindingFlags bf)
		{
			IList list=(IList) o.GetType().InvokeMember(name, bf, null, o, null);
			foreach ( object li in list )
				Serialize(w, li);
		}

		private bool IsList(Type t)
		{
			if ( t.Equals(typeof(IList)) )
				return true;

			foreach ( Type dt in t.GetInterfaces() )
			{
				if ( dt.Equals(typeof(IList)) )
					return true;
			}
			return false;
		}

		private string GetRootElement(object o)
		{
			object[] attrs=o.GetType().GetCustomAttributes(typeof(XmlRootAttribute), true);
			foreach ( XmlRootAttribute a in attrs )
				// take the first one
				return a.ElementName;

			return o.GetType().Name;
		}
	}
}