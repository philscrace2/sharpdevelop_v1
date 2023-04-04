/*
 * Created by SharpDevelop.
 * User: Andrea
 * Date: 11/1/2004
 * Time: 8:46 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

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
	public class TreeTreeView : Panel
	{
		public TreeView treeView = new TreeView();
		
		public TreeTreeView()
		{
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
			TreeNode rootNode = new TreeNode("AddInTree");
			rootNode.ImageIndex = rootNode.SelectedImageIndex = 0;
			rootNode.Expand();
			
			treeView.Nodes.Add(rootNode);
			
			IAddInTree at = AddInTreeSingleton.AddInTree;
			AddInCollection ac = at.AddIns;
			for (int i = 0; i < ac.Count; i++) {
				GetExtensions(ac[i], rootNode);
//				rootNode.Nodes.Add(newNode);
			}
		}
		
		void GetExtensions(AddIn ai, TreeNode treeNode)
		{
			foreach (AddIn.Extension ext in ai.Extensions) {
				string[] name = ext.Path.Split('/');
				TreeNode currentNode = treeNode;
				if (name.Length < 1) {
					continue;
				}
				for (int i = 1; i < name.Length; ++i) {
					bool found = false;
					foreach (TreeNode n in currentNode.Nodes) {
						if (n.Text == name[i]) {
							currentNode = n;
							found = true;
							break;
						}
					}
					if (!found) {
						TreeNode newNode = new TreeNode(name[i]);
						newNode.ImageIndex = 3;
						newNode.SelectedImageIndex = 4;
						if (i == name.Length - 1) {
							newNode.Tag = ext;
						}
						currentNode.Nodes.Add(newNode);
						currentNode = newNode;
					}
				}
			}
		}
	}
}
