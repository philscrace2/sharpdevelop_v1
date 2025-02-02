// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Andrea Paatz" email="andrea@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Drawing;

using ICSharpCode.SharpDevelop.Services;
using SharpDevelop.Internal.Parser;
using VBBinding.Parser.SharpDevelopTree;
using ICSharpCode.SharpRefactory.Parser.AST.VB;
using ICSharpCode.SharpRefactory.Parser.VB;

namespace VBBinding.Parser
{
	public class Resolver
	{
		IParserService parserService;
		ICompilationUnit cu;
		IClass callingClass;
		LookupTableVisitor lookupTableVisitor;
		
		public IParserService ParserService {
			get {
				return parserService;
			}
		}
		
		public ICompilationUnit CompilationUnit {
			get {
				return cu;
			}
		}
		
		public IClass CallingClass {
			get {
				return callingClass;
			}
		}
		
		bool showStatic = false;
		
		bool inNew = false;
		
		public bool ShowStatic {
			get {
				return showStatic;
			}
			
			set {
				showStatic = value;
			}
		}
		
		int caretLine;
		int caretColumn;
		
		public ResolveResult Resolve(IParserService parserService, string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent)
		{
			expression = expression.TrimStart(null);
			expression = expression.ToLower();
			if (expression.StartsWith("new ")) {
				inNew = true;
				expression = expression.Substring(4);
			} else {
				inNew = false;
			}
			//Console.WriteLine("\nStart Resolving expression : >{0}<", expression);
			
			Expression expr = null;
			this.caretLine     = caretLineNumber;
			this.caretColumn   = caretColumn;
			this.parserService = parserService;
			IParseInformation parseInfo = parserService.GetParseInformation(fileName);
			ICSharpCode.SharpRefactory.Parser.AST.VB.CompilationUnit fileCompilationUnit = parseInfo.MostRecentCompilationUnit.Tag as ICSharpCode.SharpRefactory.Parser.AST.VB.CompilationUnit;
			if (fileCompilationUnit == null) {
//				ICSharpCode.SharpRefactory.Parser.VB.Parser fileParser = new ICSharpCode.SharpRefactory.Parser.VB.Parser();
//				fileParser.Parse(new Lexer(new StringReader(fileContent)));
				//Console.WriteLine("!Warning: no parseinformation!");
				return null;
			}
			VBNetVisitor vBNetVisitor = new VBNetVisitor();
			cu = (ICompilationUnit)vBNetVisitor.Visit(fileCompilationUnit, null);
			if (cu != null) {
				callingClass = parserService.GetInnermostClass(cu, caretLine, caretColumn);
				//Console.WriteLine("CallingClass is " + callingClass == null ? "null" : callingClass.Name);
			}
			lookupTableVisitor = new LookupTableVisitor();
			lookupTableVisitor.Visit(fileCompilationUnit, null);
			
			if (expression == null || expression == "") {
				expr = WithResolve();
				if (expr == null) {
					return null;
				}
			}
			
			if (expression.StartsWith("imports ")) {
				return ImportsResolve(expression);
			}
			//Console.WriteLine("Not in imports >{0}<", expression);
			
			if (InMain()) {
				showStatic = true;
			}
			
			// MyBase and MyClass are no expressions, only MyBase.Identifier and MyClass.Identifier
			if (expression == "mybase") {
				expr = new BaseReferenceExpression();
			} else if (expression == "myclass") {
				expr = new ClassReferenceExpression();
			}
			
			if (expr == null) {
				Lexer l = new Lexer(new StringReader(expression));
				ICSharpCode.SharpRefactory.Parser.VB.Parser p = new ICSharpCode.SharpRefactory.Parser.VB.Parser();
				expr = p.ParseExpression(l);
				if (expr == null) {
					//Console.WriteLine("Warning: No Expression from parsing!");
					return null;
				}
			}
			
//			Console.WriteLine(expr.ToString());
			TypeVisitor typeVisitor = new TypeVisitor(this);
			IReturnType type = expr.AcceptVisitor(typeVisitor, null) as IReturnType;
//			Console.WriteLine("type visited");
			if (type == null || type.PointerNestingLevel != 0) {
//				Console.WriteLine("Type == null || type.PointerNestingLevel != 0");
//				if (type != null) {
//					Console.WriteLine("PointerNestingLevel is " + type.PointerNestingLevel);
//				} else {
//					Console.WriteLine("Type == null");
//				}
				return null;
			}
			if (type.ArrayDimensions != null && type.ArrayDimensions.Length > 0) {
				type = new ReturnType("System.Array");
			}
		//	Console.WriteLine("Here: Type is " + type.FullyQualifiedName);
			IClass returnClass = SearchType(type.FullyQualifiedName, callingClass, cu);
			if (returnClass == null) {
//				Console.WriteLine("IClass is null! Trying namespace!");
				// Try if type is Namespace:
				string n = SearchNamespace(type.FullyQualifiedName, cu);
				if (n == null) {
					return null;
				}
				ArrayList content = parserService.GetNamespaceContents(n, false);
				ArrayList classes = new ArrayList();
				for (int i = 0; i < content.Count; ++i) {
					if (content[i] is IClass) {
						if (inNew) {
							IClass c = (IClass)content[i];
//							Console.WriteLine("Testing " + c.Name);
							if ((c.ClassType == ClassType.Class) || (c.ClassType == ClassType.Struct)) {
								classes.Add(c);
//								Console.WriteLine("Added");
							}
						} else {
							classes.Add((IClass)content[i]);
						}
					}
				}
				string[] namespaces = parserService.GetNamespaceList(n, false);
				return new ResolveResult(namespaces, classes);
			}
		//	Console.WriteLine("Returning Result!");
			if (inNew) {
				return new ResolveResult(returnClass, parserService.ListTypes(new ArrayList(), returnClass, callingClass));
			} else {
				return new ResolveResult(returnClass, parserService.ListMembers(new ArrayList(), returnClass, callingClass, showStatic));
			}
		}
		
