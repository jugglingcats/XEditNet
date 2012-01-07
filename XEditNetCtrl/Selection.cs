using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using XEditNet.Layout;
// TODO: H: copy of entity very slow because it inserts all child nodes into fragment
// TODO: H: extend out in empty element selects too much

namespace XEditNet.Location
{
	/// <summary>
	/// Represents the method that will handle the SelectionChanged event
	/// </summary>
	public delegate void SelectionChangedEventHandler(object sender, SelectionChangedEventArgs e);

	/// <summary>
	/// Provides data for the SelectionChanged event
	/// </summary>
	public class SelectionChangedEventArgs
	{
		/// <summary>
		/// The previous selection.
		/// </summary>
		public Selection OldSelection;
		/// <summary>
		/// The new selection.
		/// </summary>
		public Selection NewSelection;
	}

	internal enum WhitespaceHandling
	{
		Default,
		Preserve
	}

	internal interface IWhitespaceClassifier
	{
		WhitespaceHandling Classify(XmlNode n);
	}

	public class SelectionManager
	{
		private IWhitespaceClassifier wc;

		internal SelectionManager(IWhitespaceClassifier wc)
		{
			this.wc=wc;
		}

		public static Selection Change(Selection startSelection, XmlElement original, XmlElement replacement)
		{
			foreach ( XmlAttribute a in original.Attributes )
			{
				if ( a.Specified )
					replacement.SetAttributeNode((XmlAttribute) a.CloneNode(true));
			}
	
			Queue q=new Queue();
			foreach ( XmlNode n in original.ChildNodes )
				q.Enqueue(n);
	
			while ( q.Count > 0 )
				replacement.AppendChild((XmlNode) q.Dequeue());
	
			original.ParentNode.ReplaceChild(replacement, original);
			Selection sel=startSelection;
			if ( sel.IsEmpty || sel.Start.Node.Equals(original) )
				// must be end tag
				sel=new Selection(new ElementSelectionPoint(replacement, TagType.EndTag), sel.End);

			return sel;
		}


		public static Selection Split(SelectionPoint sp)
		{
			XmlElement newElem;

			// end tag is special case - create empty copy and position inside
			if ( sp is MarkupSelectionPoint )
			{
				if ( sp.Node.Equals(sp.Node.OwnerDocument.DocumentElement) )
					// cannot split the document element
					return null;

				MarkupSelectionPoint msp=(MarkupSelectionPoint) sp;
				if ( msp.Type == TagType.EndTag )
				{
					newElem=XmlUtil.CopyElement((XmlElement) msp.Node);
					msp.Node.ParentNode.InsertAfter(newElem, msp.Node);

					sp=new ElementSelectionPoint(newElem, TagType.EndTag);
					return new Selection(sp);
				}
			}

			XmlElement parent=sp.Node.ParentNode.ParentNode as XmlElement;
			if ( parent == null )
				return null;

			// either start tag or text location
			SelectionPoint start=sp;
			if ( sp is TextSelectionPoint )
			{
				TextSelectionPoint tsp=(TextSelectionPoint) sp;
				if ( tsp.Index > 0 )
				{
					XmlText t=(XmlText) tsp.Node;
					XmlText x=t.SplitText(tsp.Index);
					start=new TextSelectionPoint(x, 0);
				} 
			} 

			Queue q=new Queue();
			XmlNode n=start.Node;

			bool first=n.PreviousSibling == null;

			while ( n != null )
			{
				q.Enqueue(n);
				n=n.NextSibling;
			}

			XmlElement copySource=(XmlElement) start.Node.ParentNode;

			newElem=XmlUtil.CopyElement(copySource);
			if ( first )
			{
				// special case for when the cursor is at the start
				// of an element - this ensures the attributes are kept
				// by the existing node with all the children, which is
				// more natural
				parent.InsertBefore(newElem, copySource);
			} 
			else
			{
				while ( q.Count > 0 )
					newElem.AppendChild((XmlNode) q.Dequeue());

				parent.InsertAfter(newElem, copySource);
			}

			return new Selection(start);
		}

		public Selection Cut(Selection sel)
		{
			if ( sel.IsRange && sel.IsBalanced )
			{
				XmlDocumentFragment l;
				Selection ret=DeleteRange(sel, out l);

				XmlDocument doc=sel.Start.Node.OwnerDocument;
				CreateClipboardFragment(doc, l);

				return ret;
			}
			return sel;
		}

		public static void Copy(Selection sel)
		{
			if ( sel.IsRange && sel.IsBalanced )
			{
				XmlDocumentFragment l=CopyRange(sel);
				CreateClipboardFragment(sel.Start.Node.OwnerDocument, l);
			}
		}

		private static void CreateClipboardFragment(XmlDocument doc, XmlDocumentFragment frag)
		{
			StringWriter sw=new StringWriter();
			XmlTextWriter xtw=new XmlTextWriter(sw);
				
			if ( doc.DocumentType != null )
			{
				xtw.WriteDocType("XEditNet", doc.DocumentType.PublicId, 
					doc.DocumentType.SystemId, doc.DocumentType.InternalSubset);
			}

			xtw.WriteStartElement("XEditNet", "ClipboardData", "http://xeditnet.com/");
			string uri=frag.FirstChild.GetNamespaceOfPrefix("");
			xtw.WriteAttributeString("xmlns", uri);

			frag.WriteTo(xtw);
			xtw.WriteEndElement();
			xtw.Close();

			Console.WriteLine(sw.ToString());
			Clipboard.SetDataObject(sw.ToString());
		}

		public Selection Paste(Selection sel)
		{
			IDataObject data=Clipboard.GetDataObject();

			string text=data.GetData(DataFormats.Text) as string;
			if ( text == null )
				return sel;

			sel=sel.Normalise();
			if ( sel.IsRange )
			{
				if ( !sel.IsBalanced )
					return sel;

				sel=DeleteRange(sel);
			}

			try 
			{
				PerfLog.Mark();

				StringReader sr=new StringReader(text);
				XmlTextReader xtw=new XmlTextReader(sr);
				// TODO: H: try to read with no resolver, then attempt with a resolver - will fail for entities
				xtw.XmlResolver=null; // new CustomXmlResolver(sel.Start.Node.OwnerDocument);
				XmlDocument doc=new XmlDocument();
				doc.Load(xtw);

				XmlDocument ours=sel.Start.Node.OwnerDocument;
				if ( doc.DocumentElement.NamespaceURI.Equals("http://xeditnet.com/") )
				{
					Queue queue=new Queue();
					// this is our own clipboard data
					// TODO: L: check that element name is ClipboardData
					foreach ( XmlNode n in doc.DocumentElement.ChildNodes )
						queue.Enqueue(n);

					while ( queue.Count > 0 )
					{
						XmlNode n=(XmlNode) queue.Dequeue();
						XmlNode newOne=ours.ImportNode(n, true);
						sel=Insert(sel.Start, newOne);
					}
				}
				else
				{
					sel=Insert(sel.Start, ours.ImportNode(doc.DocumentElement, true));
				}

				PerfLog.Write("Paste complete");
			} 
			catch (Exception ex)
			{
				Logger.Log("Failed to read from clipboard: {0}", ex.Message);
				// could not read as XML, so simply paste as text
				Insert(sel.Start, text);
			}
			return sel;
		}

