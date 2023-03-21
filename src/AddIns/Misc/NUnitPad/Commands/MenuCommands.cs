// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

using ICSharpCode.Core.AddIns.Codons;
using ICSharpCode.SharpDevelop.Gui;

namespace ICSharpCode.NUnitPad
{
	public class RunTestsInProject : AbstractMenuCommand
	{
		public override void Run()
		{
			NUnitPadContent nunitPad = (NUnitPadContent)WorkbenchSingleton.Workbench.GetPad(typeof(NUnitPadContent));
			if (nunitPad != null) {
				nunitPad.BringPadToFront();
				nunitPad.RunTests();
			}
		}
	}
}