		bool InMain()
		{
			return false;
		}
		
		Expression WithResolve()
		{
			//Console.WriteLine("in WithResolve");
			Expression expr = null;
			if (lookupTableVisitor.WithStatements != null) {
				//Console.WriteLine("{0} WithStatements", lookupTableVisitor.WithStatements.Count);
				foreach (WithStatement with in lookupTableVisitor.WithStatements) {
//					Console.WriteLine("Position: ({0}/{1})", with.StartLocation, with.EndLocation);
					if (IsInside(new Point(caretColumn, caretLine), with.StartLocation, with.EndLocation)) {
						expr = with.WithExpression;
					}
				}
			}
//			if (expr == null) {
//				Console.WriteLine("No WithStatement found");
//			} else {
//				Console.WriteLine("WithExpression : " + expr);
//			}
			return expr;
		}
		
		ResolveResult ImportsResolve(string expression)
		{
			// expression[expression.Length - 1] != '.'
			// the period that causes this Resove() is not part of the expression
			if (expression[expression.Length - 1] == '.') {
				return null;
			}
			int i;
			for (i = expression.Length - 1; i >= 0; --i) {
				if (!(Char.IsLetterOrDigit(expression[i]) || expression[i] == '_' || expression[i] == '.')) {
					break;
				}
			}
			// no Identifier before the period
			if (i == expression.Length - 1) {
				return null;
			}
			string t = expression.Substring(i + 1);
//			Console.WriteLine("in imports Statement");
			string[] namespaces = parserService.GetNamespaceList(t, false);
			if (namespaces == null || namespaces.Length <= 0) {
				return null;
			}
			return new ResolveResult(namespaces);
		}
		
		bool InStatic()
		{
			IProperty property = GetProperty();
			if (property != null) {
				return property.IsStatic;
			}
			IMethod method = GetMethod();
			if (method != null) {
				return method.IsStatic;
			}
			return false;
		}
		
		public ArrayList SearchMethod(IReturnType type, string memberName)
		{
			if (type == null || type.PointerNestingLevel != 0) {
				return new ArrayList();
			}
			IClass curType;
			if (type.ArrayDimensions != null && type.ArrayDimensions.Length > 0) {
				curType = SearchType("System.Array", null, null);
			} else {
				curType = SearchType(type.FullyQualifiedName, null, null);
				if (curType == null) {
					return new ArrayList();
				}
			}
			return SearchMethod(new ArrayList(), curType, memberName);
		}
		
		ArrayList SearchMethod(ArrayList methods, IClass curType, string memberName)
		{
			bool isClassInInheritanceTree = parserService.IsClassInInheritanceTree(curType, callingClass, false);
			foreach (IMethod m in curType.Methods) {
				if (m.Name.ToLower() == memberName.ToLower() &&
				    parserService.MustBeShown(curType, m, callingClass, showStatic, isClassInInheritanceTree) &&
				    !((m.Modifiers & ModifierEnum.Override) == ModifierEnum.Override)) {
					methods.Add(m);
				}
			}
			IClass baseClass = parserService.BaseClass(curType, false);
			if (baseClass != null) {
				return SearchMethod(methods, baseClass, memberName);
			}
			showStatic = false;
			return methods;
		}
		
