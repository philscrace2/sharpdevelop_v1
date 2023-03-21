// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Denis ERCHOFF" email="d_erchoff@hotmail.com"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Collections;
using System.Xml;

namespace ICSharpCode.SharpDevelop.FormDesigner.Gui
{
	public class ToolComponent
	{
		string fullName;
		string assemblyName;
		string hintPath;
		bool   isEnabled = true;
		
		public string HintPath {
			get {
				return hintPath;
			}
			set {
				hintPath = value;
			}
		}
		
		public string FullName {
			get {
				return fullName;
			}
			set {
				fullName = value;
			}
		}
		
		public string Name {
			get {
				int idx = fullName.LastIndexOf('.');
				if (idx > 0) {
					return fullName.Substring(idx + 1);
				}
				return fullName;
			}
		}
		
		public string Namespace {
			get {
				int idx = fullName.LastIndexOf('.');
				if (idx > 0) {
					return fullName.Substring(0, idx);
				}
				return String.Empty;
			}
		}
		
		public bool IsEnabled {
			get {
				return isEnabled;
			}
			set {
				isEnabled = value;
			}
		}
		
		string AssemblyFileNameWithoutPath {
			get {
				int idx = assemblyName.IndexOf(',');
				
				return assemblyName.Substring(0, idx) + ".dll";
			}
		}
		
		public string AssemblyName {
			get {
				return assemblyName;
			}
			set {
				assemblyName = value;
			}
		}
		protected ToolComponent()
		{
		}
		public ToolComponent(string fullName, ComponentAssembly assembly)
		{
			this.fullName = fullName;
			this.assemblyName = assembly.Name;
			this.hintPath     = assembly.HintPath;
		}
		public string FileName {
			get {
				if (HintPath == null) {
					return null;
				}
				return Path.Combine(HintPath, AssemblyFileNameWithoutPath);
			}
		}
		
		public Assembly LoadAssembly()
		{
			if (HintPath != null) {
				return Assembly.LoadFrom(FileName);
			} 
			return Assembly.Load(AssemblyName);
		}
		
		public object Clone()
		{
			ToolComponent toolComponent = new ToolComponent();
			toolComponent.FullName     = fullName;
			toolComponent.AssemblyName = assemblyName;
			toolComponent.IsEnabled    = isEnabled;
			return toolComponent;
		}
	}
	
	public class Category 
	{
		string    name;
		bool      isEnabled  = true;
		ArrayList components = new ArrayList();
		
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		
		public bool IsEnabled {
			get {
				return isEnabled;
			}
			set {
				isEnabled = value;
			}
		}
		
		public ArrayList ToolComponents {
			get {
				return components;
			}
		}
		
		protected Category()
		{
		}
		
		public Category(string name)
		{
			this.name = name;
		}
		
		public object Clone()
		{
			Category newCategory = new Category();
			newCategory.Name      = name;
			newCategory.IsEnabled = isEnabled;
			foreach (ToolComponent toolComponent in components) {
				newCategory.ToolComponents.Add(toolComponent.Clone());
			}
			return newCategory;
		}
	}
	
	public class ComponentAssembly
	{
		string name;
		string hintPath;
		
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		public string HintPath {
			get {
				return hintPath;
			}
			set {
				hintPath = value;
			}
		}
		public ComponentAssembly(string name)
		{
			this.name = name;
			this.hintPath = null;
		}
		public ComponentAssembly(string name, string hintPath)
		{
			this.name = name;
			this.hintPath = hintPath;
		}
		public override string ToString()
		{
			return name;
		}
	}
	
	public class ComponentLibraryLoader
	{
		static readonly string VERSION = "1.1.0";
		
		ArrayList assemblies = new ArrayList();
		ArrayList categories = new ArrayList();
		
		public ArrayList Categories {
			get {
				return categories;
			}
			set {
				categories = value;
			}
		}
		
