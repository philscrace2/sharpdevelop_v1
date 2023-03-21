// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Drawing;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Xml;
using System.CodeDom;

using ICSharpCode.Core.Services;
using ICSharpCode.SharpDevelop.FormDesigner.Hosts;
using ICSharpCode.SharpDevelop.FormDesigner;

namespace ICSharpCode.SharpDevelop.FormDesigner.Services
{
	public class DesignerSerializationService : IDesignerSerializationService
	{
		IDesignerHost                       host = null;
		CodeDomSerializer                   rootSerializer = null;
		CodeDomDesignerSerializetionManager manager = null;
		
		public DesignerSerializationService(IDesignerHost host)
		{
			this.host = host;
			this.manager = (CodeDomDesignerSerializetionManager)host.GetService(typeof(IDesignerSerializationManager));				
		}
		
		sealed class DefaultSerializationObjectDeserializationBinder : SerializationBinder 
		{
			public override Type BindToType(string assemblyName, string typeName) 
			{
				Type typeToDeserialize = null;
				typeToDeserialize = Type.GetType(String.Format("{0}, {1}", typeName, assemblyName));
				return typeToDeserialize;
			}
		}
		
		void Initialize()
		{
			manager.Initialize();
			this.rootSerializer = manager.GetRootSerializer(this.host.RootComponent.GetType());
			if (rootSerializer == null) {
				throw new Exception("No root serializer found");
			}
		}
		
		public ICollection Deserialize(object serializationData)
		{
			if ((serializationData as MemoryStream) == null) {
				throw new ArgumentException("SerializerBadSerializationObject"); 
			}
			BinaryFormatter formatter = new BinaryFormatter();
			formatter.Binder =new DefaultSerializationObjectDeserializationBinder();

			((Stream) serializationData).Seek(0, 0);
			object dsObject = formatter.Deserialize(((Stream) serializationData));
			if ((dsObject as DefaultSerializationObject) == null) {
				throw new ArgumentException("SerializerBadSerializationObject"); 
			}
			Initialize();
			return ((DefaultSerializationObject) dsObject).Deserialize(manager, this.rootSerializer);
		}
		
		public object Serialize(ICollection objectCollection)
		{
			Initialize();
			DefaultSerializationObject dsObject = new DefaultSerializationObject(manager, this.rootSerializer, objectCollection);
			
			Stream stream = new MemoryStream();
			BinaryFormatter	formatter = new BinaryFormatter();
			formatter.Serialize(stream, dsObject);
			return stream;
	  }
	}
}