		public static XmlElement GetInsertionContext(SelectionPoint selectionPoint)
		{
			XmlNode nval;
			return GetInsertionContext(selectionPoint, out nval);
		}
		
		public static XmlElement GetInsertionContext(SelectionPoint selectionPoint, out XmlNode node)
		{
			XmlElement p=selectionPoint.Node.ParentNode as XmlElement;
			node=selectionPoint.Node;
			MarkupSelectionPoint msp=selectionPoint as MarkupSelectionPoint;
			if ( msp != null && msp.Type == TagType.EndTag )
			{
				// cursor is at end tag, so no insert before node
				p=(XmlElement) selectionPoint.Node;
				node=null;
			}
			return p;
		}

		public Selection ExtendOut(Selection sel)
		{
			SelectionPoint start=sel.Start;
			SelectionPoint end=sel.End;

			if ( !sel.IsRange )
				end=start;

			MarkupSelectionPoint msp;
			msp=start as MarkupSelectionPoint;
			if ( msp != null && msp.Type == TagType.EndTag )
				start=PreviousSelectionPoint(start);

			msp=end as MarkupSelectionPoint;
			if ( msp != null && msp.Type == TagType.EndTag )
				end=PreviousSelectionPoint(end);

			XmlNode n=XmlUtil.FindCommonAncestor(start.Node, end.Node);
			if ( n.Equals(start.Node) && n.Equals(end.Node) )
				n=n.ParentNode;

			XmlElement e=n as XmlElement;
			if ( n.NodeType != XmlNodeType.Element )
				// assumes we found text or similar element
				e=(XmlElement) n.ParentNode;

			if ( e == null )
			{
				// we're looking at the document node, so select
				// whole doc
				Debug.Assert(n.NodeType == XmlNodeType.Document);
				e=((XmlDocument) n).DocumentElement;
			}

			start=new ElementSelectionPoint(e, TagType.StartTag);
			start=NextSelectionPoint(start);
			end=new ElementSelectionPoint(e, TagType.EndTag);

			if ( start.Equals(sel.Start) && end.Equals(sel.End) && !e.Equals(e.OwnerDocument.DocumentElement) )
			{
				// expand to include the tags
				start=new ElementSelectionPoint(e, TagType.StartTag);
				end=new ElementSelectionPoint(e, TagType.EndTag);
				end=NextSelectionPoint(end);
			}
//			start=Validate(ret.Start);
//			end=Validate(ret.End);
			return new Selection(start, end);
		}

		public Selection Wrap(Selection sel, XmlElement e)
		{
			if ( !sel.IsRange || !sel.IsBalanced )
				throw new ArgumentException("Selection must be balanced");

			XmlDocumentFragment frag;
			sel=DeleteRange(sel, out frag);
			Queue queue=new Queue();
			foreach ( XmlNode n in frag.ChildNodes )
				queue.Enqueue(n);

			while ( queue.Count > 0 )
				e.AppendChild((XmlNode) queue.Dequeue());

			Insert(sel.Start, e);

			SelectionPoint sp1=CreateSelectionPoint(e.FirstChild, false);
			SelectionPoint sp2=CreateSelectionPoint(e, true);
			return new Selection(sp1, sp2);
		}

		public Selection Delete(Selection sel)
		{
			sel=sel.Normalise();
			if ( sel.IsRange )
			{
				if ( sel.IsBalanced )
					return DeleteRange(sel);

				// TODO: L: not sure what this indicates
				return null;
			}

			SelectionPoint sp=NextSelectionPoint(sel.Start);
			if ( sp.Equals(sel.Start) )
				// nothing to do -- cannot move right
				return sel;

			return Backspace(new Selection(sp));
		}

		public Selection Backspace(Selection sel)
		{
			if ( sel.IsRange )
			{
				sel=sel.Normalise();

				if ( sel.IsBalanced )
					return DeleteRange(sel);

				// this indicates that nothing happened
				return null;
			}

			if ( sel.IsEmpty )
				return null;

			SelectionPoint prev=PreviousSelectionPoint(sel.Start, false);
			if ( prev.Equals(sel.Start) )
				return null;

			if ( prev.IsTag && sel.Start.IsTag && sel.Start.Node.Equals(prev.Node) )
			{
				// special case - backspace over start tag of empty tag
				SelectionPoint ret=NextSelectionPoint(sel.Start);
				sel.Start.Node.ParentNode.RemoveChild(sel.Start.Node);
				return new Selection(ret);
			}
			if ( prev.IsTag )
			{
				ElementSelectionPoint esp=(ElementSelectionPoint) prev;

				// remove the element
				Queue children=new Queue();
				foreach ( XmlNode n in prev.Node.ChildNodes )
					children.Enqueue(n);

				XmlNode before;
				if ( esp.Type == TagType.EndTag )
				{
					// need to know if insertion point is end tag
					MarkupSelectionPoint msp=sel.Start as MarkupSelectionPoint;
					if ( msp != null && msp.Type == TagType.EndTag )
						before=null;
					else
						before=sel.Start.Node;
				}
				else
					before=esp.Node;

				XmlNode parent=esp.Node.ParentNode;
				while ( children.Count > 0 )
				{
					XmlNode n=(XmlNode) children.Dequeue();
					parent.InsertBefore(n, before);
				}
				// TODO: L: this is failing when backspace over empty tag - don't think it is any more
				parent.RemoveChild(esp.Node);
				return sel;
			}

			SelectionPoint test=prev;
			bool wsHandling=test.IsWhiteSpace;
			do
			{
				TextSelectionPoint tsp=(TextSelectionPoint) test;
				XmlCharacterData text=(XmlCharacterData) tsp.Node;
				string val=text.Value;

				string newVal=val.Substring(0, tsp.Index);
				if ( tsp.Index < val.Length-1 )
					newVal+=val.Substring(tsp.Index+1);
				else
					test=NextSelectionPoint(tsp);

				if ( newVal.Length == 0 )
					text.ParentNode.RemoveChild(text);
				else
					text.Value=newVal;

				prev=test;
				test=PreviousSelectionPoint(prev, false);

			} while ( wsHandling && test.IsWhiteSpace );

			return new Selection(prev, null);
		}

		private static Selection DeleteRange(Selection sel)
		{
			XmlDocumentFragment tmp;
			return DeleteRange(sel, out tmp);
		}

