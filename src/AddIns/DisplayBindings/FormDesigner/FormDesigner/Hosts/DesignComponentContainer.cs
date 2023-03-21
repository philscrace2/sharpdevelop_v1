// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using ICSharpCode.SharpDevelop.FormDesigner.Services;

namespace ICSharpCode.SharpDevelop.FormDesigner.Hosts
{
	// ISite implementation
	public class ComponentSite : ISite
	{
		ArrayList extenderProviders = new ArrayList();
		IComponent               component;
		IDesignerHost            host;
		bool                     isInDesignMode;
		string                   name;
		ServiceContainer         serviceContainer;
			
			
		public ComponentSite(IDesignerHost host, IComponent component)
		{
			this.component      = component;
			this.host           = host;
			this.isInDesignMode = true;
			serviceContainer = new ServiceContainer(host);
			serviceContainer.AddService(typeof(IDictionaryService),new DictionaryService());
		}
				
		public IComponent Component {
			get {
				return component;
			}
		}
			
		public IContainer Container {
			get {
				return host.Container;
			}
		}
			
		public bool DesignMode {
			get {
				return isInDesignMode;
			}
		}
			
		public string Name {
			get {
				return name;
			}
			set {
				Control nameable = component as Control;
				if (nameable != null) {
					nameable.Name = value;
				}
				name = value;
			}
		}
			
		public object GetService(Type serviceType)
		{
			object service = serviceContainer.GetService(serviceType);
//			if (service == null) {
//				Console.WriteLine("ComponentSite FAILED TO GET SERVICE: " + serviceType);
//			}
			return service;
		}
	}

	public class DesignComponentContainer : IContainer
	{
		DefaultDesignerHost host          = null;
		
		Hashtable    components    = new Hashtable();
		Hashtable    designers     = new Hashtable();
		
		IComponent          rootComponent = null;
		bool                disposing = false;
		
		public DesignComponentContainer(DefaultDesignerHost host)
		{
			this.host = host;
			host.Activated += new EventHandler(HostActivated);
		}
		
		void HostActivated(object sender, EventArgs e)
		{
			ComponentChangeService componentChangeService = host.GetService(typeof(IComponentChangeService)) as ComponentChangeService;
			if (componentChangeService != null) {
				componentChangeService.ComponentRename += new ComponentRenameEventHandler(OnComponentRename);
			}
		}
		
		void OnComponentRename(object sender, ComponentRenameEventArgs e)
		{
			if (components.Contains(e.OldName)) {
				components.Remove(e.OldName);
				components.Add(e.NewName, e.Component);
			}
		}
		
		public Hashtable ComponentHashtable {
			get {
				return components;
			}
		}
		
		public IDictionary Designers {
			get {
				return designers;
			}
		}
		
		public IComponent RootComponent {
			get {
				return rootComponent;
			}
		}
		
		public ComponentCollection Components {
			get {
				IComponent[] componentList = new IComponent[components.Count];
				components.Values.CopyTo(componentList, 0);
				return new ComponentCollection(componentList);
			}
		}
		
		public void Reset()
		{
		}
		
		public void Dispose()
		{
			disposing = true;	
			foreach (IDesigner designer in designers.Values) {
				// might throw
				try {
					designer.Dispose();
				} catch (Exception e) {
					Console.WriteLine("Error disposing designer : " + designer + "\n" + e.ToString());
				}
			}
			designers.Clear();		
			
			foreach (IComponent component in components.Values) {
				// might throw
				try {
					component.Dispose();
				} catch (Exception e) {
					Console.WriteLine("Error disposing component : " + component + "\n" + e.ToString());
				}
			}
			components.Clear();		
			disposing = false;
		}
		
		public bool ContainsName(string name)
		{
			return components.Contains(name);
		}
		
		/// <summary>
		/// Used for components without a designer from framework (e.g. for components which have a designer in an unknown dll).
		/// </summary>
		public class DummyDesigner : ComponentDesigner
		{
			DesignerVerbCollection designerVerbCollection = new DesignerVerbCollection();
			
//			public override void DoDefaultAction()
//			{
//			}
			
			public override void Initialize(IComponent component)
			{
				base.Initialize(component);
			}
			
			public override ICollection AssociatedComponents { 
				get {
					return new ArrayList();
				}
			}
			
			public override DesignerVerbCollection Verbs {
				get {
					return designerVerbCollection;
				}
			}
		}
		
		Assembly MyResolveEventHandler(object sender, ResolveEventArgs args)
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly asm in assemblies) {
				if (args.Name == asm.FullName) {
					return asm;
				}
			}
			
