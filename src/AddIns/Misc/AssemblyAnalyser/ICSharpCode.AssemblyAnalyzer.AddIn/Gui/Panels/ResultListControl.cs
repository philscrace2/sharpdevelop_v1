// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

using ICSharpCode.Core.Services;
using ICSharpCode.AssemblyAnalyser.Rules;
using ICSharpCode.SharpDevelop.Services;

namespace ICSharpCode.AssemblyAnalyser
{
	/// <summary>
	/// Description of ResultListControl.	
	/// </summary>
	[ToolboxBitmap(typeof(System.Windows.Forms.ListView))]
	public class ResultListControl : System.Windows.Forms.UserControl
	{
		private System.Windows.Forms.ListView listView;
		private System.Windows.Forms.ColumnHeader criticalHeader;
		private System.Windows.Forms.ColumnHeader itemHeader;
		private System.Windows.Forms.ColumnHeader ruleHeader;
		private System.Windows.Forms.ColumnHeader certaintyHeader;
		private System.Windows.Forms.ColumnHeader levelHeader;
		
		ResultDetailsView resultDetailsView = null;
		
		public ResultDetailsView ResultDetailsView {
			get {
				return resultDetailsView;
			}
			set {
				resultDetailsView = value;
			}
		}
		
		public ResultListControl()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			
			StringParserService stringParserService = (StringParserService)ServiceManager.Services.GetService(typeof(StringParserService));
			levelHeader.Text     = stringParserService.Parse("${res:ICSharpCode.AssemblyAnalyser.ResultListControl.LevelHeader}");
			certaintyHeader.Text = stringParserService.Parse("${res:ICSharpCode.AssemblyAnalyser.ResultListControl.CertaintyHeader}");
			ruleHeader.Text      = stringParserService.Parse("${res:ICSharpCode.AssemblyAnalyser.ResultListControl.RuleHeader}");
			itemHeader.Text      = stringParserService.Parse("${res:ICSharpCode.AssemblyAnalyser.ResultListControl.ItemHeader}");
		}
		
		public void ClearContents()
		{
			this.listView.SelectedIndexChanged -= new System.EventHandler(this.ListViewSelectedIndexChanged);
			listView.Items.Clear();
			this.listView.SelectedIndexChanged += new System.EventHandler(this.ListViewSelectedIndexChanged);
		}
		