		private static Selection DeleteRange(Selection sel, out XmlDocumentFragment frag)
		{
			// we create a new XML document here because we don't
			// want to receive events about the newly created nodes
			frag=sel.Start.Node.OwnerDocument.CreateDocumentFragment();

			if ( !sel.IsBalanced )
				throw new ArgumentException("Selection is not balanced");

			sel=sel.Normalise();

			XmlNode n1=sel.Start.Node;
			if ( sel.Start is TextSelectionPoint )
			{
				// TODO: H: write unit tests for text, whitespace and significantwhitespace
//				if ( sel.End is TextSelectionPoint && sel.Start.Node.Equals(sel.End.Node) )
					// need to handle this separately because it may be
					// XmlCharacterData that doesn't have Split method

				TextSelectionPoint tsp=(TextSelectionPoint) sel.Start;
				if ( tsp.Index > 0 )
				{
					// TODO: M: this might get called for XmlSignificantWhitespace in
					//			which case it will throw exception
					XmlText t=(XmlText) tsp.Node;
					n1=t.SplitText(tsp.Index);

					if ( sel.End.Node.Equals(tsp.Node) )
					{
						// they're two selection points in the same node, so
						// end selectionpoint is now invalid - readjust

						TextSelectionPoint end=(TextSelectionPoint) sel.End;
						int newIndex=end.Index-tsp.Index;
						sel=new Selection(sel.Start, new TextSelectionPoint(n1,  newIndex));
					}
				}
			}

			SelectionPoint newSp=sel.End;

			// the end node
			XmlNode n2=sel.End.Node;
			if ( sel.End is TextSelectionPoint )
			{
				TextSelectionPoint tsp=(TextSelectionPoint) sel.End;
				if ( tsp.Index > 0 )
				{
					XmlText t=(XmlText) tsp.Node;
					XmlText x=t.SplitText(tsp.Index);
					newSp=new TextSelectionPoint(x, 0);
					n2=t;
				} 
				else
				{
					n2=tsp.Node.PreviousSibling;
				}

			} 
			else
			{
				n2=sel.End.PreviousNode;
			}

			Debug.Assert( n1.Equals(n2) || XmlUtil.AppearsBefore(n1, n2), "Invalid selection to delete!");

			Queue q=new Queue();
			q.Enqueue(n1);
			while ( !n1.Equals(n2) )
			{
				n1=n1.NextSibling;
				q.Enqueue(n1);
			}

			while ( q.Count > 0 )
			{
				XmlNode n=(XmlNode) q.Dequeue();
				frag.AppendChild(n);
			}

			return new Selection(newSp);
		}

		private static XmlDocumentFragment CopyRange(Selection sel)
		{
			// we create a new XML document here because we don't
			// want to receive events about the newly created nodes
			XmlDocumentFragment frag=sel.Start.Node.OwnerDocument.CreateDocumentFragment();

			if ( !sel.IsBalanced )
				throw new ArgumentException("Selection is not balanced");

			sel=sel.Normalise();

			Queue queue=new Queue();
			bool queueStart=true;

			XmlNode n1=sel.Start.Node;
			if ( sel.Start is TextSelectionPoint )
			{
				TextSelectionPoint tsp=(TextSelectionPoint) sel.Start;

				if ( sel.Start.Node.Equals(sel.End.Node) )
				{
					TextSelectionPoint tsp2=(TextSelectionPoint) sel.End;
					// special case - start and end are same node
					string val=tsp.Node.Value.Substring(tsp.Index, tsp2.Index - tsp.Index);
					XmlText text=sel.Start.Node.OwnerDocument.CreateTextNode(val);
					frag.AppendChild(text);
					return frag;
				}

				if ( tsp.Index > 0 )
				{
					string val=tsp.Node.Value.Substring(tsp.Index);
					XmlText text=sel.Start.Node.OwnerDocument.CreateTextNode(val);
					queue.Enqueue(text);
					queueStart=false;
				}
			}

			// the end node
			XmlNode endNode=null;
			XmlNode n2=sel.End.Node;
			if ( sel.End is TextSelectionPoint )
			{
				TextSelectionPoint tsp=(TextSelectionPoint) sel.End;
				if ( tsp.Index > 0 )
				{
					string val=tsp.Node.Value.Substring(tsp.Index);
					XmlText text=sel.Start.Node.OwnerDocument.CreateTextNode(val);
					endNode=text;
				}
				else
				{
					n2=tsp.Node.PreviousSibling;
				}
			} 
			else
			{
				n2=sel.End.PreviousNode;
			}

			//			Debug.Assert( n1.Equals(n2) || XmlUtil.AppearsBefore(n1, n2), "Invalid selection to delete!");

			if ( queueStart )
				queue.Enqueue(n1);
			while ( !n1.Equals(n2) )
			{
				// TODO: H: this will null pointer when selection is like
				//			<a>^blah blah</a><a>def def^</a>
				n1=n1.NextSibling;
				if ( !n1.Equals(n2) || endNode == null )
					queue.Enqueue(n1);
			}
			if ( endNode != null )
				queue.Enqueue(endNode);

			while ( queue.Count > 0 )
			{
				XmlNode n=(XmlNode) queue.Dequeue();
				frag.AppendChild(n.CloneNode(true));
			}

			return frag;
		}

		public Selection Insert(SelectionPoint sp, XmlNode n)
		{
			if ( sp == null )
			{
				// document does not have node yet
				if ( n.NodeType != XmlNodeType.Element )
					throw new ArgumentException("Can only insert element node into document node at this time");

				n.OwnerDocument.AppendChild(n);
				sp=new ElementSelectionPoint(n, TagType.EndTag);
			}
			else if ( sp is MarkupSelectionPoint )
			{
				MarkupSelectionPoint msp=(MarkupSelectionPoint) sp;
				if ( msp.Type == TagType.EndTag )
					sp.Node.AppendChild(n);
				else
					sp.Node.ParentNode.InsertBefore(n, sp.Node);
			} 
			else
			{
				TextSelectionPoint tsp=(TextSelectionPoint) sp;
				XmlCharacterData tnode=(XmlCharacterData) tsp.Node;
				if ( tsp.Index == 0 )
					sp.Node.ParentNode.InsertBefore(n, sp.Node);
				else
				{
					// at this point it must be a regular text node
					// TODO: M: check this is definitely only case
					XmlText t=(XmlText) tnode;
					XmlText t2=t.SplitText(tsp.Index);
					t.ParentNode.InsertAfter(n, t);
					sp=new TextSelectionPoint(t2, 0);
				}
			}
			return new Selection(sp);
		}

