// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Utility;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Xml;
using System.Text;

using ICSharpCode.Core.CoreProperties;
using ICSharpCode.Core.Services;
using ICSharpCode.SharpDevelop.Services;
using ICSharpCode.Core.AddIns;
using ICSharpCode.SharpDevelop.Internal.Project;
using ICSharpCode.SharpDevelop.Gui;
using SharpDevelop.Internal.Parser;

namespace ICSharpCode.SharpDevelop.Services
{
	public class DefaultParserService : AbstractService, IParserService
	{
		Hashtable classes                = new Hashtable();
		Hashtable caseInsensitiveClasses = new Hashtable();
		
		// used to map 'real' namespace hashtable inside case insensitive hashtable
		const string CaseInsensitiveKey = "__CASE_INSENSITIVE_HASH";
		Hashtable namespaces                = new Hashtable();
		Hashtable caseInsensitiveNamespaces = new Hashtable();
		
		Hashtable parsings   = new Hashtable();
		
		ParseInformation addedParseInformation = new ParseInformation();
		ParseInformation removedParseInformation = new ParseInformation();

		/// <remarks>
		/// The keys are the assemblies loaded. This hash table ensures that no
		/// assembly is loaded twice. I know that strong naming might be better but
		/// the use case isn't there. No one references 2 differnt files if he references
		/// the same assembly.
		/// </remarks>
		Hashtable loadedAssemblies = new Hashtable();
		
		ClassProxyCollection classProxies = new ClassProxyCollection();
		IParser[] parser;
		readonly static string[] assemblyList = {
			"Microsoft.VisualBasic",
			"Microsoft.JScript",
			"mscorlib",
			"System.Data",
			"System.Design",
			"System.DirectoryServices",
			"System.Drawing.Design",
			"System.Drawing",
			"System.EnterpriseServices",
			"System.Management",
			"System.Messaging",
			"System.Runtime.Remoting",
			"System.Runtime.Serialization.Formatters.Soap",

			"System.Security",
			"System.ServiceProcess",
			"System.Web.Services",
			"System.Web",
			"System.Windows.Forms",
			"System",
			"System.XML"
		};
		
		public DefaultParserService()
		{
			addedParseInformation.DirtyCompilationUnit = new DummyCompilationUnit();
			removedParseInformation.DirtyCompilationUnit = new DummyCompilationUnit();
		}
		
		public static string[] AssemblyList {
			get {
				return assemblyList;
			}
		}

		/// <remarks>
		/// The initialize method writes the location of the code completion proxy
		/// file to this string.
		/// </remarks>
		string codeCompletionProxyFile;
		string codeCompletionMainFile;

		class ClasstableEntry
		{
			IClass           myClass;
			ICompilationUnit myCompilationUnit;
			string           myFileName;

			public IClass Class {
				get {
					return myClass;
				}
			}

			public ICompilationUnit CompilationUnit {
				get {
					return myCompilationUnit;
				}
			}

			public string FileName {
				get {
					return myFileName;
				}
			}

			public ClasstableEntry(string fileName, ICompilationUnit compilationUnit, IClass c)
			{
				this.myCompilationUnit = compilationUnit;
				this.myFileName        = fileName;
				this.myClass           = c;
			}
		}
		
		public void GenerateCodeCompletionDatabase(string createPath, IProgressMonitor progressMonitor)
		{
			SetCodeCompletionFileLocation(createPath);

			// write all classes and proxies to the disc
			BinaryWriter classWriter = new BinaryWriter(new BufferedStream(new FileStream(codeCompletionMainFile, FileMode.Create, FileAccess.Write, FileShare.None)));
			BinaryWriter proxyWriter = new BinaryWriter(new BufferedStream(new FileStream(codeCompletionProxyFile, FileMode.Create, FileAccess.Write, FileShare.None)));
			if (progressMonitor != null) {
				progressMonitor.BeginTask("generate code completion database", assemblyList.Length);
			}
			
			// convert all assemblies
			for (int i = 0; i < assemblyList.Length; ++i) {
				try {
					FileUtilityService fileUtilityService = (FileUtilityService)ServiceManager.Services.GetService(typeof(FileUtilityService));
					string path = fileUtilityService.GetDirectoryNameWithSeparator(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory());
					
					AssemblyInformation frameworkAssemblyInformation = new AssemblyInformation();
					frameworkAssemblyInformation.Load(String.Concat(path, assemblyList[i], ".dll"), false);
					// create all class proxies
					foreach (IClass newClass in frameworkAssemblyInformation.Classes) {
						ClassProxy newProxy = new ClassProxy(newClass);
						classProxies.Add(newProxy);
						AddClassToNamespaceList(newProxy);

						PersistentClass pc = new PersistentClass(classProxies, newClass);
						newProxy.Offset = (uint)classWriter.BaseStream.Position;
						newProxy.WriteTo(proxyWriter);
						pc.WriteTo(classWriter);
					}
					
					if (progressMonitor != null) {
						progressMonitor.Worked(i);
					}
				} catch (Exception) {
				}
			}

			classWriter.Close();
			proxyWriter.Close();
			if (progressMonitor != null) {
				progressMonitor.Done();
			}
		}
		
		void SetCodeCompletionFileLocation(string path)
		{
			FileUtilityService fileUtilityService = (FileUtilityService)ServiceManager.Services.GetService(typeof(FileUtilityService));
			string codeCompletionTemp = fileUtilityService.GetDirectoryNameWithSeparator(path);

			codeCompletionProxyFile = codeCompletionTemp + "CodeCompletionProxyDataV02.bin";
			codeCompletionMainFile  = codeCompletionTemp + "CodeCompletionMainDataV02.bin";
		}

