using System;
using System.Collections;
using System.Diagnostics;
using System.Xml;

namespace XEditNet.Undo
{
	internal interface IUndoContextProvider
	{
		object ContextInfo
		{
			get;
		}
	}

	internal class UndoManager
	{
		private XmlDocument document;
		private Stack undoStack=new Stack();
		private Stack redoStack=new Stack();
		private bool disabled=false;
		private Stack previousSiblingStack=new Stack();
		private Stack previousValueStack=new Stack();
		private bool modified;
		private IUndoContextProvider context;

		public UndoManager(IUndoContextProvider context)
		{
			this.context=context;
		}

		private void Init()
		{
			undoStack.Clear();
			redoStack.Clear();

			previousSiblingStack.Clear();
			previousValueStack.Clear();

			disabled=false;
			document=null;
			modified=false;
		}

		public bool CanUndo
		{
			get
			{
				if ( undoStack.Count == 0 || document == null )
					return false;

				return !(UndoHead.IsEmpty && undoStack.Count == 1);
			}
		}

		public bool CanRedo
		{
			get { return redoStack.Count > 0 && document != null; }
		}

		private UndoBatch UndoHead
		{
			get
			{
				return (UndoBatch) (undoStack.Count == 0 ? null : undoStack.Peek());
			}
		}

		public void Mark(object info)
		{
//			Console.WriteLine("UndoManager: Mark({0})", info);

			UndoBatch batch=UndoHead;
			if ( batch != null )
			{
				if ( batch.IsEmpty )
					return;

				if ( batch.End == null )
					batch.End=info;
			}

			batch=new UndoBatch();
			undoStack.Push(batch);
		}

		public object Undo(object info)
		{
			if ( !CanUndo )
				throw new InvalidOperationException("Cannot undo");

			if ( UndoHead != null && UndoHead.IsEmpty )
				// ignore an empty batch
				undoStack.Pop();

			Debug.Assert(UndoHead != null && !UndoHead.IsEmpty, "Invalid undo state");

			UndoBatch batch=(UndoBatch) undoStack.Pop();
			if ( batch.End == null )
				batch.End=info;

			disabled=true;
			try
			{
				batch.Undo();
			}
			finally
			{
				disabled=false;
			}

			redoStack.Push(batch);

			if ( undoStack.Count == 0 || !UndoHead.IsEmpty )
			{
				// undo stack is empty or there is a non-empty batch on the undo
				// stack - we want to separate any future changes from that batch
				UndoBatch n=new UndoBatch();
				// TODO: M: not sure about this next line
				//			consider edit, undo, move, edit, undo
				n.Start=batch.Start;
				undoStack.Push(n);
			}

			return batch.Start;
		}

		public object Redo()
		{
			if ( !CanRedo )
				throw new InvalidOperationException("Cannot redo");

			UndoBatch batch=(UndoBatch) redoStack.Pop();

			disabled=true;
			try 
			{
				batch.Redo();
				undoStack.Push(batch);
				UndoBatch n=new UndoBatch();
				n.Start=batch.End;
				undoStack.Push(n);
			}
			finally 
			{
				disabled=false;
			}
			return batch.End;
		}

		public void Attach(XmlDocument doc)
		{
			Init();
			document=doc;
			doc.NodeChanging+=new XmlNodeChangedEventHandler(NodeChanging);
			doc.NodeChanged+=new XmlNodeChangedEventHandler(NodeChanged);
			doc.NodeInserting+=new XmlNodeChangedEventHandler(NodeInserting);
			doc.NodeInserted+=new XmlNodeChangedEventHandler(NodeInserted);
			doc.NodeRemoving+=new XmlNodeChangedEventHandler(NodeRemoving);
			doc.NodeRemoved+=new XmlNodeChangedEventHandler(NodeRemoved);
		}