		public SelectionPoint Insert(SelectionPoint s, string text)
		{
			if ( text.Length == 0 )
				return s;

			if ( s is MarkupSelectionPoint )
			{
				MarkupSelectionPoint msp=(MarkupSelectionPoint) s;
				if ( msp.Type == TagType.EndTag )
				{
					if ( s.Node is XmlCharacterData )
					{
						// special case for CDATA and comment
						XmlCharacterData cd=(XmlCharacterData) s.Node;
						cd.AppendData(text);
					}
					else if ( s.Node.HasChildNodes && s.Node.LastChild.NodeType == XmlNodeType.Text )
					{
						XmlCharacterData t=(XmlCharacterData) s.Node.LastChild;
						t.AppendData(text);
					} 
					else
					{
						XmlText t=s.Node.OwnerDocument.CreateTextNode(text);
						s.Node.AppendChild(t);
					}
				} 
				else
				{
					if ( s.Node.PreviousSibling == null )
					{
						XmlText t=s.Node.OwnerDocument.CreateTextNode(text);
						s.Node.ParentNode.InsertBefore(t, s.Node);
					} 
					else
					{
						if ( s.Node.PreviousSibling.NodeType == XmlNodeType.Text )
						{
							XmlText t=(XmlText) s.Node.PreviousSibling;
							t.AppendData(text);
						} 
						else
						{
							XmlText t=s.Node.OwnerDocument.CreateTextNode(text);
							s.Node.ParentNode.InsertBefore(t, s.Node);
						}
					}
				}
				return s;
			} 
			else if (s.Node.NodeType == XmlNodeType.SignificantWhitespace)
			{
				// we cannot generally insert content in this node and
				// cursor can only be positioned before
				XmlText tnode=s.Node.OwnerDocument.CreateTextNode(text);
				s.Node.ParentNode.InsertBefore(tnode, s.Node);
				return s;
			}
			else
			{
				TextSelectionPoint tsp=(TextSelectionPoint) s;

//				if ( tsp.Index > 0 )
//				{
//					TextSelectionPoint tsp2=new TextSelectionPoint(tsp.Node, tsp.Index-1);
//					if ( tsp2.IsWhiteSpace )
//					{
//						// don't insert additional whitespace after existing
//						text=text.TrimStart(TextUtil.WhiteSpaceChars);
//						if ( text.Length == 0 )
//							return s;
//					}
//				}
//
				XmlCharacterData tnode=(XmlCharacterData) tsp.Node;

				WhitespaceHandling wh=wc.Classify(tsp.Node);
				if ( wh != WhitespaceHandling.Preserve )
				{
					if ( TextUtil.IsWhiteSpace(tnode.Value[tsp.Index]) )
						text=text.TrimEnd(TextUtil.WhiteSpaceChars);
					else if ( tsp.Index > 0 && TextUtil.IsWhiteSpace(tnode.Value[tsp.Index-1]) )
						text=text.TrimStart(TextUtil.WhiteSpaceChars);
				}

				if ( text.Length > 0 )
				{
					tnode.InsertData(tsp.Index, text);
					tsp=new TextSelectionPoint(tsp.Node,  tsp.Index+text.Length);
				} else if ( tsp.IsWhiteSpace )
					// this only happens if whitespace not able to be inserted
					return NextSelectionPoint(tsp);

				return tsp;
			}
		}

		public Selection ExtendCharRight(Selection selection)
		{
			return new Selection(selection.Start, NextSelectionPoint(selection.IsRange ? selection.End : selection.Start));
		}

		public Selection ExtendWordRight(Selection selection)
		{
			return new Selection(selection.Start, NextWordSelectionPoint(selection.IsRange ? selection.End : selection.Start));
		}

		public Selection ExtendCharLeft(Selection selection)
		{
			return new Selection(selection.Start, PreviousSelectionPoint(selection.IsRange ? selection.End : selection.Start));
		}

		public Selection ExtendWordLeft(Selection selection)
		{
			return new Selection(selection.Start, PreviousWordSelectionPoint(selection.IsRange ? selection.End : selection.Start));
		}

//		public static Selection ExtendSelection(Selection selection, SelectionPoint sp)
//		{
//			return new Selection(selection.Start,  sp);
//		}

//		public static Selection SetSelection(Selection selection, SelectionPoint sp)
//		{
//			return new Selection(sp, null);
//		}

		public Selection MoveCharRight(Selection selection)
		{
			if ( selection.IsEmpty )
				return selection;

			if ( selection.IsRange )
			{
				if ( !selection.EndAppearsBeforeStart )
					return new Selection(selection.End);
			}

			return new Selection(NextSelectionPoint(selection.Start));
		}

		public Selection MoveCharLeft(Selection selection)
		{
			if ( selection.IsEmpty )
				return selection;

			// if a range, then simply collapse to start
			if ( selection.IsRange )
			{
				if ( selection.EndAppearsBeforeStart )
					return new Selection(selection.End);
			} 

			return new Selection(PreviousSelectionPoint(selection.Start));
		}

		public Selection MoveWordRight(Selection selection)
		{
			if ( selection.IsEmpty )
				return selection;

			if ( selection.IsRange )
			{
				if ( !selection.EndAppearsBeforeStart )
					return new Selection(selection.End);
			}

			return new Selection(NextWordSelectionPoint(selection.Start));
		}

		public Selection MoveWordLeft(Selection selection)
		{
			if ( selection.IsEmpty )
				return selection;

			// if a range, then simply collapse to start
			if ( selection.IsRange )
			{
				if ( selection.EndAppearsBeforeStart )
					return new Selection(selection.End);
			} 

			return new Selection(PreviousWordSelectionPoint(selection.Start));
		}

		public SelectionPoint PreviousWordSelectionPoint(SelectionPoint from)
		{
			if ( from.IsTag )
				return PreviousSelectionPoint(from);

			SelectionPoint ret=PreviousSelectionPoint(from);

			while ( ret.IsWhiteSpace  )
				ret=PreviousSelectionPoint(ret);

			SelectionPoint keep=ret;
			ret=PreviousSelectionPoint(ret);
			while ( !ret.IsWhiteSpace && !ret.IsTag && !ret.Equals(keep) )
			{
				keep=ret;
				ret=PreviousSelectionPoint(ret);
			}

			return keep;
		}

		private SelectionPoint PreviousSelectionPoint(SelectionPoint from)
		{
			return PreviousSelectionPoint(from, true);
		}

		private static int GetPreviousEndSpaceCount(XmlNode n, bool preserveWs)
		{
			if ( n.PreviousSibling == null )
				return 0;

			if ( n.PreviousSibling.NodeType != XmlNodeType.Text )
				return 0;

			return TextUtil.GetEndSpaceCount(n.PreviousSibling.Value);
		}