		void SetDefaultCompletionFileLocation()
		{
			PropertyService propertyService = (PropertyService)ServiceManager.Services.GetService(typeof(PropertyService));
			SetCodeCompletionFileLocation(propertyService.GetProperty("SharpDevelop.CodeCompletion.DataDirectory", String.Empty).ToString());
		}

		public void LoadProxyDataFile()
		{
			if (!File.Exists(codeCompletionProxyFile)) {
				return;
			}
			BinaryReader reader = new BinaryReader(new BufferedStream(new FileStream(codeCompletionProxyFile, FileMode.Open, FileAccess.Read, FileShare.Read)));
			while (true) {
				try {
					ClassProxy newProxy = new ClassProxy(reader);
					classProxies.Add(newProxy);
					AddClassToNamespaceList(newProxy);
				} catch (Exception) {
					break;
				}
			}
			reader.Close();
		}
		
		void LoadThread()
		{
			SetDefaultCompletionFileLocation();
			
			BinaryFormatter formatter = new BinaryFormatter();
			
			if (File.Exists(codeCompletionProxyFile)) {
				LoadProxyDataFile();
			}
		}
		
		public override void InitializeService()
		{
			parser = (IParser[])(AddInTreeSingleton.AddInTree.GetTreeNode("/Workspace/Parser").BuildChildItems(this)).ToArray(typeof(IParser));
			
			Thread myThread = new Thread(new ThreadStart(LoadThread));
			myThread.IsBackground = true;
			myThread.Priority = ThreadPriority.Lowest;
			myThread.Start();
			
			IProjectService projectService = (IProjectService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IProjectService));
			projectService.CombineOpened += new CombineEventHandler(OpenCombine);
		}
		
		public void AddReferenceToCompletionLookup(IProject project, ProjectReference reference)
		{
			if (reference.ReferenceType == ReferenceType.Project) {
				IProjectService projectService = (IProjectService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IProjectService));
				IProject refProject = projectService.GetProject(reference.Reference);
				// don't load project assemblies when a parser for them exists
				if (refProject == null)
					return;
				foreach (IParser possibleParser in parser) {
					if (possibleParser.CanParse(refProject))
						return;
				}
			}
			
			string fileName = reference.GetReferencedFileName(project);
			if (fileName == null || fileName.Length == 0) {
				return;
			}
			foreach (string assemblyName in assemblyList) {
				if (Path.GetFileNameWithoutExtension(fileName).ToUpper() == assemblyName.ToUpper()) {
					return;
				}
			}
//				// HACK : Don't load references for non C# projects
//				if (project.ProjectType != "C#") {
//					return;
//				}
			
