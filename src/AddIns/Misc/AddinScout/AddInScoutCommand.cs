using System;
using ICSharpCode.SharpDevelop.Gui;

namespace AddInScout
{
	public class AddInScoutCommand : ICSharpCode.Core.AddIns.Codons.AbstractMenuCommand
	{
		public override void Run() 
		{
			AddInScoutViewContent vw = new AddInScoutViewContent();
			WorkbenchSingleton.Workbench.ShowView(vw);
		}
	}
}
