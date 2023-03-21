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
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Xml;
using System.Resources;
using System.CodeDom;

using ICSharpCode.Core.Services;
using ICSharpCode.SharpDevelop.FormDesigner.Hosts;
using ICSharpCode.SharpDevelop.FormDesigner;

namespace ICSharpCode.SharpDevelop.FormDesigner.Services
{
	internal class DefaultSerializationObjectCodeDomSerializer : CodeDomSerializer
	{
		public DefaultSerializationObjectCodeDomSerializer()
		{
		}

		public override object Serialize(IDesignerSerializationManager manager, object value)
		{
				return new CodeStatementCollection(); 
		}

		public override object Deserialize(IDesignerSerializationManager manager, object codeObject)
		{
				return null; 
		}
	}

	/// <summary>
	/// This class is responsible for serializing and deserializing of the object that use
	/// DesignerSerializationService. It holds the information that could be divided into
	/// 4 parts: 
	///   1) CODE of all components 
	///   2) RESOURCES (such as images, and so on)
	///   3) DESIGN ONLY Properties tha are not serialized by main serializer
	///   4) OBJECTS that are not components and are serializable
	///   
	/// This object itself is a component that can be serialized, but it serializes only
	/// empty statement collection
	/// </summary>
	[Serializable]
	[DesignerSerializer(typeof(DefaultSerializationObjectCodeDomSerializer),typeof(CodeDomSerializer))]
	public class DefaultSerializationObject : Component, 
		                                        IDesignerSerializationManager, 
		                                        System.ComponentModel.Design.IResourceService, 
		                                        ISerializable, 
		                                        IContainer
	{
		private IDesignerSerializationManager manager_; 
		private CodeDomSerializer rootSerializer_;

		#region String Identifiers 
		/// <summary>
		/// these are strings used to identify the parts of the object
		/// when serialized
		/// </summary>
		private string serializedCode_                 = "Code";
		private string serializedResources_            = "Resources";
		private string serializedDesignOnlyProperties_ = "DesignOnlyProperties";
		private string serializedObjects_              = "Objects";
		#endregion

		#region SerializedInformation Holders
		private object            code_;
		private MyResourceManager resourceManager_;
		private Hashtable         designOnlyProperties_;
		private object[]          objects_; 
		#endregion

		private Hashtable componentsAdded_;
		private IComponentChangeService componentChangeService_;

		private ArrayList containerComponents_;
		private ArrayList designerSerializationProviders_;

		private Hashtable namedObjects_;
		private Hashtable objectNames_; 

		#region MyResourceManager
		private class MyResourceManager : ComponentResourceManager, IResourceWriter, IResourceReader
		{
			#region MyResourceSet
			private class MyResourceSet : ResourceSet
			{
				public MyResourceSet(Hashtable data)
				{ 
					base.Table = data;
				}
			}
			#endregion

			private Hashtable data_;

			internal IDictionary Data 
			{ 
				get
				{
					if (data_ == null)
						data_ = new Hashtable();
					return data_; 
				}
			} 

			public MyResourceManager(Hashtable data)
			{ 
				data_ = data; 
			}
  
			public MyResourceManager()
			{
			}

			#region IResourceWriter
			public void AddResource(string name, byte[] value)
			{ 
				Data[name] = value;
			}
 
			public void AddResource(string name, object value)
			{ 
				Data[name] = value;
			}
  
			public void AddResource(string name, string value)
			{ 
				Data[name] = value;
			}
  
			public void Close()
			{  
			}

			public void Generate()
			{  
			}
			#endregion
 
			#region IResourceReader
			public IDictionaryEnumerator GetEnumerator()
			{ 
				return Data.GetEnumerator(); 
			}
			#endregion

			#region ResourceSet
			public void Dispose()
			{ 
				Data.Clear(); 
			}

 			public override object GetObject(string name)
			{ 
				return Data[name]; 
			}
 
			public override string GetString(string name)
			{ 
				return (Data[name] as string); 
			}

			public override ResourceSet GetResourceSet(CultureInfo culture, bool createIfNotExists, bool tryParents)
			{
				return new MyResourceSet(data_); 
			}
			#endregion
  
			IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{ 
				return this.GetEnumerator(); 
			}
		}
		#endregion

		#region MyResourceManagerCodeDomSerializer
		private class MyResourceManagerCodeDomSerializer : CodeDomSerializer
		{
			private CodeDomSerializer          serializer_;
			private DefaultSerializationObject owner_;

			public MyResourceManagerCodeDomSerializer(DefaultSerializationObject owner, CodeDomSerializer serializer)
			{
				owner_           = owner;
				serializer_      = serializer;
			}

			public override object Serialize(IDesignerSerializationManager manager, object value)
			{ 
				return serializer_.Serialize(manager, value); 
			}

			public override object Deserialize(IDesignerSerializationManager manager, object codeObject)
			{ 
				object resourceManager = null;
				foreach (CodeStatement statement in ((CodeStatementCollection) codeObject))
				{
					CodeVariableDeclarationStatement varStatement =	statement as CodeVariableDeclarationStatement;
					if (varStatement != null)
					{
						resourceManager = owner_.resourceManager_;
						manager.SetName(resourceManager, varStatement.Name);
						codeObject = new CodeStatementCollection(((CodeStatementCollection) codeObject));
						((CodeStatementCollection) codeObject).Remove(statement);
					}
				}
				serializer_.Deserialize(manager, codeObject);
				return resourceManager; 
			}
		}
		#endregion

		#region MySite
		private class MySite : ISite, ITypeDescriptorFilterService
		{
			private ITypeDescriptorFilterService filter_;

			public MySite(DefaultSerializationObject container, IComponent component, string name)
			{ 
				container_ = container;
				component_ = component;
				name_ = name;
			}
   
			public object GetService(Type type)
			{ 
				if (type == typeof(ITypeDescriptorFilterService))
				{
					if (filter_ == null)
					{
						filter_ = ((ITypeDescriptorFilterService) container_.GetService(type));
						if (filter_ == null)
							filter_ = this; 
					}
					return this; 
				}
				return container_.GetService(type); 
			}
  
			bool System.ComponentModel.Design.ITypeDescriptorFilterService.FilterAttributes(IComponent component, IDictionary attributes)
			{ 
				if (filter_ != this)
				{
					return filter_.FilterAttributes(component, attributes); 
				}
				return true; 
			}

			bool System.ComponentModel.Design.ITypeDescriptorFilterService.FilterEvents(IComponent component, IDictionary events)
			{ 
				if (filter_ != this)
				{
					return filter_.FilterEvents(component, events); 
				}
				return true; 
			}
  
			bool System.ComponentModel.Design.ITypeDescriptorFilterService.FilterProperties(IComponent component, IDictionary properties)
			{ 
				ArrayList   readOnlyDescriptors = null;
				Attribute[] attributes;
				if (filter_ != this)
				{
					filter_.FilterProperties(component, properties);
				}
				foreach (DictionaryEntry property in properties)
				{
					PropertyDescriptor descriptor = ((PropertyDescriptor) property.Value);
					if (descriptor.IsReadOnly)
					{
						if (readOnlyDescriptors == null)
							readOnlyDescriptors = new ArrayList(); 
						attributes = new Attribute[1] {ReadOnlyAttribute.No};
						readOnlyDescriptors.Add(TypeDescriptor.CreateProperty(descriptor.ComponentType, descriptor, attributes));
					}
				}
				if (readOnlyDescriptors != null)
				{
					foreach (PropertyDescriptor readOnlyDescriptor in readOnlyDescriptors)
					{
						properties[readOnlyDescriptor.Name] = readOnlyDescriptor;
					}
				}
				return false; 
			}

			#region ISite
			#region Component
			private IComponent component_;  
			public IComponent Component 
			{ 
				get { return component_; } 
			}
			#endregion

			#region Container
			private DefaultSerializationObject container_;
			public IContainer Container 
			{ 
				get { return container_; } 
			}
			#endregion

			#region DesignMode
			public bool DesignMode 
			{ 
				get
				{
					return true; 
				} 
			}
			#endregion

			#region Name
			private string name_; 
			public string Name 
			{ 
				get { return name_; } 
				set { name_ = value; } 
			}
			#endregion
			#endregion
		}
		#endregion 

		public DefaultSerializationObject(IDesignerSerializationManager manager, CodeDomSerializer rootSerializer, ICollection objectCollection)
		{ 
			manager_        = manager;
			rootSerializer_ = rootSerializer;

			code_           = null;
			objects_        = new object[objectCollection.Count];
			objectCollection.CopyTo(objects_, 0);
		}

		public DefaultSerializationObject(SerializationInfo info, StreamingContext context)
		{ 
			code_ = info.GetValue(serializedCode_, typeof(object));

			ArrayList serializedObjects = ((ArrayList) info.GetValue(serializedObjects_, typeof(ArrayList)));
			objects_ = serializedObjects.ToArray();

			Hashtable serializedResources = ((Hashtable) info.GetValue(serializedResources_, typeof(Hashtable)));
			if (serializedResources != null)
				resourceManager_ = new MyResourceManager(serializedResources);

			designOnlyProperties_ = ((Hashtable) info.GetValue(serializedDesignOnlyProperties_, typeof(Hashtable)));
		}

		protected override void Dispose(bool disposing)
		{
			if (componentChangeService_ != null)
			{
				componentChangeService_.ComponentAdded -= new ComponentEventHandler(OnComponentAdded);
				componentChangeService_ = null;
			}
			if (disposing)
			{
				objects_ = null;
			}
		}

		private ArrayList FinalizeSerialization(bool serialize)
		{
			if (SerializationComplete != null) {
				try {
					SerializationComplete(this, EventArgs.Empty);
				} catch (Exception e) { 
					Console.WriteLine(e);
				} 
			}
			// clear all callbacks
			ResolveName = null;
			SerializationComplete = null;

			// clear all serialization things
			manager_ = null;
			designerSerializationProviders_ = null;
			contextStack_ = null;

			// clear all hashtables
			namedObjects_ = null;
			objectNames_ = null;

			if (serialize == false)
			{
				((IContainer)this).Remove(this);
			}
			ArrayList comps = containerComponents_;
			containerComponents_ = null;
			return comps;
		}

		#region Deserialize
		public ICollection Deserialize(IDesignerSerializationManager manager, CodeDomSerializer rootSerializer)
		{ 
			manager_ = manager;
			
			ArrayList deserializedObjects = null;
			object[]  arrayOfObjects;
			
			if ((objects_ != null) && (code_ == null)) {
				return objects_; 
			}
			IDesignerHost host = ((IDesignerHost) manager.GetService(typeof(IDesignerHost)));
			if (host != null) {
				componentChangeService_ = ((IComponentChangeService) host.GetService(typeof(IComponentChangeService)));
				if (componentChangeService_ != null) {
					host.RemoveService(typeof(IComponentChangeService));
					host.AddService(typeof(IComponentChangeService), new ComponentChangeService());
				}
			}
			try {
				rootSerializer.Deserialize(this, code_);
			} finally {
				if (componentChangeService_ != null) {
					host.RemoveService(typeof(IComponentChangeService));
					host.AddService(typeof(IComponentChangeService), componentChangeService_);
				}
				deserializedObjects = FinalizeSerialization(false);
			}
			if (deserializedObjects != null) {
				
				if ((host != null) && deserializedObjects.Contains(host.RootComponent)) {
					deserializedObjects.Remove(host.RootComponent);
				}
				
				arrayOfObjects = new object[objects_.Length + deserializedObjects.Count];
				objects_.CopyTo(arrayOfObjects, 0);
				deserializedObjects.CopyTo(arrayOfObjects, objects_.Length);
				objects_ = arrayOfObjects;
				if (host != null && componentChangeService_ != null) {
					componentChangeService_.ComponentAdded += new ComponentEventHandler(OnComponentAdded);
					componentsAdded_ = new Hashtable();
					NameCreationService ncs = (NameCreationService)host.GetService(typeof(INameCreationService));
					foreach (IComponent component in deserializedObjects) {
						componentsAdded_.Add(component.Site.Name, component);
//						System.Windows.Forms.Control control =  component as System.Windows.Forms.Control;
//						if (control != null && control.Parent == null) {
//							component.Site = null;
//						}
					}
				}
			}
			return objects_; 
		} 

		private void OnComponentAdded(object sender, ComponentEventArgs ce)
		{ 
			string componentName = null;
			if ((componentsAdded_ == null) || !componentsAdded_.ContainsValue(ce.Component)) {
				return; 
			}
			Attribute[] designOnlyAttributes = new Attribute[1] { DesignOnlyAttribute.Yes };
			foreach (DictionaryEntry componentEntry in componentsAdded_) {
				if (componentEntry.Value == ce.Component) {
					string oldName = ce.Component.Site.Name;
					
					PropertyDescriptorCollection designOnlyPropCollection = TypeDescriptor.GetProperties(ce.Component, designOnlyAttributes);
					componentName = (string)componentEntry.Key;
				
					foreach(DictionaryEntry designOnlyEntry in designOnlyProperties_) {
						string designOnlyPropertyName = ((string) designOnlyEntry.Key);
		
						int dotIndex = designOnlyPropertyName.IndexOf('.');
							
						if ((dotIndex != -1) && (designOnlyPropertyName.Substring(0, dotIndex) == componentName)) {
							string propertyName = designOnlyPropertyName.Substring((dotIndex + 1));
							
							if (designOnlyPropertyName == ".Name") {
								string newName = designOnlyEntry.Value.ToString();
								// check for duplicate/invalid name using name creation service.
								NameCreationService ncs = (NameCreationService)ce.Component.Site.GetService(typeof(INameCreationService));
								if (ncs.IsValidName(newName)) {
									ce.Component.Site.Name = newName;
									((ComponentChangeService)this.componentChangeService_).OnComponentRename(ce.Component, oldName, newName);
								}
								continue;
							}
							
							PropertyDescriptor propDescriptor = designOnlyPropCollection[propertyName];
							if (propDescriptor != null) {
								
								try {
									propDescriptor.SetValue(ce.Component, designOnlyEntry.Value); 
								} catch (Exception e) { 
									Console.WriteLine(e);
								} 
							}
						}
					}
				} 
			}
			if (componentName != null)
			  componentsAdded_.Remove(componentName);
			if ((componentsAdded_.Count == 0) && (componentChangeService_ != null))
			{
				componentChangeService_.ComponentAdded -= new ComponentEventHandler(OnComponentAdded);
				componentChangeService_ = null;
				componentsAdded_ = null;
			}
		}
		#endregion

		#region Serialize
		private Hashtable SerializeDesignOnlyProperties(IDesignerSerializationManager manager, ICollection objectList)
		{
			Hashtable arrayOfDesignTimeProperties = new Hashtable();
			Attribute[] designOnlyAttribute = new Attribute[1] { DesignOnlyAttribute.Yes };
			foreach(object myObject in objectList) {
				PropertyDescriptorCollection designOnlyProperties = TypeDescriptor.GetProperties(myObject, designOnlyAttribute);
				string objectName = manager.GetName(myObject);
				foreach(PropertyDescriptor propDescriptor in designOnlyProperties) {
					string designOnlyPropertyName = objectName + "." + propDescriptor.Name;
					object designOnlyPropertyValue = propDescriptor.GetValue(myObject);
					
					if (propDescriptor.ShouldSerializeValue(myObject) && ((designOnlyPropertyValue == null) || designOnlyPropertyValue.GetType().IsSerializable)) {
						arrayOfDesignTimeProperties[designOnlyPropertyName] = designOnlyPropertyValue;
						//Console.WriteLine("Serialize : " + designOnlyPropertyName + " == " + designOnlyPropertyValue);
					}
				}
			}
			return arrayOfDesignTimeProperties;
		}
		#endregion

		#region IResourceService
		IResourceReader System.ComponentModel.Design.IResourceService.GetResourceReader(CultureInfo info)
		{ 
			if (resourceManager_ == null)
			{
				resourceManager_ = new MyResourceManager();
			}
			return resourceManager_; 
		}

		IResourceWriter System.ComponentModel.Design.IResourceService.GetResourceWriter(CultureInfo info)
		{ 
			if (resourceManager_ == null)
			{
				resourceManager_ = new MyResourceManager();
			}
			return resourceManager_; 
		}
		#endregion

		#region IDesignerSerializationManager
		public event ResolveNameEventHandler ResolveName;
		public event EventHandler SerializationComplete;

		void System.ComponentModel.Design.Serialization.IDesignerSerializationManager.AddSerializationProvider(IDesignerSerializationProvider provider)
		{ 
			if (designerSerializationProviders_ == null)
			{
				designerSerializationProviders_ = new ArrayList();
			}
			designerSerializationProviders_.Add(provider);
		}

		object System.ComponentModel.Design.Serialization.IDesignerSerializationManager.CreateInstance(Type type, ICollection arguments, string name, bool addToContainer)
		{ 
			object        myObject = null;
			IDesignerHost host = ((IDesignerHost) manager_.GetService(typeof(IDesignerHost)));
			object[]      argumentArray = null;

			if (type == base.GetType())
			{
				myObject = host.RootComponent;
			}
			if (typeof(ResourceManager).IsAssignableFrom(type))
			{
				myObject = resourceManager_;
			}
			if (myObject == null)
			{
				if (arguments != null && arguments.Count > 0)
				{
					argumentArray = new object[arguments.Count];
					arguments.CopyTo(argumentArray, 0); 
					myObject = Activator.CreateInstance(type, argumentArray); 
				}
				else
				  myObject = Activator.CreateInstance(type); 
			}
			if (name != null)
			{
				if (namedObjects_ == null)
				{
					namedObjects_ = new Hashtable();
					objectNames_ = new Hashtable();
				}
				namedObjects_[name] = myObject;
				objectNames_[myObject] = name;
				IComponent myComponent = myObject as IComponent;
				if (addToContainer && myComponent != null)
				{
//					if (myComponent.Site == null)
//						ISite site = new MySite(this, myComponent, name);
					((IContainer)this).Add(myComponent, name);
				}
			}
			return myObject; 
		}

		private ContextStack contextStack_; 
		ContextStack System.ComponentModel.Design.Serialization.IDesignerSerializationManager.Context
		{ 
			get
			{
				if (contextStack_ == null)
				{
					contextStack_ = new ContextStack();
				}
				return contextStack_;
			}
		}

		PropertyDescriptorCollection System.ComponentModel.Design.Serialization.IDesignerSerializationManager.Properties
		{ 
			get
			{
				return TypeDescriptor.GetProperties(this); 
			}
		}

		object System.ComponentModel.Design.Serialization.IDesignerSerializationManager.GetInstance(string name)
		{ 
			object myObject = null;
			if (name == null)
			{
				throw new ArgumentNullException("name"); 
			}
			if (namedObjects_ != null)
			{
				myObject = namedObjects_[name];
			}
			if (myObject == null && name.Equals("components"))
			{
				myObject = this;
			}
			if ((myObject == null) && (ResolveName != null))
			{
				ResolveNameEventArgs args = new ResolveNameEventArgs(name);
				ResolveName(this, args);
				myObject = args.Value;
			}
			if ((myObject == null) && (manager_ != null))
			{
				myObject = manager_.GetInstance(name);
			}
			if (myObject == null)
			{
				IReferenceService referenceService = ((IReferenceService) base.GetService(typeof(IReferenceService)));
				if (referenceService != null)
				{
					myObject = referenceService.GetReference(name); 
				}
			}
			return myObject; 
		}

		string System.ComponentModel.Design.Serialization.IDesignerSerializationManager.GetName(object value)
		{ 
			string name = null;
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (objectNames_ != null)
			{
				name = (string) objectNames_[value]; 
			}
			if (name == null && (value as IComponent) != null && ((IComponent) value).Site != null)
			{
				name = ((IComponent) value).Site.Name;
			}
			return name; 
		}

		object System.ComponentModel.Design.Serialization.IDesignerSerializationManager.GetSerializer(Type objectType, Type serializerType)
		{ 
			object serializer = manager_.GetSerializer(objectType, serializerType);
			if ((objectType != null && serializer != null) && (typeof(ResourceManager).IsAssignableFrom(objectType) && typeof(CodeDomSerializer).IsAssignableFrom(serializerType)))
			{
				serializer = new MyResourceManagerCodeDomSerializer(this, ((CodeDomSerializer) serializer)); 
			}
			if (objectType != null && ((serializer != null && typeof(ResourceManager).IsAssignableFrom(objectType)) && typeof(CodeDomSerializer).IsAssignableFrom(serializerType)))
			{
				serializer = new MyResourceManagerCodeDomSerializer(this, ((CodeDomSerializer) serializer)); 
			}
			if (designerSerializationProviders_ != null)
			{
				foreach (IDesignerSerializationProvider provider in designerSerializationProviders_) 
				{
					object providerSerializer = provider.GetSerializer(this, serializer, objectType, serializerType);
					if (providerSerializer != null)
						serializer = providerSerializer;
				}
			}
			return serializer; 
		}

		Type System.ComponentModel.Design.Serialization.IDesignerSerializationManager.GetType(string typeName)
		{ 
			if (typeName.Equals(typeof(DefaultSerializationObject).FullName))
				return typeof(DefaultSerializationObject); 
			return manager_.GetType(typeName); 
		}

		void System.ComponentModel.Design.Serialization.IDesignerSerializationManager.RemoveSerializationProvider(IDesignerSerializationProvider provider)
		{ 
			if (designerSerializationProviders_ != null)
				designerSerializationProviders_.Remove(provider);
		}

		void System.ComponentModel.Design.Serialization.IDesignerSerializationManager.ReportError(object errorInformation)
		{  
		}

		void System.ComponentModel.Design.Serialization.IDesignerSerializationManager.SetName(object instance, string name)
		{ 
			object[] serializedNames = null;
			if (instance == null)
			{
				throw new ArgumentNullException("instance"); 
			}
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (namedObjects_ == null)
			{
				namedObjects_ = new Hashtable();
				objectNames_ = new Hashtable();
			}
			if (namedObjects_[name] != null)
			{
				serializedNames = new object[1];
				serializedNames[0] = name;
				throw new ArgumentException(string.Format("SerializerNameInUse {0}", serializedNames));
			}
			if (objectNames_[instance] != null)
			{
				serializedNames = new object[2];
				serializedNames[0] = name;
				serializedNames[1] = ((string) objectNames_[instance]);
				throw new ArgumentException(string.Format("SerializerObjectHasName {0}", serializedNames));
			}
			namedObjects_[name] = instance;
			objectNames_[instance] = name;
		}
		#endregion

		#region ISerializable
		void System.Runtime.Serialization.ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{ 
			int        index;
			IComponent myComponent;
  
			if ((code_ == null) && (manager_ != null))
			{
				namedObjects_ = new Hashtable(objects_.Length);
				objectNames_ = new Hashtable(objects_.Length);
				for (index = 0; index < objects_.Length; index++)
				{
					myComponent = objects_[index] as IComponent;
					if (myComponent != null && myComponent.Site != null && myComponent.Site.Name != null)
					{
						((IContainer)this).Add(myComponent, myComponent.Site.Name);
						namedObjects_[myComponent.Site.Name] = myComponent;
						objectNames_[myComponent] = myComponent.Site.Name;
					}
				}
				if ((containerComponents_ != null) && (containerComponents_.Count > 0))
				{
					((IContainer)this).Add(this, ComponentName);
					try
					{
						code_ = rootSerializer_.Serialize((IDesignerSerializationManager)this, this);
						designOnlyProperties_ = SerializeDesignOnlyProperties(manager_, objects_); 
					}
					finally
					{
						FinalizeSerialization(true);
					}
				}
				manager_ = null;
				rootSerializer_ = null;
			}
			ArrayList  nonComponentArray = new ArrayList();
			if (code_ != null)
			{
				for (index = 0; index < objects_.Length; index++)
				{
					myComponent = objects_[index] as IComponent;
					if (myComponent == null)
					{
						nonComponentArray.Add(objects_[index]); 
					}
				}
			}
			info.AddValue(serializedObjects_,              nonComponentArray);
			info.AddValue(serializedCode_,                 code_);
			info.AddValue(serializedResources_,            ((resourceManager_ != null) ? resourceManager_.Data : null));
			info.AddValue(serializedDesignOnlyProperties_, designOnlyProperties_); 
		}
		#endregion

		#region IContainer
		void System.ComponentModel.IContainer.Add(IComponent component)
		{ 
			((IContainer)this).Add(component, null);
		}

		void System.ComponentModel.IContainer.Add(IComponent component, string name)
		{ 
			if (name == null) {
				return; 
			}
			if (containerComponents_ == null) {
				containerComponents_ = new ArrayList();
				containerComponents_.Add(component);
			} else {
				bool found = false;
				foreach(object myObject in containerComponents_) {
					if (myObject == component) {
						found = true;
						break; 
					} 
				}
				if (found == false) {
					containerComponents_.Add(component);
				}
			}
 
			if (component.Site == null) {
				component.Site = new MySite(this, component, name);
			}
		}

		ComponentCollection System.ComponentModel.IContainer.Components { 
			get {
				IComponent[] components;
				if (containerComponents_ == null) {
					components = new IComponent[0]; 
				} else { 
					components = new IComponent[containerComponents_.Count];
					containerComponents_.CopyTo(components, 0);
				}
				return new ComponentCollection(components); 
			}
		}

		void System.ComponentModel.IContainer.Remove(IComponent component)
		{ 
			if (component.Site is MySite) {
				component.Site = null; 
			}
			if (containerComponents_ != null) {
				if (containerComponents_.Contains(component)) {
					containerComponents_.Remove(component); 
				}
			}
		}
		#endregion

		#region IServiceProvider
		object System.IServiceProvider.GetService(Type serviceType)
		{ 
			if (serviceType == typeof(System.ComponentModel.Design.IResourceService)) {
				return this; 
			}
			if (manager_ != null) {
				return manager_.GetService(serviceType); 
			}
			return null; 
		}
		#endregion

		#region Component
		protected override object GetService(Type t)
		{
			return ((IServiceProvider)this).GetService(t);
		}
		#endregion

		/// <summary>
		/// This is component name used for identifivcation of my object in the container
		/// Hopefully, this name is unique enough not to be really used
		/// </summary>
		private string ComponentName 
		{ 
			get
			{ 
				return "sdSerializationComponentSD";  
			} 
		}
	}
}