			if (File.Exists(fileName)) {
				try {
					AssemblyLoader assemblyLoader = new AssemblyLoader(this, fileName);
					assemblyLoader.NonLocking = reference.ReferenceType != ReferenceType.Gac;
					Thread t = new Thread(new ThreadStart(assemblyLoader.LoadAssemblyParseInformations));
					t.IsBackground = true;
					t.Start();
				} catch (Exception e) { Console.WriteLine(e); }
			}
		}
		
		class AssemblyLoader
		{
			DefaultParserService parserService;
			string assemblyFileName;
			bool nonLocking = false;
			
			public bool NonLocking {
				get {
					return nonLocking;
				}
				set {
					nonLocking = value;
				}
			}
			
			public AssemblyLoader(DefaultParserService parserService, string assemblyFileName)
			{
				this.parserService    = parserService;
				this.assemblyFileName = assemblyFileName;
			}
			
			public void LoadAssemblyParseInformations()
			{
				if (parserService.loadedAssemblies[assemblyFileName] != null) {
					return;
				}
				parserService.loadedAssemblies[assemblyFileName] = true;
				//Console.WriteLine("Before: " + Environment.WorkingSet / (1024 * 1024) + " -- " + assemblyFileName);
				try {
					AssemblyInformation assemblyInformation = new AssemblyInformation();
					assemblyInformation.Load(assemblyFileName, nonLocking);
					
					foreach (IClass newClass in assemblyInformation.Classes) {
						Console.WriteLine(newClass);
						parserService.AddClassToNamespaceList(newClass);
						lock (parserService.classes) {
							parserService.caseInsensitiveClasses[newClass.FullyQualifiedName.ToLower()] = parserService.classes[newClass.FullyQualifiedName] = new ClasstableEntry(null, null, newClass);
						}
					}
				} catch (Exception) {
				}
				//Console.WriteLine("After: " + Environment.WorkingSet / (1024 * 1024) + " -- " + assemblyFileName);
			}
		}
		
		public virtual void OpenCombine(object sender, CombineEventArgs e)
		{
			ArrayList projects =  Combine.GetAllProjects(e.Combine);
			foreach (ProjectCombineEntry entry in projects) {
				foreach (ProjectReference r in entry.Project.ProjectReferences) {
					AddReferenceToCompletionLookup(entry.Project, r);
				}
			}
		}
		
		public void StartParserThread()
		{
			Thread parserThread = new Thread(new ThreadStart(ParserUpdateThread));
			parserThread.IsBackground  = true;
			parserThread.Start();
		}
		
		public override void UnloadService()
		{
			doneParserThread = true;
		}
		
		bool doneParserThread = false;
		Hashtable lastUpdateSize = new Hashtable();
		
		void ParserUpdateThread()
		{
// 			string fn=null;
			while (!doneParserThread) {
				////Thread.Sleep(1000); // not required
//// Alex: if some file was pulsed - during editor load and after - get file to reparse
//				fn = null; // set to null for each repetition
// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
//	Mike: Doesn't work with folding marker update --> look at the folding markers
//  Mike: You can't simply BREAK a feature and say I should fix it ... either bring the folding
//        markers in a working state or leave this change ... I don't see that your change is a good
//        alternative ... the current parserthread looks at the text and if it changed it reparses ...
//        it is better than the old version you fixed 
// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

//				lock(DefaultParserService.ParserPulse) {
//					//Console.WriteLine("Pulse got: {0} entries",DefaultParserService.ParserPulse.Count);
//					Monitor.Wait(DefaultParserService.ParserPulse);
//					if (DefaultParserService.ParserPulse.Count>0) {
//						fn = (string)DefaultParserService.ParserPulse.Dequeue();
//					}
//				}
				try {
					if (WorkbenchSingleton.Workbench.ActiveWorkbenchWindow != null && WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ActiveViewContent != null) {
						IEditable editable = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ActiveViewContent as IEditable;
						if (editable != null) {
							string fileName = null;
							
							IViewContent viewContent = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ViewContent;
							IParseableContent parseableContent = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ActiveViewContent as IParseableContent;
							
							//ivoko: Pls, do not throw text = parseableContent.ParseableText away. I NEED it.
							string text = null;
							if (parseableContent != null) {
								fileName = parseableContent.ParseableContentName;
								text = parseableContent.ParseableText;
							} else {
								fileName = viewContent.IsUntitled ? viewContent.UntitledName : viewContent.FileName;
							}
							
							if (!(fileName == null || fileName.Length == 0)) {
//								Thread.Sleep(300); // not required 
								IParseInformation parseInformation = null;
								bool updated = false;
								if (text == null) {
									text = editable.Text;
								}
								int hash = text.GetHashCode();
								if (lastUpdateSize[fileName] == null || (int)lastUpdateSize[fileName] != hash) {
									parseInformation = ParseFile(fileName, text, !viewContent.IsUntitled);
									lastUpdateSize[fileName] = hash;
									updated = true;
								}
								if (updated) {
									if (parseInformation != null && editable is IParseInformationListener) {
										((IParseInformationListener)editable).ParseInformationUpdated(parseInformation);
									}
								}
//								if (fn != null) {
//									ParseFile(fn); // TODO: this one should update file parsings requested through queue
//								}
							}
						}
					}
				} catch (Exception) {
				}
				Thread.Sleep(2000);
			}
		}
		
		Hashtable AddClassToNamespaceList(IClass addClass)
		{
			string nSpace = addClass.Namespace;
			if (nSpace == null) {
				nSpace = String.Empty;
			}
			
			string[] path = nSpace.Split('.');
			
			lock (namespaces) {
				Hashtable cur                = namespaces;
				Hashtable caseInsensitiveCur = caseInsensitiveNamespaces;
				
				for (int i = 0; i < path.Length; ++i) {
					if (cur[path[i]] == null) {
						Hashtable hashTable                = new Hashtable();
						Hashtable caseInsensitivehashTable = new Hashtable();
						cur[path[i]] = hashTable;
						caseInsensitiveCur[path[i].ToLower()] = caseInsensitivehashTable;
						caseInsensitivehashTable[CaseInsensitiveKey] = hashTable;
					} else {
						if (!(cur[path[i]] is Hashtable)) {
							return null;
						}
					}
					cur = (Hashtable)cur[path[i]];
					
					if (caseInsensitiveCur[path[i].ToLower()] == null) {
					    caseInsensitiveCur[path[i].ToLower()] = new Hashtable();
					}
					caseInsensitiveCur = (Hashtable)caseInsensitiveCur[path[i].ToLower()];
				}
				
				string name = addClass.Name == null ? "" : addClass.Name;
				
				caseInsensitiveCur[name.ToLower()] = cur[name] = addClass;
				return cur;
			}
		}
		