		public ArrayList SearchIndexer(IReturnType type)
		{
			IClass curType = SearchType(type.FullyQualifiedName, null, null);
			if (curType != null) {
				return SearchIndexer(new ArrayList(), curType);
			}
			return new ArrayList();
		}
		
		public ArrayList SearchIndexer(ArrayList indexer, IClass curType)
		{
			bool isClassInInheritanceTree = parserService.IsClassInInheritanceTree(curType, callingClass, false);
			foreach (IIndexer i in curType.Indexer) {
				if (parserService.MustBeShown(curType, i, callingClass, showStatic, isClassInInheritanceTree) && !((i.Modifiers & ModifierEnum.Override) == ModifierEnum.Override)) {
					indexer.Add(i);
				}
			}
			IClass baseClass = parserService.BaseClass(curType, false);
			if (baseClass != null) {
				return SearchIndexer(indexer, baseClass);
			}
			showStatic = false;
			return indexer;
		}
		
		// no methods or indexer
		public IReturnType SearchMember(IReturnType type, string memberName)
		{
			if (type == null || memberName == null || memberName == "") {
				return null;
			}
//			Console.WriteLine("searching member {0} in {1}", memberName, type.Name);
			IClass curType = SearchType(type.FullyQualifiedName, callingClass, cu);
			bool isClassInInheritanceTree = parserService.IsClassInInheritanceTree(curType, callingClass, false);
			
			if (curType == null) {
//				Console.WriteLine("Type not found in SearchMember");
				return null;
			}
			if (type.PointerNestingLevel != 0) {
				return null;
			}
			if (type.ArrayDimensions != null && type.ArrayDimensions.Length > 0) {
				curType = SearchType("System.Array", null, null);
			}
			if (curType.ClassType == ClassType.Enum) {
				foreach (IField f in curType.Fields) {
					if (f.Name.ToLower() == memberName.ToLower() && parserService.MustBeShown(curType, f, callingClass, showStatic, isClassInInheritanceTree)) {
						showStatic = false;
						return type; // enum members have the type of the enum
					}
				}
			}
			if (showStatic) {
//				Console.WriteLine("showStatic == true");
				foreach (IClass c in curType.InnerClasses) {
					if (c.Name.ToLower() == memberName.ToLower() && parserService.IsAccessible(curType, c, callingClass, isClassInInheritanceTree)) {
						return new ReturnType(c.FullyQualifiedName);
					}
				}
			}
//			Console.WriteLine("#Properties " + curType.Properties.Count);
			foreach (IProperty p in curType.Properties) {
//				Console.WriteLine("checke Property " + p.Name);
//				Console.WriteLine("member name " + memberName);
				if (p.Name.ToLower() == memberName.ToLower() && parserService.MustBeShown(curType, p, callingClass, showStatic, isClassInInheritanceTree)) {
//					Console.WriteLine("Property found " + p.Name);
					showStatic = false;
					return p.ReturnType;
				}
			}
			foreach (IField f in curType.Fields) {
//				Console.WriteLine("checke Feld " + f.Name);
//				Console.WriteLine("member name " + memberName);
				if (f.Name.ToLower() == memberName.ToLower() && parserService.MustBeShown(curType, f, callingClass, showStatic, isClassInInheritanceTree)) {
//					Console.WriteLine("Field found " + f.Name);
					showStatic = false;
					return f.ReturnType;
				}
			}
			foreach (IEvent e in curType.Events) {
				if (e.Name.ToLower() == memberName.ToLower() && parserService.MustBeShown(curType, e, callingClass, showStatic, isClassInInheritanceTree)) {
					showStatic = false;
					return e.ReturnType;
				}
			}
			foreach (IMethod m in curType.Methods) {
//				Console.WriteLine("checke Method " + m.Name);
//				Console.WriteLine("member name " + memberName);
				if (m.Name.ToLower() == memberName.ToLower() && parserService.MustBeShown(curType, m, callingClass, showStatic, isClassInInheritanceTree) /* check if m has no parameters && m.*/) {
//					Console.WriteLine("Method found " + m.Name);
					showStatic = false;
					return m.ReturnType;
				}
			}
			foreach (string baseType in curType.BaseTypes) {
				IClass c = SearchType(baseType, curType);
				if (c != null) {
					IReturnType erg = SearchMember(new ReturnType(c.FullyQualifiedName), memberName);
					if (erg != null) {
						return erg;
					}
				}
			}
			return null;
		}
		
