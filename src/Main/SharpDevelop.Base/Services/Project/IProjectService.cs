// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;

using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Internal.Project;

namespace ICSharpCode.SharpDevelop.Services
{
	/// <summary>
	/// This interface describes the basic functions of the 
	/// SharpDevelop project service.
	/// </summary>
	public interface IProjectService
	{
		/// <remarks>
		/// Gets/Sets the current selected project. (e.g. the project
		/// that contains the file that has the focus)
		/// </remarks>
		IProject CurrentSelectedProject {
			get;
			set;
		}
		
		/// <remarks>
		/// Gets/Sets the current selected combine. (e.g. the combine
		/// that contains the CurrentSelectedProject)
		/// </remarks>
		Combine CurrentSelectedCombine {
			get;
			set;
		}
		
		/// <remarks>
		/// Gets the root combine, if no combine is open it returns null. 
		/// </remarks>
		Combine CurrentOpenCombine {
			get;
		}
		
		/// <remarks>
		/// Returns true, if one open project that should be compiled is dirty.
		/// </remarks>
		bool NeedsCompiling {
			get;
		}
		
		/// <remarks>
		/// Closes the root combine
		/// </remarks>
		void CloseCombine();
		
		/// <remarks>
		/// Closes the root combine
		/// </remarks>
		void CloseCombine(bool saveCombinePreferencies);
		
		/// <remarks>
		/// Gets the file name from one combine which is currently opened
		/// </remarks>
		string GetFileName(Combine combine);
		
		/// <remarks>
		/// Gets the file name from one project which is currently opened
		/// </remarks>
		string GetFileName(IProject project);
		
		/// <remarks>
		/// Compile the root combine.
		/// </remarks>
		void CompileCombine();
		
		/// <remarks>
		/// Compile the root combine. Forces Recompile for all projects.
		/// </remarks>
		void RecompileAll();
		
		/// <remarks>
		/// Compiles a specific project, if the project isn't dirty this
		/// method does nothing
		/// </remarks>
		ICompilerResult CompileProject(IProject project);
		
		/// <remarks>
		/// Compiles a specific project (forced!)
		/// </remarks>
		ICompilerResult RecompileProject(IProject project);
		
		/// <remarks>
		/// Opens a new root combine, closes the old root combine automatically.
		/// </remarks>
		void OpenCombine(string filename);
		
		/// <remarks>
		/// Saves the whole root combine.
		/// </remarks>
		void SaveCombine();
		
		/// <remarks>
		/// Saves the IDE preferences for the root combine (open files, class browser
		/// status etc.) SHOULD NOT BE CALLED BY YOU ! (except you know what you do)
		/// </remarks>
		void SaveCombinePreferences();
		
		/// <summary>
		/// Gets a reference (only assembly references) for a given project, if the reference
		/// doesn't exits it creates an assembly reference to the filename.
		/// </summary>
		/// <remarks>
		/// This method doesn't update the project browser tree or save the project. It
		/// is merly used to prevent reference duplication.
		/// </remarks>
		ProjectReference GetReferenceFromProject(IProject prj, string filename);
		
		/// <remarks>
		/// Add a file to a given project
		/// </remarks>
		ProjectFile AddFileToProject(IProject prj, string filename, BuildAction action);
		
		/// <summary>
		/// Adds a reference to the project, updates the combine tree and saves the project.
		/// </summary>
		/// <remarks>
		/// Duplicated references won't be added.
		/// </remarks>
		/// <returns>
		/// True, if the reference was added successfully, false when the reference
		/// was already in the project.
		/// </returns>
		bool AddReferenceToProject(IProject prj, ProjectReference reference);
		
		/// <remarks>
		/// Add a file to a given project
		/// </remarks>
		void AddFileToProject(IProject prj, ProjectFile projectFile);
		
		/// <remarks>
		/// Mark a file dirty, the project in which the file is in will be compiled
		/// in the next compiler run.
		/// </remarks>
		void MarkFileDirty(string filename);
		
		/// <remarks>
		/// Mark a project dirty, the project will be compiled
		/// in the next compiler run.
		void MarkProjectDirty(IProject project);
		
		IProject GetProject(string projectName);
		
		/// <remarks>
		/// If the file given by fileName is inside a currently open project this method
		/// gets the ProjectFile for this file, returns null otherwise.
		/// </remarks>
		ProjectFile RetrieveFileInformationForFile(string fileName);
		
		/// <summary>
		/// Returns true if a project has this name
		/// </summary>
		bool ExistsEntryWithName(string name);
		
		/// <remarks>
		/// Only to be called by the compile actions.
		/// </remarks>
		void OnStartBuild();
		
		/// <remarks>
		/// Only to be called by the compile actions.
		/// </remarks>
		void OnEndBuild();
		
		/// <remarks>
		/// Only to be called by the compile actions.
		/// </remarks>
		void OnBeforeStartProject();
		
		/// <remarks>
		/// Only to be called by AbstractProject
		/// </remarks>
		void OnRenameProject(ProjectRenameEventArgs e);
		
		/// <remarks>
		/// Only to be called by AbstractProject
		/// </remarks>
		void OnActiveConfigurationChanged(ConfigurationEventArgs e);
		
		/// <remarks>
		/// Only to be called by AbstractProject
		/// </remarks>
		void OnConfigurationAdded(EventArgs e);
		
		/// <remarks>
		/// Only to be called by AbstractProject
		/// </remarks>
		void OnConfigurationRemoved(EventArgs e);
		
		/// <remarks>
		/// Gets the file name of the output assembly of a given project
		/// (Or at least the 'main' file)
		/// </remarks>
		string GetOutputAssemblyName(IProject project);
		
		/// <remarks>
		/// Gets the file name of the output assembly of a given file
		/// (Or at least the 'main' file)
		/// </remarks>
		string GetOutputAssemblyName(string fileName);
		
		/// <remarks>
		/// Removes a file from it's project(s)
		/// </remarks>
		void RemoveFileFromProject(string fileName);
		
		/// <remarks>
		/// Is called, when a file is removed from a project.
		/// </remarks>
		event FileEventHandler FileRemovedFromProject;
				
		/// <remarks>
		/// Called before a build run
		/// </remarks>
		event EventHandler StartBuild;
		
		/// <remarks>
		/// Called after a build run
		/// </remarks>
		event EventHandler EndBuild;
		
		/// <remarks>
		/// Called before execution
		/// </remarks>
		event EventHandler BeforeStartProject;
		
		/// <remarks>
		/// Called after a new root combine is opened
		/// </remarks>
		event CombineEventHandler CombineOpened;
		
		/// <remarks>
		/// Called after a root combine is closed
		/// </remarks>
		event CombineEventHandler CombineClosed;
		
		/// <remarks>
		/// Called after the current selected project has chaned
		/// </remarks>
		event ProjectEventHandler CurrentProjectChanged;
		
		/// <remarks>
		/// Called after the current selected combine has chaned
		/// </remarks>
		event CombineEventHandler CurrentSelectedCombineChanged;
		
		/// <remarks>
		/// Called after a project got renamed
		/// </remarks>
		event ProjectRenameEventHandler ProjectRenamed;
		
		/// <remarks>
		/// Called after the active project configuration has
		/// been changed.
		/// </remarks>
		event ConfigurationEventHandler ActiveConfigurationChanged;
		
		/// <summary>
		/// Called after a new configurations has been
		/// added to the project.
		/// </summary>
		event EventHandler ConfigurationAdded;
		
		/// <summary>
		/// Called after a configuration has been removed
		/// from the project.
		/// </summary>
		event EventHandler ConfigurationRemoved;
	}
}
