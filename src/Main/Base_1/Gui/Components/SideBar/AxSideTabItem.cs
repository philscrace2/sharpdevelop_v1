// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections;

using ICSharpCode.Core.Properties;

namespace ICSharpCode.SharpDevelop.Gui.Components
{
	public enum SideTabItemStatus {
		Normal,
		Selected,
		Choosed,
		Drag
	}
	
	public class AxSideTabItem
	{
		string name;
		object tag;
		SideTabItemStatus sideTabItemStatus;
		Bitmap icon;
		
		public Bitmap Icon {
			get {
				return icon;
			} //
			set {
				icon = value;
			}
		}
		
		public SideTabItemStatus SideTabItemStatus {
			get {
				return sideTabItemStatus;
			}
			set {
				sideTabItemStatus = value;
			}
		}
		
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		
		public object Tag {
			get {
				return tag;
			}
			set {
				tag = value;
			}
		}
		
		public AxSideTabItem(string name)
		{
			int idx = name.IndexOf("\n");
			if (idx > 0) {
				this.name = name.Substring(0, idx);
			} else {
				this.name = name;
			}			
		}
		
		public AxSideTabItem(string name, object tag) : this(name)
		{
			this.tag = tag;
		}
		
		public AxSideTabItem(string name, object tag, Bitmap icon) : this(name, tag)
		{
			this.icon = new Bitmap(icon);
		}
		
		public AxSideTabItem Clone()
		{
			return (AxSideTabItem)MemberwiseClone();
		}
		
		public virtual void DrawItem(Graphics g, Font f, Rectangle rectangle)
		{
			int width = 0;
			switch (sideTabItemStatus) {
				case SideTabItemStatus.Normal:
					if (Icon != null) {
						g.DrawImage(Icon, 0, rectangle.Y);
						width = Icon.Width;
					}
					g.DrawString(name, f, SystemBrushes.ControlText, new PointF(rectangle.X + width + 1, rectangle.Y + 1));
					break;
				case SideTabItemStatus.Drag:
					ControlPaint.DrawBorder3D(g, rectangle, Border3DStyle.RaisedInner);
					rectangle.X += 1;
					rectangle.Y += 1;
					rectangle.Width  -= 2;
					rectangle.Height -= 2;
					
					g.FillRectangle(SystemBrushes.ControlDarkDark, rectangle);
					if (Icon != null) {
						g.DrawImage(Icon, 0, rectangle.Y);
						width = Icon.Width;
					}
					g.DrawString(name, f, SystemBrushes.HighlightText, new PointF(rectangle.X + width + 1, rectangle.Y + 1));
					break;
				case SideTabItemStatus.Selected:
					ControlPaint.DrawBorder3D(g, rectangle, Border3DStyle.RaisedInner);
					if (Icon != null) {
						g.DrawImage(Icon, 0, rectangle.Y);
						width = Icon.Width;
					}
					g.DrawString(name, f, SystemBrushes.ControlText, new PointF(rectangle.X + width + 1, rectangle.Y + 1));
					break;
				case SideTabItemStatus.Choosed:
					ControlPaint.DrawBorder3D(g, rectangle, Border3DStyle.Sunken);
					rectangle.X += 1;
					rectangle.Y += 1;
					rectangle.Width  -= 2;
					rectangle.Height -= 2;
					
					using (Brush brush = new SolidBrush(ControlPaint.Light(SystemColors.Control))) {
						g.FillRectangle(brush , rectangle);
					}
					
					if (Icon != null) {
						g.DrawImage(Icon, 1, rectangle.Y + 1);
						width = Icon.Width;
					}
					g.DrawString(name, f, SystemBrushes.ControlText, new PointF(rectangle.X + width + 2, rectangle.Y + 2));
					break;
			}
		}
	}
}
