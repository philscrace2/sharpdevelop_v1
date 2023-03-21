// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Reflection;
using System.Diagnostics;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using ICSharpCode.SharpDevelop.FormDesigner.Hosts;
using System.Drawing.Design;
using ICSharpCode.Core.Services;
using ICSharpCode.SharpDevelop.Services;
using ICSharpCode.SharpDevelop.FormDesigner.Services;

namespace ICSharpCode.SharpDevelop.FormDesigner
{	
	public class DesignPanel : Panel, ITabOrder
	{
		Control view;
		IDesignerHost host;
		
		bool isTabOrderMode = false; // True, if show tab order is active
		Hashtable mappingTable = new Hashtable(); // mapping table contains an association from form control to tab index control
		
		public Control View {
			get {
				return view;
			}
		}
		
		public IDesignerHost Host {
			get {
				return host;
			}
			set {
				Debug.Assert(value != null);
				this.host = value;
			}
		}
		
		public DesignPanel(IDesignerHost host) 
		{
			Debug.Assert(host != null);
			this.host      = host;
			this.BackColor = Color.White;
		}
		
		public void Reset()
		{
			if (view != null) {
				Controls.Clear();
				view.Dispose();
				view = null;
			}	
		}
		
		public void SetRootDesigner()
		{
			IRootDesigner rootDesigner = host.GetDesigner(host.RootComponent) as IRootDesigner;
			if (rootDesigner == null) {
				IMessageService messageService =(IMessageService)ServiceManager.Services.GetService(typeof(IMessageService));
				StringParserService stringParserService = (StringParserService)ServiceManager.Services.GetService(typeof(StringParserService));
				stringParserService.Properties["RootComponent"] = host.RootComponent == null ? "null" : host.RootComponent.ToString();
				messageService.ShowError("${res:ICSharpCode.SharpDevelop.FormDesigner.DesignPanel.CantCreateRootDesignerError}");
				return;
			}
			
			if (!TechnologySupported(rootDesigner.SupportedTechnologies, ViewTechnology.WindowsForms)) {
				IMessageService messageService =(IMessageService)ServiceManager.Services.GetService(typeof(IMessageService));
				messageService.ShowError("${ICSharpCode.SharpDevelop.FormDesigner.DesignPanel.RootDesignerNotWindowsFormsError}");
				return;
			}
		
			Reset();
			
			view = (Control)rootDesigner.GetView(ViewTechnology.WindowsForms);
			view.BackColor = Color.White;
			view.Dock = DockStyle.Fill;
		}
		
		public void SetErrorState(string errors)
		{
			Disable();
			Controls.Add(new DesignErrorPanel(errors));
		}
		
		public void Disable()
		{
			HideTabOrder();
			Controls.Clear();
		}
		
		public void Enable()
		{
			if (!Controls.Contains(view)) {
				Controls.Add(view);
			}
		}
		
		bool TechnologySupported(ViewTechnology[] technologies, ViewTechnology requiredTechnology)
		{
			foreach (ViewTechnology technology in technologies) {
				if (technology == requiredTechnology) {
					return true;
				}
			}
			return false;
		}
		
		#region TabOrder Mode Implementation
		/// <summary>
		/// Checks if tab order mode is active
		/// </summary>
		public bool IsTabOrderMode {
			get {
				return isTabOrderMode; 
			}		
		}
		
		void AddToTabIndexValue(Point p, int number)
		{
			// If not in tab order mode
			if (!isTabOrderMode) {
				return;
			}
			
			// Get the root Control
			Control rootControl = (Control)host.RootComponent;
			Control child = rootControl;
			
			while (true) {
				Control newChild = child.GetChildAtPoint(child.PointToClient(p));
				if (newChild == null) {
					break;
				}
				child = newChild;
			}
			
			if (child is TabIndexControl) {
				child = ((TabIndexControl)child).AssociatedControl;
			}
			if (child != null) {
				// ...Update tab index value for the control
				DesignerTransaction transaction = host.CreateTransaction("Alter tabindex");
				
				ComponentChangeService changeService = (ComponentChangeService)host.GetService(typeof(System.ComponentModel.Design.IComponentChangeService));
				PropertyDescriptor pd = TypeDescriptor.CreateProperty(child.GetType(), "TabIndex", typeof(int), null);
				changeService.OnComponentChanging(child, pd);
				int tabIndex    = (int)pd.GetValue(child);
				int newTabIndex = Math.Max(0, tabIndex + number);
				pd.SetValue(child, newTabIndex);
				changeService.OnComponentChanged(child, pd, tabIndex, newTabIndex);
				transaction.Commit();
			}
		}
		
		/// <summary>
		/// Sets the previous tab index if over a control.
		/// </summary>
		public void SetPrevTabIndex(Point p)
		{
			AddToTabIndexValue(p, -1);
		}
		
		/// <summary>
		/// Sets the next tab index if over a control.
		/// </summary>
		public void SetNextTabIndex(Point p)
		{
			AddToTabIndexValue(p, 1);
		}

		/// <summary>
		/// Show tab order.
		/// </summary>
		public void ShowTabOrder()
		{
			// Store new mode
			isTabOrderMode = true;
			
			foreach (object o in host.Container.Components) {
				Control ctrl = o as Control;
				if (o != host.RootComponent && ctrl != null) {
					TabIndexControl tic = new TabIndexControl(ctrl);
					try {
						ctrl.Parent.Controls.Add(tic);
						mappingTable.Add(ctrl, tic);
						tic.BringToFront();
					} catch {}
				}
			}
		}
		
		/// <summary>
		/// Hide tab order.
		/// </summary>
		public void HideTabOrder()
		{
			// Store new mode
			isTabOrderMode = false;
			
			// Remove all tab index controls.
			foreach (DictionaryEntry entry in mappingTable) {
				Control         motherControl   = entry.Key as Control;
				TabIndexControl tabIndexControl = entry.Value as TabIndexControl;
				motherControl.Parent.Controls.Remove(tabIndexControl);
			}
			mappingTable.Clear();
		}
		#endregion
	}
}