		public ArrayList CopyCategories()
		{
			ArrayList newCategories = new ArrayList();
			foreach (Category category in categories) {
				newCategories.Add(category.Clone());
			}
			return newCategories;
		}
		
		
		public bool LoadToolComponentLibrary(string fileName)
		{
			if (!File.Exists(fileName)) {
				return false;
			}
			
			try {
				XmlDocument doc = new XmlDocument();
				doc.Load(fileName);
				
				if (doc.DocumentElement.Name != "SharpDevelopControlLibrary" ||
				    doc.DocumentElement.Attributes["version"] == null ||
				    doc.DocumentElement.Attributes["version"].InnerText != VERSION) {
					return false;
				}
				
				foreach (XmlNode node in doc.DocumentElement["Assemblies"].ChildNodes) {
					if (node.Name == "Assembly") {
						string assemblyName = node.Attributes["assembly"].InnerText;
						Console.WriteLine("Load assembly :" + assemblyName);
						if (node.Attributes["path"] != null) {
							Console.WriteLine("path :>" + node.Attributes["path"].InnerText+"<");
							assemblies.Add(new ComponentAssembly(assemblyName, node.Attributes["path"].InnerText));
						} else {
							assemblies.Add(new ComponentAssembly(assemblyName));
						}
					}
				}
				
				foreach (XmlNode node in doc.DocumentElement["Categories"].ChildNodes) {
					if (node.Name == "Category") {
						string name = node.Attributes["name"].InnerText;
						Category newCategory = new Category(name);
						foreach (XmlNode componentNode in node.ChildNodes) {
														ToolComponent newToolComponent = new ToolComponent(componentNode.Attributes["class"].InnerText,
							                                                   (ComponentAssembly)assemblies[Int32.Parse(componentNode.Attributes["assembly"].InnerText)]);
							newCategory.ToolComponents.Add(newToolComponent);
						}
						categories.Add(newCategory);
					}
				}
			} catch (Exception e) {
				Console.WriteLine(e);
				return false;
			}
			return true;
		}
		
		public Bitmap GetIcon(ToolComponent component)
		{
			Assembly asm = component.LoadAssembly();
			Type type = asm.GetType(component.FullName);
			Bitmap b = null;
			if (type != null) {
				object[] attributes = type.GetCustomAttributes(false);
				foreach (object attr in attributes) {
					if (attr is ToolboxBitmapAttribute) {
						ToolboxBitmapAttribute toolboxBitmapAttribute = (ToolboxBitmapAttribute)attr;
						b = new Bitmap(toolboxBitmapAttribute.GetImage(type));
						b.MakeTransparent();
						break;
					}
				}
			}
			if (b == null) {
				try {
				 	b = new Bitmap(Image.FromStream(asm.GetManifestResourceStream(component.FullName + ".bmp")));
					b.MakeTransparent();
				} catch (Exception e) {
					Console.WriteLine(e);
				}
			
			}
			
			// TODO: Maybe default icon needed ??!?!
			return b;
		}
		
		public ToolComponent GetToolComponent(string assemblyName)
		{
			foreach (Category category in categories) {
				foreach (ToolComponent component in category.ToolComponents) {
					if (component.AssemblyName == assemblyName) {
						return component;
					}
				}
			}
			return null;
		}
		
		public void SaveToolComponentLibrary(string fileName)
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<SharpDevelopControlLibrary version=\"" + VERSION + "\"/>");
			Hashtable assemblyHashTable = new Hashtable();
			
			XmlElement assemblyNode = doc.CreateElement("Assemblies");
			doc.DocumentElement.AppendChild(assemblyNode);
			for (int i = 0; i < assemblies.Count; ++i) {
				ComponentAssembly componentAssembly = (ComponentAssembly)assemblies[i];
				assemblyHashTable[componentAssembly.Name] = i;
				
				XmlElement newAssembly = doc.CreateElement("Assembly");
				
				newAssembly.SetAttribute("assembly", componentAssembly.Name);
				if (componentAssembly.HintPath != null) {
					newAssembly.SetAttribute("path", componentAssembly.HintPath);
				}
				assemblyNode.AppendChild(newAssembly);
			}
			
			XmlElement categoryNode = doc.CreateElement("Categories");
			doc.DocumentElement.AppendChild(categoryNode);
			foreach (Category category in categories) {
				XmlElement newCategory = doc.CreateElement("Category");
				newCategory.SetAttribute("name", category.Name);
				newCategory.SetAttribute("enabled", category.IsEnabled.ToString());
				categoryNode.AppendChild(newCategory);
				
				foreach (ToolComponent component in category.ToolComponents) {
					XmlElement newToolComponent = doc.CreateElement("ToolComponent");
					newToolComponent.SetAttribute("class", component.FullName);
					
					if (assemblyHashTable[component.AssemblyName] == null) {
						XmlElement newAssembly = doc.CreateElement("Assembly");
						newAssembly.SetAttribute("assembly", component.AssemblyName);
						if (component.HintPath != null) {
							newAssembly.SetAttribute("path", component.HintPath);
						}
						
						assemblyNode.AppendChild(newAssembly);
						assemblyHashTable[component.AssemblyName] = assemblyHashTable.Values.Count;
					}
					
					newToolComponent.SetAttribute("assembly", assemblyHashTable[component.AssemblyName].ToString());
					newToolComponent.SetAttribute("enabled", component.IsEnabled.ToString());
					newCategory.AppendChild(newToolComponent);
				}
			}
			doc.Save(fileName);
		}
	}
}
