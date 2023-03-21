using System;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

using ICSharpCode.SharpDevelop.Gui;
using SharpQuery.Gui;
using SharpQuery.Gui.TreeView;

using ICSharpCode.Core.Services;
using ICSharpCode.SharpDevelop.Services;
using ICSharpCode.Core.AddIns;

//TODO : dans les SharpQueryList faire correspondre les restrictions vec les objets ajoutés
//TODO : dans les SharpQueryList faire correspondre les dataconnection avec les objets ajoutés
//TODO : ajout statistiques.

namespace SharpQuery.Pads
{
	/// <summary>
	/// This Pad Show a tree where you can add/remove databases connections.
	/// You can administrate databases from this tree.
	/// </summary>
	public class SharpQueryView : AbstractPadContent
	{		
		private static SharpQueryTree sharpQueryTree = null;
#region AbstractPadContent requirements
		/// <summary>
		/// The <see cref="System.Windows.Forms.Control"/> representing the pad
		/// </summary>
		public override Control Control {
			get {
				return sharpQueryTree;
			}
		}
				
		/// <summary>
		/// Creates a new SharpQueryView object
		/// </summary>
		public SharpQueryView() : base("${res:SharpQuery.Label.SharpQuery}", "Icons.16x16.SharpQuery.DatabaseConnection")
		{												
			CreateDefaultSharpQuery();
			sharpQueryTree.Dock = DockStyle.Fill;
		}
		
		void CreateDefaultSharpQuery()
		{
			sharpQueryTree = new SharpQueryTree();
		}		
		
		public void SaveSharpQueryView()
		{
		}		
		
		/// <summary>
		/// Refreshes the pad
		/// </summary>
		public override void RedrawContent()
		{
			OnTitleChanged(null);
			OnIconChanged(null);	
			sharpQueryTree.Refresh();
		}
		
		/// <summary>
		/// Cleans up all used resources
		/// </summary>
		public override void Dispose()
		{
			this.SaveSharpQueryView();
			sharpQueryTree.Dispose();
		}
#endregion
	}
	
}
