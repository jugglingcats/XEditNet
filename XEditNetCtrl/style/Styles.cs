using System;
using System.Collections;
using System.Drawing;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using XEditNet.Dtd;
using XEditNet.Layout;
using XEditNet.Layout.Tables;
using XEditNet.Location;
using XEditNet.Xml.Serialization;
using Image = XEditNet.Layout.Image;
using WhitespaceHandling = XEditNet.Location.WhitespaceHandling;

namespace XEditNet.Styles
{
	internal struct NamespaceMapping
	{
		[XmlAttribute]
		public string Prefix;
		[XmlAttribute]
		public string NamespaceURI;

		public NamespaceMapping(string prefix, string uri)
		{
			Prefix=prefix;
			NamespaceURI=uri;
		}
	}

	internal struct StyleIntValue
	{
		private int val;
		private bool specified;

		public static implicit operator StyleIntValue(int val)
		{
			return new StyleIntValue(val);
		}

		public static implicit operator int(StyleIntValue v)
		{
			return v.val;
		}

		public StyleIntValue(int val)
		{
			this.val=val;
			this.specified=true;
		}

		public void Cascade(StyleIntValue baseValue)
		{
			if ( !specified && baseValue.specified )
			{
				this.val=baseValue.val;
				this.specified=true;
			}
		}
	}

	internal struct StyleBoolValue
	{
		private bool val;
		private bool specified;

		public static implicit operator StyleBoolValue(bool val)
		{
			return new StyleBoolValue(val);
		}

		public static implicit operator bool(StyleBoolValue v)
		{
			return v.val;
		}

		public StyleBoolValue(bool val)
		{
			this.val=val;
			this.specified=true;
		}

		public void Cascade(StyleBoolValue baseValue)
		{
			if ( !specified )
			{
				this.val=baseValue.val;
				this.specified=baseValue.specified;
			}
		}
	}

	internal class Box
	{
		private StyleIntValue top;
		private StyleIntValue right;
		private StyleIntValue bottom;
		private StyleIntValue left;

		public Box()
		{
		}

		public Box(Box o) : this(o.top, o.right, o.bottom, o.left)
		{
		}

		public Box(StyleIntValue top, StyleIntValue right, StyleIntValue bottom, StyleIntValue left)
		{
			this.top=top;
			this.right=right;
			this.bottom=bottom;
			this.left=left;
		}

		#region Public properties
		[XmlAttribute]
		public int Top 
		{
			get { return top; }
			set { top=value; }
		}
		[XmlAttribute]
		public int Right 
		{
			get { return right; }
			set { right=value; }
		}
		[XmlAttribute]
		public int Bottom
		{
			get { return bottom; }
			set { bottom=value; }
		}
		[XmlAttribute]
		public int Left 
		{
			get { return left; }
			set { left=value; }
		}
		#endregion

		public void Cascade(Box baseBox)
		{
			top.Cascade(baseBox.top);
			right.Cascade(baseBox.right);
			bottom.Cascade(baseBox.bottom);
			left.Cascade(baseBox.left);
		}
	}

	public class FontDesc
	{
		[XmlAttribute]
		public string Family;
		[XmlAttribute]
		public int Size;

		private StyleBoolValue bold;
		private StyleBoolValue italic;
		private StyleBoolValue underline;

		[XmlAttribute]
		public bool Bold
		{
			get { return bold; }
			set { bold=value; }
		}
		[XmlAttribute]
		public bool Italic
		{
			get { return italic; }
			set { italic=value; }
		}
		[XmlAttribute]
		public bool Underline
		{
			get { return underline; }
			set { underline=value; }
		}

		public FontDesc()
		{
		}

		public FontDesc(FontDesc o) : this(o.Family, o.Size, o.bold, o.italic, o.underline)
		{
		}

		internal FontDesc(string family, int size, StyleBoolValue bold, StyleBoolValue italic, StyleBoolValue underline)
		{
			this.Family=family;
			this.Size=size;
			this.bold=bold;
			this.italic=italic;
			this.underline=underline;
		}

