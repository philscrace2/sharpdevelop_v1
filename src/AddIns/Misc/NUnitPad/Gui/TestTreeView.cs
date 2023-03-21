// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.IO;
using System.Windows.Forms;

using ICSharpCode.Core.AddIns;
using ICSharpCode.Core.Services;
using ICSharpCode.SharpDevelop.Services;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Gui.Pads;

using NUnit.Core;
using NUnit.Framework;

namespace ICSharpCode.NUnitPad
{
	/// <summary>
	/// Description of TestTreeView.	
	/// </summary>
	public class TestTreeView : System.Windows.Forms.UserControl, EventListener, IOwnerState
	{
		const string ContextMenuAddInTreePath = "/NUnitPad/TestTreeView/ContextMenu";
		
		[Flags]
		public enum TestTreeViewState {
			Nothing                = 0,
			SourceCodeItemSelected = 1,
			TestItemSelected       = 2,
		}
		
		protected TestTreeViewState internalState = TestTreeViewState.Nothing;

		public System.Enum InternalState {
			get {
				return internalState;
			}
		}
		
		TreeView  treeView;
		Hashtable treeNodeHash = new Hashtable();
		MessageViewCategory testRunnerCategory;
		MenuService menuService = (MenuService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(MenuService));
		
		bool IsSourceCodeItemSelected {
			get {
				if (treeView.SelectedNode == null) {
					return false;
				}
				ITest test = treeView.SelectedNode.Tag as ITest;
				if (test != null) {
					IParserService parserService = (IParserService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IParserService));
					Position position = parserService.GetPosition(test.FullName.Replace('+', '.'));
					return position != null && position.Cu != null;
				}
				return false;
			}
		}
		
