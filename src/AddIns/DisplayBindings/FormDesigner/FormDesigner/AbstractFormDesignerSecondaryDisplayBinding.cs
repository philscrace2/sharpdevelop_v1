// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Xml;

using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Internal.Project;
using ICSharpCode.SharpDevelop.Internal.Undo;
using ICSharpCode.SharpDevelop.Gui.Components;
using ICSharpCode.SharpDevelop.DefaultEditor.Gui.Editor;

using ICSharpCode.Core.Properties;
using ICSharpCode.Core.AddIns;
using ICSharpCode.Core.Services;
using ICSharpCode.SharpDevelop.Services;
using SharpDevelop.Internal.Parser;
using ICSharpCode.SharpDevelop.FormDesigner.Services;
using ICSharpCode.SharpDevelop.FormDesigner.Hosts;
using ICSharpCode.SharpDevelop.FormDesigner.Util;
using ICSharpCode.Core.AddIns.Codons;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

using ICSharpCode.SharpRefactory.Parser;
using ICSharpCode.SharpRefactory.Parser.AST;
using ICSharpCode.SharpRefactory.PrettyPrinter;

using System.CodeDom;
using System.CodeDom.Compiler;

using Microsoft.CSharp;
using Microsoft.VisualBasic;

namespace ICSharpCode.SharpDevelop.FormDesigner
{
	public abstract class AbstractFormDesignerSecondaryDisplayBinding : ISecondaryDisplayBinding
	{
		protected abstract string Extension {
			get;
		}
		
		IMethod GetInitializeComponents(IClass c)
		{
			foreach (IMethod method in c.Methods) {
				if ((method.Name == "InitializeComponents" || method.Name == "InitializeComponent") && method.Parameters.Count == 0) {
					return method;
				}
			}
			return null;
		}
		
		static Hashtable oldTypes = new Hashtable();
		public static bool BaseClassIsFormOrControl(IClass c)
		{
			if (c == null || oldTypes.Contains(c.FullyQualifiedName)) {
				oldTypes.Clear();
				return false;
			}
			oldTypes.Add(c.FullyQualifiedName, null);
			IParserService parserService = (IParserService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IParserService));
			foreach (string baseType in c.BaseTypes) {
				IClass type = parserService.SearchType(baseType, c, c.Region != null ? c.Region.BeginLine : 0, c.Region != null ? c.Region.BeginColumn : 0);
				if (type != null) {
					if (type.FullyQualifiedName == "System.Windows.Forms.Form" ||
					    type.FullyQualifiedName == "System.Windows.Forms.UserControl" ||
					    BaseClassIsFormOrControl(type)) {
						oldTypes.Clear();
						return true;
					}
				}
			}
			oldTypes.Clear();
			return false;
		}
		
		public bool CanAttachTo(IViewContent viewContent)
		{
			if (viewContent is ITextEditorControlProvider) {
				ITextEditorControlProvider textAreaControlProvider = (ITextEditorControlProvider)viewContent;
				string fileExtension = String.Empty;
				string fileName      = viewContent.IsUntitled ? viewContent.UntitledName : viewContent.FileName;
				
				try {
					fileExtension = Path.GetExtension(fileName).ToLower();
				} catch (Exception e) {
					Console.WriteLine(e);
				}
				
				if (fileExtension == Extension) {
					IParserService parserService  = (IParserService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IParserService));
					IParseInformation info = viewContent.IsUntitled ? null : parserService.GetParseInformation(fileName);
					
					if (info == null) {
						info = parserService.ParseFile(fileName, textAreaControlProvider.TextEditorControl.Document.TextContent, false);
					}
					
					if (info != null) {
						ICompilationUnit cu = (ICompilationUnit)info.BestCompilationUnit;
						foreach (IClass c in cu.Classes) {
							if (BaseClassIsFormOrControl(c)) {
								IMethod method = GetInitializeComponents(c);
								if (method == null) {
									continue;
								}
								return true;
							}
						}
					}
				}
			}
			return false;
		}
		public abstract ISecondaryViewContent [] CreateSecondaryViewContent(IViewContent viewContent);
	}
}
