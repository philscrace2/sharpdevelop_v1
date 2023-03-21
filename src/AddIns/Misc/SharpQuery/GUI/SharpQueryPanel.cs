// created on 04/11/2003 at 14:27

using System;
using System.Windows.Forms;

using ICSharpCode.Core.Services;
using ICSharpCode.SharpDevelop.Services;


namespace SharpQuery.Gui
{	
	public class SharpQueryPanel : System.Windows.Forms.Panel
	{		
		private SharpQuery.Gui.TreeView.SharpQueryTree sharpQueryTreeView;
		private System.Windows.Forms.ImageList sharpQueryImageList;
		private System.Windows.Forms.ToolBar sharpQueryToolBar;	
		private System.Windows.Forms.ToolBarButton btnRefresh;
		private System.Windows.Forms.ToolBarButton btnAddConnection;
		private System.Windows.Forms.ToolBarButton btnSep;
		
		
		public SharpQueryPanel() : base()
		{		
			IconService iconService = (IconService)ServiceManager.Services.GetService(typeof(IconService));
			StringParserService stringParserService = (StringParserService)ServiceManager.Services.GetService(typeof(StringParserService));
			
			this.sharpQueryToolBar = new System.Windows.Forms.ToolBar();
			this.sharpQueryImageList = new System.Windows.Forms.ImageList();
			this.sharpQueryTreeView = new SharpQuery.Gui.TreeView.SharpQueryTree();
			this.btnRefresh = new System.Windows.Forms.ToolBarButton();
			this.btnAddConnection = new System.Windows.Forms.ToolBarButton();
			this.btnSep = new System.Windows.Forms.ToolBarButton();
			this.SuspendLayout();
					
			// 
			// sharpQueryImageList
			// 
			this.sharpQueryImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth24Bit;
			this.sharpQueryImageList.ImageSize = new System.Drawing.Size(16, 16);
			this.sharpQueryImageList.TransparentColor = System.Drawing.Color.DarkCyan;
			
			this.sharpQueryImageList.Images.Add(iconService.GetBitmap("Icons.16x16.SharpQuery.AddConnection"));
			this.sharpQueryImageList.Images.Add(iconService.GetBitmap("Icons.16x16.SharpQuery.Refresh"));								
						
			// 
			// toolBar
			// 
			this.sharpQueryToolBar.Appearance = System.Windows.Forms.ToolBarAppearance.Flat;
			this.sharpQueryToolBar.Buttons.AddRange(new System.Windows.Forms.ToolBarButton[] {
								this.btnRefresh, this.btnSep, this.btnAddConnection});
			this.sharpQueryToolBar.DropDownArrows = true;
			this.sharpQueryToolBar.Location = new System.Drawing.Point(0, 0);
			this.sharpQueryToolBar.Name = "toolBar";
			this.sharpQueryToolBar.ShowToolTips = true;
			this.sharpQueryToolBar.Size = new System.Drawing.Size(292, 42);
			this.sharpQueryToolBar.TabIndex = 0;						
			this.sharpQueryToolBar.ImageList = this.sharpQueryImageList;
			
			this.btnRefresh.ImageIndex = 1;
			this.btnRefresh.ToolTipText = stringParserService.Parse("${res:SharpQuery.ToolTip.Refresh}");

			this.btnSep.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;

			this.btnAddConnection.ImageIndex = 0;
			this.btnAddConnection.ToolTipText = stringParserService.Parse("${res:SharpQuery.ToolTip.AddConnection}");			
			
			// 
			// treeView
			// 
			this.sharpQueryTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.sharpQueryTreeView.ImageIndex = -1;
			this.sharpQueryTreeView.Location = new System.Drawing.Point(0, 42);
			this.sharpQueryTreeView.Name = "treeView";
			this.sharpQueryTreeView.SelectedImageIndex = -1;
			this.sharpQueryTreeView.Size = new System.Drawing.Size(292, 224);
			this.sharpQueryTreeView.TabIndex = 1;
			// 
			// CreatedForm
			// 
			this.ClientSize = new System.Drawing.Size(292, 266);
			this.Controls.Add(this.sharpQueryTreeView);
			this.Controls.Add(this.sharpQueryToolBar);
			this.Name = "SharpQueryPanel";
			this.ResumeLayout(false);									
		}		
	}
}
