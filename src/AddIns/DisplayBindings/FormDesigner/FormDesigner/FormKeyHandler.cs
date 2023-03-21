// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;
using System.Drawing;
using System.Drawing.Design;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.Design;
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

using ICSharpCode.SharpDevelop.FormDesigner.Services;
using ICSharpCode.SharpDevelop.FormDesigner.Hosts;
using ICSharpCode.SharpDevelop.FormDesigner.Util;
using ICSharpCode.Core.AddIns.Codons;

using System.CodeDom;
using System.CodeDom.Compiler;

using Microsoft.CSharp;
using Microsoft.VisualBasic;

namespace ICSharpCode.SharpDevelop.FormDesigner
{
	public class FormKeyHandler : IMessageFilter
	{
		const int keyPressedMessage          = 0x100;
		const int leftMouseButtonDownMessage = 0x0202;
		
		Hashtable keyTable = new Hashtable();
		
		public FormKeyHandler()
		{
			// normal keys
			keyTable[Keys.Left]  = new CommandWrapper(MenuCommands.KeyMoveLeft);
			keyTable[Keys.Right] = new CommandWrapper(MenuCommands.KeyMoveRight);
			keyTable[Keys.Up]    = new CommandWrapper(MenuCommands.KeyMoveUp);
			keyTable[Keys.Down]  = new CommandWrapper(MenuCommands.KeyMoveDown);
			keyTable[Keys.Tab]   = new CommandWrapper(MenuCommands.KeySelectNext, false);
			keyTable[Keys.Delete]   = new CommandWrapper(MenuCommands.Delete, false);
			keyTable[Keys.Back]   = new CommandWrapper(MenuCommands.Delete, false);
			
			// shift modified keys
			keyTable[Keys.Left | Keys.Shift]  = new CommandWrapper(MenuCommands.KeySizeWidthDecrease);
			keyTable[Keys.Right | Keys.Shift] = new CommandWrapper(MenuCommands.KeySizeWidthIncrease);
			keyTable[Keys.Up | Keys.Shift]    = new CommandWrapper(MenuCommands.KeySizeHeightDecrease);
			keyTable[Keys.Down | Keys.Shift]  = new CommandWrapper(MenuCommands.KeySizeHeightIncrease);
			keyTable[Keys.Tab | Keys.Shift]   = new CommandWrapper(MenuCommands.KeySelectPrevious, false);
			keyTable[Keys.Delete| Keys.Shift]   = new CommandWrapper(MenuCommands.Delete, false);
			keyTable[Keys.Back| Keys.Shift]   = new CommandWrapper(MenuCommands.Delete, false);
			
			// ctrl modified keys
			keyTable[Keys.Left | Keys.Control]  = new CommandWrapper(MenuCommands.KeyNudgeLeft);
			keyTable[Keys.Right | Keys.Control] = new CommandWrapper(MenuCommands.KeyNudgeRight);
			keyTable[Keys.Up | Keys.Control]    = new CommandWrapper(MenuCommands.KeyNudgeUp);
			keyTable[Keys.Down | Keys.Control]  = new CommandWrapper(MenuCommands.KeyNudgeDown);
			
			// ctrl + shift modified keys
			keyTable[Keys.Left | Keys.Control | Keys.Shift]  = new CommandWrapper(MenuCommands.KeyNudgeWidthDecrease);
			keyTable[Keys.Right | Keys.Control | Keys.Shift] = new CommandWrapper(MenuCommands.KeyNudgeWidthIncrease);
			keyTable[Keys.Up | Keys.Control | Keys.Shift]    = new CommandWrapper(MenuCommands.KeyNudgeHeightDecrease);
			keyTable[Keys.Down | Keys.Control | Keys.Shift]  = new CommandWrapper(MenuCommands.KeyNudgeHeightIncrease);
		} 
		
		public bool PreFilterMessage(ref Message m)
		{
			if (m.Msg != keyPressedMessage && m.Msg != leftMouseButtonDownMessage) {
				return false;
			}
			
			if (WorkbenchSingleton.Workbench.ActiveWorkbenchWindow == null ||
			    !WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ActiveViewContent.Control.ContainsFocus) {
				return false;
			}
			
			FormDesignerDisplayBindingBase formDesigner = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ActiveViewContent as FormDesignerDisplayBindingBase;
			
			if (formDesigner == null) {
				return false;
			}
			
			if (!formDesigner.IsFormDesignerVisible) {
				return false;
			}
			
			if (AbstractMenuEditorControl.MenuEditorFocused) {
				return false;
			}
			
			if (m.Msg == leftMouseButtonDownMessage) {
				if (formDesigner.IsTabOrderMode) {
					Point p = new Point(m.LParam.ToInt32());
					Control c = Control.FromHandle(m.HWnd);
					try {
						if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift) {
							formDesigner.SetPrevTabIndex(c.PointToScreen(p));
						} else {
							formDesigner.SetNextTabIndex(c.PointToScreen(p));
						}
					} catch (Exception e) {
						Console.WriteLine(e);
					}
				}	
				return false;
			}
			
			Keys keyPressed = (Keys)m.WParam.ToInt32() | Control.ModifierKeys;
			if (keyPressed == Keys.F1) {
				return false;
			}
			
			if (keyPressed == Keys.Escape) {
				formDesigner.HideTabOrder();
				return true;
			}
			
			CommandWrapper commandWrapper = (CommandWrapper)keyTable[keyPressed];
			if (commandWrapper != null) {
				IMenuCommandService menuCommandService = (IMenuCommandService)formDesigner.DesignerHost.GetService(typeof(IMenuCommandService));
				ISelectionService   selectionService = (ISelectionService)formDesigner.DesignerHost.GetService(typeof(ISelectionService));
				ICollection components = selectionService.GetSelectedComponents();
				
				menuCommandService.GlobalInvoke(commandWrapper.CommandID);
				
				if (commandWrapper.RestoreSelection) {
					selectionService.SetSelectedComponents(components);
				}
				return true;
			}
			
			return (System.Windows.Forms.Control.ModifierKeys & Keys.Alt)     != Keys.Alt &&
			       (System.Windows.Forms.Control.ModifierKeys & Keys.Control) != Keys.Control;
		}
		
		class CommandWrapper
		{
			CommandID commandID;
			bool      restoreSelection;
			
			public CommandID CommandID {
				get {
					return commandID;
				}
			}
			
			public bool RestoreSelection {
				get {
					return restoreSelection;
				}
			}
			
			public CommandWrapper(CommandID commandID) : this(commandID, true)
			{
			}
			public CommandWrapper(CommandID commandID, bool restoreSelection)
			{
				this.commandID        = commandID;
				this.restoreSelection = restoreSelection;
			}
		}
	}
}
