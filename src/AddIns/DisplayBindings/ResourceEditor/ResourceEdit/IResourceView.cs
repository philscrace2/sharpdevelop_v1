using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace ResourceEditor
{
	delegate void ResourceChangedEventHandler(object sender, ResourceEventArgs e);
	
	interface IResourceView : IDisposable
	{
		bool WriteProtected
		{
			get;
			set;
		}
		
		ResourceItem ResourceItem
		{
			get;
			set;
		}
		
		event ResourceChangedEventHandler ResourceChanged;
	}
	
	class ResourceEventArgs
	{
		string resourceName;
		object resourceValue;
		
		public ResourceEventArgs(string resourceName, object resourceValue)
		{
			this.resourceName = resourceName;
			this.resourceValue = resourceValue;
		}
		
		public string ResourceName
		{
			get {
				return resourceName;
			}
			set {
				resourceName = value;
			}
		}
		
		public object ResourceValue
		{
			get {
				return resourceValue;
			}
			set {
				resourceValue = value;
			}
		}
	}
}
