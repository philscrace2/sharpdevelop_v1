// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace ICSharpCode.SharpDevelop.Gui
{
	/// <summary>
	/// </summary>
	public interface ICustomizedCommands
	{
		/// <summary>
		/// Returns true, if the save as command is handled, false otherwise
		/// </summary>
		bool SaveAsCommand();
	}
}
