using System;
using System.Windows.Forms;

using ICSharpCode.Core.AddIns.Codons;
using ICSharpCode.SharpDevelop.Gui;

namespace ResourceEditor
{
		
	class SelectAllCommand : AbstractMenuCommand
	{
		public override void Run()
		{
			IWorkbenchWindow window = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow;
			ResourceEditorControl editor = (ResourceEditorControl)window.ViewContent.Control;
			
			editor.ResourceList.ClipboardHandler.SelectAll(null, EventArgs.Empty);
		}
	}
	
}
