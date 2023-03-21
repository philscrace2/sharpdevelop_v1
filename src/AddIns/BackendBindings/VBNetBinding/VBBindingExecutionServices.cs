// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Markus Palme" email="MarkusPalme@gmx.de"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;
using System.Xml;
using System.CodeDom.Compiler;
using System.Threading;

using ICSharpCode.SharpDevelop.Internal.Project;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Services;
using ICSharpCode.Core.Services;

namespace VBBinding
{
	/// <summary>
	/// This class controls the compilation of C Sharp files and C Sharp projects
	/// </summary>
	public class VBBindingExecutionServices
	{	

		public void Execute(string filename, bool debug)
		{
			string exe = Path.ChangeExtension(filename, ".exe");
			DebuggerService debuggerService  = (DebuggerService)ServiceManager.Services.GetService(typeof(DebuggerService));
			if (debug) {
				debuggerService.Start(exe, Path.GetDirectoryName(exe), "");
			} else {
				ProcessStartInfo psi = new ProcessStartInfo(Environment.GetEnvironmentVariable("ComSpec"), "/c " + "\"" + exe + "\"" + " & pause");
				psi.WorkingDirectory = Path.GetDirectoryName(exe);
				psi.UseShellExecute = false;
				
				debuggerService.StartWithoutDebugging(psi);
			}
		}
		
		public void Execute(IProject project, bool debug)
		{
			VBCompilerParameters parameters = (VBCompilerParameters)project.ActiveConfiguration;
			
			FileUtilityService fileUtilityService = (FileUtilityService)ServiceManager.Services.GetService(typeof(FileUtilityService));
			string directory = fileUtilityService.GetDirectoryNameWithSeparator(parameters.OutputDirectory);
			string exe = parameters.OutputAssembly + ".exe";
			string args = parameters.CommandLineParameters;

			ProcessStartInfo psi;
			bool customStartup = false;
			if (parameters.CompileTarget != CompileTarget.WinExe && parameters.PauseConsoleOutput) {
				customStartup = true;
				psi = new ProcessStartInfo(Environment.GetEnvironmentVariable("ComSpec"), "/c \"" + directory + exe + "\" " + args +  " & pause");
			} else {
				if (parameters.CompileTarget == CompileTarget.Library) {
					IMessageService messageService =(IMessageService)ServiceManager.Services.GetService(typeof(IMessageService));
					messageService.ShowError("${res:BackendBindings.ExecutionManager.CantExecuteDLLError}");
					return;
				}
			
				psi = new ProcessStartInfo(directory + exe);
				psi.Arguments = args;
			}
			
			psi.WorkingDirectory = Path.GetDirectoryName(directory);
			psi.UseShellExecute = false;
			DebuggerService debuggerService  = (DebuggerService)ServiceManager.Services.GetService(typeof(DebuggerService));
			if (debug && !customStartup) {
				debuggerService.Start(Path.Combine(directory, exe), directory, args);
			} else {
				debuggerService.StartWithoutDebugging(psi);
			}
		}
	}
}
