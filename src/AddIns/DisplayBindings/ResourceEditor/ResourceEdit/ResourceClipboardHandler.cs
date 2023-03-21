// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Windows.Forms;

using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.Core.Services;

namespace ResourceEditor
{
	class ResourceClipboardHandler : IClipboardHandler
	{
		ResourceList resourceList;
		
		public bool EnableCut
		{
			get {
				return resourceList.SelectedItems.Count > 0;
			}
		}
		
		public bool EnableCopy
		{
			get {
				return resourceList.SelectedItems.Count > 0;
			}
		}
		
		public bool EnablePaste
		{
			get {
				return true;
			}
		}
		
		public bool EnableDelete
		{
			get {
				return resourceList.SelectedItems.Count > 0;
			}
		}
		
		public bool EnableSelectAll
		{
			get {
				return true;
			}
		}
		
		public ResourceClipboardHandler(ResourceList resourceList)
		{
			this.resourceList = resourceList;
		}
		
		public void Cut(object sender, EventArgs e)
		{
			if (resourceList.WriteProtected || resourceList.SelectedItems.Count < 1) 
				return;
			
			Hashtable tmphash = new Hashtable();
			foreach (ListViewItem item in resourceList.SelectedItems) {
				tmphash.Add(item.Text, resourceList.Resources[item.Text].ResourceValue);
				resourceList.Resources.Remove(item.Text);
				resourceList.Items.Remove(item);
			}
			resourceList.OnChanged();
			Clipboard.SetDataObject(tmphash);
		}
		
		public void Copy(object sender, EventArgs e)
		{
			if (resourceList.SelectedItems.Count < 1) {
				return;
			}
			
			Hashtable tmphash = new Hashtable();
			foreach (ListViewItem item in resourceList.SelectedItems) {
				object resourceValue = GetClonedResource(resourceList.Resources[item.Text].ResourceValue);
				tmphash.Add(item.Text, resourceValue); // copy a clone to clipboard
			}
			Clipboard.SetDataObject(tmphash);
		}
		
		public void Paste(object sender, EventArgs e)
		{
			if (resourceList.WriteProtected) {
				return;
			}
			
			IDataObject dob = Clipboard.GetDataObject();
			
			if (dob.GetDataPresent(typeof(Hashtable).FullName)) {
				Hashtable tmphash = (Hashtable)dob.GetData(typeof(Hashtable));
				foreach (DictionaryEntry entry in tmphash) {
					
					object resourceValue = GetClonedResource(entry.Value);					
					ResourceItem item;
					
					if (!resourceList.Resources.ContainsKey((string)entry.Key)) {
						item  = new ResourceItem(entry.Key.ToString(), resourceValue);						
					} else {
						int count = 1;
						string newNameBase = entry.Key.ToString() + " ";
						string newName = newNameBase + count.ToString();
						
						while(resourceList.Resources.ContainsKey(newName)) {
							count++;
							newName = newNameBase + count.ToString();
						}
						item = new ResourceItem(newName, resourceValue);
					}
					resourceList.Resources.Add(item.Name, item);
					resourceList.OnChanged();
				}
				resourceList.InitializeListView();
			}
		}

		/// <summary>
		/// Clones a resource if the <paramref name="resource"/>
		/// is cloneable.
		/// </summary>
		/// <param name="resource">A resource to clone.</param>
		/// <returns>A cloned resource if the object implements
		/// the ICloneable interface, otherwise the 
		/// <paramref name="resource"/> object.</returns>
		object GetClonedResource(object resource)
		{
			object clonedResource = null;
			
			ICloneable cloneableResource = resource as ICloneable;
			if (cloneableResource != null) {
				clonedResource = cloneableResource.Clone();
			} else {
				clonedResource = resource;
			}
				
			return clonedResource;
		}
		
		public void Delete(object sender, EventArgs e)
		{
			if (resourceList.WriteProtected) {
				return;
			}
			
			if (resourceList.SelectedItems.Count==0) return; // nothing to do
			DialogResult rc;
			
			try {
				ResourceService resourceService = (ResourceService)ServiceManager.Services.GetService(typeof(IResourceService));			
				rc=MessageBox.Show(resourceService.GetString("ResourceEditor.DeleteEntry.Confirm"),resourceService.GetString("ResourceEditor.DeleteEntry.Title"),MessageBoxButtons.OKCancel);
			}
			catch {
				// when something happens - like resource is missing - try to use default message
				rc = MessageBox.Show("Do you really want to delete?","Delete-Warning!",MessageBoxButtons.OKCancel);
			}
			
			if (rc != DialogResult.OK) {
				return;
			}
			
			foreach (ListViewItem item in resourceList.SelectedItems) {
				//// not clear why this check is present here - seems to be extra
				////if (item.Text != null) {
				resourceList.Resources.Remove(item.Text);
				resourceList.Items.Remove(item);
				// and set dirty flag	
				resourceList.OnChanged();
			}
		}
		
		public void SelectAll(object sender, EventArgs e)
		{
			foreach (ListViewItem i in resourceList.Items) {
				i.Selected=true;
			}
		}
		
		protected virtual void OnEnableCutChanged(EventArgs e)
		{
			if (EnableCutChanged != null) {
				EnableCutChanged(this, e);
			}
		}
		protected virtual void OnEnableCopyChanged(EventArgs e)
		{
			if (EnableCopyChanged != null) {
				EnableCopyChanged(this, e);
			}
		}
		protected virtual void OnEnablePasteChanged(EventArgs e)
		{
			if (EnablePasteChanged != null) {
				EnablePasteChanged(this, e);
			}
		}
		protected virtual void OnEnableDeleteChanged(EventArgs e)
		{
			if (EnableDeleteChanged != null) {
				EnableDeleteChanged(this, e);
			}
		}
		protected virtual void OnEnableSelectAllChanged(EventArgs e)
		{
			if (EnableSelectAllChanged != null) {
				EnableSelectAllChanged(this, e);
			}
		}
		
		public event EventHandler EnableCutChanged;
		public event EventHandler EnableCopyChanged;
		public event EventHandler EnablePasteChanged;
		public event EventHandler EnableDeleteChanged;
		public event EventHandler EnableSelectAllChanged;
		
	}
}