		bool IsInside(Point between, Point start, Point end)
		{
			if (between.Y < start.Y || between.Y > end.Y) {
//				Console.WriteLine("Y = {0} not between {1} and {2}", between.Y, start.Y, end.Y);
				return false;
			}
			if (between.Y > start.Y) {
				if (between.Y < end.Y) {
					return true;
				}
				// between.Y == end.Y
//				Console.WriteLine("between.Y = {0} == end.Y = {1}", between.Y, end.Y);
//				Console.WriteLine("returning {0}:, between.X = {1} <= end.X = {2}", between.X <= end.X, between.X, end.X);
				return between.X <= end.X;
			}
			// between.Y == start.Y
//			Console.WriteLine("between.Y = {0} == start.Y = {1}", between.Y, start.Y);
			if (between.X < start.X) {
				return false;
			}
			// start is OK and between.Y <= end.Y
			return between.Y < end.Y || between.X <= end.X;
		}
		
		ReturnType SearchVariable(string name)
		{
//			Console.WriteLine("Searching Variable");
//			
//			Console.WriteLine("LookUpTable has {0} entries", lookupTableVisitor.variables.Count);
//			Console.WriteLine("Listing Variables:");
//			IDictionaryEnumerator enumerator = lookupTableVisitor.variables.GetEnumerator();
//			while (enumerator.MoveNext()) {
//				Console.WriteLine(enumerator.Key);
//			}
//			Console.WriteLine("end listing");
			ArrayList variables = (ArrayList)lookupTableVisitor.Variables[name.ToLower()];
//			if (variables == null || variables.Count <= 0) {
//				Console.WriteLine(name + " not in LookUpTable");
//				return null;
//			}
			
			ReturnType found = null;
			if (variables != null) {
				foreach (LocalLookupVariable v in variables) {
//					Console.WriteLine("Position: ({0}/{1})", v.StartPos, v.EndPos);
					if (IsInside(new Point(caretColumn, caretLine), v.StartPos, v.EndPos)) {
						found = new ReturnType(v.TypeRef);
//						Console.WriteLine("Variable found");
						break;
					}
				}
			}
//			if (found == null) {
//				Console.WriteLine("No Variable found");
//				return null;
//			}
			return found;
		}
		
		/// <remarks>
		/// does the dynamic lookup for the typeName
		/// </remarks>
		public IReturnType DynamicLookup(string typeName)
		{
//			Console.WriteLine("starting dynamic lookup");
//			Console.WriteLine("name == " + typeName);
			
			// try if it exists a variable named typeName
			ReturnType variable = SearchVariable(typeName);
			if (variable != null) {
				showStatic = false;
				return variable;
			}
//			Console.WriteLine("No Variable found");
			
			if (callingClass == null) {
				return null;
			}
			//// somehow search in callingClass fields is not returning anything, so I am searching here once again
			foreach (IField f in callingClass.Fields) {
				if (f.Name.ToLower() == typeName.ToLower()) {
//					Console.WriteLine("Field found " + f.Name);
					return f.ReturnType;
				}
			}
			//// end of mod for search in Fields
		
			// try if typeName is a method parameter
			IReturnType p = SearchMethodParameter(typeName);
			if (p != null) {
//				Console.WriteLine("MethodParameter Found");
				showStatic = false;
				return p;
			}
//			Console.WriteLine("No Parameter found");
			
			// check if typeName == value in set method of a property
			if (typeName == "value") {
				p = SearchProperty();
				if (p != null) {
					showStatic = false;
					return p;
				}
			}
//			Console.WriteLine("No Property found");
			
			// try if there exists a nonstatic member named typeName
			showStatic = false;
			IReturnType t = SearchMember(callingClass == null ? null : new ReturnType(callingClass.FullyQualifiedName), typeName);
			if (t != null) {
				return t;
			}
//			Console.WriteLine("No nonstatic member found");
			
			// try if there exists a static member named typeName
			showStatic = true;
			t = SearchMember(callingClass == null ? null : new ReturnType(callingClass.FullyQualifiedName), typeName);
			if (t != null) {
				showStatic = false;
				return t;
			}
//			Console.WriteLine("No static member found");
			
			// try if there exists a static member in outer classes named typeName
			ClassCollection classes = parserService.GetOuterClasses(cu, caretLine, caretColumn);
			foreach (IClass c in classes) {
				t = SearchMember(callingClass == null ? null : new ReturnType(c.FullyQualifiedName), typeName);
				if (t != null) {
					showStatic = false;
					return t;
				}
			}
//			Console.WriteLine("No static member in outer classes found");
//			Console.WriteLine("DynamicLookUp resultless");
			return null;
		}
		