#region Default Parser Layer dependent functions
		public IClass GetClass(string typeName)
		{
			return GetClass(typeName, true);
		}
		public IClass GetClass(string typeName, bool caseSensitive)
		{
			if (!caseSensitive) {
				typeName = typeName.ToLower();
			}
			
			ClasstableEntry entry = (caseSensitive ? classes[typeName] : caseInsensitiveClasses[typeName]) as ClasstableEntry;
			if (entry != null) {
				return entry.Class;
			}
			
			// try to load the class from our data file
			int idx = classProxies.IndexOf(typeName, caseSensitive);
			if (idx >= 0) {
				if ((classes[typeName]==null || (((ClasstableEntry)classes[typeName]).Class.FullyQualifiedName!=((ClassProxy)classProxies[idx]).FullyQualifiedName)) ||
				    (caseInsensitiveClasses[typeName.ToLower()]==null || (((ClasstableEntry)caseInsensitiveClasses[typeName.ToLower()]).Class.FullyQualifiedName!=((ClassProxy)classProxies[idx]).FullyQualifiedName))) {
					BinaryReader reader = new BinaryReader(new BufferedStream(new FileStream(codeCompletionMainFile, FileMode.Open, FileAccess.Read, FileShare.Read)));
					reader.BaseStream.Seek(classProxies[idx].Offset, SeekOrigin.Begin);
					IClass c = new PersistentClass(reader, classProxies);
					reader.Close();
					lock (classes) {
						caseInsensitiveClasses[typeName.ToLower()] = classes[typeName] = new ClasstableEntry(null, null, c);
					}
					return c;
				}
			}
			
			// not found -> maybe nested type -> trying to find class that contains this one.
			int lastIndex = typeName.LastIndexOf('.');
			if (lastIndex > 0) {
				string innerName = typeName.Substring(lastIndex + 1);
				string outerName = typeName.Substring(0, lastIndex);
				IClass upperClass = GetClass(outerName, caseSensitive);
				if (upperClass != null && upperClass.InnerClasses != null) {
					foreach (IClass c in upperClass.InnerClasses) {
						if (c.Name == innerName) {
							return c;
						}
					}
				}
			}
			return null;
		}
		
		public string[] GetNamespaceList(string subNameSpace)
		{
			return GetNamespaceList(subNameSpace, true);
		}
		public string[] GetNamespaceList(string subNameSpace, bool caseSensitive)
		{
//			Console.WriteLine("GetNamespaceList >{0}<", subNameSpace);
			
			System.Diagnostics.Debug.Assert(subNameSpace != null);
			if (!caseSensitive) {
				subNameSpace = subNameSpace.ToLower();
			}
			
			string[] path = subNameSpace.Split('.');
			Hashtable cur = caseSensitive ? namespaces : caseInsensitiveNamespaces;
			
			if (subNameSpace.Length > 0) {
				for (int i = 0; i < path.Length; ++i) {
					if (!(cur[path[i]] is Hashtable)) {
						return null;
					}
					cur = (Hashtable)cur[path[i]];
				}
			}
			
			if (!caseSensitive) {
				cur = (Hashtable)cur[CaseInsensitiveKey];
			}
			
			ArrayList namespaceList = new ArrayList();
			foreach (DictionaryEntry entry in cur) {
				if (entry.Value is Hashtable && entry.Key.ToString().Length > 0) {
					namespaceList.Add(entry.Key);
				}
			}
			
			return (string[])namespaceList.ToArray(typeof(string));
		}
		
		public ArrayList GetNamespaceContents(string subNameSpace)
		{
			return GetNamespaceContents(subNameSpace, true);
		}
		public ArrayList GetNamespaceContents(string subNameSpace, bool caseSensitive)
		{
//			Console.WriteLine("GetNamespaceContents >{0}<", subNameSpace);
			
			ArrayList namespaceList = new ArrayList();
			if (subNameSpace == null) {
				return namespaceList;
			}
			if (!caseSensitive) {
				subNameSpace = subNameSpace.ToLower();
			}
			
			string[] path = subNameSpace.Split('.');
			Hashtable cur = caseSensitive ? namespaces : caseInsensitiveNamespaces;
			
			for (int i = 0; i < path.Length; ++i) {
				if (!(cur[path[i]] is Hashtable)) {
					foreach (DictionaryEntry entry in cur)  {
						if (entry.Value is Hashtable) {
							namespaceList.Add(entry.Key);
						}
					}
					
					return namespaceList;
				}
				cur = (Hashtable)cur[path[i]];
			}
			
			if (!caseSensitive) {
				cur = (Hashtable)cur[CaseInsensitiveKey];
			}
			
			foreach (DictionaryEntry entry in cur) {
				if (entry.Value is Hashtable) {
					namespaceList.Add(entry.Key);
				} else {
					namespaceList.Add(entry.Value);
				}
			}
			return namespaceList;
		}
		
		public bool NamespaceExists(string name)
		{
			return NamespaceExists(name, true);
		}
		public bool NamespaceExists(string name, bool caseSensitive)
		{
//			Console.WriteLine("NamespaceExists >{0}<", name);
			if (name == null) {
				return false;
			}
			if (!caseSensitive) {
				name = name.ToLower();
			}
			string[] path = name.Split('.');
			Hashtable cur = caseSensitive ? namespaces : caseInsensitiveNamespaces;
			
			for (int i = 0; i < path.Length; ++i) {
				if (!(cur[path[i]] is Hashtable)) {
					return false;
				}
				cur = (Hashtable)cur[path[i]];
			}
			return true;
		}
		
		/// <remarks>
		/// Returns the innerst class in which the carret currently is, returns null
		/// if the carret is outside any class boundaries.
		/// </remarks>
		public IClass GetInnermostClass(ICompilationUnit cu, int caretLine, int caretColumn)
		{
			if (cu != null) {
				foreach (IClass c in cu.Classes) {
					if (c != null && c.Region != null && c.Region.IsInside(caretLine, caretColumn)) {
						return GetInnermostClass(c, caretLine, caretColumn);
					}
				}
			}
			return null;
		}
		IClass GetInnermostClass(IClass curClass, int caretLine, int caretColumn)
		{
			if (curClass == null) {
				return null;
			}
			if (curClass.InnerClasses == null) {
				return curClass;
			}
			foreach (IClass c in curClass.InnerClasses) {
				if (c != null && c.Region != null && c.Region.IsInside(caretLine, caretColumn)) {
					return GetInnermostClass(c, caretLine, caretColumn);
				}
			}
			return curClass;
		}
		
		/// <remarks>
		/// Returns all (nestet) classes in which the carret currently is exept
		/// the innermost class, returns an empty collection if the carret is in 
		/// no class or only in the innermost class.
		/// the most outer class is the last in the collection.
		/// </remarks>
		public ClassCollection GetOuterClasses(ICompilationUnit cu, int caretLine, int caretColumn)
		{
			ClassCollection classes = new ClassCollection();
			if (cu != null) {
				foreach (IClass c in cu.Classes) {
					if (c != null && c.Region != null && c.Region.IsInside(caretLine, caretColumn)) {
						if (c != GetInnermostClass(cu, caretLine, caretColumn)) {
							GetOuterClasses(classes, c, cu, caretLine, caretColumn);
							if (!classes.Contains(c)) {
								classes.Add(c);
							}
						}
						break;
					}
				}
			}
			
			return classes;
		}
		void GetOuterClasses(ClassCollection classes, IClass curClass, ICompilationUnit cu, int caretLine, int caretColumn)
		{
			if (curClass != null) {
				foreach (IClass c in curClass.InnerClasses) {
					if (c != null && c.Region != null && c.Region.IsInside(caretLine, caretColumn)) {
						if (c != GetInnermostClass(cu, caretLine, caretColumn)) {
							GetOuterClasses(classes, c, cu, caretLine, caretColumn);
							if (!classes.Contains(c)) {
								classes.Add(c);
							}
						}
						break;
					}
				}
			}
		}
		public string SearchNamespace(string name, ICompilationUnit unit, int caretLine, int caretColumn)
		{
			return SearchNamespace(name, unit, caretLine, caretColumn, true);
		}
		
		/// <remarks>
		/// use the usings to find the correct name of a namespace
		/// </remarks>
		public string SearchNamespace(string name, ICompilationUnit unit, int caretLine, int caretColumn, bool caseSensitive)
		{
			if (NamespaceExists(name, caseSensitive)) {
				return name;
			}
			if (unit == null) {
//				Console.WriteLine("done, resultless");
				return null;
			}
			
			foreach (IUsing u in unit.Usings) {
				if (u != null && (u.Region == null || u.Region.IsInside(caretLine, caretColumn))) {
					string nameSpace = u.SearchNamespace(name, caseSensitive);
					if (nameSpace != null) {
						return nameSpace;
					}
				}
			}
//			Console.WriteLine("done, resultless");
			return null;
		}
		
		/// <remarks>
		/// use the usings and the name of the namespace to find a class
		/// </remarks>
		public IClass SearchType(string name, IClass curType, int caretLine, int caretColumn)
		{
			return SearchType(name, curType, caretLine, caretColumn, true);
		}
		public IClass SearchType(string name, IClass curType, int caretLine, int caretColumn, bool caseSensitive)
		{
			if (curType == null) {
				return SearchType(name, null, null, caretLine, caretColumn, caseSensitive);
			}
			return SearchType(name, curType, curType.CompilationUnit, caretLine, caretColumn, caseSensitive);
		}
		
		public IClass SearchType(string name, IClass curType, ICompilationUnit unit, int caretLine, int caretColumn)
		{
			return SearchType(name, curType, unit, caretLine, caretColumn, true);
		}
		
		/// <remarks>
		/// use the usings and the name of the namespace to find a class
		/// </remarks>
		public IClass SearchType(string name, IClass curType, ICompilationUnit unit, int caretLine, int caretColumn, bool caseSensitive)
		{
//			Console.WriteLine("Searching Type " + name);
			if (name == null || name == String.Empty) {
//				Console.WriteLine("No Name!");
				return null;
			}
			IClass c  = GetClass(name, caseSensitive);
			if (c != null) {
//				Console.WriteLine("Found!");
				return c;
			}
//			Console.WriteLine("No FullName");
			if (unit != null) {
//				Console.WriteLine(unit.Usings.Count + " Usings");
				foreach (IUsing u in unit.Usings) {
					if (u != null && (u.Region == null || u.Region.IsInside(caretLine, caretColumn))) {
//						Console.WriteLine("In UsingRegion");
						c = u.SearchType(name, caseSensitive);
						if (c != null) {
//							Console.WriteLine("SearchType Successfull!!!");
							return c;
						}
					}
				}
			}
			if (curType == null) {
//				Console.WriteLine("curType == null");
				return null;
			}
			string fullname = curType.FullyQualifiedName;
//			Console.WriteLine("Fullname of class is: " + fullname);
//// Alex - remove unnecessary new object allocation
			//string[] namespaces = fullname.Split(new char[] {'.'});
			string[] namespaces = fullname.Split('.');
//// Alex - change to stringbuilder version to remove unnecessary allocations
			//string curnamespace = "";
			StringBuilder curnamespace = new StringBuilder();
			for (int i = 0; i < namespaces.Length; ++i) {
				//curnamespace += namespaces[i] + '.';
				curnamespace.Append(namespaces[i]);
				curnamespace.Append('.');
//				Console.WriteLine(curnamespace);
				//c = GetClass(curnamespace + name, caseSensitive);
				StringBuilder nms=new StringBuilder(curnamespace.ToString());
				nms.Append(name);
				c = GetClass(nms.ToString(), caseSensitive);
				if (c != null) {
//					Console.WriteLine("found in Namespace " + curnamespace + name);
					return c;
				}
			}
//// Alex: try to find in namespaces referenced excluding system ones which were checked already
			string[] innamespaces=GetNamespaceList("");
			foreach (string ns in innamespaces) {
				if (Array.IndexOf(DefaultParserService.assemblyList,ns)>=0) continue;
				ArrayList objs=GetNamespaceContents(ns);
				if (objs==null) continue;
				foreach (object o in objs) {
					if (o is IClass) {
						IClass oc=(IClass)o;
						//  || oc.Name==name
						if (oc.FullyQualifiedName == name) {
							//Debug.WriteLine(((IClass)o).Name);
							/// now we can set completion data
							objs.Clear();
							objs = null;
							return oc;
						}
					}
				}
				if (objs == null) {
					break;
				}
			}
			innamespaces=null;
//// Alex: end of mod
			return null;
		}
		
		/// <remarks>
		/// Returns true, if class possibleBaseClass is in the inheritance tree from c
		/// </remarks>
		public bool IsClassInInheritanceTree(IClass possibleBaseClass, IClass c)
		{
			return IsClassInInheritanceTree(possibleBaseClass, c, true);
		}
		
		public bool IsClassInInheritanceTree(IClass possibleBaseClass, IClass c, bool caseSensitive)
		{
			if (possibleBaseClass == null || c == null) {
				return false;
			}
			if (caseSensitive && possibleBaseClass.FullyQualifiedName == c.FullyQualifiedName ||
			    !caseSensitive && possibleBaseClass.FullyQualifiedName.ToLower() == c.FullyQualifiedName.ToLower()) {
				return true;
			}
			foreach (string baseClass in c.BaseTypes) {
				if (IsClassInInheritanceTree(possibleBaseClass, SearchType(baseClass, c, c.CompilationUnit, c.Region != null ? c.Region.BeginLine : -1, c.Region != null ? c.Region.BeginColumn : -1))) {
					return true;
				}
			}
			return false;
		}
		
		public IClass BaseClass(IClass curClass)
		{
			return BaseClass(curClass, true);
		}
		public IClass BaseClass(IClass curClass, bool caseSensitive)
		{
			foreach (string s in curClass.BaseTypes) {
//				Console.WriteLine("BaseType = " + s + "?");
				IClass baseClass = SearchType(s, curClass, curClass.Region != null ? curClass.Region.BeginLine : 0, curClass.Region != null ? curClass.Region.BeginColumn : 0, caseSensitive);
				if (baseClass == null) {
//					Console.WriteLine("Not found!");
				}
				if (baseClass != null && baseClass.ClassType != ClassType.Interface) {
//					if (baseClass != null) {
//						Console.WriteLine("Interface");
//					}
					return baseClass;
				}
			}
			// no baseType found
			if (curClass.ClassType == ClassType.Enum) {
//				Console.WriteLine("BaseType = System.Enum");
				return GetClass("System.Enum", true);
			} else if (curClass.ClassType == ClassType.Class) {
				if (curClass.FullyQualifiedName != "System.Object") {
//					Console.WriteLine("BaseType = System.Object");
					return GetClass("System.Object", true);
				}
			} else if (curClass.ClassType == ClassType.Delegate) {
//				Console.WriteLine("BaseType = System.Delegate");
				return GetClass("System.Delegate", true);
			} else if (curClass.ClassType == ClassType.Struct) {
//				Console.WriteLine("BaseType = System.ValueType");
				return GetClass("System.ValueType", true);
			}
			return null;
		}
		
		public bool IsAccessible(IClass c, IDecoration member, IClass callingClass, bool isClassInInheritanceTree)
		{
//			Console.WriteLine("testing Accessibility");
			if ((member.Modifiers & ModifierEnum.Internal) == ModifierEnum.Internal) {
				return true;
			}
			if ((member.Modifiers & ModifierEnum.Public) == ModifierEnum.Public) {
				return true;
			}
			if ((member.Modifiers & ModifierEnum.Protected) == ModifierEnum.Protected && isClassInInheritanceTree) {
				return true;
			}
			return c != null && callingClass != null && c.FullyQualifiedName == callingClass.FullyQualifiedName;
		}
		
		public bool MustBeShown(IClass c, IDecoration member, IClass callingClass, bool showStatic, bool isClassInInheritanceTree)
		{
//			Console.WriteLine("MustBeShown, Class: " + (c == null ? "null": c.FullyQualifiedName));
			if (c != null && c.ClassType == ClassType.Enum) {
				return true;
			}
//			Console.WriteLine("showStatic = " + showStatic);
//			Console.WriteLine(member.Modifiers);
			if ((!showStatic &&  ((member.Modifiers & ModifierEnum.Static) == ModifierEnum.Static)) ||
			    ( showStatic && !(((member.Modifiers & ModifierEnum.Static) == ModifierEnum.Static) ||
			                      ((member.Modifiers & ModifierEnum.Const)  == ModifierEnum.Const)))) { // const is automatically static
				return false;
			}
			return IsAccessible(c, member, callingClass, isClassInInheritanceTree);
		}
		
		public ArrayList ListTypes(ArrayList types, IClass curType, IClass callingClass)
		{
			bool isClassInInheritanceTree = IsClassInInheritanceTree(curType, callingClass);
			foreach (IClass c in curType.InnerClasses) {
				if (((c.ClassType == ClassType.Class) || (c.ClassType == ClassType.Struct)) &&
				      IsAccessible(curType, c, callingClass, isClassInInheritanceTree)) {
					types.Add(c);
				}
			}
			IClass baseClass = BaseClass(curType);
			if (baseClass != null) {
				ListTypes(types, baseClass, callingClass);
			}
			return types;
		}
		
		public ArrayList ListMembers(ArrayList members, IClass curType, IClass callingClass, bool showStatic)
		{
			DateTime now = DateTime.Now;
			
			// enums must be handled specially, because there are several things defined we don't want to show
			// and enum members have neither the modifier static nor the modifier public
			if (curType.ClassType == ClassType.Enum) {
//				Console.WriteLine("listing enum members");
				foreach (IField f in curType.Fields) {
//					Console.WriteLine("testing " + f.Name);
					if (f.IsLiteral) {
//						Console.WriteLine("SpecialName found");
						members.Add(f);
					}
				}
				ListMembers(members, GetClass("System.Enum", true), callingClass, showStatic);
				return members;
			}
			
			bool isClassInInheritanceTree = IsClassInInheritanceTree(curType, callingClass);
			
			if (showStatic) {
				foreach (IClass c in curType.InnerClasses) {
					if (IsAccessible(curType, c, callingClass, isClassInInheritanceTree)) {
						members.Add(c);
					}
				}
			}
			
			foreach (IProperty p in curType.Properties) {
				if (MustBeShown(curType, p, callingClass, showStatic, isClassInInheritanceTree)) {
					members.Add(p);
				}
			}
			
			foreach (IMethod m in curType.Methods) {
				if (MustBeShown(curType, m, callingClass, showStatic, isClassInInheritanceTree)) {
					members.Add(m);
				}
			}
			
			foreach (IEvent e in curType.Events) {
				if (MustBeShown(curType, e, callingClass, showStatic, isClassInInheritanceTree)) {
					members.Add(e);
				}
			}
			
			foreach (IField f in curType.Fields) {
//				Console.WriteLine("testing field " + f.Name);
				if (MustBeShown(curType, f, callingClass, showStatic, isClassInInheritanceTree)) {
//					Console.WriteLine("field added");
					members.Add(f);
				}
			}
			
			if (curType.ClassType == ClassType.Interface && !showStatic) {
				foreach (string s in curType.BaseTypes) {
					IClass baseClass = SearchType(s, curType, curType.Region != null ? curType.Region.BeginLine : -1, curType.Region != null ? curType.Region.BeginColumn : -1);
					if (baseClass != null && baseClass.ClassType == ClassType.Interface) {
						ListMembers(members, baseClass, callingClass, showStatic);
					}
				}
			} else {
				IClass baseClass = BaseClass(curType);
				if (baseClass != null) {
					ListMembers(members, baseClass, callingClass, showStatic);
				}
			}
			
			return members;
		}
		
		public IMember SearchMember(IClass declaringType, string memberName)
		{
			if (declaringType == null || memberName == null || memberName.Length == 0) {
				return null;
			}
			foreach (IField f in declaringType.Fields) {
				if (f.Name == memberName) {
					return f;
				}
			}
			foreach (IProperty p in declaringType.Properties) {
				if (p.Name == memberName) {
					return p;
				}
			}
			foreach (IIndexer i in declaringType.Indexer) {
				if (i.Name == memberName) {
					return i;
				}
			}
			foreach (IEvent e in declaringType.Events) {
				if (e.Name == memberName) {
					return e;
				}
			}
			foreach (IMethod m in declaringType.Methods) {
				if (m.Name == memberName) {
					return m;
				}
			}
			if (declaringType.ClassType == ClassType.Interface) {
				foreach (string baseType in declaringType.BaseTypes) {
					int line = -1;
					int col = -1;
					if (declaringType.Region != null) {
						line = declaringType.Region.BeginLine;
						col = declaringType.Region.BeginColumn;
					}
					IClass c = SearchType(baseType, declaringType, line, col);
					if (c != null) {
						return SearchMember(c, memberName);
					}
				}
			} else {
				IClass c = BaseClass(declaringType);
				return SearchMember(c, memberName);
			}
			return null;
		}
		
		public Position GetPosition(string fullMemberName)
		{
			string[] name = fullMemberName.Split(new char[] {'.'});
			string curName = name[0];
			int i = 1;
			while (i < name.Length && NamespaceExists(curName)) {
				curName += '.' + name[i];
				++i;
			}
			Debug.Assert(i <= name.Length);
			IClass curClass = GetClass(curName);
			if (curClass == null) {
				//Console.WriteLine("Class not found: " + curName);
				return new Position(null, -1, -1);
			}
			ICompilationUnit cu = curClass.CompilationUnit;
			while (i < name.Length) {
				ClassCollection innerClasses = curClass.InnerClasses;
				foreach (IClass c in innerClasses) {
					if (c.Name == name[i]) {
						curClass = c;
						break;
					}
				}
				if (curClass.Name != name[i]) {
					break;
				}
				++i;
			}
			if (i >= name.Length) {
				return new Position(cu, curClass.Region != null ? curClass.Region.BeginLine : -1, curClass.Region != null ? curClass.Region.BeginColumn : -1);
			}
			IMember member = SearchMember(curClass, name[i]);
			if (member == null || member.Region == null) {
				return new Position(cu, -1, -1);
			}
			return new Position(cu, member.Region.BeginLine, member.Region.BeginColumn);
		}
		