		private SelectionPoint PreviousSelectionPoint(SelectionPoint from, bool skipWhiteSpace)
		{
			SelectionPoint ret=from;

			if ( ret is TextSelectionPoint )
			{
				WhitespaceHandling ws=wc.Classify(from.Node);

				TextSelectionPoint tsp=(TextSelectionPoint) ret;
				TextSelectionHelper tsh=new TextSelectionHelper(tsp, ws);
				tsp=tsh.Previous;
				if ( tsp == null )
				{
					XmlNode prev=ret.Node.PreviousSibling;

					if ( ret.Node.NodeType == XmlNodeType.CDATA || ret.Node.NodeType == XmlNodeType.Comment )
						return new ElementSelectionPoint(ret.Node, TagType.StartTag);
					
					if ( prev == null )
					{
						if ( !ret.Node.ParentNode.Equals(ret.Node.OwnerDocument.DocumentElement) )
							ret=CreateSelectionPoint(ret.Node.ParentNode, false);
					}
					else
						ret=CreateSelectionPoint(prev, true);
				} else
					ret=tsp;
			} 
			else if ( ret is ElementSelectionPoint )
			{
				ElementSelectionPoint esp=(ElementSelectionPoint) ret;

				if ( esp.Type == TagType.EndTag )
				{
					if ( esp.Node is XmlCharacterData )
					{
						// special case for CDATA and comment
						ret=new TextSelectionPoint(esp.Node, esp.Node.Value.Length - 1);
					}
					else if ( esp.Node.HasChildNodes )
						ret=CreateSelectionPoint(esp.Node.LastChild, true);
					else
						ret=CreateSelectionPoint(esp.Node, false);
				} 
				else
				{
					if ( esp.Node.PreviousSibling != null )
						ret=CreateSelectionPoint(esp.Node.PreviousSibling, true);
					else if ( !esp.Node.ParentNode.Equals(esp.Node.OwnerDocument.DocumentElement) )
						ret=CreateSelectionPoint(esp.Node.ParentNode, false);
				}
			}
			else // must be MarkupSelectionPoint
			{
				// TODO: L: this is exactly same as above!
				Debug.Assert(ret is MarkupSelectionPoint, "Expected MarkupSelectionPoint!");
				//				ret=CreateSelectionPoint(ret.Node.PreviousSibling, true);
				if ( ret.Node.PreviousSibling != null )
					ret=CreateSelectionPoint(ret.Node.PreviousSibling, true);
				else if ( !ret.Node.ParentNode.Equals(ret.Node.OwnerDocument.DocumentElement) )
					ret=CreateSelectionPoint(ret.Node.ParentNode, false);
			}

			return Validate(ret);
		}

		public Selection SelectAll(Selection selection)
		{
			if ( selection == null || selection.IsEmpty )
				throw new ArgumentException("Invalid selection passed to SelectAll");

			XmlElement docNode=selection.Start.Node.OwnerDocument.DocumentElement;

			SelectionPoint start=new ElementSelectionPoint(docNode, TagType.StartTag);
			start=Validate(start);

			SelectionPoint end=new ElementSelectionPoint(docNode, TagType.EndTag);

			return new Selection(start, end);
		}

		public SelectionPoint Validate(SelectionPoint sp)
		{
			if ( sp == null )
				return null;

			SelectionPoint ret=sp;

			if ( ret.Node.Equals(ret.Node.OwnerDocument.DocumentElement) )
			{
				ElementSelectionPoint esp=(ElementSelectionPoint) ret;
				if ( esp.Type == TagType.StartTag )
					return NextSelectionPoint(esp);
			}
			return ret;
		}

		public SelectionPoint NextWordSelectionPoint(SelectionPoint from)
		{
			if ( from.IsTag )
				return NextSelectionPoint(from);

			SelectionPoint ret=from;

			while ( !ret.IsWhiteSpace && !ret.IsTag )
				ret=NextSelectionPoint(ret);

			while ( ret.IsWhiteSpace )
				ret=NextSelectionPoint(ret);

			return ret;
		}

		public SelectionPoint NextSelectionPoint(SelectionPoint from)
		{
			SelectionPoint ret=from;

			if ( ret is TextSelectionPoint )
			{
				WhitespaceHandling ws=wc.Classify(from.Node);

				TextSelectionPoint tsp=(TextSelectionPoint) ret;
				TextSelectionHelper tsh=new TextSelectionHelper(tsp, ws);
				tsp=tsh.Next;
				if ( tsp == null )
				{
					if ( ret.Node.NodeType == XmlNodeType.CDATA || ret.Node.NodeType == XmlNodeType.Comment )
					{
						// special case because these nodes derive from XmlCharacterData but
						// still have start/end tags (visually)
						return new ElementSelectionPoint(ret.Node, TagType.EndTag);
					}

					XmlNode next=ret.Node.NextSibling;

					if ( next == null )
						ret=CreateSelectionPoint(ret.Node.ParentNode, true);
					else
						ret=CreateSelectionPoint(next, false);
				}
				else
					ret=tsp;
			} 
			else if ( ret is ElementSelectionPoint )
			{
				ElementSelectionPoint esp=(ElementSelectionPoint) ret;

				if ( esp.Type == TagType.StartTag )
				{
					if ( esp.Node is XmlCharacterData )
						ret=new TextSelectionPoint(esp.Node, 0);
					else if ( esp.Node.HasChildNodes )
						ret=CreateSelectionPoint(esp.Node.FirstChild, false);
					else
						ret=CreateSelectionPoint(esp.Node, true);
				} 
				else
				{
					if ( esp.Node.NextSibling != null )
						ret=CreateSelectionPoint(esp.Node.NextSibling, false);
					else if ( esp.Node.ParentNode.NodeType != XmlNodeType.Document )
						ret=CreateSelectionPoint(esp.Node.ParentNode, true);
				}
			}
			else // must be MarkupSelectionPoint
			{
				Debug.Assert(ret is MarkupSelectionPoint, "Expected MarkupSelectionPoint!");
				if ( ret.Node.NextSibling != null )
					ret=CreateSelectionPoint(ret.Node.NextSibling, false);
				else if ( ret.Node.ParentNode.NodeType != XmlNodeType.Document )
					ret=CreateSelectionPoint(ret.Node.ParentNode, true);
			}
			if ( !ret.IsValid )
				Logger.Log("WARN: Invalid selection point!");

			return ret;
		}

		internal static SelectionPoint CreateSelectionPoint(XmlNode n, bool atEnd)
		{
			switch ( n.NodeType )
			{
				case XmlNodeType.Text:
				case XmlNodeType.SignificantWhitespace:
					return new TextSelectionPoint(n, atEnd ? n.Value.Length - 1 : 0);

				case XmlNodeType.Comment:
				case XmlNodeType.CDATA:
				case XmlNodeType.Element:
					return new ElementSelectionPoint(n, atEnd ? TagType.EndTag : TagType.StartTag);

				case XmlNodeType.EntityReference:
					return new MarkupSelectionPoint(n);
			}
			Debug.Assert(false, "Unrecognised node type!");
			return null;
		}

		public Selection CreateSelection(XmlNode n)
		{
			// select the entire node
			SelectionPoint s1=CreateSelectionPoint(n, false);
			SelectionPoint s2=CreateSelectionPoint(n, true);
			s2=NextSelectionPoint(s2);
			return new Selection(s1, s2);
		}

		internal Selection SelectWord(SelectionPoint sp)
		{
			SelectionPoint next=NextWordSelectionPoint(sp);
			SelectionPoint start;
			if ( PreviousSelectionPoint(sp).IsWhiteSpace )
				start=sp;
			else
				start=PreviousWordSelectionPoint(sp);
			
			return new Selection(start, next);
		}
	}

//	public class SelectionFactory
//	{
//		public static Selection CreateSelection(XmlElement e)
//		{
//			return new Selection(new ElementSelectionPoint(e, TagType.StartTag),
//				new ElementSelectionPoint(e, TagType.EndTag));
//		}
//	}

