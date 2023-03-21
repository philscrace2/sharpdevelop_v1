// created on 10/10/2002 at 16:13

using System;
using System.Drawing;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms;
using System.Threading;
using Microsoft.Win32;

using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Gui.Pads;

namespace ICSharpCode.SharpDevelop.FormDesigner.Services
{
	/// <summary>
	/// BaseImlementation of IHelpService
	/// </summary>
	/// <remarks>
	/// 	created by - Niv
	/// 	created on - 10/10/2002 11:44:46
	/// </remarks>
	public class HelpService : IHelpService
	{
		Hashtable LocalContexts     = new Hashtable();
		ArrayList ContextAttributes = new ArrayList();
		
		string f1Keyword      = null;
		string generalKeyword = null;
		
		
		public HelpService()
		{
		}
		
		public void AddContextAttribute(string name, string value, HelpKeywordType keywordType)
		{
			switch (keywordType) {
				case HelpKeywordType.F1Keyword:
					f1Keyword = value;
					return;
				case HelpKeywordType.GeneralKeyword:
					generalKeyword = value;
					return;
			}
		}
		
		public void ClearContextAttributes()
		{
		}
		
		public IHelpService CreateLocalContext(HelpContextType contextType)
		{
			return this;
		}
		
		public void RemoveContextAttribute(string name, string value)
		{
//			System.Console.WriteLine("child removeing {0} : {1}",name,value);
//			object att = helpGUI.RemoveContextAttributeFromView(name,value);
//			ContextAttributes.Remove(att);;
		}
		
		public void RemoveLocalContext(IHelpService localContext)
		{
			LocalContexts.Remove(LocalContexts);
		}
		
		public void ShowHelpFromKeyword(string helpKeyword)
		{
			if (helpKeyword == null) {
				helpKeyword = f1Keyword;
			}
			string classStr  = helpKeyword;
			string memberStr = String.Empty;
			
			// show member help
			if (helpKeyword == f1Keyword) {
				int idx   = helpKeyword.LastIndexOf('.');
				classStr  = helpKeyword.Substring(0, idx);
				memberStr = helpKeyword.Substring(idx + 1);
			}
			HelpBrowser helpBrowser = (HelpBrowser)WorkbenchSingleton.Workbench.GetPad(typeof(HelpBrowser));
			helpBrowser.ShowHelpFromType(classStr, memberStr);
		}
		public void ShowGeneralHelp()
		{
			ShowHelpFromKeyword(generalKeyword);
		}
		public void ShowHelp()
		{
			ShowHelpFromKeyword(f1Keyword);
		}
		
		public void ShowHelpFromUrl(string helpURL)
		{
			HelpBrowser helpBrowser = (HelpBrowser)WorkbenchSingleton.Workbench.GetPad(typeof(HelpBrowser));
			helpBrowser.ShowHelpBrowser(helpURL);
		}
	}
}