		public void Detach()
		{
			if ( document != null )
			{
				document.NodeChanging-=new XmlNodeChangedEventHandler(NodeChanging);
				document.NodeChanged-=new XmlNodeChangedEventHandler(NodeChanged);
				document.NodeInserting-=new XmlNodeChangedEventHandler(NodeInserting);
				document.NodeInserted-=new XmlNodeChangedEventHandler(NodeInserted);
				document.NodeRemoving-=new XmlNodeChangedEventHandler(NodeRemoving);
				document.NodeRemoved-=new XmlNodeChangedEventHandler(NodeRemoved);
			}
			Init();
		}

		private void SetStart()
		{
			redoStack.Clear();

			UndoBatch b=UndoHead;
			if ( b == null )
			{
				b=new UndoBatch();
				undoStack.Push(b);
			}
			if ( b.IsEmpty && context != null )
				b.Start=context.ContextInfo;
		}

		public void NodeChanging(object sender, XmlNodeChangedEventArgs e)
		{
			if ( disabled )
				return;

//			Console.WriteLine("UndoManager: NodeChanging {0} / Value={1}", e.Node.Name, e.Node.Value);

			previousValueStack.Push(e.Node.Value);
		}

		public void NodeChanged(object sender, XmlNodeChangedEventArgs e)
		{
			modified=true;

			if ( disabled )
				return;

//			Console.WriteLine("UndoManager: NodeChanged {0} / Value={1}", e.Node.Name, e.Node.Value);

			SetStart();

			UndoChangeCommand ucc=new UndoChangeCommand(e.Node, (string) previousValueStack.Pop());
			NewCommand(ucc);
		}

		public void NodeInserting(object sender, XmlNodeChangedEventArgs e)
		{
			if ( disabled || XmlUtil.IsNamespaceAttribute(e.Node, e.NewParent) )
				return;

//			Console.WriteLine("UndoManager: NodeInserting {0}", e.Node.Name);

			if ( e.OldParent != null )
				previousSiblingStack.Push(e.Node.PreviousSibling);
			else
				previousSiblingStack.Push(null);
		}

		public void NodeInserted(object sender, XmlNodeChangedEventArgs e)
		{
			if ( disabled || XmlUtil.IsNamespaceAttribute(e.Node, e.NewParent) )
				return;

			XmlNode ps=(XmlNode) previousSiblingStack.Pop();

			modified=true;

//			if ( !XmlUtil.HasAncestor(e.Node, document.DocumentElement) )
//				return;

//			Console.WriteLine("UndoManager: NodeInserted {0}", e.Node.Name);

			SetStart();

			UndoInsertCommand uic=new UndoInsertCommand(e.Node, e.NewParent, e.OldParent, ps);
			NewCommand(uic);
		}

		private void NewCommand(UndoCommand c)
		{
			if ( UndoHead == null )
				throw new InvalidOperationException("UndoMark not called before document change event");

			UndoHead.AddCommand(c);
		}

		public void NodeRemoving(object sender, XmlNodeChangedEventArgs e)
		{
			if ( disabled || XmlUtil.IsNamespaceAttribute(e.Node, e.NewParent) )
				return;

//			Console.WriteLine("UndoManager: NodeRemoving {0}", e.Node.Name);

			modified=true;
			SetStart();
			UndoRemoveCommand urc=new UndoRemoveCommand(e.Node);
			NewCommand(urc);
		}

		// TODO: L: remove - not doing anything
		private void NodeRemoved(object sender, XmlNodeChangedEventArgs e)
		{
			if ( disabled || XmlUtil.IsNamespaceAttribute(e.Node, e.NewParent) )
				return;
		}

		private class UndoBatch
		{
			private ArrayList commands=new ArrayList();
			private object startInfo;
			private object endInfo;

			public UndoBatch()
			{
			}

			public void AddCommand(UndoCommand cmd)
			{
				if ( Head != null && Head.Merge(cmd) )
					return;

				commands.Add(cmd);
			}

			public UndoCommand Head
			{
				get
				{
					if ( commands.Count == 0 )
						return null;

					return (UndoCommand) commands[commands.Count-1];
				}
			}

			public object Undo()
			{
				int n=commands.Count;
				while ( --n >= 0 )
					((UndoCommand) commands[n]).Undo();