	/// <summary>
	/// Represents a selection in the current document.
	/// </summary>
	/// <remarks>
	/// <para>Selections should be treated as immutable even though they are not.
	/// If passed a selection that you need to change, use the Clone method to take
	/// a copy before making the change.</para>
	/// <para>If the End property is non-null, the selection represents a range, and other
	/// properties such as IsBalanced become relevant.</para>
	/// </remarks>
	public class Selection
	{
		private readonly SelectionPoint start;
		private readonly SelectionPoint end;

		/// <summary>
		/// Initialises a new range selection.
		/// </summary>
		/// <param name="start">The first selection point.</param>
		/// <param name="end">The second selection point.</param>
		/// <remarks>It is not necessary for the start point to appear first in 
		/// document order. See the EndAppearsBeforeStart property.</remarks>
		public Selection(SelectionPoint start, SelectionPoint end)
		{
			this.start=start;
			this.end=end;

			Validate();
		}

		/// <summary>
		/// Initialises a new point selection.
		/// </summary>
		/// <param name="s">The selection point where the caret will be positioned.</param>
		public Selection(SelectionPoint s) : this(s, null)
		{
		}

		/// <summary>
		/// Initialises a new empty selection.
		/// </summary>
		/// <remarks>This is not typically used.</remarks>
		public Selection() : this (null, null)
		{
		}

		/// <summary>
		/// Gets or sets the starting selection point.
		/// </summary>
		public SelectionPoint Start
		{
			get { return start; }
//			set 
//			{
//				start=value;
//				Validate();
//			}
		}

		/// <summary>
		/// Gets or sets the ending selection point.
		/// </summary>
		public SelectionPoint End
		{
			get { return end; }
//			set 
//			{
//				end=value;
//				Validate();
//			}
		}

		// TODO: L: empty method - remove
		internal void Validate()
		{
//			if ( start != null && start.Equals(end) )
//				throw new InvalidOperationException("Selection end must not equal selection start");
		}

		/// <summary>
		/// Gets an empty selection.
		/// </summary>
		public static Selection Empty
		{
			get 
			{
				// TODO: L: make static member to avoid create each time
				return new Selection();
			}
		}

		/// <seebase/>
		public override string ToString()
		{
			if ( IsEmpty )
				return "No Selection";

			if ( IsRange )
			{
				return string.Format("{0} - {1}{2}",
					start.ToString(),
					end.ToString(),
					IsBalanced ? "- Balanced" : ""
					);
			} 
			else
				return string.Format("{0}", start.ToString());
		}

		/// <seebase/>
		public override bool Equals(object o)
		{
			if ( o == null )
				return false;

			Selection other=o as Selection;
			if ( o == null )
				return false;

			if ( IsEmpty )
				return other.IsEmpty;

			if ( !start.Equals(other.start) )
				return false;

			if ( IsRange )
				return end.Equals(other.end);

			// our end is null so other end must also be
			return other.end == null;
		}

		/// <seebase/>
		public override int GetHashCode()
		{
			int n=0;
			if ( start != null )
				n ^= start.GetHashCode();
			if ( end != null )
				n ^= end.GetHashCode();

			return base.GetHashCode();
		}

		/// <summary>
		/// Gets a value indicating if this selection is empty (has no start or end)
		/// </summary>
		public bool IsEmpty
		{
			get { return (start == null); }
		}

		/// <summary>
		/// Creates a duplicate of this selection.
		/// </summary>
		/// <returns>The cloned selection.</returns>
//		public Selection Clone()
//		{
//			SelectionPoint s=start == null ? null : start.Clone();
//			SelectionPoint e=end == null ? null : end.Clone();
//			return new Selection(s, e);
//		}

		/// <summary>
		/// Gets a value indicating if this selection is a range.
		/// </summary>
		/// <remarks>A selection is a range if it is not empty and has an end point.</remarks>
		public bool IsRange
		{
			get
			{
				return (start != null && end != null);
			}
		}

		/// <summary>
		/// Returns a new selection in which the selection start appears before selection end. Should
		/// only be called for ranges.
		/// </summary>
		/// <returns>A new selection</returns>
		public Selection Normalise()
		{
			if ( IsRange && EndAppearsBeforeStart )
				return new Selection(end, start);

			return this;
		}

		/// <summary>
		/// Gets a value indicating if this selection is balanced.
		/// </summary>
		/// <remarks>A selection is balanced if removing the content of the selection
		/// would leave the document well-formed.</remarks>
		public bool IsBalanced
		{
			get 
			{
				if ( !IsRange )
					return false;

				Selection sel=Normalise();

				if ( sel.Start.IsTag )
				{
					ElementSelectionPoint esp=(ElementSelectionPoint) sel.Start;
					if ( esp.Type == TagType.EndTag )
						// selection starting in an end tag can never be balanced
						return false;
				}

				SelectionPoint sp=sel.End;
				XmlNode comp=sel.start.Node.ParentNode;
				if ( sp.IsAtStart )
				{
					if ( sp.Node.PreviousSibling == null )
						// selection spans start tag so cannot be balanced
						return false;
				}
				else if ( sp.IsTag ) // end tag
				{
					if ( sp.Node.LastChild == null )
						// selection spans start tag so cannot be balanced
						return false;

					comp=comp.ParentNode;
				}

				// to get an idea of whether the selection is balanced
				// 'visually' we need to move left from the end point
//				SelectionPoint sp=SelectionManager.PreviousSelectionPoint(sel.end);

				return comp.Equals(sp.Node.ParentNode);
			}
		}

		public bool IsSingleNode
		{
			get
			{
				if ( !IsBalanced )
					return false;

				if ( start.Node.Equals(end.Node) )
					return true;

				if ( end.Node.Equals(start.Node.ParentNode) && start.Node.NextSibling == null )
					return true;

				return start.Node.Equals(end.Node.PreviousSibling);
			}	
		}

		/// <summary>
		/// Gets a value indicating if the start point of the selection appears after the end point in the document.
		/// </summary>
		public bool EndAppearsBeforeStart
		{
			get 
			{
				if ( end.Node.Equals(start.Node) )
					return end.IsBefore(start);

				if ( XmlUtil.HasAncestor(end.Node, start.Node) )
				{
					ElementSelectionPoint esp=(ElementSelectionPoint) start;
					return esp.Type == TagType.EndTag;
				}
				if ( XmlUtil.HasAncestor(start.Node, end.Node) )
				{
					ElementSelectionPoint esp=(ElementSelectionPoint) end;
					return esp.Type == TagType.StartTag;
				}

				return XmlUtil.AppearsBefore(end.Node, start.Node);
			}
		}
	}

	/// <summary>
	/// Abstract class representing a selection point (a point within an XEditNet document).
	/// </summary>
	public abstract class SelectionPoint
	{
		/// <summary>
		/// The XmlNode represented by this selection point.
		/// </summary>
		protected readonly XmlNode node;

