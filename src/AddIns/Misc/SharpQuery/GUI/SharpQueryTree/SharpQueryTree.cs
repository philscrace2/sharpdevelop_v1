// created on 04/11/2003 at 16:05

namespace SharpQuery.Gui.TreeView
{
	
using System;
using System.Windows.Forms;
using System.Xml;
using System.Collections;

using ICSharpCode.Core.Services;
using ICSharpCode.SharpDevelop.Services;
using SharpQuery.Gui;
using SharpQuery.Collections;

using ICSharpCode.Core.AddIns.Conditions;
using ICSharpCode.Core.AddIns.Codons;
using ICSharpCode.Core.AddIns;
	
using SharpQuery.Codons;
	
	///<summary>
	/// This class shows all databases connections in a treeview. 
	///</summary>	
	public class SharpQueryTree : System.Windows.Forms.TreeView
	{									
		private System.Windows.Forms.ImageList pNodeImages;
		private MenuService menuService = null;

		public static SharpQueryStringDictionary SchemaClassDict;		

		///<summary>
		/// Create a SharpQueryTree objec 
		///</summary>			
		public SharpQueryTree() : base()
		{		
			IconService iconService = (IconService)ServiceManager.Services.GetService(typeof(IconService));
			
			this.menuService = (MenuService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(MenuService));
			
			this.pNodeImages = new ImageList();
			this.pNodeImages.ColorDepth = System.Windows.Forms.ColorDepth.Depth24Bit;
			this.pNodeImages.ImageSize = new System.Drawing.Size(16, 16);
			this.pNodeImages.TransparentColor = System.Drawing.Color.DarkCyan;
			
			this.pNodeImages.Images.Add(iconService.GetBitmap("Icons.16x16.SharpQuery.DataBaseRoot"));
			this.pNodeImages.Images.Add(iconService.GetBitmap("Icons.16x16.SharpQuery.DatabaseConnectionClose"));
			this.pNodeImages.Images.Add(iconService.GetBitmap("Icons.16x16.SharpQuery.DatabaseConnection"));
			this.pNodeImages.Images.Add(iconService.GetBitmap("Icons.16x16.SharpQuery.TablesRoot"));	
			this.pNodeImages.Images.Add(iconService.GetBitmap("Icons.16x16.SharpQuery.ViewsRoot"));
			this.pNodeImages.Images.Add(iconService.GetBitmap("Icons.16x16.SharpQuery.ProceduresRoot"));
			this.pNodeImages.Images.Add(iconService.GetBitmap("Icons.16x16.SharpQuery.Table"));
			this.pNodeImages.Images.Add(iconService.GetBitmap("Icons.16x16.SharpQuery.View"));
			this.pNodeImages.Images.Add(iconService.GetBitmap("Icons.16x16.SharpQuery.Procedure"));
			this.pNodeImages.Images.Add(iconService.GetBitmap("Icons.16x16.SharpQuery.Column"));
			this.pNodeImages.Images.Add(iconService.GetBitmap("Icons.16x16.SharpQuery.NodeError"));
			
			this.ImageList = this.pNodeImages;

			SchemaClassDict = new SharpQueryStringDictionary();
						
			LoadSharpQueryConnectionCodon();
			
			//Add the Root Node.			
			this.Nodes.Add( new SharpQueryNodeDatabaseRoot() );			
		}
		
		protected void LoadSharpQueryConnectionCodon()
		{
			IAddInTreeNode AddinNode;		
			
			AddinNode = (IAddInTreeNode)AddInTreeSingleton.AddInTree.GetTreeNode("/SharpQuery/Connection");
			foreach ( DictionaryEntry entryChild in AddinNode.ChildNodes)
			{
				IAddInTreeNode ChildNode = entryChild.Value as IAddInTreeNode;
				if ( ChildNode != null )
				{
					SharpQueryConnectionCodon codon = ChildNode.Codon as SharpQueryConnectionCodon;
					if ( codon != null )
					{
					  if ( SchemaClassDict.Contains( codon.Schema ) )
					  {
					  	SchemaClassDict[codon.Schema] = codon.Node;
					  }
					  else
					  {
					  	SchemaClassDict.Add(codon.Schema,codon.Node);
					  }					  	
					}					
				}
			}			
		}
		
		///<summary>
		/// Select the node under the mouse cursor
		///</summary>		
		protected override void OnMouseDown(MouseEventArgs e)
		{						
			if (e.Button == MouseButtons.Right || e.Button == MouseButtons.Left)
			{
				this.SelectedNode = this.GetNodeAt( e.X, e.Y );
			}
			
			
			base.OnMouseDown(e);
		}
		
		///<summary>
		/// Display the context menu associated with a node type 
		///</summary>		
		protected override void OnMouseUp(MouseEventArgs e)
		{						
			if (e.Button == MouseButtons.Right && this.SelectedNode != null && SelectedNode is ISharpQueryNode) {
				ISharpQueryNode selectedBrowserNode = SelectedNode as ISharpQueryNode;		
				if ( selectedBrowserNode.AddinContextMenu != "" )
				{
					menuService.ShowContextMenu(this, selectedBrowserNode.AddinContextMenu, this, e.X, e.Y);
				}
			}

			base.OnMouseUp(e);
		}
		
		protected override void OnItemDrag(ItemDragEventArgs e)
		{
			base.OnItemDrag(e);
			AbstractSharpQueryNode node = e.Item as AbstractSharpQueryNode;
			if (node != null) 
			{
				DataObject dataObject = null;
				
				if ( node.SchemaClass != null )
				{
					dataObject = node.SchemaClass.DragObject;
					
					if (dataObject != null )
					{
						dataObject.SetData(node.GetType() , node);
						DoDragDrop(dataObject, DragDropEffects.All);
					}
				}												
			}
		}
		
		protected override void OnBeforeExpand( TreeViewCancelEventArgs e )
		{
			SharpQueryNodeConnection node = e.Node as SharpQueryNodeConnection;
			
			if ( node != null )
			{
				if ( node.IsConnected == false )
				{
					node.Connect();
				}
			}
			
			base.OnBeforeExpand( e );
		}

		
		protected override void OnAfterExpand(TreeViewEventArgs e)
		{
			ISharpQueryNode node = e.Node as ISharpQueryNode;
			
			if ( node != null )
			{
				node.Refresh();
			}
			
			base.OnAfterExpand( e );
		}	
				
		protected override void OnAfterCollapse( TreeViewEventArgs e )
		{
			ISharpQueryNode node = e.Node as ISharpQueryNode;
			
			if ( node != null )
			{
				node.Clear();
			}
			
			base.OnAfterCollapse( e );
		}
	}
}
