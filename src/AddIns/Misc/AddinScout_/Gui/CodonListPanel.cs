using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

using ICSharpCode.Core.Services;
using ICSharpCode.SharpDevelop.Services;
using ICSharpCode.Core.AddIns;
using ICSharpCode.Core.AddIns.Codons;
using ICSharpCode.Core.AddIns.Conditions;

namespace AddInScout
{
	public class CodonListPanel : Panel
	{
		ListView CodonLV   = new ListView();    // show codin details
		Label ExtLabel     = new Label();	    // show extension name
		AddIn currentAddIn = null;
		
		public AddIn CurrentAddIn {
			get {
				return currentAddIn;
			}
			set {
				currentAddIn = value;
				this.OnCurrentAddinChanged(EventArgs.Empty);
			}
		}
		
		public CodonListPanel()
		{
			CodonLV.Dock = DockStyle.Fill;
			CodonLV.GridLines = true;
			CodonLV.View = View.Details;
			CodonLV.FullRowSelect = true;
			CodonLV.MultiSelect = false;
			CodonLV.BorderStyle = BorderStyle.FixedSingle;
			CodonLV.SelectedIndexChanged += new EventHandler(CodonLVSelectedIndexChanged);
			CodonLV.Columns.Add("Codon", 100,HorizontalAlignment.Left);
			CodonLV.Columns.Add("Codon ID", 175,HorizontalAlignment.Left);
			CodonLV.Columns.Add("Codon Class", 400,HorizontalAlignment.Left);
			CodonLV.Columns.Add("Codon Condition -> Action on Fail", 600,HorizontalAlignment.Left);
			
			
			ExtLabel.Text = "Extension : ";
			ExtLabel.Dock = DockStyle.Top;
			ExtLabel.FlatStyle = FlatStyle.Flat;
			ExtLabel.TextAlign = ContentAlignment.MiddleLeft;
			ExtLabel.BorderStyle = BorderStyle.FixedSingle;
			
			Controls.Add(CodonLV);
			Controls.Add(ExtLabel);
		}
		
		
		void CodonLVSelectedIndexChanged(object sender, EventArgs e)
		{
			if (CodonLV.SelectedItems.Count != 1) {
				return;
			}
			ICodon c = CodonLV.SelectedItems[0].Tag as ICodon;
			if (c == null) {
				return;
			}
			
			CurrentAddIn = c.AddIn;
		}
		
		public void ClearList()
		{
			ExtLabel.Text = "Extension : ";
			CodonLV.Items.Clear();
		}
		
		public void ListCodons(string path)
		{
			CodonLV.Items.Clear();
			if (path == null) {
				ExtLabel.Text = "Extension : ";
				return;
			}
			
			ExtLabel.Text = "Extension : " + path;
			
//			Hashtable CondTbl = ext.Conditions;
			
			IAddInTreeNode node = ICSharpCode.Core.AddIns.AddInTreeSingleton.AddInTree.GetTreeNode(path);
			foreach (IAddInTreeNode childNode in node.ChildNodes.Values) {
				ICodon c = childNode.Codon;
				if (c == null) {
					continue;
				}
				ListViewItem lvi = new ListViewItem(c.Name);
				lvi.Tag = c;
				lvi.SubItems.Add(c.ID);
				lvi.SubItems.Add(c.Class);
				
//				ConditionCollection cc = (ConditionCollection) CondTbl[c.ID];
//				if (cc != null) {
//					string ccs = "";
//					string ccs0 = "";
//					for (int i = 0; i < cc.Count; ++i) {
//						ccs0 = cc[i].ToString() + " -> " + cc[i].Action;
//						if (i == 0) {
//							ccs=ccs0;
//						} else {
//							ccs = ccs + " , " + ccs0;
//						}
//					}
//					if (ccs != "") {
//						lvi.SubItems.Add(ccs);
//					}
//				}
				CodonLV.Items.Add(lvi);
			}
		}
		
		public void ListCodons(AddIn.Extension ext)
		{
			CodonLV.Items.Clear();
			if (ext == null) {
				ExtLabel.Text = "Extension : ";
				return;
			}
			
			ExtLabel.Text = "Extension : " + ext.Path;
			
			Hashtable CondTbl = ext.Conditions;
			
			foreach (ICodon c in ext.CodonCollection) {
				ListViewItem lvi = new ListViewItem(c.Name);
				lvi.SubItems.Add(c.ID);
				lvi.SubItems.Add(c.Class);
				ConditionCollection cc = (ConditionCollection) CondTbl[c.ID];
				if (cc != null) {
					string ccs = "";
					string ccs0 = "";
					for (int i = 0; i < cc.Count; ++i) {
						ccs0 = cc[i].ToString() + " -> " + cc[i].Action;
						if (i == 0) {
							ccs=ccs0;
						} else {
							ccs = ccs + " , " + ccs0;
						}
					}
					if (ccs != "") {
						lvi.SubItems.Add(ccs);
					}
				}
				CodonLV.Items.Add(lvi);
			}
		}
		
		protected virtual void OnCurrentAddinChanged(EventArgs e)
		{
			if (CurrentAddinChanged != null) {
				CurrentAddinChanged(this, e);
			}
		}
		
		public event EventHandler CurrentAddinChanged;
	}
}
	