		public FontStyle Style
		{
			get 
			{
				FontStyle f=(FontStyle) 0;
				if ( bold )
					f |= FontStyle.Bold;

				if ( italic )
					f |= FontStyle.Italic;

				if ( underline )
					f |= FontStyle.Underline;

				return f;
			}
		}

		public override int GetHashCode()
		{
			int ret=0;
			ret += Family == null ? 0 : Family.GetHashCode();
			ret += Size;
			ret += Bold.GetHashCode();
			ret += Italic.GetHashCode();
			ret += Underline.GetHashCode();
			return ret;
		}

		public override bool Equals(object obj)
		{
			FontDesc other=obj as FontDesc;
			if ( other == null )
				return false;

			if ( !(Size == other.Size && Bold.Equals(other.Bold) && Italic.Equals(other.Italic)
						&& Underline.Equals(other.Underline)) )
				return false;

			return Family == null ? other.Family == null : Family.Equals(other.Family);
		}

		public void Cascade(FontDesc baseStyle)
		{
			if ( this.Family == null || this.Family.Length == 0 )
				this.Family=baseStyle.Family;
			if ( this.Size == 0 )
				this.Size=baseStyle.Size;

			this.bold.Cascade(baseStyle.bold);
			this.italic.Cascade(baseStyle.italic);
			this.underline.Cascade(baseStyle.underline);
		}
	}

	internal abstract class Style
	{
		private XPathExpression xpathExpression;
		private string xpath;
		private string name;
		private int fontAscent;
		private int fontHeight;
		private FontDesc desc=new FontDesc();
		private StyleBoolValue empty;
		private StyleBoolValue pre;

		protected Stylesheet stylesheet;

		public Box Padding=new Box();
		public Box Margin=new Box();

		protected Style()
		{
		}

		[XmlAttribute]
		public string XPath
		{
			get { return xpath; }
			set { xpath=value; }
		}

		[XmlAttribute]
		public bool Empty
		{
			get { return empty; }
			set { empty=value; }
		}

		[XmlAttribute]
		public bool Pre
		{
			get { return pre; }
			set { pre=value; }
		}

		public int Top
		{
			get { return Padding.Top+Margin.Top; }
		}
		public int Right
		{
			get { return Padding.Right+Margin.Right; }
		}
		public int Bottom
		{
			get { return Padding.Bottom+Margin.Bottom; }
		}
		public int Left
		{
			get { return Padding.Left+Margin.Left; }
		}

		protected Style(Style s)
		{
			this.desc=new FontDesc(s.desc);
			this.Margin=new Box(s.Margin);
			this.Padding=new Box(s.Padding);
			this.Empty=s.Empty;
			this.Pre=s.Pre;
		}

		public abstract Style Clone();
		internal abstract IReflowObject CreateReflowObject(IContainer parent, XmlElement e);

		public void Bind(IGraphics gr, Stylesheet s)
		{
			stylesheet=s;
			FontDesc fd=desc;
			if ( fd.Family == null )
				fd=s.DefaultFont;

			object fontHandle=gr.GetFontHandle(fd);
			gr.PushFont(fontHandle);
			fontAscent=gr.GetFontAscent();
			fontHeight=gr.GetFontHeight();
			gr.PopFont();
		}

		public int FontAscent
		{
			get { return fontAscent; }
		}

		public int FontHeight
		{
			get { return fontHeight; }
		}

		public Stylesheet Stylesheet 
		{
			get { return stylesheet; }
			set { stylesheet=value; }
		}

		[XmlElement("Font", typeof(FontDesc))]
		public FontDesc FontDesc
		{
			get { return desc; }
			set { desc=value; }
		}

		[XmlIgnore]
//		public object FontHandle
//		{
//			get { return fontHandle; }
//			set { fontHandle=value; }
//		}

		[XmlAttribute]
		public string Name
		{
			set { name=value; }
			get { return name; }
		}

