// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Threading;
using System.Windows.Forms;

using NUnit.Core;
using NUnit.Framework;
using NUnit.Util;

using Reflector.UserInterface;

using ICSharpCode.SharpDevelop.Internal.Project;
using ICSharpCode.Core.Services;
using ICSharpCode.SharpDevelop.Services;
using ICSharpCode.SharpDevelop.Gui;

namespace ICSharpCode.NUnitPad
{
	/// <summary>
	/// Description of the pad content
	/// </summary>
	public class NUnitPadContent : AbstractPadContent
	{
		TestTreeView testTreeView;
		Panel        contentPanel;
		bool         autoLoadItems = false;
		ArrayList testDomains = new ArrayList();
		
		#region AbstractPadContent requirements
		/// <summary>
		/// The <see cref="System.Windows.Forms.Control"/> representing the pad
		/// </summary>
		public override Control Control {
			get {
				return contentPanel;
			}
		}
		
		/// <summary>
		/// Creates a new NUnitPadContent object
		/// </summary>
		public NUnitPadContent() : base("${res:ICSharpCode.NUnitPad.NUnitPadContent.PadName}", "Icons.16x16.TestRunner")
		{
			IconService iconService = (IconService)ServiceManager.Services.GetService(typeof(IconService));
			StringParserService stringParserService = (StringParserService)ServiceManager.Services.GetService(typeof(StringParserService));
			
			testTreeView = new TestTreeView();
			testTreeView.Dock = DockStyle.Fill;
			CommandBar commandBar = new CommandBar();
			commandBar.Dock = DockStyle.Top;
			
			CommandBarButton refreshItem = new CommandBarButton(stringParserService.Parse("${res:NUnitPad.NUnitPadContent.RefreshItem}"));
			refreshItem.Image = iconService.GetBitmap("Icons.16x16.BrowserRefresh");
			refreshItem.Click += new EventHandler(RefreshItemClick);
			commandBar.Items.Add(refreshItem);
			
			CommandBarButton cancelItem = new CommandBarButton(stringParserService.Parse("${res:NUnitPad.NUnitPadContent.CancelItem}"));
			cancelItem.Image = iconService.GetBitmap("Icons.16x16.BrowserCancel");
			cancelItem.Click += new EventHandler(CancelItemClick);
			
			commandBar.Items.Add(cancelItem);
			
			commandBar.Items.Add(new CommandBarSeparator());
			
			CommandBarButton referenceItem = new CommandBarButton(stringParserService.Parse("${res:NUnitPad.NUnitPadContent.ReferenceItem}"));
			referenceItem.Image = iconService.GetBitmap("Icons.16x16.Reference");
			referenceItem.Click += new EventHandler(AddNUnitReference);
			commandBar.Items.Add(referenceItem);
			
			commandBar.Items.Add(new CommandBarSeparator());
			
			CommandBarButton runItem = new CommandBarButton(stringParserService.Parse("${res:NUnitPad.NUnitPadContent.RunItem}"));
			runItem.Image = iconService.GetBitmap("Icons.16x16.RunProgramIcon");
			runItem.Click += new EventHandler(RunItemClick);
			commandBar.Items.Add(runItem);
			
			contentPanel = new Panel();
			contentPanel.Controls.Add(testTreeView);
			contentPanel.Controls.Add(commandBar);
			
			IProjectService projectService = (IProjectService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IProjectService));
			projectService.CombineOpened += new CombineEventHandler(CombineEventHandler);
			projectService.CombineClosed += new CombineEventHandler(ProjectServiceCombineClosed);
			projectService.StartBuild += new EventHandler(ProjectServiceStartBuild);
			projectService.EndBuild += new EventHandler(ProjectServiceEndBuild);
			testTreeView.SetAutoLoadState(autoLoadItems);
		}
		
		/// <summary>
		/// Refreshes the pad
		/// </summary>
		public override void RedrawContent()
		{
		}
		
		/// <summary>
		/// Cleans up all used resources
		/// </summary>
		public override void Dispose()
		{
			base.Dispose();
			UnloadAppDomains();
			testTreeView.Dispose();
			contentPanel.Dispose();
			IProjectService projectService = (IProjectService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IProjectService));
			projectService.StartBuild -= new EventHandler(ProjectServiceStartBuild);
			projectService.EndBuild   -= new EventHandler(ProjectServiceEndBuild);
		}
		#endregion
		
		void ProjectServiceStartBuild(object sender, EventArgs e)
		{
		}
		
		void CombineEventHandler(object sender, CombineEventArgs e)
		{
			if (autoLoadItems) {
				RefreshProjectAssemblies();
			}
		}
		
		void ProjectServiceEndBuild(object sender, EventArgs e)
		{
			if (autoLoadItems) {
				testTreeView.Invoke(new ThreadStart(RefreshProjectAssemblies));
			}
		}
		
		void AddNUnitReference(object sender, EventArgs e)
		{
			IProjectService projectService = (IProjectService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IProjectService));
			IProject project = projectService.CurrentSelectedProject;
			if (project != null) {
				foreach (ProjectReference reference in project.ProjectReferences) {
					if (reference.ReferenceType == ReferenceType.Gac && reference.Reference.ToLower().StartsWith("nunit.framework")) {
						return;
					}
				}
				projectService.AddReferenceToProject(project, new ProjectReference(ReferenceType.Gac, "nunit.framework"));
			}
		}
		
		void RunItemClick(object sender, EventArgs e)
		{
			RunTests();
		}
		
		void RefreshItemClick(object sender, EventArgs e)
		{
			autoLoadItems = true;
			RefreshProjectAssemblies();
		}
		
		void CancelItemClick(object sender, EventArgs e)
		{
			autoLoadItems = false;
			UnloadAppDomains();
			testTreeView.SetAutoLoadState(autoLoadItems);
		}
		
		void ProjectServiceCombineClosed(object sender, CombineEventArgs e)
		{
			if (testDomains.Count > 0) {
				UnloadAppDomains();
			}
		}
		
		void UnloadAppDomains()
		{
			foreach (TestDomain domain in testDomains) {
				try {
					domain.Unload();
				} catch (Exception) {}
			}
			testDomains.Clear();
			testTreeView.ClearTests();
		}
		
		public void RunTests()
		{
			if (!autoLoadItems) {
				autoLoadItems = true;
				RefreshProjectAssemblies();
			}
			testTreeView.RunTests();
		}
		
		public void RefreshProjectAssemblies()
		{
			UnloadAppDomains();
			
			IProjectService projectService = (IProjectService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IProjectService));
			ArrayList projectCombineEntries = Combine.GetAllProjects(projectService.CurrentOpenCombine);
			foreach (ProjectCombineEntry projectEntry in projectCombineEntries) {
				string outputAssembly = projectService.GetOutputAssemblyName(projectEntry.Project);
				TestDomain testDomain = new TestDomain();
				try {
					Test testsFromAssembly = testDomain.Load(outputAssembly);
					testTreeView.PrintTests(outputAssembly, testsFromAssembly);
					testDomains.Add(testDomain);
				} catch (Exception e) {
					testDomain.Unload();
					Console.WriteLine(e);
					testTreeView.PrintTestErrors(outputAssembly);
				}
				
			}
		}
	}
}