		/// <summary>
		/// Clone this selection point.
		/// </summary>
		/// <returns>The newly created selection point.</returns>
//		public abstract SelectionPoint Clone();

		/// <summary>
		/// Test whether this selection point appears before another selection point in the document.
		/// </summary>
		/// <param name="otherPoint">The reference point.</param>
		/// <returns>Returns true if this selection point appears before otherPoint.</returns>
		public abstract bool IsBefore(SelectionPoint otherPoint);
		/// <seebase/>
		public override abstract bool Equals(object o);
		/// <seebase/>
		public override abstract int GetHashCode();

		/// <summary>
		/// Initialises a new selection point with an XmlNode.
		/// </summary>
		/// <remarks>Only ever called by constructors of concrete derived classes.</remarks>
		/// <param name="n">The XmlNode.</param>
		public SelectionPoint(XmlNode n)
		{
			node=n;
		}

		/// <summary>
		/// Gets the value of the of the XmlNode for this selection point.
		/// </summary>
		public XmlNode Node
		{
			get { return node; }
		}

		// TODO: L: review
		internal virtual bool IsValid
		{
			get { return true; }
		}

		/// <summary>
		/// Gets a value indicating if the selection point is positioned on whitespace.
		/// </summary>
		public virtual bool IsWhiteSpace
		{
			get { return false; }
		}

		/// <summary>
		/// Gets a value indicating if the selection point is positioned at the start of the node.
		/// </summary>
		public abstract bool IsAtStart
		{
			get;
		}
		
		/// <summary>
		/// Gets a value indicating if the selection point is positioned on a tag.
		/// </summary>
		/// <remarks>A tag could be a start tag, end tag, comment and so on.
		/// See also IsElement.</remarks>
		public virtual bool IsTag
		{
			get { return false; }
		}
		/// <summary>
		/// Gets whether this selection point is positioned on an element start or end tag.
		/// </summary>
		public virtual bool IsElement
		{
			get { return node.NodeType == XmlNodeType.Element; }
		}
		/// <summary>
		/// Gets the previous node to the current node for this selection point.
		/// </summary>
		/// <remarks>By default this returns Node.PreviousSibling.</remarks>
		public virtual XmlNode PreviousNode
		{
			get { return node.PreviousSibling; }
		}
		/// <summary>
		/// Gets the previous node to the current node for this selection point.
		/// </summary>
		/// <remarks>By default this returns Node.NextSibling.</remarks>
		public virtual XmlNode NextNode
		{
			get { return node.NextSibling; }
		}
	}

	/// <summary>
	/// Specifies types of tags used in selections.
	/// </summary>
	/// <remarks>Differentiates between start tags and end tags for elements, comments and so on.</remarks>
	public enum TagType 
	{
		/// <summary>
		/// Indicates that this is a start tag (or empty item).
		/// </summary>
		StartTag,
		/// <summary>
		/// Indicates that this is an end tag.
		/// </summary>
		EndTag
	}

	/// <summary>
	/// A selection point for markup items rather than text.
	/// </summary>
	/// <remarks>Examples of objects requiring this type of selection point are element tags,
	/// comment delimeters, entities and so on.</remarks>
	public class MarkupSelectionPoint : SelectionPoint
	{
		/// <summary>
		/// Initialises a new instance of the MarkupSelectionPoint with the specified XmlNode.
		/// </summary>
		/// <param name="n">The XmlNode.</param>
		public MarkupSelectionPoint(XmlNode n) : base(n)
		{
		}

		/// <summary>
		/// Gets a value indicating whether this is a start or end tag.
		/// </summary>
		/// <remarks>For markup items that have no conent, such as entities,
		/// this property is always equal to TagType.StartTag.</remarks>
		public virtual TagType Type 
		{
			get { return TagType.StartTag; }
		}

		/// <seebase/>
// 		public override SelectionPoint Clone()
//		{
//			return new MarkupSelectionPoint(this.node);
//		}

		/// <seebase/>
		public override bool Equals(object obj)
		{
			MarkupSelectionPoint other=obj as MarkupSelectionPoint;
			if ( other == null )
				return false;

			return other.node.Equals(this.node);
		}

		/// <seebase/>
		public override int GetHashCode()
		{
			return node.GetHashCode();
		}

		public override bool IsAtStart
		{
			get { return true; }
		}

		/// <seebase/>
		public override bool IsBefore(SelectionPoint otherPoint)
		{
			if ( !node.Equals(otherPoint.Node) )
				throw new ArgumentException("IsBefore must be called with a SelectionPoint that shares the same node");

			return true;
		}
	}

	/// <summary>
	/// Represents a selection point at the start or end of an XmlElement node.
	/// </summary>
	/// <remarks>An element selection point with Type equal to StartTag represents
	/// a caret located just in front of the element start tag. If Type is equal to
	/// EndTag the caret is located just in front of the element end tag.</remarks>
	public class ElementSelectionPoint : MarkupSelectionPoint
	{
		private readonly TagType type;

		/// <summary>
		/// Initialises a new instance with a node and tag type.
		/// </summary>
		/// <param name="n">The XmlNode.</param>
		/// <param name="type">The TagType value.</param>
		public ElementSelectionPoint(XmlNode n, TagType type) : base(n)
		{
			this.type=type;
		}

		/// <seebase/>
		public override bool IsTag
		{
			get	{ return true; }
		}

		/// <seebase/>
		public override TagType Type
		{
			get { return type; }
		}

		/// <seebase/>
		public override bool IsAtStart
		{
			get { return type == TagType.StartTag; }
		}

		/// <summary>
		/// Returns the previous node.
		/// </summary>
		/// <remarks>If this selection point is an end tag, this returns the element's
		/// last child, otherwise it returns the elements previous sibling.</remarks>
		public override XmlNode PreviousNode
		{
			get
			{
				return Type == TagType.StartTag ? node.PreviousSibling : node.LastChild;
			}
		}
		/// <summary>
		/// Returns the next node.
		/// </summary>
		/// <remarks>If this selection point is a start tag, this returns the element's
		/// first child, otherwise it returns the elements next sibling.</remarks>
		public override XmlNode NextNode
		{
			get
			{
				return Type == TagType.StartTag ? node.FirstChild : node.NextSibling;
			}
		}

		/// <seebase/>
		public override bool IsBefore(SelectionPoint otherPoint)
		{
			if ( !node.Equals(otherPoint.Node) )
				throw new ArgumentException("IsBefore must be called with a SelectionPoint that shares the same node");

			// check if we're first
			return (Type == TagType.StartTag);
		}

		/// <seebase/>
		public override bool Equals(object o)
		{
			if ( o == null )
				return false;

			ElementSelectionPoint other=o as ElementSelectionPoint;
			if ( other == null )
				return false;

			if ( !node.Equals(other.node) )
				return false;

			return Type.Equals(other.Type);
		}