		IProperty GetProperty()
		{
			foreach (IProperty property in callingClass.Properties) {
				if (property.BodyRegion != null && property.BodyRegion.IsInside(caretLine, caretColumn)) {
					return property;
				}
			}
			return null;
		}
		
		IMethod GetMethod()
		{
			foreach (IMethod method in callingClass.Methods) {
				if (method.BodyRegion != null && method.BodyRegion.IsInside(caretLine, caretColumn)) {
					return method;
				}
			}
			return null;
		}
		
		IReturnType SearchProperty()
		{
			IProperty property = GetProperty();
			if (property == null) {
				return null;
			}
			if (property.SetterRegion != null && property.SetterRegion.IsInside(caretLine, caretColumn)) {
				return property.ReturnType;
			}
			return null;
		}
		
		IReturnType SearchMethodParameter(string parameter)
		{
			IMethod method = GetMethod();
			if (method == null) {
				//Console.WriteLine("Method not found");
				return null;
			}
			foreach (IParameter p in method.Parameters) {
				if (p.Name.ToLower() == parameter.ToLower()) {
				//	Console.WriteLine("Parameter found");
					return p.ReturnType;
				}
			}
			return null;
		}
		
		/// <remarks>
		/// use the usings to find the correct name of a namespace
		/// </remarks>
		public string SearchNamespace(string name, ICompilationUnit unit)
		{
			return parserService.SearchNamespace(name, unit, caretLine, caretColumn, false);
		}
		
		/// <remarks>
		/// use the usings and the name of the namespace to find a class
		/// </remarks>
		public IClass SearchType(string name, IClass curType)
		{
			return parserService.SearchType(name, curType, caretLine, caretColumn, false);
		}
		
		/// <remarks>
		/// use the usings and the name of the namespace to find a class
		/// </remarks>
		public IClass SearchType(string name, IClass curType, ICompilationUnit unit)
		{
			return parserService.SearchType(name, curType, unit, caretLine, caretColumn, false);
		}
		
		public ArrayList CtrlSpace(IParserService parserService, int caretLine, int caretColumn, string fileName)
		{
			ArrayList result = new ArrayList(TypeReference.PrimitiveTypes);
			this.parserService = parserService;
			IParseInformation parseInfo = parserService.GetParseInformation(fileName);
			ICSharpCode.SharpRefactory.Parser.AST.VB.CompilationUnit fileCompilationUnit = parseInfo.MostRecentCompilationUnit.Tag as ICSharpCode.SharpRefactory.Parser.AST.VB.CompilationUnit;
			if (fileCompilationUnit == null) {
				//Console.WriteLine("!Warning: no parseinformation!");
				return null;
			}
			LookupTableVisitor lookupTableVisitor = new LookupTableVisitor();
			lookupTableVisitor.Visit(fileCompilationUnit, null);
			VBNetVisitor vBNetVisitor = new VBNetVisitor();
			cu = (ICompilationUnit)vBNetVisitor.Visit(fileCompilationUnit, null);
			if (cu != null) {
				callingClass = parserService.GetInnermostClass(cu, caretLine, caretColumn);
//				Console.WriteLine("CallingClass is " + callingClass == null ? "null" : callingClass.Name);
			}
			foreach (string name in lookupTableVisitor.Variables.Keys) {
				ArrayList variables = (ArrayList)lookupTableVisitor.Variables[name.ToLower()];
				if (variables != null && variables.Count > 0) {
					foreach (LocalLookupVariable v in variables) {
						if (IsInside(new Point(caretColumn, caretLine), v.StartPos, v.EndPos)) {
							result.Add(v);
							break;
						}
					}
				}
			}
			if (callingClass != null) {
				result = parserService.ListMembers(result, callingClass, callingClass, InStatic());
			}
			string n = "";
			result.AddRange(parserService.GetNamespaceContents(n, false));
			foreach (IUsing u in cu.Usings) {
				if (u != null && (u.Region == null || u.Region.IsInside(caretLine, caretColumn))) {
					foreach (string name in u.Usings) {
						result.AddRange(parserService.GetNamespaceContents(name, false));
					}
					foreach (string alias in u.Aliases.Keys) {
						result.Add(alias);
					}
				}
			}
			return result;
		}
	}
}