			return null;
		}
		
		public void Add(IComponent component, string name)
		{
			if (name == null) {
				INameCreationService nameCreationService = (INameCreationService)host.GetService(typeof(INameCreationService));
				name = nameCreationService.CreateName(this, component.GetType());
			}
			
			if (ContainsName(name)) {
				throw new ArgumentException("name", "A component named " + name + " already exists in this container");
			}
			
			try {
				ISite site = new ComponentSite(host, component);
				site.Name = name;
				AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(MyResolveEventHandler);
				component.Site = site;
				
			} catch (Exception e) {
				Console.WriteLine("Can't site component {0} ({1}) : " + e, component, name);
			} finally {
				AppDomain.CurrentDomain.AssemblyResolve -= new ResolveEventHandler(MyResolveEventHandler);
			}
				
			ComponentChangeService componentChangeService = host.GetService(typeof(IComponentChangeService)) as ComponentChangeService;
			
			if (componentChangeService != null) {
				componentChangeService.OnComponentAdding(component);
			}
			
			try {
				IDesigner designer = null;
				if (rootComponent == null) {
					// this is the first component. It must be the
					// "root" component and therefore it must offer
					// a root designer
					try {
						designer = TypeDescriptor.CreateDesigner(component, typeof(IRootDesigner));
					} catch (Exception e) { 
						Console.WriteLine("Can't create root designer : " + e); 
					}
					if (designer != null) {
						rootComponent = component;
					} else {
						designer = this.CreateDesigner(component);
//						designer = TypeDescriptor.CreateDesigner(component, typeof(IDesigner));
					}
				} else {
					designer = this.CreateDesigner(component);
//					designer = TypeDescriptor.CreateDesigner(component, typeof(IDesigner));
				}
				
				if (designer == null) {
					designer = new DummyDesigner();
				}
				
				// If we got a designer, initialize it
				if (designer != null) {
					designers[component] = designer;
					designer.Initialize(component);
				}
			} catch (Exception ex) {
				Console.WriteLine("Error creating the designer for component: " + component + " : " + ex.ToString());
			}
			
			if (component is IExtenderProvider) {
				IExtenderProviderService extenderProviderService = (IExtenderProviderService)host.GetService(typeof(IExtenderProviderService));
				extenderProviderService.AddExtenderProvider((IExtenderProvider)component);
			}
			this.components[name] = component;
			
			if (componentChangeService != null) {
				componentChangeService.OnComponentAdded(component);
			}
		}
		
		
		// The DesignerAttribute.DesignerTypeName when it's created from [Designer(typeof(CustomDesigner))]
		// is an AssemblyFullyQualifiedName like:
		// "MyNameSpace.MyDesigner, MyDll, Version=1.0.1777.23070, Culture=neutral, PublicKeyToken=null"
		// However, the TypeDescriptor.CreateDesigner method expects a FullName like: "MyNameSpace.MyDesigner"
		// Solution:
		public IDesigner CreateDesigner(IComponent component)
		{
			Type type = null;
			IDesigner designer = null;
			AttributeCollection collection = TypeDescriptor.GetAttributes(component);
			
			for (int i = 0; i < collection.Count; i++) {
				if (collection[i] is DesignerAttribute) {
					DesignerAttribute attribute = (DesignerAttribute) collection[i];
					string designerTypeName = attribute.DesignerTypeName;
					int pos = designerTypeName.IndexOf(",");
					
					if (pos != -1){
						designerTypeName = designerTypeName.Substring(0, pos);
					}
					string baseTypeName = attribute.DesignerBaseTypeName;
					pos = baseTypeName.IndexOf(",");
					
					if (pos != -1){
						baseTypeName = baseTypeName.Substring(0, pos);
					}
					if (baseTypeName.EndsWith("IDesigner")) {
						ISite site = component.Site;
						bool flag = false;
						if (site != null) {
							ITypeResolutionService service = (ITypeResolutionService) site.GetService(typeof(ITypeResolutionService));
							if (service != null) {
								flag = true;
								type = service.GetType(designerTypeName);
							}
						}
						if (!flag) {
							type = Type.GetType(designerTypeName);
						}
						if (type != null) {
							break;
						}
					}
				}
			}
			if (type != null) {
				designer = (IDesigner) Activator.CreateInstance(type, 
				                                                BindingFlags.CreateInstance | 
				                                                BindingFlags.NonPublic | 
				                                                BindingFlags.Public | 
				                                                BindingFlags.Instance, 
				                                                null, null, null);
			}
			if (designer == null) {
				designer = TypeDescriptor.CreateDesigner(component, typeof(IDesigner));
			}
			return designer;
		}
		
		public void Add(IComponent component)
		{
			this.Add(component, null);
		}
		
		public void Clear()
		{
			ArrayList c = new ArrayList();
			foreach (object o in components.Values) {
				c.Add(o);
			}
			foreach (IComponent component in c) {
				Remove(component);
			}
				
		}
		public void Remove(IComponent component)
		{
			if (disposing == true) {
				return;
			}
			
			string name = null;
			ISite site  = component.Site;
			
			if (site != null) {
				name = site.Name;
			} else {
				foreach (string k in components.Keys) {
					IComponent c = components[k] as IComponent;
					if (c == component) {
						name = k;
						break;
					}
				}
			}
			
			if (name == null) {
				return;
			}
			
			ComponentChangeService componentChangeService = componentChangeService = host.GetService(typeof(IComponentChangeService)) as ComponentChangeService;
			if (componentChangeService != null) {
				componentChangeService.OnComponentRemoving(component);
			}
			
			if (components.Contains(name)) {
				// Remove Component from Tray (ComponentTray part of System.Windows.Forms.Design)
				ComponentTray tray = host.GetService(typeof(ComponentTray)) as ComponentTray;
				if (tray != null) {
					tray.RemoveComponent(component);
				}
				
				components.Remove(name);
				
				// remove & dispose designer
				IDesigner designer = designers[component] as IDesigner;
				if (designer != null) {
					designers.Remove(component);
					try {
						designer.Dispose();
					} catch (Exception e) {
						Console.WriteLine("Can't dispose designer " + e);
					}
				}
			}
			
			if (componentChangeService != null) {
				componentChangeService.OnComponentRemoved(component);
			}
			
			// remove site from component
			if (site != null) {
				component.Site = null;
			}
			
			if (component is IExtenderProvider) {
				IExtenderProviderService extenderProviderService = (IExtenderProviderService)host.GetService(typeof(IExtenderProviderService));
				extenderProviderService.RemoveExtenderProvider((IExtenderProvider)component);
			}
		}
	}
}
