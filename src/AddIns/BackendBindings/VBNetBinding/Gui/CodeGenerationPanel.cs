// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Markus Palme" email="MarkusPalme@gmx.de"/>
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

namespace VBBinding
{
	public class CodeGenerationPanel : AbstractOptionPanel
	{
		VBCompilerParameters compilerParameters = null;
		
		ResourceService resourceService = (ResourceService)ServiceManager.Services.GetService(typeof(IResourceService));
		static FileUtilityService fileUtilityService = (FileUtilityService)ServiceManager.Services.GetService(typeof(FileUtilityService));
		
		public override bool ReceiveDialogMessage(DialogMessage message)
		{
			if (message == DialogMessage.OK) {
				if (compilerParameters == null) {
					return true;
				}
				FileUtilityService fileUtilityService = (FileUtilityService)ServiceManager.Services.GetService(typeof(FileUtilityService));
				
				
				compilerParameters.DefineSymbols = ControlDictionary["symbolsTextBox"].Text;
				compilerParameters.MainClass     = ControlDictionary["mainClassTextBox"].Text;
				compilerParameters.Imports       = ControlDictionary["importsTextBox"].Text;
				compilerParameters.RootNamespace = ControlDictionary["RootNamespaceTextBox"].Text;
				
				compilerParameters.Debugmode = ((CheckBox)ControlDictionary["generateDebugInformationCheckBox"]).Checked;
				compilerParameters.Optimize = ((CheckBox)ControlDictionary["enableOptimizationCheckBox"]).Checked;
				compilerParameters.GenerateOverflowChecks = ((CheckBox)ControlDictionary["generateOverflowChecksCheckBox"]).Checked;
				compilerParameters.TreatWarningsAsErrors  = ((CheckBox)ControlDictionary["warningsAsErrorsCheckBox"]).Checked;
				
				compilerParameters.OptionExplicit = ((CheckBox)ControlDictionary["optionExplicitCheckBox"]).Checked ;
				compilerParameters.OptionStrict = ((CheckBox)ControlDictionary["optionStrictCheckBox"]).Checked;
			}
			return true;
		}
		
		void SetValues(object sender, EventArgs e)
		{
			this.compilerParameters = (VBCompilerParameters)((IProperties)CustomizationObject).GetProperty("Config");
			
			ControlDictionary["symbolsTextBox"].Text = compilerParameters.DefineSymbols;
			ControlDictionary["mainClassTextBox"].Text = compilerParameters.MainClass;
			ControlDictionary["importsTextBox"].Text = compilerParameters.Imports;
			ControlDictionary["RootNamespaceTextBox"].Text = compilerParameters.RootNamespace;
			
			
			((CheckBox)ControlDictionary["generateDebugInformationCheckBox"]).Checked = compilerParameters.Debugmode;
			((CheckBox)ControlDictionary["enableOptimizationCheckBox"]).Checked = compilerParameters.Optimize;
			((CheckBox)ControlDictionary["generateOverflowChecksCheckBox"]).Checked = compilerParameters.GenerateOverflowChecks;
			((CheckBox)ControlDictionary["warningsAsErrorsCheckBox"]).Checked = compilerParameters.TreatWarningsAsErrors;
			
			((CheckBox)ControlDictionary["optionExplicitCheckBox"]).Checked = compilerParameters.OptionExplicit;
			((CheckBox)ControlDictionary["optionStrictCheckBox"]).Checked = compilerParameters.OptionStrict;
		}
		
		static PropertyService propertyService = (PropertyService)ServiceManager.Services.GetService(typeof(PropertyService));
		public CodeGenerationPanel() : base(propertyService.DataDirectory + @"\resources\panels\ProjectOptions\VBNetCodeGenerationPanel.xfrm")
		{
			CustomizationObjectChanged += new EventHandler(SetValues);
			
		}
	}
}
