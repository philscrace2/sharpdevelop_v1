/*
 * Created by SharpDevelop.
 * User: Omnibrain
 * Date: 01.11.2004
 * Time: 12:03
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using ICSharpCode.Core.Services;
using ICSharpCode.SharpDevelop.Services;
using ICSharpCode.Core.AddIns;
using System.Windows.Forms;

namespace AddInScout
{
	/// <summary>
	/// Description of AddinTreeView.
	/// </summary>
	public class AddinTreeView : Panel
	{
		public TreeView treeView = new TreeView();
		
		public AddinTreeView()
		{
//			treeView.BorderStyle = BorderStyle.;
//			treeView.AfterSelect += new TreeViewEventHandler(this.tvSelectHandler);
			
			PopulateTreeView();
			
			IconService iconService = (IconService)ServiceManager.Services.GetService(typeof(IconService));
			treeView.ImageList = new ImageList();
			treeView.ImageList.ColorDepth = ColorDepth.Depth32Bit;
			treeView.ImageList.Images.Add(iconService.GetBitmap("Icons.16x16.Class"));
			treeView.ImageList.Images.Add(iconService.GetBitmap("Icons.16x16.Assembly"));
			treeView.ImageList.Images.Add(iconService.GetBitmap("Icons.16x16.OpenAssembly"));
			treeView.ImageList.Images.Add(iconService.GetBitmap("Icons.16x16.ClosedFolderBitmap"));
			treeView.ImageList.Images.Add(iconService.GetBitmap("Icons.16x16.OpenFolderBitmap"));
			
			treeView.Dock = DockStyle.Fill;
			Controls.Add(treeView);
		}
		
		void PopulateTreeView()
		{
			TreeNode rootNode = new TreeNode("Addins");
			rootNode.ImageIndex = rootNode.SelectedImageIndex = 0;
			rootNode.Expand();
			
			treeView.Nodes.Add(rootNode);
			
			IAddInTree at = AddInTreeSingleton.AddInTree;
			AddInCollection ac = at.AddIns;
			for (int i = 0; i < ac.Count; i++) {
				TreeNode newNode = new TreeNode(ac[i].Name);
				newNode.ImageIndex = 1;
				newNode.SelectedImageIndex = 2;
				newNode.Tag = ac[i];
				GetExtensions(ac[i], newNode);
				rootNode.Nodes.Add(newNode);
			}
		}
		
		void GetExtensions(AddIn ai, TreeNode treeNode)
		{
			foreach (AddIn.Extension ext in ai.Extensions) {
				TreeNode newNode = new TreeNode(ext.Path);
				newNode.ImageIndex = 3;
				newNode.SelectedImageIndex = 4;
				newNode.Tag = ext;
				treeNode.Nodes.Add(newNode);
			}
		}
		
	}
}