		public void PrintReport(ArrayList resolutions)
		{
			try {
				listView.BeginUpdate();
				listView.Items.Clear();
				StringParserService stringParserService = (StringParserService)ServiceManager.Services.GetService(typeof(StringParserService));
				int cerr = 0, err = 0, cwar = 0, war = 0, inf = 0;
				foreach (Resolution resolution in resolutions) {
					string critical = String.Empty;
					string type     = String.Empty;
					Color foreColor = Color.Black;
					
					switch (resolution.FailedRule.PriorityLevel) {
						case PriorityLevel.CriticalError:
							critical = "!";
							type = stringParserService.Parse("${res:ICSharpCode.AssemblyAnalyser.ResultListControl.ErrorType}");
							foreColor = Color.Red;
							++cerr;
							break;
						case PriorityLevel.Error:
							type = stringParserService.Parse("${res:ICSharpCode.AssemblyAnalyser.ResultListControl.ErrorType}");
							foreColor = Color.DarkRed;
							++err;
							break;
						case PriorityLevel.CriticalWarning:
							critical = "!";
							type = stringParserService.Parse("${res:ICSharpCode.AssemblyAnalyser.ResultListControl.WarningType}");
							foreColor = Color.Blue;
							++cwar;
							break;
						case PriorityLevel.Warning:
							type = stringParserService.Parse("${res:ICSharpCode.AssemblyAnalyser.ResultListControl.WarningType}");
							foreColor = Color.DarkBlue;
							++war;
							break;
						case PriorityLevel.Information:
							type = stringParserService.Parse("${res:ICSharpCode.AssemblyAnalyser.ResultListControl.InformationType}");
							++inf;
							break;
					}
					string certainity = resolution.FailedRule.Certainty.ToString() + "%";
					string text = stringParserService.Parse(resolution.FailedRule.Description);
					string item = stringParserService.Parse(resolution.Item);
					ListViewItem listViewItem = new ListViewItem(new string[] {critical, type, certainity, text, item});
					listViewItem.Font      = new Font("Arial", 9, FontStyle.Bold);
					listViewItem.ForeColor = foreColor;
					listViewItem.Tag = resolution;
					listView.Items.Add(listViewItem);
					
				}
				IStatusBarService statusBarService = (IStatusBarService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IStatusBarService));
				if (resolutions.Count == 0) {
					statusBarService.SetMessage("${res:ICSharpCode.AssemblyAnalyser.ResultListControl.NoDefectsFoundStatusBarMessage}");
				} else {
					statusBarService.SetMessage(stringParserService.Parse("${res:ICSharpCode.AssemblyAnalyser.ResultListControl.TotalDefectsStatusBarMessage}",
					                                          new string[,] {
					                                          	{"TotalDefects", resolutions.Count.ToString()},
					                                          	{"CriticalErrors", cerr.ToString()},
					                                          	{"Errors",err.ToString()},
					                                          	{"CriticalWarnings", cwar.ToString()},
					                                          	{"Warnings", war.ToString()},
					                                          	{"Informations", inf.ToString()}
					                                          }));
				}
			} catch (Exception e) {
				Console.WriteLine("Got exception : " + e.ToString());
			} finally {
				listView.EndUpdate();
			}
		}
		
		#region Windows Forms Designer generated code
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent() {
			this.levelHeader = new System.Windows.Forms.ColumnHeader();
			this.certaintyHeader = new System.Windows.Forms.ColumnHeader();
			this.ruleHeader = new System.Windows.Forms.ColumnHeader();
			this.itemHeader = new System.Windows.Forms.ColumnHeader();
			this.criticalHeader = new System.Windows.Forms.ColumnHeader();
			this.listView = new System.Windows.Forms.ListView();
			this.SuspendLayout();
			// 
			// levelHeader
			// 
			this.levelHeader.Text = "Level";
			this.levelHeader.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// certaintyHeader
			// 
			this.certaintyHeader.Text = "Certainty";
			this.certaintyHeader.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// ruleHeader
			// 
			this.ruleHeader.Text = "Rule";
			this.ruleHeader.Width = 350;
			// 
			// itemHeader
			// 
			this.itemHeader.Text = "Item";
			this.itemHeader.Width = 200;
			// 
			// criticalHeader
			// 
			this.criticalHeader.Text = "!";
			this.criticalHeader.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.criticalHeader.Width = 20;
			// 
			// listView
			// 
			this.listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
						this.criticalHeader,
						this.levelHeader,
						this.certaintyHeader,
						this.ruleHeader,
						this.itemHeader});
			this.listView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listView.FullRowSelect = true;
			this.listView.HideSelection = false;
			this.listView.Location = new System.Drawing.Point(0, 0);
			this.listView.MultiSelect = false;
			this.listView.Name = "listView";
			this.listView.Size = new System.Drawing.Size(572, 396);
			this.listView.TabIndex = 3;
			this.listView.View = System.Windows.Forms.View.Details;
			this.listView.ItemActivate += new System.EventHandler(this.ListViewItemActivate);
			this.listView.SelectedIndexChanged += new System.EventHandler(this.ListViewSelectedIndexChanged);
			// 
			// ResultListControl
			// 
			this.Controls.Add(this.listView);
			this.Name = "ResultListControl";
			this.Size = new System.Drawing.Size(572, 396);
			this.ResumeLayout(false);
		}
		#endregion
		void ListViewSelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (resultDetailsView != null && listView.SelectedItems.Count > 0) {
				resultDetailsView.ViewResolution((Resolution)listView.SelectedItems[0].Tag);
			}
			listView.Focus();
		}
		void ListViewItemActivate(object sender, System.EventArgs e)
		{
			ListViewItem item  = listView.SelectedItems[0];
			if (item != null && item.Tag != null) {
				Resolution res = (Resolution)item.Tag;
				IParserService parserService = (IParserService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IParserService));
				Position position = parserService.GetPosition(res.Item.Replace('+', '.'));
				
				if (position != null && position.Cu != null) {
					IFileService fileService = (IFileService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IFileService));
					Console.WriteLine("File name : " + position.Cu.FileName);
					fileService.JumpToFilePosition(position.Cu.FileName, Math.Max(0, position.Line - 1), Math.Max(0, position.Column - 1));
				}
			}
		}
		
	}
}