		public bool IsMatch(XPathNavigator xpn, XmlNamespaceManager xnm)
		{
			if ( xpathExpression == null )
			{
				xpathExpression=xpn.Compile(xpath);
				xpathExpression.SetContext(xnm);
			}

			return xpn.Matches(xpathExpression);

//			Debug.Assert(e.Name.Equals(name), "IsMatch called for incorrect element!");
//			// TODO: L: bit messy
//			return Conditions == null || Conditions.Conditions ==null || 
//				Conditions.Conditions.Count == 0 || Conditions.MatchesAll(e);
		}

		public void Cascade(Style baseStyle)
		{
			Padding.Cascade(baseStyle.Padding);
			Margin.Cascade(baseStyle.Margin);
			FontDesc.Cascade(baseStyle.FontDesc);
		}
	}

	internal class CustomProperty
	{
		public CustomProperty()
		{
		}
		public CustomProperty(CustomProperty o)
		{
			this.Name=o.Name;
			this.Value=o.Value;
		}
		[XmlAttribute]
		public string Name;
		[XmlAttribute]
		public string Value;
	}

	internal class CustomStyle : Style
	{
		[XmlAttribute]
		public string Class;

		[XmlElement("Property", typeof(CustomProperty))]
		public ArrayList Properties=new ArrayList();

		public CustomStyle()
		{
		}

		public CustomStyle(Style s) : base(s)
		{
			CustomStyle other=s as CustomStyle;
			if ( other == null )
				return;

			foreach ( CustomProperty p in other.Properties )
				Properties.Add(new CustomProperty(p));

			this.Class=other.Class;
		}

		internal override IReflowObject CreateReflowObject(IContainer parent, XmlElement e)
		{
			if ( Class == null || Class.Length == 0 )
				throw new InvalidOperationException("Class attribute must be specified for custom style types");

			string actualClass="XEditNet.Layout."+Class;

			object[] args=new object[] {parent, e, this};
			Assembly a=GetType().Assembly;

			object o=a.CreateInstance(actualClass, false, BindingFlags.CreateInstance, null, args, null, null);
			if ( o == null )
				throw new InvalidOperationException("Could not find class with name "+actualClass);

			IReflowObject ro=o as IReflowObject;
			if ( o == null )
				throw new InvalidOperationException("Object "+actualClass+" does not implement IReflowObject");

			return ro;
		}

		public override Style Clone()
		{
			return new CustomStyle(this);
		}
	}

	internal class TableStyle : Style
	{
		public TableStyle()
		{
		}

		public TableStyle(Style s) : base(s)
		{
		}

		internal override IReflowObject CreateReflowObject(IContainer parent, XmlElement e)
		{
			return new Table(parent, e, this);
		}

		public override Style Clone()
		{
			return new TableStyle(this);
		}
	}

	internal class TableRowStyle : Style
	{
		public TableRowStyle()
		{
		}

		public TableRowStyle(Style s) : base(s)
		{
		}

		internal override IReflowObject CreateReflowObject(IContainer parent, XmlElement e)
		{
			Table tbl=FindParentTable(parent);
			// either parent must be a table or row group
			if ( tbl != null && tbl.IsValid )
				return new TableRow(parent, e, this);

			return new BlockImpl(parent, e, this);
		}

		private Table FindParentTable(IContainer parent)
		{
			if ( parent is Table )
				return (Table) parent;

			parent=parent.Parent;
			if ( parent == null )
				return null;

			return parent as Table;
		}

		public override Style Clone()
		{
			return new TableRowStyle(this);
		}
	}

	internal class TableRowGroupStyle : Style
	{
		public TableRowGroupStyle()
		{
		}

		public TableRowGroupStyle(Style s) : base(s)
		{
		}

		internal override IReflowObject CreateReflowObject(IContainer parent, XmlElement e)
		{
			return new BlockImpl(parent, e, this);
		}

		public override Style Clone()
		{
			return new TableRowGroupStyle(this);
		}
	}


	internal class TableCellStyle : Style
	{
		public TableCellStyle()
		{
		}

		public TableCellStyle(Style s) : base(s)
		{
		}

