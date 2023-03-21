// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Windows.Forms;

using ICSharpCode.SharpDevelop.Gui;

using ResourceEditor;
using ICSharpCode.Core.AddIns.Codons;

namespace ResourceEditor
{
	public class ResourceEditorDisplayBinding : IDisplayBinding
	{
		// IDisplayBinding interface
		public bool CanCreateContentForFile(string fileName)
		{
			return Path.GetExtension(fileName).ToUpper() == ".RESOURCES" || 
			       Path.GetExtension(fileName).ToUpper() == ".RESX";
		}
		
		public bool CanCreateContentForLanguage(string language)
		{
			return language == "ResourceFiles";
		}
		
		public IViewContent CreateContentForFile(string fileName)
		{
			ResourceEditWrapper di2 = new ResourceEditWrapper();
			di2.Load(fileName);
			return di2;
		}
		
		public IViewContent CreateContentForLanguage(string language, string content)
		{
			return new ResourceEditWrapper();
		}
	}
	
	/// <summary>
	/// This class describes the main functionality of a language codon
	/// </summary>
	public class ResourceEditWrapper : AbstractViewContent, IEditable
	{
		ResourceEditorControl resourceEditor = new ResourceEditorControl();
		
		public override Control Control {
			get {
				return resourceEditor;
			}
		}
		
		public override bool IsReadOnly {
			get {
				return false;
			}
		}
		public bool EnableUndo {
			get {
				return false;
			}
		}
		public bool EnableRedo {
			get {
				return false;
			}
		}

		void SetDirty(object sender, EventArgs e)
		{
			IsDirty = true;
		}
		
		public ResourceEditWrapper()
		{
			resourceEditor.ResourceList.Changed += new EventHandler(SetDirty);
		}
		
		public override void RedrawContent()
		{
		}
		
		public override void Dispose()
		{
			resourceEditor.Dispose();
		}
		
		public override void Save()
		{
			Save(FileName);
		}
	
		public override void Load(string filename)
		{
			resourceEditor.ResourceList.LoadFile(filename);
			TitleName = Path.GetFileName(filename);
			FileName = filename;
			IsDirty = false;
		}
		
		public override void Save(string filename)
		{
			OnSaving(EventArgs.Empty);
			resourceEditor.ResourceList.SaveFile(filename);
			TitleName = Path.GetFileName(filename);
			FileName = filename;
			IsDirty = false;
			OnSaved(new SaveEventArgs(true));
		}
		
		public void Redo()
		{
			// TODO
		}
		
		public void Undo()
		{
			// TODO
		}
		
		public IClipboardHandler ClipboardHandler
		{
			get {
				return resourceEditor.ResourceList.ClipboardHandler;
			}
		}
		
		public string Text
		{
			get {
				return null;
			}
			set {
				
			}
		}
		
	}
}
