// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

using ICSharpCode.SharpDevelop.Internal.Project;
using ICSharpCode.Core.Properties;

using ICSharpCode.Core.AddIns.Codons;
using ICSharpCode.SharpDevelop.Gui.Dialogs;
using ICSharpCode.Core.Services;

namespace ICSharpCode.ILAsmBinding
{
	public class ChooseRuntimePanel : AbstractOptionPanel
	{
		ILAsmCompilerParameters config = null;
		
		public override void LoadPanelContents()
		{
			SetupFromXml(Path.Combine(PropertyService.DataDirectory, 
			                          @"resources\panels\ChooseRuntimePanel.xfrm"));
			
			this.config = (ILAsmCompilerParameters)((IProperties)CustomizationObject).GetProperty("Config");
			
			((RadioButton)ControlDictionary["msnetRadioButton"]).Checked = config.NetRuntime == NetRuntime.MsNet;
			((RadioButton)ControlDictionary["monoRadioButton"]).Checked  = config.NetRuntime == NetRuntime.Mono;
			((RadioButton)ControlDictionary["mintRadioButton"]).Checked  = config.NetRuntime == NetRuntime.MonoInterpreter;
			
			((RadioButton)ControlDictionary["cscRadioButton"]).Checked = config.ILAsmCompiler == ILAsmCompiler.Microsoft;
			((RadioButton)ControlDictionary["mcsRadioButton"]).Checked = config.ILAsmCompiler == ILAsmCompiler.Mono;
		
			((RadioButton)ControlDictionary["cscRadioButton"]).CheckedChanged += new EventHandler(CompilerRadioButtonCheckedChanged);
			
			FileUtilityService fileUtilityService = (FileUtilityService)ServiceManager.Services.GetService(typeof(FileUtilityService));
			((ComboBox)ControlDictionary["compilerVersionComboBox"]).Items.Add("Standard");
			foreach (string runtime in fileUtilityService.GetAvaiableRuntimeVersions()) {
				((ComboBox)ControlDictionary["compilerVersionComboBox"]).Items.Add(runtime);
			}
			
			((ComboBox)ControlDictionary["compilerVersionComboBox"]).Text = config.ILAsmCompilerVersion.Length == 0 ? "Standard" : config.ILAsmCompilerVersion;
			CompilerRadioButtonCheckedChanged(this, EventArgs.Empty);
		}
		
		void CompilerRadioButtonCheckedChanged(object sender, EventArgs e)
		{
			ControlDictionary["compilerVersionLabel"].Enabled    = ((RadioButton)ControlDictionary["cscRadioButton"]).Checked;
			ControlDictionary["compilerVersionComboBox"].Enabled = ((RadioButton)ControlDictionary["cscRadioButton"]).Checked;
		}
		
		public override bool StorePanelContents()
		{
			if (((RadioButton)ControlDictionary["msnetRadioButton"]).Checked) {
				config.NetRuntime =  NetRuntime.MsNet;
			} else if (((RadioButton)ControlDictionary["monoRadioButton"]).Checked) {
				config.NetRuntime =  NetRuntime.Mono;
			} else {
				config.NetRuntime =  NetRuntime.MonoInterpreter;
			}
			config.ILAsmCompiler = ((RadioButton)ControlDictionary["cscRadioButton"]).Checked ? ILAsmCompiler.Microsoft : ILAsmCompiler.Mono;
			config.ILAsmCompilerVersion = ControlDictionary["compilerVersionComboBox"].Text;
			
			return true;
		}
	}
}
