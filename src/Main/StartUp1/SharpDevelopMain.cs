// <file>
//	 <copyright see="prj:///doc/copyright.txt"/>
//	 <license see="prj:///doc/license.txt"/>
//	 <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//	 <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using System.Resources;
using System.Xml;
using System.Threading;
using System.Runtime.Remoting;
using System.Security.Policy;

using ICSharpCode.Core.Properties;
using ICSharpCode.Core.AddIns.Codons;
using ICSharpCode.Core.AddIns;
using ICSharpCode.Core.Services;
using ICSharpCode.SharpDevelop.Gui.Dialogs;

namespace ICSharpCode.SharpDevelop
{
	/// <summary>
	/// This Class is the Core main class, it starts the program.
	/// </summary>
	public class SharpDevelopMain
	{
		static string[] commandLineArgs = null;
		
		public static string[] CommandLineArgs {
			get {
				return commandLineArgs;
			}
		}
		
		static void ShowErrorBox(object sender, ThreadExceptionEventArgs eargs)
		{
			DialogResult result = new ExceptionBox(eargs.Exception).ShowDialog();

			switch (result) {
				case DialogResult.Ignore:
					break;
				case DialogResult.Abort:
					Application.Exit();
					break;
				case DialogResult.Yes:
					break;
			}
		}
		
		/// <summary>
		/// Starts the core of SharpDevelop.
		/// </summary>
		[STAThread()]
		public static void Main(string[] args)
		{
			commandLineArgs = args;
			bool noLogo = false;
			
			SplashScreenForm.SetCommandLineArgs(args);
			
			foreach (string parameter in SplashScreenForm.GetParameterList()) {
				switch (parameter.ToUpper()) {
					case "NOLOGO":
						noLogo = true;
						break;
				}
			}
			
			if (!noLogo) {
				SplashScreenForm.SplashScreen.Show();
			}
			Application.ThreadException += new ThreadExceptionEventHandler(ShowErrorBox);
			
			bool ignoreDefaultPath = false;
			string [] addInDirs = ICSharpCode.SharpDevelop.AddInSettingsHandler.GetAddInDirectories(out ignoreDefaultPath);
			AddInTreeSingleton.SetAddInDirectories(addInDirs, ignoreDefaultPath);
			
			ArrayList commands = null;
			try {
				ServiceManager.Services.AddService(new MessageService());
				ServiceManager.Services.AddService(new ResourceService());
				ServiceManager.Services.AddService(new IconService());
				ServiceManager.Services.InitializeServicesSubsystem("/Workspace/Services");
			
				commands = AddInTreeSingleton.AddInTree.GetTreeNode("/Workspace/Autostart").BuildChildItems(null);
				for (int i = 0; i < commands.Count - 1; ++i) {
					((ICommand)commands[i]).Run();
				}
			} catch (XmlException e) {
				MessageBox.Show("Could not load XML :" + Environment.NewLine + e.Message);
				return;
			} catch (Exception e) {
				MessageBox.Show("Loading error, please reinstall :"  + Environment.NewLine + e.ToString());
				return;
			} finally {
				if (SplashScreenForm.SplashScreen != null) {
					SplashScreenForm.SplashScreen.Close();
				}
			}
			
			try {
				// run the last autostart command, this must be the workbench starting command
				if (commands.Count > 0) {
					((ICommand)commands[commands.Count - 1]).Run();
				}
			} finally {
				// unloading services
				ServiceManager.Services.UnloadAllServices();
			}
		}
	}
}
