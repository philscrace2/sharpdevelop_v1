using System;
using System.Windows.Forms;
using System.IO;
using System.Drawing;

using ICSharpCode.Core.AddIns.Codons;
using ICSharpCode.SharpDevelop.Gui;

namespace ResourceEditor
{
	class RenameEntryCommand : AbstractMenuCommand
	{
		public override void Run()
		{
			IWorkbenchWindow window = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow;
			ResourceEditorControl editor = (ResourceEditorControl)window.ViewContent.Control;
			
			if(editor.ResourceList.SelectedItems.Count != 0) {
				editor.ResourceList.SelectedItems[0].BeginEdit();
			}
		}
	}
}