#endregion
		
		public IParseInformation ParseFile(string fileName)
		{
			return ParseFile(fileName, null);
		}
		
		public IParseInformation ParseFile(string fileName, string fileContent)
		{
			return ParseFile(fileName, fileContent, true);
		}
		
		public IParseInformation ParseFile(string fileName, string fileContent, bool updateCommentTags)
		{
			//Console.WriteLine("PARSE : " + fileName);
			IParser parser = GetParser(fileName);
			
			if (parser == null) {
				return null;
			}
			
			ICompilationUnitBase parserOutput = null;
			
			if (fileContent == null) {
				IProjectService projectService = (IProjectService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IProjectService));
				if (projectService.CurrentOpenCombine != null) {
					ArrayList projects = Combine.GetAllProjects(projectService.CurrentOpenCombine);
					foreach (ProjectCombineEntry entry in projects) {
						if (entry.Project.IsFileInProject(fileName)) {
							fileContent = entry.Project.GetParseableFileContent(fileName);
							break;
						}
					}
				}
			}
			
			if (fileContent != null) {
				parserOutput = parser.Parse(fileName, fileContent);
			} else {
				if (!File.Exists(fileName)) {
					return null;
				}
				parserOutput = parser.Parse(fileName);
			}
			if (updateCommentTags && parserOutput is ICompilationUnit) {
				ICompilationUnit cu = (ICompilationUnit)parserOutput;
				TaskService taskService = (TaskService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(TaskService));
				taskService.RemoveCommentTasks(fileName);
				if (cu.TagComments.Count > 0) {
					foreach (Tag tag in cu.TagComments) {
						taskService.CommentTasks.Add(new Task(fileName, tag.Key + tag.CommentString, tag.Region.BeginColumn, tag.Region.BeginLine, TaskType.Comment));
					}
					taskService.NotifyTaskChange();
				}
			}
			ParseInformation parseInformation = parsings[fileName] as ParseInformation;
			
			int itemsAdded = 0;
			int itemsRemoved = 0;
			
			if (parseInformation == null) {
				parseInformation = new ParseInformation();
			} else {
				itemsAdded = GetAddedItems(
				                           (ICompilationUnit)parseInformation.MostRecentCompilationUnit,
				                           (ICompilationUnit)parserOutput,
				                           (ICompilationUnit)addedParseInformation.DirtyCompilationUnit
				                           );
				
				itemsRemoved = GetRemovedItems(
				                               (ICompilationUnit)parseInformation.MostRecentCompilationUnit,
				                               (ICompilationUnit)parserOutput,
				                               (ICompilationUnit)removedParseInformation.DirtyCompilationUnit
				                               );
			}
			if (parserOutput.ErrorsDuringCompile) {
				Console.WriteLine("ERRORS DURING COMPILE");
				parseInformation.DirtyCompilationUnit = parserOutput;
			} else {
				parseInformation.ValidCompilationUnit = parserOutput;
				parseInformation.DirtyCompilationUnit = null;
			}
			
			parsings[fileName] = parseInformation;
			
			if (parseInformation.BestCompilationUnit is ICompilationUnit) {
				ICompilationUnit cu = (ICompilationUnit)parseInformation.BestCompilationUnit;
				foreach (IClass c in cu.Classes) {
					AddClassToNamespaceList(c);
					lock (classes) {
						caseInsensitiveClasses[c.FullyQualifiedName.ToLower()] = classes[c.FullyQualifiedName] = new ClasstableEntry(fileName, cu, c);
					}
				}
			} else {
//				Console.WriteLine("SKIP!");
			}
			
			OnParseInformationChanged(new ParseInformationEventArgs(fileName, parseInformation));
			
			if(itemsRemoved > 0) {
				OnParseInformationRemoved(new ParseInformationEventArgs(fileName, removedParseInformation));
			}
			
			if(itemsAdded > 0) {
				OnParseInformationAdded(new ParseInformationEventArgs(fileName, addedParseInformation));
			}
			return parseInformation;
		}
		
		void RemoveClasses(ICompilationUnit cu)
		{
			if (cu != null) {
				lock (classes) {
					foreach (IClass c in cu.Classes) {
							classes.Remove(c.FullyQualifiedName);
							caseInsensitiveClasses.Remove(c.FullyQualifiedName.ToLower());
					}
				}
			}
		}

		public IParseInformation GetParseInformation(string fileName)
		{
			if (fileName == null || fileName.Length == 0) {
				return null;
			}
			object cu = parsings[fileName];
			if (cu == null) {
				return ParseFile(fileName);
			}
			return (IParseInformation)cu;
		}
		
		public IExpressionFinder GetExpressionFinder(string fileName)
		{
			IParser parser = GetParser(fileName);
			if (parser != null) {
				return parser.ExpressionFinder;
			}
			return null;
		}
		
		public virtual IParser GetParser(string fileName)
		{
			IParser curParser = null;
			foreach (IParser p in parser) {
				if (p.CanParse(fileName)) {
					curParser = p;
					break;
				}
			}
			
			if (curParser != null) {
				PropertyService propertyService = (PropertyService)ServiceManager.Services.GetService(typeof(PropertyService));
				string tasklisttokens = propertyService.GetProperty("SharpDevelop.TaskListTokens", "HACK;TODO;UNDONE;FIXME");
				curParser.LexerTags = tasklisttokens.Split(';');
			}
			
			return curParser;
		}
		
		int GetAddedItems(ICompilationUnit original, ICompilationUnit changed, ICompilationUnit result)
		{
			int count = 0;
			//result.LookUpTable.Clear();
			//result.Usings.Clear();
			//result.Attributes.Clear();
			result.Classes.Clear();
			//result.MiscComments.Clear();
			//result.DokuComments.Clear();
			//result.TagComments.Clear();
			
			//count += DiffUtility.GetAddedItems(original.LookUpTable,  changed.LookUpTable,  result.LookUpTable);
			//count += DiffUtility.GetAddedItems(original.Usings,       changed.Usings,       result.Usings);
			//count += DiffUtility.GetAddedItems(original.Attributes,   changed.Attributes,   result.Attributes);
			count += DiffUtility.GetAddedItems(original.Classes,      changed.Classes,      result.Classes);
			//count += DiffUtility.GetAddedItems(original.MiscComments, changed.MiscComments, result.MiscComments);
			//count += DiffUtility.GetAddedItems(original.DokuComments, changed.DokuComments, result.DokuComments);
			//count += DiffUtility.GetAddedItems(original.TagComments,  changed.TagComments,  result.TagComments);
			return count;
		}
		
		int GetRemovedItems(ICompilationUnit original, ICompilationUnit changed, ICompilationUnit result) {
			return GetAddedItems(changed, original, result);
		}
		
		////////////////////////////////////
		
		public ArrayList CtrlSpace(IParserService parserService, int caretLine, int caretColumn, string fileName)
		{
			IParser parser = GetParser(fileName);
			if (parser != null) {
				return parser.CtrlSpace(parserService, caretLine, caretColumn, fileName);
			}
			return null;
		}
		
		public ResolveResult Resolve(string expression,
		                             int caretLineNumber,
		                             int caretColumn,
		                             string fileName,
		                             string fileContent)
		{
			// added exception handling here to prevent silly parser exceptions from
			// being thrown and corrupting the textarea control
			//try {
				Console.WriteLine("Get parser for : " + fileName);
				IParser parser = GetParser(fileName);
				Console.WriteLine(parser);
				if (parser != null) {
					return parser.Resolve(this, expression, caretLineNumber, caretColumn, fileName, fileContent);
				}
				return null;
			//} catch {
//				return null;
			//}
		}

		protected void OnParseInformationAdded(ParseInformationEventArgs e)
		{
			if (ParseInformationAdded != null) {
				ParseInformationAdded(this, e);
			}
		}

		protected void OnParseInformationRemoved(ParseInformationEventArgs e)
		{
			if (ParseInformationRemoved != null) {
				ParseInformationRemoved(this, e);
			}
		}
		protected virtual void OnParseInformationChanged(ParseInformationEventArgs e)
		{
			if (ParseInformationChanged != null) {
				ParseInformationChanged(this, e);
			}
		}
		
		public event ParseInformationEventHandler ParseInformationAdded;
		public event ParseInformationEventHandler ParseInformationRemoved;
		public event ParseInformationEventHandler ParseInformationChanged;
	}
	
	[Serializable]
	public class DummyCompilationUnit : AbstractCompilationUnit
	{
		CommentCollection miscComments = new CommentCollection();
		CommentCollection dokuComments = new CommentCollection();
		
		public override CommentCollection MiscComments {
			get {
				return miscComments;
			}
		}
		
		public override CommentCollection DokuComments {
			get {
				return dokuComments;
			}
		}
	}
}