		bool IsTestItemSelected {
			get {
				if (treeView.SelectedNode == null) {
					return false;
				}
				ITest test = treeView.SelectedNode.Tag as ITest;
				return test != null;
			}
		}
		
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				treeView.Dispose();
			}
			base.Dispose(disposing);
		}
		
		public TestTreeView()
		{
			IconService iconService = (IconService)ServiceManager.Services.GetService(typeof(IconService));
			ImageList imageList = new ImageList();
			imageList.ColorDepth = ColorDepth.Depth32Bit;
			imageList.Images.Add(iconService.GetBitmap("Icons.16x16.TestRunner.Gray"));
			imageList.Images.Add(iconService.GetBitmap("Icons.16x16.TestRunner.Green"));
			imageList.Images.Add(iconService.GetBitmap("Icons.16x16.TestRunner.Yellow"));
			imageList.Images.Add(iconService.GetBitmap("Icons.16x16.TestRunner.Red"));
			imageList.Images.Add(iconService.GetBitmap("Icons.16x16.AboutIcon"));
			imageList.Images.Add(iconService.GetBitmap("Icons.16x16.Error"));
			
			this.treeView = new TreeView();
			treeView.ImageList = imageList;
			treeView.Dock = DockStyle.Fill;
			treeView.DoubleClick += new EventHandler(ActivateItem);
			treeView.KeyPress += new KeyPressEventHandler(TestTreeViewKeyPress);
			treeView.MouseDown += new MouseEventHandler(TreeViewMouseDown);
			treeView.HideSelection = false;
			Controls.Add(treeView);
			treeView.ContextMenu = menuService.CreateContextMenu(this, ContextMenuAddInTreePath);
		}
		void TreeViewMouseDown(object sender, MouseEventArgs e)
		{
			TreeNode node = treeView.GetNodeAt(e.X, e.Y);

			treeView.SelectedNode = node;
			
			internalState = TestTreeViewState.Nothing;
			if (IsSourceCodeItemSelected) {
				internalState |= TestTreeViewState.SourceCodeItemSelected;
			} 
			
			if (IsTestItemSelected) {
				internalState |= TestTreeViewState.TestItemSelected;
			}
		}
		
		public void SetAutoLoadState(bool state)
		{
			if (!state) {
				ClearTests();
				StringParserService stringParserService = (StringParserService)ServiceManager.Services.GetService(typeof(StringParserService));
				TreeNode noAutoLoad = new TreeNode(stringParserService.Parse("${res:NUnitPad.NUnitPadContent.TestTreeView.ClickOnRunInformationNode}"));
				noAutoLoad.ImageIndex = noAutoLoad.SelectedImageIndex = 4;
				treeView.Nodes.Add(noAutoLoad);
			}
		}
		public void ClearTests()
		{
			if (!treeView.IsDisposed) {
				treeView.Nodes.Clear();
			}
		}
		
		public void PrintTestErrors(string assembly)
		{
			TreeNode assemblyNode = new TreeNode(Path.GetFileName(assembly));
			StringParserService stringParserService = (StringParserService)ServiceManager.Services.GetService(typeof(StringParserService));
			TreeNode failedNode = new TreeNode(stringParserService.Parse("${res:NUnitPad.NUnitPadContent.TestTreeView.LoadingErrorNode}"));
			failedNode.ImageIndex = failedNode.SelectedImageIndex = 5;
			assemblyNode.Nodes.Add(failedNode);
			
			treeView.Nodes.Add(assemblyNode);
		}
		
		public void PrintTests(string assembly, Test test)
		{
			TreeNode assemblyNode = new TreeNode(Path.GetFileName(assembly));
			assemblyNode.Tag = test;
			treeView.Nodes.Add(assemblyNode);
			if (test != null) {
				AddTests(assemblyNode, test);
			}
			assemblyNode.Expand();
		}
		
		public void AddTests(TreeNode node, ITest test)
		{
			foreach (ITest childTest in test.Tests) {
				TreeNode newNode = new TreeNode(childTest.Name);
				treeNodeHash[childTest.UniqueName] = newNode;
				newNode.ImageIndex = newNode.SelectedImageIndex = 0;
				newNode.Tag = childTest;
				if (childTest.IsSuite) {
					AddTests(newNode, childTest);
					node.Expand();
				}
				node.Nodes.Add(newNode);
			}
		}
		
		public void GotoDefinition()
		{
			if (treeView.SelectedNode != null) {
				ITest test = treeView.SelectedNode.Tag as ITest;
				if (test != null) {
					IParserService parserService = (IParserService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IParserService));
					Position position = parserService.GetPosition(test.FullName.Replace('+', '.'));
					
					if (position != null && position.Cu != null) {
						IFileService fileService = (IFileService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IFileService));
						fileService.JumpToFilePosition(position.Cu.FileName, Math.Max(0, position.Line - 1), Math.Max(0, position.Column - 1));
					}
				}
			}
		}
		
		public void RunTests()
		{
			ResetNodeIcons(treeView.Nodes);
			
			if (testRunnerCategory == null) {
				testRunnerCategory = new MessageViewCategory("NUnit");
				CompilerMessageView cmv = (CompilerMessageView)WorkbenchSingleton.Workbench.GetPad(typeof(CompilerMessageView));
				cmv.AddCategory(testRunnerCategory);
			} else {
				testRunnerCategory.ClearText();
			}
			
			TreeNode selectedNode = treeView.SelectedNode;
			
			if (selectedNode != null) {
				Test test = selectedNode.Tag as Test;
				if (test != null) {
					test.Run(this);
				} else {
					selectedNode.ImageIndex = selectedNode.SelectedImageIndex = 2;
				}
			} else {
				foreach (TreeNode node in treeView.Nodes) {
					Test test = node.Tag as Test;
					if (test != null) {
						test.Run(this);
					} else {
						node.ImageIndex = node.SelectedImageIndex = 2;
					}
				}
			}
			treeView.Focus();
		}
		
		void ResetNodeIcons(ICollection col) 
		{
			foreach (TreeNode node in col) {
				if (node.ImageIndex <= 3) {
					node.ImageIndex = node.SelectedImageIndex = 0;
				}
				ResetNodeIcons(node.Nodes);
			}
		}
		
		void SetResultIcon(TestResult testResult)
		{
			TreeNode node = (TreeNode)treeNodeHash[testResult.Test.UniqueName];
			if (node != null) {
				if (testResult.IsSuccess && testResult.Executed) {
					node.ImageIndex = node.SelectedImageIndex = 1;
				} else if (testResult.IsFailure) {
					node.ImageIndex = node.SelectedImageIndex = 3;
				} else {
					node.ImageIndex = node.SelectedImageIndex = 2;
				}
				if (node.Parent != null || node.Parent.Parent == null) {
					node.Parent.ImageIndex = node.Parent.SelectedImageIndex = node.ImageIndex;
				}
			}
		}
		
		
		void ActivateItem(object sender, EventArgs e)
		{
			GotoDefinition();
		}
		
		void TestTreeViewKeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == '\r') {
				GotoDefinition();
			} else if (e.KeyChar == ' ') {
				RunTests();
			}
		}
		
		
		
		
		#region NUnit.Core.EventListener interface implementation
		public void RunStarted(NUnit.Core.Test[] tests)
		{
			
		}
		public void RunFinished(NUnit.Core.TestResult[] tests)
		{
			
		}
		public void RunFinished(Exception exception)
		{
			
		}
		public void UnhandledException(Exception exception)
		{
			
		}
		
		public void SuiteStarted(NUnit.Core.TestSuite suite)
		{
//			testRunnerCategory.AppendText(suite.FullName + " started.\n");
		}
		
		public void SuiteFinished(NUnit.Core.TestSuiteResult result)
		{
			SetResultIcon(result);
//			testRunnerCategory.AppendText(result.Test.FullName + " finished.\n");
		}
		
		public void TestStarted(NUnit.Core.TestCase testCase)
		{
//			testRunnerCategory.AppendText(testCase.FullName + " started.\n");
		}
		
		public void TestFinished(NUnit.Core.TestCaseResult result)
		{
			if (!result.IsSuccess) {
				testRunnerCategory.AppendText(result.Test.FullName + " failed:\n");
				testRunnerCategory.AppendText(result.Description + "\n");
				testRunnerCategory.AppendText(result.Message + "\n");
				testRunnerCategory.AppendText(result.StackTrace + "\n");
			} else if (!result.Executed) {
				testRunnerCategory.AppendText(result.Test.FullName + " not executed:\n");
				testRunnerCategory.AppendText(result.Description + "\n");
				testRunnerCategory.AppendText(result.Message + "\n");
				testRunnerCategory.AppendText(result.StackTrace + "\n");
			}
			
			SetResultIcon(result);
		}
		#endregion
	}
}
