// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Threading;
using System.Drawing;
using System.Drawing.Printing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;

using ICSharpCode.Core.AddIns;

using ICSharpCode.Core.CoreProperties;
using ICSharpCode.Core.AddIns.Codons;
using ICSharpCode.Core.Services;

using ICSharpCode.SharpDevelop.Services;
using ICSharpCode.SharpDevelop.Internal.Project;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Gui.Dialogs;

namespace ICSharpCode.SharpDevelop.Commands
{
	public class CreateNewProject : AbstractMenuCommand
	{
		public override void Run()
		{
			IProjectService projectService = (IProjectService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IProjectService));
			
			using (NewProjectDialog npdlg = new NewProjectDialog(true)) {
				npdlg.Owner = (Form)WorkbenchSingleton.Workbench;
				npdlg.ShowDialog();
			}
		}
	}
	
	public class CreateNewFile : AbstractMenuCommand
	{
		public override void Run()
		{
			using (NewFileDialog nfd = new NewFileDialog(null)) {
				nfd.Owner = (Form)WorkbenchSingleton.Workbench;
				nfd.ShowDialog();
			}
			if (WorkbenchSingleton.Workbench.ActiveWorkbenchWindow != null) {
				WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.SelectWindow();
			}
		}
	}
	
	public class CloseFile : AbstractMenuCommand
	{
		public override void Run()
		{
			if (WorkbenchSingleton.Workbench.ActiveWorkbenchWindow != null) {
				WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.CloseWindow(false);
			}
		}
	}

	public class SaveFile : AbstractMenuCommand
	{
		public override void Run()
		{
			IWorkbenchWindow window = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow;
			if (window != null) {
				if (window.ViewContent.IsViewOnly) {
					return;
				}
				
				if (window.ViewContent.FileName == null) {
					SaveFileAs sfa = new SaveFileAs();
					sfa.Run();
				} else {
					FileAttributes attr = FileAttributes.ReadOnly | FileAttributes.Directory | FileAttributes.Offline | FileAttributes.System;
					if (File.Exists(window.ViewContent.FileName) && (File.GetAttributes(window.ViewContent.FileName) & attr) != 0) {
						SaveFileAs sfa = new SaveFileAs();
						sfa.Run();
					} else {
						IProjectService projectService = (IProjectService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IProjectService));
						FileUtilityService fileUtilityService = (FileUtilityService)ServiceManager.Services.GetService(typeof(FileUtilityService));
						projectService.MarkFileDirty(window.ViewContent.FileName);
						fileUtilityService.ObservedSave(new FileOperationDelegate(window.ViewContent.Save), window.ViewContent.FileName);
					}
				}
			}
		}
	}
	
	public class ReloadFile : AbstractMenuCommand
	{
		public override void Run()
		{
			IWorkbenchWindow window = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow;
			
			if (window != null && window.ViewContent.FileName != null && !window.ViewContent.IsViewOnly) {
				IMessageService messageService =(IMessageService)ServiceManager.Services.GetService(typeof(IMessageService));
				if (messageService.AskQuestion("${res:ICSharpCode.SharpDevelop.Commands.ReloadFile.ReloadFileQuestion}")) {
					IXmlConvertable memento = null;
					if (window.ViewContent is IMementoCapable) {
						memento = ((IMementoCapable)window.ViewContent).CreateMemento();
					}
					window.ViewContent.Load(window.ViewContent.FileName);
					if (memento != null) {
						((IMementoCapable)window.ViewContent).SetMemento(memento);
					}
				}
			}
		}
	}
	
	public class SaveFileAs : AbstractMenuCommand
	{
		public override void Run()
		{
			IWorkbenchWindow window = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow;
			
			if (window != null) {
				if (window.ViewContent.IsViewOnly) {
					return;
				}
				if (window.ViewContent is ICustomizedCommands) {
					if (((ICustomizedCommands)window.ViewContent).SaveAsCommand()) {
						return;
					}
				}
				using (SaveFileDialog fdiag = new SaveFileDialog()) {
					fdiag.OverwritePrompt = true;
					fdiag.AddExtension    = true;
					
					string[] fileFilters  = (string[])(AddInTreeSingleton.AddInTree.GetTreeNode("/SharpDevelop/Workbench/FileFilter").BuildChildItems(this)).ToArray(typeof(string));
					fdiag.Filter          = String.Join("|", fileFilters);
					for (int i = 0; i < fileFilters.Length; ++i) {
						if (fileFilters[i].IndexOf(Path.GetExtension(window.ViewContent.FileName == null ? window.ViewContent.UntitledName : window.ViewContent.FileName)) >= 0) {
							fdiag.FilterIndex = i + 1;
							break;
						}
					}
					
					if (fdiag.ShowDialog() == DialogResult.OK) {
						string fileName = fdiag.FileName;
						
						IFileService fileService = (IFileService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IFileService));
						FileUtilityService fileUtilityService = (FileUtilityService)ServiceManager.Services.GetService(typeof(FileUtilityService));
						if (!fileUtilityService.IsValidFileName(fileName)) {
							IMessageService messageService =(IMessageService)ServiceManager.Services.GetService(typeof(IMessageService));
							StringParserService stringParserService = (StringParserService)ServiceManager.Services.GetService(typeof(StringParserService));
							messageService.ShowMessage(stringParserService.Parse("${res:ICSharpCode.SharpDevelop.Commands.SaveFile.InvalidFileNameError}", new string[,] {{"FileName", fileName}}));
							
							return;
						}
						
						if (fileUtilityService.ObservedSave(new NamedFileOperationDelegate(window.ViewContent.Save), fileName) == FileOperationResult.OK) {
							fileService.RecentOpen.AddLastFile(fileName);
							IMessageService messageService =(IMessageService)ServiceManager.Services.GetService(typeof(IMessageService));
							messageService.ShowMessage(fileName, "${res:ICSharpCode.SharpDevelop.Commands.SaveFile.FileSaved}");
							((DefaultWorkbench)WorkbenchSingleton.Workbench).UpdateToolbars();
						}
					}
				}
			}
		}
	}
	
	public class SaveAllFiles : AbstractMenuCommand
	{
		public override void Run()
		{
			FileUtilityService fileUtilityService = (FileUtilityService)ServiceManager.Services.GetService(typeof(FileUtilityService));
			
			foreach (IViewContent content in WorkbenchSingleton.Workbench.ViewContentCollection) {
				if (content.IsViewOnly) {
					continue;
				}
				
				if (content.FileName == null) {
					if (content is ICustomizedCommands) {
						if (((ICustomizedCommands)content).SaveAsCommand()) {
							continue;
						}
					} else {
						using (SaveFileDialog fdiag = new SaveFileDialog()) {
							fdiag.OverwritePrompt = true;
							fdiag.AddExtension    = true;
							
							fdiag.Filter          = String.Join("|", (string[])(AddInTreeSingleton.AddInTree.GetTreeNode("/SharpDevelop/Workbench/FileFilter").BuildChildItems(this)).ToArray(typeof(string)));
						
							if (fdiag.ShowDialog() == DialogResult.OK) {
								string fileName = fdiag.FileName;
								// currently useless, because the fdiag.FileName can't
								// handle wildcard extensions :(
								if (Path.GetExtension(fileName).StartsWith("?") || Path.GetExtension(fileName) == "*") {
									fileName = Path.ChangeExtension(fileName, "");
								}
								if (fileUtilityService.ObservedSave(new NamedFileOperationDelegate(content.Save), fileName) == FileOperationResult.OK) {
									IMessageService messageService =(IMessageService)ServiceManager.Services.GetService(typeof(IMessageService));
									messageService.ShowMessage(fileName, "${res:ICSharpCode.SharpDevelop.Commands.SaveFile.FileSaved}");
								}
							}
						}
					}
				} else {
					fileUtilityService.ObservedSave(new FileOperationDelegate(content.Save), content.FileName);
				}
			}
		}
	}
	
	public class OpenCombine : AbstractMenuCommand
	{
		public override void Run()
		{
			using (OpenFileDialog fdiag  = new OpenFileDialog()) {
				fdiag.AddExtension    = true;
				fdiag.Filter          = String.Join("|", (string[])(AddInTreeSingleton.AddInTree.GetTreeNode("/SharpDevelop/Workbench/Combine/FileFilter").BuildChildItems(this)).ToArray(typeof(string)));
				fdiag.Multiselect     = false;
				fdiag.CheckFileExists = true;
				if (fdiag.ShowDialog() == DialogResult.OK) {
					switch (Path.GetExtension(fdiag.FileName).ToUpper()) {
						case ".CMBX": // Don't forget the 'recent' projects if you chance something here
						case ".PRJX":
							IProjectService projectService = (IProjectService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IProjectService));
							FileUtilityService fileUtilityService = (FileUtilityService)ServiceManager.Services.GetService(typeof(FileUtilityService));
							fileUtilityService.ObservedLoad(new NamedFileOperationDelegate(projectService.OpenCombine), fdiag.FileName);
							break;
						default:
							IMessageService messageService =(IMessageService)ServiceManager.Services.GetService(typeof(IMessageService));
							StringParserService stringParserService = (StringParserService)ServiceManager.Services.GetService(typeof(StringParserService));
							messageService.ShowError(stringParserService.Parse("${res:ICSharpCode.SharpDevelop.Commands.OpenCombine.InvalidProjectOrCombine}", new string[,] {{"FileName", fdiag.FileName}}));
							break;
					}
				}
			}
		}
	}
	
	public class OpenFile : AbstractMenuCommand
	{
		public override void Run()
		{
			using (OpenFileDialog fdiag  = new OpenFileDialog()) {
				fdiag.AddExtension    = true;
				
				string[] fileFilters  = (string[])(AddInTreeSingleton.AddInTree.GetTreeNode("/SharpDevelop/Workbench/FileFilter").BuildChildItems(this)).ToArray(typeof(string));
				fdiag.Filter          = String.Join("|", fileFilters);
				bool foundFilter      = false;
				// search filter like in the current selected project
				// TODO: remove duplicate code (FolderNodeCommands has the same)
				IProjectService projectService = (IProjectService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IProjectService));
				IProject project = projectService.CurrentSelectedProject;
				if (project == null && projectService.CurrentOpenCombine != null) {
					ArrayList projects = Combine.GetAllProjects(projectService.CurrentOpenCombine);
					if (projects.Count > 0) {
						project = ((ProjectCombineEntry)projects[0]).Project;
					}
				}
				if (project != null) {
					LanguageBindingService languageBindingService = (LanguageBindingService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(LanguageBindingService));
					LanguageBindingCodon languageCodon = languageBindingService.GetCodonPerLanguageName(project.ProjectType);
					
					for (int i = 0; !foundFilter && i < fileFilters.Length; ++i) {
						for (int j = 0; !foundFilter && j < languageCodon.Supportedextensions.Length; ++j) {
							if (fileFilters[i].IndexOf(languageCodon.Supportedextensions[j]) >= 0) {
								fdiag.FilterIndex = i + 1;
								foundFilter       = true;
								break;
							}
						}
					}
				}
				
				// search filter like in the current open file
				if (!foundFilter) {
					IWorkbenchWindow window = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow;
					if (window != null) {
						for (int i = 0; i < fileFilters.Length; ++i) {
							if (fileFilters[i].IndexOf(Path.GetExtension(window.ViewContent.FileName == null ? window.ViewContent.UntitledName : window.ViewContent.FileName)) >= 0) {
								fdiag.FilterIndex = i + 1;
								break;
							}
						}
					}
				}
				
				fdiag.Multiselect     = true;
				fdiag.CheckFileExists = true;
				
				if (fdiag.ShowDialog() == DialogResult.OK) {
					IFileService fileService = (IFileService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IFileService));
					foreach (string name in fdiag.FileNames) {
						fileService.OpenFile(name);
					}
				}
			}
		}
	}
	
	public class ClearCombine : AbstractMenuCommand
	{
		public override void Run()
		{
			IProjectService projectService = (IProjectService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IProjectService));
			
			projectService.CloseCombine();
		}
	}
		
	public class ExitWorkbenchCommand : AbstractMenuCommand
	{
		public override void Run()
		{			
			((Form)WorkbenchSingleton.Workbench).Close();
		}
	}
	
	public class Print : AbstractMenuCommand
	{
		public override void Run()
		{
			IWorkbenchWindow window = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow;
			
			if (window != null) {
				if (window.ViewContent is IPrintable) {
					PrintDocument pdoc = ((IPrintable)window.ViewContent).PrintDocument;
					if (pdoc != null) {
						using (PrintDialog ppd = new PrintDialog()) {
							ppd.Document  = pdoc;
							ppd.AllowSomePages = true;
							if (ppd.ShowDialog() == DialogResult.OK) { // fixed by Roger Rubin
								pdoc.Print();
							}
						}
					} else {
						IMessageService messageService =(IMessageService)ServiceManager.Services.GetService(typeof(IMessageService));
						messageService.ShowError("${res:ICSharpCode.SharpDevelop.Commands.Print.CreatePrintDocumentError}");
					}
				} else {
					IMessageService messageService =(IMessageService)ServiceManager.Services.GetService(typeof(IMessageService));
					messageService.ShowError("${res:ICSharpCode.SharpDevelop.Commands.Print.CantPrintWindowContentError}");
				}
			}
		}
	}
	
	public class PrintPreview : AbstractMenuCommand
	{
		public override void Run()
		{
			try {
				IWorkbenchWindow window = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow;
				
				if (window != null) {
					if (window.ViewContent is IPrintable) {
						using (PrintDocument pdoc = ((IPrintable)window.ViewContent).PrintDocument) {
							if (pdoc != null) {
								PrintPreviewDialog ppd = new PrintPreviewDialog();
								ppd.Owner     = (Form)WorkbenchSingleton.Workbench;
								ppd.TopMost   = true;
								ppd.Document  = pdoc;
								ppd.Show();
							} else {
								IMessageService messageService =(IMessageService)ServiceManager.Services.GetService(typeof(IMessageService));
								messageService.ShowError("${res:ICSharpCode.SharpDevelop.Commands.Print.CreatePrintDocumentError}");
							}
						}
					}
				}
			} catch (System.Drawing.Printing.InvalidPrinterException) {
			}
		}
	}
	
	public class ClearRecentFiles : AbstractMenuCommand
	{
		public override void Run()
		{			
			try {
				IFileService fileService = (IFileService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IFileService));
				fileService.RecentOpen.ClearRecentFiles();
			} catch {}
		}
	}
	
	public class ClearRecentProjects : AbstractMenuCommand
	{
		public override void Run()
		{			
			try {
				IFileService fileService = (IFileService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IFileService));
				fileService.RecentOpen.ClearRecentProjects();
			} catch {}
		}
	}
}
