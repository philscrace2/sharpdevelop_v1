// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

using ICSharpCode.SharpDevelop.Internal.Project;
using ICSharpCode.SharpDevelop.Internal.ExternalTool;
using ICSharpCode.SharpDevelop.Gui.Dialogs;
using ICSharpCode.Core.Services;
using ICSharpCode.Core.Properties;
using ICSharpCode.Core.AddIns.Codons;

namespace ICSharpCode.WixBinding
{
	public class LinkerParametersPanel : AbstractOptionPanel
	{
		WixCompilerParameters compilerParameters = null;
		
		public override void LoadPanelContents()
		{
			this.compilerParameters = (WixCompilerParameters)((IProperties)CustomizationObject).GetProperty("Config");
			
			System.Windows.Forms.PropertyGrid grid = new System.Windows.Forms.PropertyGrid();
			grid.Dock = DockStyle.Fill;
			grid.SelectedObject = compilerParameters;
			Controls.Add(grid);
		}
		
		public override bool StorePanelContents()
		{
			return true;
		}
	}
}