				return startInfo;
			}

			public object Redo()
			{
				foreach ( UndoCommand cmd in commands )
					cmd.Redo();

				return endInfo;
			}

			public bool IsEmpty
			{
				get { return commands.Count == 0; }
			}

			public object Start
			{
				set
				{
					startInfo=value;
				}
				get
				{
//					Debug.Assert(commands.Count > 0, "Attempting to get start for empty batch");
					return startInfo;
				}
			}

			public object End
			{
				set
				{
					endInfo=value;
				}
				get
				{
//					Debug.Assert(commands.Count > 0, "Attempting to get end for empty batch");
					return endInfo;
				}
			}
		}

		private abstract class UndoCommand
		{
			protected XmlNode node;

			public abstract void Undo();
			public abstract void Redo();

			public virtual bool Merge(UndoCommand cmd)
			{
				return false;
			}

			public object Node
			{
				get { return node; }
			}
		}

//		private  class UndoMark
//		{
//			public object Info;
//
//			public UndoMark(object info)
//			{
//				this.Info=info;
//			}
//		}

		private class UndoInsertCommand : UndoCommand
		{
			private XmlNode newParent;
			private XmlNode newPreviousSibling;
			private XmlNode oldParent;
			private XmlNode oldPreviousSibling;

			public UndoInsertCommand(XmlNode node, XmlNode newParent, XmlNode oldParent, XmlNode oldPreviousSibling)
			{
				this.node=node;

				Debug.Assert((oldParent == null && oldPreviousSibling == null) || (
					oldParent != null && oldPreviousSibling != null), "Invalid state for insert command");

				this.newParent=newParent;
				this.newPreviousSibling=node.PreviousSibling;

				this.oldParent=oldParent;
				this.oldPreviousSibling=oldPreviousSibling;
			}

			public override void Undo()
			{
				// either remove the node or move it to the old location
				if ( oldParent == null )
				{
					if ( node.NodeType == XmlNodeType.Attribute )
						newParent.Attributes.Remove((XmlAttribute) node);
					else
						newParent.RemoveChild(node);
				}
				else
					// could be problem for attributes but they cannot be moved
					oldParent.InsertAfter(node, oldPreviousSibling);
			}

			public override void Redo()
			{
				switch ( node.NodeType )
				{
					case XmlNodeType.Attribute:
						newParent.Attributes.SetNamedItem(node);
						break;

					default:
						newParent.InsertAfter(node, newPreviousSibling);
						break;
				}
			}
		}

		private class UndoRemoveCommand : UndoCommand
		{
			private XmlNode oldParent;
			private XmlNode previous;

			public UndoRemoveCommand(XmlNode node)
			{
				this.node=node;
				this.previous=node.PreviousSibling;
				if ( node.NodeType == XmlNodeType.Attribute )
					this.oldParent=((XmlAttribute) node).OwnerElement;
				else
					this.oldParent=node.ParentNode;
			}

			public override void Undo()
			{
				if ( node.NodeType == XmlNodeType.Attribute )
					oldParent.Attributes.Append((XmlAttribute) node);
				else
					oldParent.InsertAfter(node, previous);
			}
			public override void Redo()
			{
				oldParent.RemoveChild(node);
			}
		}

		private class UndoChangeCommand : UndoCommand
		{
			private string previousValue;
			private string newValue;

			public UndoChangeCommand(XmlNode node, string previousValue)
			{
				this.node=node;
				this.previousValue=previousValue;
				this.newValue=node.Value;
			}

			public override void Undo()
			{
				node.Value=previousValue;
			}
			public override void Redo()
			{
				node.Value=newValue;
			}

			public override bool Merge(UndoCommand cmd)
			{
				UndoChangeCommand other=cmd as UndoChangeCommand;

				if ( other == null || !cmd.Node.Equals(node) )
					return false;

				// can merge these changes
				newValue=other.newValue;
				return true;
			}
		}

		public bool Modified
		{
			get { return modified; }
			set { modified=value; }
		}
	}
}
