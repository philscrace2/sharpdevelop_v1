// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections;
using System.Resources;
using System.Xml;
using System.Runtime.Serialization.Formatters.Binary;

using ICSharpCode.Core.Services;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Services;
using ICSharpCode.SharpDevelop.Internal.Undo;
using System.Drawing.Printing;

namespace ResourceEditor
{
	/// <summary>
	/// This class allows viewing and editing of windows resource files
	/// both in XML as in normal format.
	/// </summary>
	public class ResourceList : ListView
	{
		ColumnHeader name     = new ColumnHeader();
		ColumnHeader type     = new ColumnHeader();
		ColumnHeader content  = new ColumnHeader();
		
		ResourceItemHashtable resources = new ResourceItemHashtable();
		ImageList images = new ImageList();
		
		ResourceClipboardHandler clipboardHandler = null;
		UndoStack undoStack = null;
		bool writeProtected = false;
		
		public event EventHandler Changed;
		
		public bool WriteProtected
		{
			get {
				return writeProtected;
			}
			set {
				writeProtected = value;
			}
		}
		
		public ResourceItemHashtable Resources
		{
			get {
				return resources;
			}
		}
		
		public UndoStack UndoStack
		{
			get {
				return undoStack;
			}
		}
		public IClipboardHandler ClipboardHandler
		{
			get {
				return clipboardHandler;
			}
		}
		
		public PrintDocument PrintDocument
		{
			get {
				return null; 
			}
		}
		
		public ResourceList(ResourceEditorControl editor)
		{
			clipboardHandler = new ResourceClipboardHandler(this);
			undoStack        = new UndoStack();
			
			ResourceService resourceService = (ResourceService)ServiceManager.Services.GetService(typeof(IResourceService));
			
			name.Text     = resourceService.GetString("ResourceEditor.ResourceEdit.NameColumn");
			name.Width    = 250;
			
			type.Text     = resourceService.GetString("ResourceEditor.ResourceEdit.TypeColumn");
			type.Width    = 170;
			
			content.Text  = resourceService.GetString("ResourceEditor.ResourceEdit.ContentColumn");
			content.Width = 300;
			
			Columns.AddRange(new ColumnHeader[] {name, type, content});
			
			FullRowSelect = true;
			AutoArrange   = true;
			Alignment     = ListViewAlignment.Left;
			View          = View.Details;
			GridLines     = true;
			LabelEdit     = true;
			Sorting       = SortOrder.Ascending;
			Dock          = DockStyle.Fill;
			HideSelection = false;
			
			BorderStyle   = System.Windows.Forms.BorderStyle.None;
			
			images.Images.Add(resourceService.GetIcon("Icons.16x16.ResourceEditor.string"));
			images.Images.Add(resourceService.GetIcon("Icons.16x16.ResourceEditor.bmp"));
			images.Images.Add(resourceService.GetIcon("Icons.16x16.ResourceEditor.icon"));
			images.Images.Add(resourceService.GetIcon("Icons.16x16.ResourceEditor.cursor"));
			images.Images.Add(resourceService.GetIcon("Icons.16x16.ResourceEditor.bin"));
			images.Images.Add(resourceService.GetIcon("Icons.16x16.ResourceEditor.obj"));
			SmallImageList = images;
			
			AfterLabelEdit += new LabelEditEventHandler(afterLabelEdit);
			
			MenuService menuService = (MenuService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(MenuService));
			ContextMenu = menuService.CreateContextMenu(editor, "/SharpDevelop/ResourceEdtior/ResourceList/ContextMenu");
		}
		
		public void LoadFile(string filename)
		{
			Stream s = File.OpenRead(filename);
			switch (Path.GetExtension(filename).ToLower()) {
				case ".resx":
					ResXResourceReader rx = new ResXResourceReader(s);
					IDictionaryEnumerator n = rx.GetEnumerator();
					while (n.MoveNext()) 
						if (!resources.ContainsKey(n.Key.ToString()))
							resources.Add(n.Key.ToString(), new ResourceItem(n.Key.ToString(), n.Value));
					
					rx.Close();
					break;
				case ".resources":
					//// new file will fail here - so we have to ignore exception(s)
					ResourceReader rr=null;
					try {
						rr = new ResourceReader(s);
						foreach (DictionaryEntry entry in rr) {
							if (!resources.ContainsKey(entry.Key.ToString()))
								resources.Add(entry.Key.ToString(), new ResourceItem(entry.Key.ToString(), entry.Value));
						}
					}
					catch {}
					finally {
						if (rr != null) {
							rr.Close();
						}
					}
					break;
			}
			s.Close();
			InitializeListView();
		}
		
		public void SaveFile(string filename)
		{
			Debug.Assert(!writeProtected, "ICSharpCode.SharpDevelop.Gui.Edit.Resource.ResourceEdit.SaveFile(string filename) : trying to save a write protected file");
			switch (Path.GetExtension(filename).ToUpper()) {
				
				// write XML resource
				case ".RESX":
					ResXResourceWriter rxw    = new ResXResourceWriter(filename);
					foreach (DictionaryEntry entry in resources) {
						if (entry.Value != null) {
							ResourceItem item = (ResourceItem)entry.Value;
							rxw.AddResource(item.Name, item.ResourceValue);
						}
					}
					rxw.Generate();
					rxw.Close();
				break;
				
				// write default resource
				default:
					ResourceWriter rw = new ResourceWriter(filename);
					foreach (DictionaryEntry entry in resources) {
						ResourceItem item = (ResourceItem)entry.Value;
						rw.AddResource(item.Name, item.ResourceValue);
					}
					rw.Generate();
					rw.Close();
				break;
			}
		}

		public void SetResourceValue(string resourceName, object resourceValue)
		{
			ResourceItem item = ((ResourceItem)Resources[resourceName]);
			item.ResourceValue = resourceValue;
			SelectedItems[0].SubItems[2].Text = item.ToString();
			OnChanged();
		}
		
		public void OnChanged()
		{
			if (Changed != null) {
				Changed(this, null);
			}
		}
		
		void afterLabelEdit(object sender, LabelEditEventArgs e)
		{
			if (writeProtected) {
				e.CancelEdit = true;
				return;
			}
			string oldName = this.Items[e.Item].Text;
			string newName = e.Label;
			
			if(newName == null) {
				// no change
				return;
			}
			
			ResourceItem item = (ResourceItem)resources[oldName];
			
			if(resources.ContainsKey(newName)) {
				IMessageService messageService =(IMessageService)ServiceManager.Services.GetService(typeof(IMessageService));
				messageService.ShowWarning("${res:ResourceEditor.ResourceList.KeyAlreadyDefinedWarning}");
				e.CancelEdit = true;
				return;
			}
			
			resources.Remove(oldName);
			item.Name = newName;
			resources.Add(newName, item);
			OnChanged();
		}
		
		public void InitializeListView()
		{
			BeginUpdate();
			Items.Clear();
			
			foreach (DictionaryEntry entry in resources) {
				ResourceItem item = (ResourceItem)entry.Value;
				
				string tmp  = item.ToString();
				string type = item.ResourceValue.GetType().FullName;
				
				ListViewItem lv = new ListViewItem(new String[] {item.Name, type, tmp}, item.ImageIndex);
				Items.Add(lv);
			}
			EndUpdate();
		}
	}
}
