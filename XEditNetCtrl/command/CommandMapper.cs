using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using XEditNet.Xml.Serialization;

namespace XEditNet.Keyboard
{
	internal interface ICommandTarget
	{
		void DispatchCommand(CommandMapping cmd);
	}

	[Serializable]
	internal class CommandMapper
	{
		[NonSerialized]
		private ICommandTarget target;

		private string queue=string.Empty;

		[XmlElement("CommandMapping", typeof(CommandMapping))]
		public ArrayList commandMappings=new ArrayList();

		public static CommandMapper CreateInstance(ICommandTarget o)
		{
			Assembly a = typeof(CommandMapper).Assembly;
			string name = a.FullName.Split(',')[0]+".keys.xml";
			Stream stm = a.GetManifestResourceStream(name);
			XmlTextReader xtr=new XmlTextReader(stm);
			return CreateInstance(o, xtr);
		}

		public static CommandMapper CreateInstance(ICommandTarget o, string filename)
		{
			XmlTextReader xtr=new XmlTextReader(filename);
			return CreateInstance(o, xtr);
		}

		public static CommandMapper CreateInstance(ICommandTarget o, XmlTextReader xtr)
		{
			SimpleXmlSerializer serializer = 
				new SimpleXmlSerializer(typeof(CommandMapper));

			try 
			{
				CommandMapper km=(CommandMapper) serializer.Deserialize(xtr);
				km.target=o;
				return km;
			}
			finally 
			{
				xtr.Close();
			}
		}

		public static CommandMapper CreateInstance(ICommandTarget o, Stream s)
		{
			try 
			{
				BinaryFormatter bf=new BinaryFormatter();
				bf.AssemblyFormat=FormatterAssemblyStyle.Simple;
				CommandMapper km=(CommandMapper) bf.Deserialize(s);
				km.target=o;
				return km;
			}
			finally 
			{
				s.Close();
			}
		}

//		public void BindMenuItem(MenuItem mi)
//		{
//			string path=GetMenuPath(mi);
//			MenuMapping mm=FindByPath(path);
//			if ( mm == null )
//				return;
//
//			mi.Click+=new EventHandler(InvokeMenu);
//			
//			CommandMapping km=FindKeyMappingByMethod(mm.Method);
//			if ( km == null || km.Sequence.IndexOf(',') > 0 )
//				// cannot map key as either null or multiple
//				return;
//
//			string[] combo=km.Sequence.Split('+');
//			Keys c=Keys.None;
//			foreach ( string k in combo )
//				c |= (Keys) Enum.Parse(typeof(Keys), k);
//
//			mi.ShowShortcut=true;
//			mi.Shortcut=(Shortcut) c;
//		}

		private string GetMenuPath(MenuItem mi)
		{
			string path="";
			while ( mi != null )
			{
				path="/"+mi.Text+path;
				mi=mi.Parent as MenuItem;
			}
			return path.Replace("&", "").Replace(" ", "");
		}

//		public void InvokeMenu(object sender, EventArgs e)
//		{
//			MenuItem mi=(MenuItem) sender;
//			string path=GetMenuPath(mi);
//			MenuMapping mm=FindByPath(path);
//			if ( mm == null )
//				// TODO: L: this could be considered an error
//				return;
//
//			Invoke(mm.Method);
//		}

//		private MenuMapping FindByPath(string path)
//		{
//			foreach ( MenuMapping mm in menuMappings )
//			{
//				if ( mm.MenuPath.Equals(path) )
//					return mm;
//			}
//			return null;
//		}

		private CommandMapping FindKeyMappingByMethod(string method)
		{
			foreach ( CommandMapping km in commandMappings )
			{
				if ( km.Method.Equals(method) )
					return km;
			}
			return null;
		}

		public bool Invoke(Keys keyData)
		{
			bool shift=(keyData & Keys.Shift) == Keys.Shift;
			bool ctrl=(keyData & Keys.Control) == Keys.Control;
			Keys key=(Keys) (Int16) keyData;

			if ( key == Keys.ControlKey || key == Keys.ShiftKey )
				return false;

			StringBuilder sb=new StringBuilder();
			if ( ctrl )
				sb.Append(Keys.Control.ToString()+"+");

			if ( shift )
				sb.Append(Keys.Shift.ToString()+"+");
			
			sb.Append(key.ToString());

			bool empty=queue.Length == 0;
			queue+=sb.ToString();

			CommandMapping km=Find(queue);
			if ( km == null )
			{
				queue=string.Empty;
				return !empty; // not handled
			}

			if ( !km.KeySequence.Equals(queue) )
			{
				// haven't found exact match yet
				queue+=",";
				return true;
			}

			queue=string.Empty;

			try
			{
				Invoke(km.Method);
			}
			catch ( XEditNetCommandException e )
			{
				MessageBox.Show("Failed to invoke command: "+km.Method);
			}
			return true;
		}

		private void Invoke(string method)
		{
			try
			{
				target.GetType().InvokeMember(method, BindingFlags.InvokeMethod, null, target, null);
			} 
			catch (Exception e)
			{
				Console.WriteLine("Exception "+e.StackTrace);
				throw new XEditNetCommandException("Failed to invoke command: "+method, e);
			}
		}

		private CommandMapping Find(string lookup)
		{
			foreach ( CommandMapping km in commandMappings )
			{
				if ( km.KeySequence.Equals(lookup) || km.KeySequence.StartsWith(lookup+",") )
					return km;
			}
			return null;
		}

		public CommandMapping[] Commands
		{
			get { return commandMappings.ToArray(typeof(CommandMapping)) as CommandMapping[]; }
		}
	}

	[Serializable]
	public class CommandMapping
	{
		[XmlAttribute]
		public string KeySequence=null;
		[XmlAttribute]
		public string Method=null;
		[XmlAttribute]
		public string MenuPath=null;
		[XmlAttribute]
		public int MenuIndex=-1;
		[XmlAttribute]
		public bool MenuBreak=false;
		[XmlAttribute]
		public string ButtonGroup=null;
		[XmlAttribute]
		public string ImagePath=null;

		public Shortcut[] Keys
		{
			get
			{
				if ( KeySequence == null )
					return new Shortcut[] {};

				ArrayList list=new ArrayList();
				foreach ( string kc in KeySequence.Split(',') )
				{
					Keys c=System.Windows.Forms.Keys.None;
					foreach ( string k in kc.Split('+') )
						c |= (Keys) Enum.Parse(typeof(Keys), k);

					list.Add((Shortcut) c);
				}
				return list.ToArray(typeof(Shortcut)) as Shortcut[];
			}
		}
	}

//	[Serializable]
//	internal class MenuMapping
//	{
//		[XmlAttribute]
//		public string MenuPath=null;
//		[XmlAttribute]
//		public string Method=null;
//	}

	[AttributeUsage(AttributeTargets.Method)]
	internal class CommandTarget : Attribute
	{
		public string Group=null;
		public string Alias=null;
	}
}
