// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms.Design;

namespace ICSharpCode.SharpDevelop.FormDesigner.Services 
{
	public class ExtenderService : IExtenderListService, IExtenderProviderService
	{
		ArrayList extenderProviders = new ArrayList();
		IDesignerHost            host;
		
		public ArrayList ExtenderProviders {
			get {
				return extenderProviders;
			}
		}
		
		public ExtenderService(IDesignerHost host)
		{
			this.host = host;
		}
		
		#region System.ComponentModel.Design.IExtenderListService interface implementation
		public IExtenderProvider[] GetExtenderProviders()
		{
			IExtenderProvider[] extenderProvidersArray = new IExtenderProvider[extenderProviders.Count];
			extenderProviders.CopyTo(extenderProvidersArray, 0);
			return extenderProvidersArray;
		}
		#endregion
		
		#region System.ComponentModel.Design.IExtenderProviderService interface implementation
		public void RemoveExtenderProvider(System.ComponentModel.IExtenderProvider provider)
		{
			extenderProviders.Remove(provider);
		}
		
		public void AddExtenderProvider(System.ComponentModel.IExtenderProvider provider)
		{
			extenderProviders.Add(provider);
		}
		#endregion
	}
}
