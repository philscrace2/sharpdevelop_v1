// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Georg Brandl" email="g.brandl@gmx.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Windows.Forms;
using System.Drawing;
using System.Xml;

using ICSharpCode.TextEditor.Document;
using ICSharpCode.Core.Services;
using ICSharpCode.Core.Properties;

using ICSharpCode.SharpDevelop.Gui.Dialogs;

namespace ICSharpCode.SharpDevelop.AddIns.HighlightingEditor.Nodes
{
	class EnvironmentNode : AbstractNode
	{
		public static string[]        ColorNames;
		public string[]               ColorDescs;
		public EditorHighlightColor[] Colors;
		
		public EnvironmentNode(XmlElement el)
		{
			StringParserService stringParserService = (StringParserService)ServiceManager.Services.GetService(typeof(StringParserService));
			
			ArrayList envColors            = new ArrayList();
			ArrayList envColorNames        = new ArrayList();
			ArrayList envColorDescriptions = new ArrayList();
			if (el != null) {
				foreach (XmlNode node in el.ChildNodes) {
					if (node is XmlElement) {
						Console.WriteLine("Node : " + node);
				
						envColorNames.Add(node.Name);
						envColorDescriptions.Add("${res:Dialog.HighlightingEditor.EnvColors." + node.Name + "}");
						envColors.Add(new EditorHighlightColor((XmlElement)node));
					}
				}
			}
			EnvironmentNode.ColorNames = (string[])envColorNames.ToArray(typeof(string));
			this.ColorDescs = (string[])envColorDescriptions.ToArray(typeof(string));
			this.Colors     = (EditorHighlightColor[])envColors.ToArray(typeof(EditorHighlightColor));
			stringParserService.Parse(ref ColorDescs);
			
			Text = ResNodeName("EnvironmentColors");
		
			panel = new EnvironmentOptionPanel(this);
		}

		public override void UpdateNodeText()
		{
		}
		
		public override string ToXml()
		{
			string str = "\t<Environment>\n";
			for (int i = 0; i <= ColorNames.GetUpperBound(0); ++i) {
				str += "\t\t<" + ColorNames[i] + " " + Colors[i].ToXml() + "/>\n";
			}
			str += "\t</Environment>\n\n";
			return str;
		}
	}
	
	class EnvironmentOptionPanel : NodeOptionPanel
	{
		private System.Windows.Forms.Button button;
		private System.Windows.Forms.ListView listView;

		public EnvironmentOptionPanel(EnvironmentNode parent) : base(parent)
		{
			SetupFromXml(System.IO.Path.Combine(PropertyService.DataDirectory, 
			                          @"resources\panels\HighlightingEditor\Environment.xfrm"));
			
			button = (Button)ControlDictionary["button"];
			button.Click += new EventHandler(EditButtonClicked);
			listView  = (ListView)ControlDictionary["listView"];
			
			listView.Font = new Font(listView.Font.FontFamily, 10);
		}
		
		public override void StoreSettings()
		{
			EnvironmentNode node = (EnvironmentNode)parent;
			
			foreach (EnvironmentItem item in listView.Items) {
				node.Colors[item.arrayIndex] = item.Color;
			}
		}
		
		public override void LoadSettings()
		{
			EnvironmentNode node = (EnvironmentNode)parent;
			listView.Items.Clear();
			
			for (int i = 0; i <= EnvironmentNode.ColorNames.GetUpperBound(0); ++i) {
				listView.Items.Add(new EnvironmentItem(i, node.ColorDescs[i], node.Colors[i], listView.Font));
			}
		}
		
		void EditButtonClicked(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count != 1) return;
			
			EnvironmentItem item = (EnvironmentItem)listView.SelectedItems[0];
			using (EditHighlightingColorDialog dlg = new EditHighlightingColorDialog(item.Color)) {
				if (dlg.ShowDialog(this) == DialogResult.OK) {
					item.Color = dlg.Color;
					item.ColorUpdate();
				}
			}
		}
		
		private class EnvironmentItem : ListViewItem
		{
			public string Name;
			public EditorHighlightColor Color;
			public int arrayIndex;
			
			Font basefont;
			Font listfont;
			
			static Font ParseFont(string font)
			{
				string[] descr = font.Split(new char[]{',', '='});
				return new Font(descr[1], Single.Parse(descr[3]));
			}
			
			public EnvironmentItem(int index, string name, EditorHighlightColor color, Font listFont) : base(new string[] {name, "Sample"})
			{
				Name = name;
				Color = color;
				arrayIndex = index;
				
				this.UseItemStyleForSubItems = false;
				
				IProperties properties = ((IProperties)PropertyService.GetProperty("ICSharpCode.TextEditor.Document.Document.DefaultDocumentAggregatorProperties", new DefaultProperties()));
				basefont = ParseFont(properties.GetProperty("DefaultFont", new Font("Courier New", 10).ToString()));
				listfont = listFont;
				
				ColorUpdate();
			}
			
			public void ColorUpdate()
			{
				FontStyle fs = FontStyle.Regular;
				if (Color.Bold)   fs |= FontStyle.Bold;
				if (Color.Italic) fs |= FontStyle.Italic;
				
				this.Font = new Font(listfont.FontFamily, 8);
				
				Font font = new Font(basefont, fs);
				
				this.SubItems[1].Font = font;
				
				this.SubItems[1].ForeColor = Color.GetForeColor();
				this.SubItems[1].BackColor = Color.GetBackColor();
			}
		}
	}
}