		/// <seebase/>
		public override int GetHashCode()
		{
			return node.GetHashCode() ^ Type.GetHashCode();
		}

		/// <seebase/>
//		public override SelectionPoint Clone()
//		{
//			return new ElementSelectionPoint(node, Type);
//		}

		/// <seebase/>
		public override string ToString()
		{
			if ( Type == TagType.StartTag )
				return "<"+node.Name+">";
			else
				return "</"+node.Name+">";
		}
	}

	/// <summary>
	/// Represents a selection point within text content.
	/// </summary>
	/// <remarks>
	/// <para>Typically the underlying node is an XmlText node, but this class
	/// is also used to represent a selection point in an XmlComment.</para>
	/// <para>The Index property specifies the location of the caret within the text node.</para></remarks>
	public sealed class TextSelectionPoint : SelectionPoint
	{
		private int index;

		/// <summary>
		/// Initialises a new instance with the specified node and index.
		/// </summary>
		/// <param name="n"></param>
		/// <param name="index"></param>
		public TextSelectionPoint(XmlNode n, int index) : base(n)
		{
			this.index=index;
			if ( index > 0 && Char == '\n' && PreviousChar == '\r' )
				this.index--;
		}

		/// <summary>
		/// Gets a value indicating the position of the caret within the text node.
		/// </summary>
		/// <remarks>
		/// If the index is zero the caret is positioned just before the first character
		/// in the text node. It is not valid for the index to be greater than or equal
		/// to the number of characters in the text node.</remarks>
		public int Index
		{
			get { return index; }
		}

		internal override bool IsValid
		{
			get	{ return Index >= 0 && Index < node.Value.Length; }
		}

		/// <seebase/>
		public override bool IsWhiteSpace
		{
			get { return TextUtil.IsWhiteSpace(node.Value[Index]);	}
		}

		public override bool IsAtStart
		{
			get { return index == 0; }
		}

		/// <seebase/>
		public override bool IsBefore(SelectionPoint otherPoint)
		{
			if ( !node.Equals(otherPoint.Node) )
				throw new ArgumentException("IsBefore must be called with a SelectionPoint that shares the same node");

			TextSelectionPoint tsp=otherPoint as TextSelectionPoint;
			if ( tsp == null )
			{
				MarkupSelectionPoint msp=(MarkupSelectionPoint) otherPoint;
				return msp.Type == TagType.EndTag;
			}

			// check if we're first
			return (Index < tsp.index);
		}

		/// <seebase/>
		public override bool Equals(object o)
		{
			if ( o == null )
				return false;

			TextSelectionPoint other=o as TextSelectionPoint;
			if ( other == null )
				return false;

			if ( !node.Equals(other.Node) )
				return false;

			return Index == other.index;
		}

		/// <seebase/>
		public override int GetHashCode()
		{
			return node.GetHashCode() ^ Index.GetHashCode();
		}

		/// <seebase/>
//		public override SelectionPoint Clone()
//		{
//			return new TextSelectionPoint(node, Index);
//		}

		/// <summary>
		/// Gets the character at the index position in the text node.
		/// </summary>
		public char Char
		{
			get { return node.Value[Index]; }
		}

		/// <seebase/>
		public override string ToString()
		{
			string text="#text("+Index+",'";
			text+=node.Value[Index];
			text+="'="+(int) node.Value[Index]+")";
			return text;
		}

		public bool IsAtEnd
		{
			get { return Index == node.Value.Length-1; }
		}

		public char NextChar
		{
			get
			{
				if ( index < node.Value.Length - 1 )
					return node.Value[index+1];

				return '\0';
			}
		}

		public char PreviousChar
		{
			get
			{
				if ( index > 0 )
					return node.Value[index-1];

				return '\0';
			}
		}
	}

	internal class TextSelectionHelper
	{
		private int index;
		private int rollbackIndex;
		private XmlCharacterData currentNode;
		private XmlCharacterData rollbackNode;
		private WhitespaceHandling ws;

		public TextSelectionHelper(TextSelectionPoint tsp, WhitespaceHandling ws)
		{
			this.ws=ws;
			currentNode=(XmlCharacterData) tsp.Node;
			index=tsp.Index;
		}

		private TextSelectionHelper(TextSelectionHelper copy)
		{
			this.index=copy.index;
			this.rollbackIndex=copy.rollbackIndex;
			this.currentNode=copy.currentNode;
			this.rollbackNode=copy.rollbackNode;
			this.ws=copy.ws;
		}

		public char Char
		{
			get { return currentNode.Value[index]; }
		}

		private TextSelectionPoint CurrentSelectionPoint
		{
			get
			{
				if ( currentNode == null )
					return null;

				return new TextSelectionPoint(currentNode, index);
			}
		}

		private int Length
		{
			get { return 0; }
		}

		private bool Increment()
		{
			rollbackIndex=index;
			rollbackNode=currentNode;

			bool cr=Char == '\r';

			index++;
			if ( index >= currentNode.Value.Length )
			{
				index=0;
				currentNode=currentNode.NextSibling as XmlCharacterData;
				if ( currentNode == null )
					return false;
			}

			if ( cr && Char == '\n' )
				// skip over \n in \r\n sequence
				return Increment();

			return true;
		}

		private bool Decrement()
		{
			rollbackIndex=index;
			rollbackNode=currentNode;

			index--;
			if ( index < 0 )
			{
				currentNode=currentNode.PreviousSibling as XmlCharacterData;
				if ( currentNode == null )
					return false;

				index=currentNode.Value.Length-1;
			}

			if ( Char == '\n' )
			{
				TextSelectionHelper c=new TextSelectionHelper(this);
				if ( c.Decrement() && c.Char == '\r' )
					return Decrement();
			}
			return true;
		}

		private bool IsWhiteSpace
		{
			get
			{
				if ( currentNode == null )
					return false;

				return TextUtil.IsWhiteSpace(Char);
			}
		}

		private bool IsValid
		{
			get { return currentNode != null; }
		}

		public TextSelectionPoint Next
		{
			get
			{
				if ( !IsWhiteSpace || (ws==WhitespaceHandling.Preserve) )
				{
					Increment();
					return CurrentSelectionPoint;
				}

				while ( IsValid && IsWhiteSpace )
					Increment();

				return CurrentSelectionPoint;
			}
		}

		public TextSelectionPoint Previous
		{
			get
			{
				if ( ws==WhitespaceHandling.Preserve )
				{
					Decrement();
					return CurrentSelectionPoint;
				}

				Decrement();

				if ( !IsValid )
					return null;

				if ( IsWhiteSpace )
				{
					while ( IsValid && IsWhiteSpace )
						Decrement();

					Rollback();
				}

				return CurrentSelectionPoint;
			}	
		}

		private void Rollback()
		{
			Debug.Assert(rollbackNode != null, "Invalid rollback - no rollback node!");
			currentNode=rollbackNode;
			index=rollbackIndex;
			rollbackNode=null;
		}
	}
}