		internal override IReflowObject CreateReflowObject(IContainer parent, XmlElement e)
		{
			TableRow tblrow=FindParentRow(parent);
			// parent must be a row
			if ( tblrow != null )
				return new TableCell(parent, e, this);

			return new BlockImpl(parent, e, this);
		}

		private TableRow FindParentRow(IContainer parent)
		{
			return parent as TableRow;
		}

		public override Style Clone()
		{
			return new TableCellStyle(this);
		}
	}


	internal class InlineStyle : Style
	{
		public InlineStyle()
		{
		}

		public InlineStyle(Style s) : base(s)
		{
		}

		internal override IReflowObject CreateReflowObject(IContainer parent, XmlElement e)
		{
			return new InlineImpl(parent, e, this);
		}

		public override Style Clone()
		{
			return new InlineStyle(this);
		}
	}

	internal class BlockStyle : Style
	{
		public BlockStyle()
		{
		}

		public BlockStyle(Style s) : base(s)
		{
		}

		internal override IReflowObject CreateReflowObject(IContainer parent, XmlElement e)
		{
			return new BlockImpl(parent, e, this);
		}

		public override Style Clone()
		{
			return new BlockStyle(this);
		}
	}

	internal class ImageStyle : BlockStyle
	{
		[XmlAttribute]
		public string SourceAttribute;

		public ImageStyle()
		{
		}

		public ImageStyle(Style s) : base(s)
		{
		}

		internal override IReflowObject CreateReflowObject(IContainer parent, XmlElement e)
		{
			return new Image(parent, e, this);
		}

		public override Style Clone()
		{
			ImageStyle ret=new ImageStyle(this);
			ret.SourceAttribute=this.SourceAttribute;
			return ret;
		}
	}

	internal enum TagViewMode
	{
		Full,
		None
	}

	internal class Stylesheet : IWhitespaceClassifier
	{
		private Hashtable fontHandles=new Hashtable();
//		private object defaultFontHandle;
//		private object tagFontHandle;
		private InlineStyle inlineStyle;
		private BlockStyle blockStyle;
		private InlineStyle commentStyle;
		private XmlNamespaceManager nsMgr;

		[XmlElement("NamespaceMapping", typeof(NamespaceMapping))]
		public ArrayList Namespaces=new ArrayList();

		[
			XmlElement("Block", typeof(BlockStyle)),
			XmlElement("Inline", typeof(InlineStyle)),
			XmlElement("Image", typeof(ImageStyle)),
			XmlElement("Table", typeof(TableStyle)),
			XmlElement("Row", typeof(TableRowStyle)),
			XmlElement("Cell", typeof(TableCellStyle)),
			XmlElement("RowGroup", typeof(TableRowGroupStyle)),
			XmlElement("Custom", typeof(CustomStyle))
		]
		public ArrayList Styles=new ArrayList();

		public FontDesc DefaultFont;
		public FontDesc TagFont;

		public Box DefaultPadding;
		public TagViewMode TagMode=TagViewMode.Full;
		public Color HighlightColor=Color.Black;

		public Stylesheet()
		{
			DefaultFont=new FontDesc("Times New Roman", 12, false, false, false);
			TagFont=new FontDesc("Verdana", 9, false, false, false);

			DefaultPadding=new Box(0, 0, 0, 15);

			BindStyles(new NameTable());
		}

//		~Stylesheet()
//		{
//			Win32Util.DeleteObject(tagFontHandle);
//			Win32Util.DeleteObject(defaultFontHandle);
//			foreach ( IntPtr h in fontHandles.Values )
//				Win32Util.DeleteObject(h);
//		}

		public Style GetCommentStyle(IGraphics gr)
		{
			// TODO: L: optimise - don't need to create each time
			Style ret=commentStyle.Clone();
			ret.Bind(gr, this);
			return ret;
		}

		public static Stylesheet Load(string filename, XmlNameTable tbl)
		{
			SimpleXmlSerializer serializer = 
				new SimpleXmlSerializer(typeof(Stylesheet));

			XmlTextReader xtr=new XmlTextReader(filename);
			try 
			{
				Stylesheet s=(Stylesheet) serializer.Deserialize(xtr);

				s.BindStyles(tbl);
				return s;
			}
			finally 
			{
				xtr.Close();
			}
		}

