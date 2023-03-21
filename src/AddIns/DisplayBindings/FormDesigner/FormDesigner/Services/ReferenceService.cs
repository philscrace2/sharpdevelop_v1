//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Collections;
using System.Collections.Specialized;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using ICSharpCode.SharpDevelop.FormDesigner.Util;
using ICSharpCode.Core.Services;
using ICSharpCode.SharpDevelop.Gui;

namespace ICSharpCode.SharpDevelop.FormDesigner.Services
{
	public class ReferenceService : IReferenceService
	{
		public class MyReference
		{
			private IComponent parentComponent;
			private string fullName;
			private object reference;
			private string name;

			public MyReference(IComponent parentComponent, object reference, string name)
			{
				this.parentComponent = parentComponent;
				this.reference = reference;
				this.name = name;
				this.fullName = null;
			}

			public void Reinit()
			{
				this.fullName = null;
			}

			public virtual IComponent ParentComponent
			{
				get {
					return this.parentComponent;
				}
			}

			public object Reference
			{
				get {
					return this.reference;
				}
			}

			public string Name
			{
				get {
					if (this.fullName == null) {
						this.fullName = this.parentComponent.Site.Name + this.name;
					}
					return this.fullName;
				}
			}
		
		}

		private IDesignerHost host;
		private ArrayList addedComponents;
		private bool initialized;
		private ComponentEventHandler onComponentAdd;
		private ComponentEventHandler onComponentRemove;
		private ComponentRenameEventHandler onComponentRename;
		private ArrayList referenceList;
		private ArrayList removedComponents;

		public ReferenceService(IDesignerHost host)
		{
			this.host = host;
			this.initialized = false;
			this.addedComponents = new ArrayList();
			this.removedComponents = new ArrayList();
			this.onComponentAdd = new ComponentEventHandler(this.OnComponentAdd);
			this.onComponentRemove = new ComponentEventHandler(this.OnComponentRemove);
			this.onComponentRename = new ComponentRenameEventHandler(this.OnComponentRename);
			this.referenceList = new ArrayList();
			IComponentChangeService ccservice = (IComponentChangeService) host.GetService(typeof(IComponentChangeService));
			if (ccservice != null) {
				ccservice.ComponentAdded += this.onComponentAdd;
				ccservice.ComponentRemoved += this.onComponentRemove;
				ccservice.ComponentRename += this.onComponentRename;
			}
		}

		public virtual void Dispose()
		{
			this.referenceList = null;
			if (this.host != null) {
				IComponentChangeService ccservice = (IComponentChangeService) this.host.GetService(typeof(IComponentChangeService));
				if (ccservice != null) {
					ccservice.ComponentAdded -= this.onComponentAdd;
					ccservice.ComponentRemoved -= this.onComponentRemove;
					ccservice.ComponentRename -= this.onComponentRename;
				}
			}
			this.host = null;
		}

		public void Clear()
		{
			if (this.referenceList != null) {
				this.referenceList.Clear();
			}
		}

		private void CheckReferences()
		{
			if (!this.initialized) {
				foreach (IComponent hostComponent in this.host.Container.Components) {
					this.CreateReferences(hostComponent);
				}
				this.initialized = true;
			}
			else {
				if (this.addedComponents.Count > 0) {
					foreach (IComponent addComponent in this.addedComponents) {
						this.RemoveReferences(addComponent);
						this.CreateReferences(addComponent);
					}
					this.addedComponents.Clear();
				}
				if (this.removedComponents.Count > 0) {
					foreach (IComponent remComponent in this.removedComponents) {
						this.RemoveReferences(remComponent);
					}
					this.removedComponents.Clear();
				}
			}
		}

		private void CreateReferences(IComponent component)
		{
			this.CreateReferences(component, component, "");
		}

		private void CreateReferences(IComponent parentComponent, object reference, string name)
		{
			if (reference != null)
			{
				this.referenceList.Add(new ReferenceService.MyReference(parentComponent, reference, name));
				Attribute[] desSerVisAttr = new Attribute[1] { DesignerSerializationVisibilityAttribute.Content } ;
				PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(reference, desSerVisAttr);
				for (int index = 0; index < properties.Count; index++) {
					if (!properties[index].IsReadOnly) {
						continue;
					}
					try {
						string propName = properties[index].Name;
						CreateReferences(parentComponent, properties[index].GetValue(reference), name + "." + propName);
					}
					catch (Exception) {
					}
				}
			}
		}

		#region System.ComponentModel.Design.IReferenceService interface implementation
		public object[] GetReferences(Type baseType)
		{
			CheckReferences();
			ArrayList references = new ArrayList();
			foreach(ReferenceService.MyReference myRef in this.referenceList) {
				if (baseType.IsAssignableFrom(myRef.Reference.GetType())) {
					references.Add(myRef.Reference);
				}
			}
			object[] objArray = new object[references.Count];
			references.CopyTo(objArray, 0);
			return objArray;
		}

		public object[] GetReferences()
		{
			CheckReferences();
			object[] references = new object[this.referenceList.Count];
			for (int index = 0; index < this.referenceList.Count; index++) {
				references[index] = ((ReferenceService.MyReference) this.referenceList[index]).Reference;
			}
			return references;
		}

		public string GetName(object reference)
		{
			CheckReferences();
			foreach(ReferenceService.MyReference myRef in this.referenceList) {
				if (myRef.Reference == reference) {
					return myRef.Name;
				}
			}
			return null;
		}

		public object GetReference(string name)
		{
			CheckReferences();
			foreach(ReferenceService.MyReference myRef in this.referenceList) {
				if (myRef.Name == name) {
					return myRef.Reference;
				}
			}
			return null;
		}

		public IComponent GetComponent(object reference)
		{
			CheckReferences();
			foreach(ReferenceService.MyReference myRef in this.referenceList) {
				if (myRef.Reference == reference) {
					return myRef.ParentComponent;
				}
			}
			return null;
		}
		#endregion

		private void OnComponentAdd(object sender, ComponentEventArgs ce)
		{
			if (this.initialized) {
				this.addedComponents.Add(ce.Component);
				this.removedComponents.Remove(ce.Component);
			}
		}

		private void OnComponentRemove(object sender, ComponentEventArgs ce)
		{
			if (this.initialized) {
				this.removedComponents.Add(ce.Component);
				this.addedComponents.Remove(ce.Component);
			}
		}

		private void OnComponentRename(object sender, ComponentRenameEventArgs ce)
		{
			foreach (ReferenceService.MyReference myRef in this.referenceList) {
				if (myRef.ParentComponent != ce.Component) {
					continue;
				}
				myRef.Reinit();
				return;
			}
		}

		private void RemoveReferences(IComponent component)
		{
			for (int index = this.referenceList.Count - 1; index >= 0; index--) {
				if (((ReferenceService.MyReference) this.referenceList[index]).ParentComponent == component) {
					this.referenceList.RemoveAt(index);
				}
			}
		}

	}
}
