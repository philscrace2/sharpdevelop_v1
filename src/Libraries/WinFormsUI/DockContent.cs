// *****************************************************************************
// 
//  Copyright 2004, Weifen Luo
//  All rights reserved. The software and associated documentation 
//  supplied hereunder are the proprietary information of Weifen Luo
//  and are supplied subject to licence terms.
// 
//  WinFormsUI Library Version 1.0
// *****************************************************************************

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace WeifenLuo.WinFormsUI
{
	/// <summary>
	/// Each DockContent instance represents a single dockable unit within the docking window framework.
	/// </summary>
	public class DockContent : Form
	{
		// Tab width and X position used by DockPane and DockPanel class
		internal int TabWidth = 0;
		internal int TabX = 0;

		/// <summary>
		/// Initialize a new DockContent instance.
		/// </summary>
		public DockContent()
		{
			RefreshMdiIntegration();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (m_hiddenMdiChild != null)
				{
					m_hiddenMdiChild.Close();
					m_hiddenMdiChild = null;
				}

				DockPanel = null;
			}

			base.Dispose(disposing);
		}

		private bool m_allowRedocking = true;
		/// <summary>
		/// This property determines if drag and drop re-docking is allowed.
		/// </summary>
		[LocalizedCategory("Category.Docking")]
		[LocalizedDescription("DockContent.AllowRedocking.Description")]
		[DefaultValue(true)]
		public bool AllowRedocking
		{
			get	{	return m_allowRedocking;	}
			set	{	m_allowRedocking = value;	}
		}

		private DockAreas m_allowedAreas = DockAreas.DockLeft | DockAreas.DockRight | DockAreas.DockTop | DockAreas.DockBottom | DockAreas.Document | DockAreas.Float;
		/// <summary>
		/// This property determines the areas this DockContent can be displayed.
		/// </summary>
		[LocalizedCategory("Category.Docking")]
		[LocalizedDescription("DockContent.DockableAreas.Description")]
		[DefaultValue(DockAreas.DockLeft|DockAreas.DockRight|DockAreas.DockTop|DockAreas.DockBottom|DockAreas.Document|DockAreas.Float)]
		public DockAreas DockableAreas
		{
			get	{	return m_allowedAreas;	}
			set
			{
				if (m_allowedAreas == value)
					return;

				if (!DockHelper.IsDockStateValid(DockState, value))
					throw(new InvalidOperationException(ResourceHelper.GetString("DockContent.DockableAreas.InvalidValue")));

				m_allowedAreas = value;

				if (!DockHelper.IsDockStateValid(ShowHint, m_allowedAreas))
					ShowHint = DockState.Unknown;
			}
		}

		private double m_autoHidePortion = 0.25;
		/// <summary>
		/// This property determines the portion of the screen size when showing in auto-hide mode.
		/// </summary>
		[LocalizedCategory("Category.Docking")]
		[LocalizedDescription("DockContent.AutoHidePortion.Description")]
		[DefaultValue(0.25)]
		public double AutoHidePortion
		{
			get	{	return m_autoHidePortion;	}
			set
			{
				if (value <= 0 || value > 1)
					throw(new ArgumentOutOfRangeException(ResourceHelper.GetString("DockContent.AutoHidePortion.OutOfRange")));

				if (m_autoHidePortion == value)
					return;

				m_autoHidePortion = value;

				if (DockPanel == null)
					return;

				if (DockPanel.ActiveAutoHideContent == this)
					DockPanel.PerformLayout();
			}
		}

		private string m_tabText = null;
		/// <summary>
		/// This property determines the tab text displayed for the dock pane caption. By setting value to this property can display different text for dock pane tab and caption.
		/// </summary>
		[LocalizedCategory("Category.Docking")]
		[LocalizedDescription("DockContent.TabText.Description")]
		[DefaultValue(null)]
		public string TabText
		{
			get	{	return DesignMode ? m_tabText : (m_tabText==null ? this.Text : m_tabText);	}
			set
			{
				if (m_tabText == value)
					return;

				m_tabText = value;
				if (Pane != null)
					Pane.Invalidate();
			}
		}
		private bool ShouldSerializeTabText()
		{
			return (m_tabText != null);
		}

		private bool m_closeButton = true;
		/// <summary>
		/// This property determines whether this DockContent can be closed by clicking the close button of its dock pane.
		/// </summary>
		[LocalizedCategory("Category.Docking")]
		[LocalizedDescription("DockContent.CloseButton.Description")]
		[DefaultValue(true)]
		public bool CloseButton
		{
			get	{	return m_closeButton;	}
			set
			{
				if (m_closeButton == value)
					return;

				m_closeButton = value;
				if (Pane != null)
					if (Pane.ActiveContent == this)
						Pane.Invalidate();
			}
		}
		
		private DockPane m_pane = null;
		/// <summary>
		/// This property determines the dock pane which contains this dock content.
		/// </summary>
		[Browsable(false)]
		public DockPane Pane
		{
			get {	return m_pane; }
			set
			{
				if (m_pane == value)
					return;

				DockPane oldValue = m_pane;
				m_pane = value;

				if (oldValue != null)
					oldValue.RemoveContent(this);

				if (value != null)
					value.AddContent(this);

				SetDockState();
			}
		}

		private DockPanel m_dockPanel = null;
		/// <summary>
		/// This property determines the dock panel which this dock content attached to.
		/// </summary>
		[Browsable(false)]
		public DockPanel DockPanel
		{
			get { return m_dockPanel; }
			set
			{
				if (m_dockPanel == value)
					return;

				Pane = null;

				if (m_dockPanel != null)
					m_dockPanel.RemoveContent(this);

				m_dockPanel = value;

				if (m_dockPanel != null)
				{
					m_dockPanel.AddContent(this);
					TopLevel = false;
					FormBorderStyle = FormBorderStyle.None;
					ShowInTaskbar = false;
					Visible = true;
				}

				RefreshMdiIntegration();
			}
		}

		private DockState DefaultShowState
		{
			get
			{
				if (ShowHint != DockState.Unknown)
					return ShowHint;

				if ((DockableAreas & DockAreas.Document) != 0)
					return DockState.Document;
				if ((DockableAreas & DockAreas.DockRight) != 0)
					return DockState.DockRight;
				if ((DockableAreas & DockAreas.DockLeft) != 0)
					return DockState.DockLeft;
				if ((DockableAreas & DockAreas.DockBottom) != 0)
					return DockState.DockBottom;
				if ((DockableAreas & DockAreas.DockTop) != 0)
					return DockState.DockTop;
				if ((DockableAreas & DockAreas.Float) != 0)
					return DockState.Float;

				return DockState.Unknown;
			}
		}

		/// <summary>
		/// This property retrieves this contentís docking state. This value is identical to Pane.DockState property
		/// </summary>
		private DockState m_dockState = DockState.Unknown;
		[Browsable(false)]
		public DockState DockState
		{
			get	{	return m_dockState;	}
			set
			{
				if (m_dockState == value)
					return;

				if (value == DockState.Hidden || value == DockState.Unknown)
					throw new InvalidOperationException(ResourceHelper.GetString("DockContent.DockState.InvalidState"));
				if (DockPanel == null)
					throw new InvalidOperationException(ResourceHelper.GetString("DockContent.DockState.NullPanel"));

				if (value == DockState.Hidden)
				{
					IsHidden = true;
					return;
				}

				if (!DockHelper.IsDockStateValid(value, DockableAreas))
					throw new InvalidOperationException(ResourceHelper.GetString("DockContent.DockState.InvalidState"));

				if (Pane.Contents.Count == 1)
					Pane.DockState = value;
				else
					Pane = DockPanel.DockPaneFactory.CreateDockPane(this, value, false);

				if (IsHidden)
					IsHidden = true;
			}
		}
		private DockState GetDockState()
		{
			if (Pane == null)
				return DockState.Unknown;
			else if (IsHidden)
				return DockState.Hidden;
			else
				return Pane.DockState;
		}
		internal void SetDockState()
		{
			DockState value = GetDockState();
			if (DockState == value)
				return;

			m_dockState = value;
			RefreshMdiIntegration();
			OnDockStateChanged(EventArgs.Empty);
		}

		internal string PersistString
		{
			get	{	return GetPersistString();	}
		}
		protected virtual string GetPersistString()
		{
			return GetType().ToString();
		}

		private HiddenMdiChild m_hiddenMdiChild = null;
		internal HiddenMdiChild HiddenMdiChild
		{
			get	{	return m_hiddenMdiChild;	}
		}

		private bool m_hideOnClose = false;
		/// <summary>
		/// HideOnClose Property
		/// </summary>
		[LocalizedCategory("Category.Docking")]
		[LocalizedDescription("DockContent.HideOnClose.Description")]
		[DefaultValue(false)]
		public bool HideOnClose
		{
			get	{	return m_hideOnClose;	}
			set	{	m_hideOnClose = value;	}
		}

		public new MainMenu Menu
		{
			get	{	return HiddenMdiChild == null ? base.Menu : HiddenMdiChild.Menu;	}
			set
			{
				if (HiddenMdiChild == null)
					base.Menu = value;
				else
					HiddenMdiChild.Menu = value;
			}
		}

		/// <summary>
		/// ShowHint Property
		/// </summary>
		private DockState m_showHint = DockState.Unknown;
		[LocalizedCategory("Category.Docking")]
		[LocalizedDescription("DockContent.ShowHint.Description")]
		[DefaultValue(DockState.Unknown)]
		public DockState ShowHint
		{
			get	{	return m_showHint;	}
			set
			{	
				if (!DockHelper.IsDockStateValid(value, DockableAreas))
					throw (new InvalidOperationException(ResourceHelper.GetString("DockContent.ShowHint.InvalidValue")));

				if (m_showHint == value)
					return;

				m_showHint = value;
			}
		}

		private bool m_isActivated = false;
		[Browsable(false)]
		public bool IsActivated
		{
			get	{	return m_isActivated;	}
		}
		internal void SetIsActivated(bool value)
		{
			if (m_isActivated == value)
				return;

			m_isActivated = value;
		}

		private bool m_isHidden = false;
		[Browsable(false)]
		public bool IsHidden
		{
			get	{	return m_isHidden;	}
			set
			{
				if (m_isHidden == value)
					return;

				m_isHidden = value;
				Visible = !IsHidden;
				if (Pane != null)
				{
					Pane.SetDockState();
					SetDockState();
					Pane.ValidateActiveContent();
					Pane.Invalidate();
				}
				
				if (HiddenMdiChild != null)
					HiddenMdiChild.Visible = (!IsHidden);
			}
		}

		public bool IsDockStateValid(DockState dockState)
		{
			return DockHelper.IsDockStateValid(dockState, DockableAreas);
		}

		private ContextMenu m_tabPageContextMenu = null;
		[LocalizedCategory("Category.Docking")]
		[LocalizedDescription("DockContent.TabPageContextMenu.Description")]
		[DefaultValue(null)]
		public ContextMenu TabPageContextMenu
		{
			get	{	return m_tabPageContextMenu;	}
			set	{	m_tabPageContextMenu = value;	}
		}

		private string m_toolTipText = null;
		[Category("Appearance")]
		[LocalizedDescription("DockContent.ToolTipText.Description")]
		[DefaultValue(null)]
		public string ToolTipText
		{
			get	{	return m_toolTipText;	}
			set {	m_toolTipText = value;	}
		}

		public new void Activate()
		{
			if (DockPanel == null)
				base.Activate();
			else if (Pane == null)
				Show(DockPanel);
			else
			{
				IsHidden = false;
				Pane.ActiveContent = this;
				Pane.Activate();
			}
		}

		public new void Hide()
		{
			IsHidden = true;
		}

		internal void SetParent(Control value)
		{
			if (Parent == value)
				return;

			Control oldParent = Parent;

			///!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
			/// Workaround for .Net Framework bug: removing control from Form may cause form
			/// unclosable. Set focus to another dummy control.
			///!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
			Form form = FindForm();
			if (ContainsFocus)
			{
				if (form is FloatWindow)
					((FloatWindow)form).DummyControl.Focus();
				else if (DockPanel != null)
					DockPanel.DummyControl.Focus();
			}
			//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

			Parent = value;
		}

		public new void Show()
		{
			if (DockPanel == null)
				base.Show();
			else
				Show(DockPanel);
		}

		public void Show(DockPanel dockPanel)
		{
			if (dockPanel == null)
				throw(new ArgumentNullException(ResourceHelper.GetString("DockContent.Show.NullDockPanel")));

			if (DockState == DockState.Unknown)
				Show(dockPanel, DefaultShowState);
			else			
				Activate();
		}

		public void Show(DockPanel dockPanel, DockState dockState)
		{
			if (dockPanel == null)
				throw(new ArgumentNullException(ResourceHelper.GetString("DockContent.Show.NullDockPanel")));

			if (dockState == DockState.Unknown || dockState == DockState.Hidden)
				throw(new ArgumentException(ResourceHelper.GetString("DockContent.Show.InvalidDockState")));

			DockPanel = dockPanel;

			if (Pane == null)
			{
				DockPane paneExisting = null;
				foreach (DockPane pane in DockPanel.Panes)
					if (pane.DockState == dockState)
					{
						paneExisting = pane;
						break;
					}

				if (paneExisting == null || dockState == DockState.Float)
					Pane = DockPanel.DockPaneFactory.CreateDockPane(this, dockState, false);
				else
					Pane = paneExisting;
			}

			Activate();
		}

		protected override void OnTextChanged(EventArgs e)
		{
			if (m_hiddenMdiChild != null)
				m_hiddenMdiChild.Text = this.Text;
			if (DockHelper.IsDockStateAutoHide(DockState))
				DockPanel.Invalidate();
			else if (Pane != null)
			{
				if (Pane.FloatWindow != null)
					Pane.FloatWindow.SetText();
				Pane.Invalidate();
			}

			base.OnTextChanged(e);
		}

		internal void RefreshMdiIntegration()
		{
			Form mdiParent = GetMdiParentForm();

			if (mdiParent == null)
			{
				if (HiddenMdiChild != null)
				{
					m_hiddenMdiChild.Close();
					m_hiddenMdiChild = null;
				}
			}
			else
			{
				if (HiddenMdiChild == null)
					m_hiddenMdiChild = new HiddenMdiChild(this);

				m_hiddenMdiChild.SetMdiParent(mdiParent);
			}
			
			if (DockPanel != null)
				if (DockPanel.ActiveDocument != null)
					if (DockPanel.ActiveDocument.HiddenMdiChild != null)
						DockPanel.ActiveDocument.HiddenMdiChild.Activate();
		}
		private Form GetMdiParentForm()
		{
			if (DockPanel == null)
				return null;

			if (!DockPanel.MdiIntegration)
				return null;

			if (DockState != DockState.Document)
				return null;

			Form parentMdi = DockPanel.FindForm();
			if (parentMdi != null)
				if (!parentMdi.IsMdiContainer)
					parentMdi = null;

			return parentMdi;
		}

		#region Events
		private static readonly object DockStateChangedEvent = new object();
		[LocalizedCategory("Category.PropertyChanged")]
		[LocalizedDescription("Pane.DockStateChanged.Description")]
		public event EventHandler DockStateChanged
		{
			add	{	Events.AddHandler(DockStateChangedEvent, value);	}
			remove	{	Events.RemoveHandler(DockStateChangedEvent, value);	}
		}
		protected virtual void OnDockStateChanged(EventArgs e)
		{
			EventHandler handler = (EventHandler)Events[DockStateChangedEvent];
			if (handler != null)
				handler(this, e);
		}
		#endregion
	}
}