		public void BindStyles(XmlNameTable tbl)
		{
//			Font def;
//			
//			def=new Font(DefaultFont.Family, DefaultFont.Size, DefaultFont.Style);
//			this.defaultFontHandle=def.ToHfont();
//			def=new Font(TagFont.Family, TagFont.Size, TagFont.Style);
//			this.tagFontHandle=def.ToHfont();

			inlineStyle=new InlineStyle();
			inlineStyle.Padding=DefaultPadding;
			inlineStyle.FontDesc=this.DefaultFont;

			blockStyle=new BlockStyle();
			blockStyle.Padding=DefaultPadding;
			blockStyle.FontDesc=this.DefaultFont;

			commentStyle=new InlineStyle();
			commentStyle.FontDesc=new FontDesc("Courier New", 9, false, false, false);
			commentStyle.Pre=true;

			nsMgr=new XmlNamespaceManager(tbl);
			foreach ( NamespaceMapping nm in Namespaces )
				nsMgr.AddNamespace(nm.Prefix, nm.NamespaceURI);	
		}

		public Style Cascade(IGraphics gr, Style parentStyle, Style changes)
		{
			Style ret;
			ret=changes.Clone();
			if ( parentStyle != null )
				ret.Cascade(parentStyle);

			ret.Bind(gr, this);
			return ret;
		}

		internal Style GetStyle(IGraphics gr, XmlElement e, ElementType et)
		{
			return GetStyle(gr, null, e, et);
		}

		public Type GetStyleType(XmlElement e)
		{
			foreach ( Style s in Styles )
			{
				if ( s.IsMatch(e.CreateNavigator(), nsMgr) )
					return s.GetType();
			}
			// TODO: L: this is wrong according to GetStyle (won't always match)
			//			but this is currently only used to find table classes, so doesn't matter
			return null;
		}

		internal Style GetStyle(IGraphics gr, Style parentStyle, XmlElement e, ElementType et) 
		{
			Style ret=parentStyle == null ? blockStyle : parentStyle;

			bool foundMatch=false;
			foreach ( Style s in Styles )
			{
				if ( s.IsMatch(e.CreateNavigator(), nsMgr) )
				{
					ret=Cascade(gr, ret, s);
					foundMatch=true;
				}
			}

			bool emptyModel=et != null && et.ContentType == ElementContentType.Empty;

			if ( !foundMatch )
			{
				// TODO: M: could optimise this perhaps
				XmlElement parent=e.ParentNode as XmlElement;
				if ( parent == null )
					ret=Cascade(gr, ret, new BlockStyle());
				else if ( HasText(parent) || emptyModel )
					ret=Cascade(gr, ret, new InlineStyle());
				else
					ret=Cascade(gr, ret, new BlockStyle());
			}

			if ( e.HasChildNodes )
				// if element has child nodes, it cannot be shown as empty
				ret.Empty=false;
			else if ( emptyModel )
				// empty element in DTD so flag as such
				ret.Empty=true;

			return ret;
		}

		private bool HasText(XmlElement e)
		{
			foreach ( XmlNode n in e.ChildNodes )
			{
				if ( n.NodeType == XmlNodeType.Text )
					return true;
			}

			return false;
		}

		public WhitespaceHandling Classify(XmlNode n)
		{
			while ( n != null )
			{
				XmlElement e=n as XmlElement;
				if ( e != null )
				{
					bool found=false;
					bool pre=false;
					foreach ( Style s in Styles )
					{
						if ( s.IsMatch(e.CreateNavigator(), nsMgr) )
						{
							// TODO: M: test that processing order is the same when rendering
							//			(ie. that last definition takes priority)
							found=true;
							pre=s.Pre;
						}
					}
					if ( found )
						return pre ? WhitespaceHandling.Preserve : WhitespaceHandling.Default;
				}
				n=XmlUtil.GetParentNode(n);
			}
			return WhitespaceHandling.Default;
		}
	}
}
