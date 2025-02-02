using System;
using System.Windows.Forms;

using ICSharpCode.Core.AddIns.Codons;
using SharpQuery.Gui.TreeView;

namespace SharpQuery.Commands
{
	public class SharpQueryRefreshCommand : AbstractSharpQueryCommand
	{	
		public override bool IsEnabled
		{
			get
			{				
				return base.IsEnabled && (this.sharpQueryNode  is AbstractSharpQueryNode)
									  && (this.sharpQueryNode  as AbstractSharpQueryNode).Connection != null
									  && (this.sharpQueryNode  as AbstractSharpQueryNode).Connection.IsOpen == true;
			}			
			set{}
		}
		/// <summary>
		/// Refresh the selected <see cref="SharpQuery.Gui.TreeView.ISharpQueryNode">node</see> of the <see cref="SharpQuery.Gui.TreeView.SharpQueryTree"> SharpQuery Tree.</see>
		/// </summary>
		public override void Run()
		{
			( this.sharpQueryNode as ISharpQueryNode).Refresh();
		}
	}

	/// <summary>
	/// Add a connection to a database server into the <see cref="SharpQuery.Gui.TreeView.SharpQueryTree"></see>
	/// </summary>
	public class SharpQueryAddconnectionCommand : AbstractSharpQueryCommand
	{			
		
		public override bool IsEnabled
		{
			get
			{				
				return base.IsEnabled && (this.sharpQueryNode  is SharpQueryNodeDatabaseRoot);				
			}			
			set{}
		}		
		
		/// <summary>
		/// Add a connection to a database server into the <see cref="SharpQuery.Gui.TreeView.SharpQueryTree"></see>
		/// </summary>
		public override void Run()
		{
			( this.sharpQueryNode as SharpQueryNodeDatabaseRoot).BuildsChilds();
		}
	}
	
	/// <summary>
	/// Remove a connection from a database server into the <see cref="SharpQuery.Gui.TreeView.SharpQueryTree"></see>
	/// </summary>	
	public class SharpQueryRemoveConnectionCommand : AbstractSharpQueryCommand
	{			
		public override bool IsEnabled
		{
			get
			{				
				return base.IsEnabled && (this.sharpQueryNode  is SharpQueryNodeConnection);				
			}			
			set{}
		}		
		
		/// <summary>
		/// Remove a connection from a database server into the <see cref="SharpQuery.Gui.TreeView.SharpQueryTree"></see>
		/// </summary>
		public override void Run()
		{
			( this.sharpQueryNode as SharpQueryNodeConnection).RemoveConnection();
		}
	}	
	
	
	/// <summary>
	/// Remove a connection from a database server into the <see cref="SharpQuery.Gui.TreeView.SharpQueryTree"></see>
	/// </summary>	
	public class SharpQueryModifyConnectionCommand : AbstractSharpQueryCommand
	{			
		
		public override bool IsEnabled
		{
			get
			{				
				return base.IsEnabled && (this.sharpQueryNode  is SharpQueryNodeConnection);				
			}			
			set{}
		}
		
		/// <summary>
		/// Remove a connection from a database server into the <see cref="SharpQuery.Gui.TreeView.SharpQueryTree"></see>
		/// </summary>
		public override void Run()
		{
			( this.sharpQueryNode as SharpQueryNodeConnection).ModifyConnection();
		}
	}	
		
	/// <summary>
	/// Disconnect From a database server
	/// </summary>		
	public class SharpQueryDisconnectCommand : AbstractSharpQueryCommand	
	{

		public override bool IsEnabled
		{
			get
			{				
				return base.IsEnabled && (this.sharpQueryNode  is SharpQueryNodeConnection)
									  && (this.sharpQueryNode  as SharpQueryNodeConnection).IsConnected == true;
			}			
			set{}
		}
		
		public SharpQueryDisconnectCommand() : base()
		{
		}
		
		/// <summary>
		/// Disconnect From a database server
		/// </summary>
		public override void Run()
		{
			(this.sharpQueryNode as SharpQueryNodeConnection).Disconnect();
		}
	}
	
	/// <summary>
	/// Disconnect From a database server
	/// </summary>		
	public class SharpQueryConnectCommand : AbstractSharpQueryCommand	
	{

		public override bool IsEnabled
		{
			get
			{				
				return base.IsEnabled && (this.sharpQueryNode  is SharpQueryNodeConnection)
									  && (this.sharpQueryNode  as SharpQueryNodeConnection).IsConnected == false;
			}			
			set{}
		}
		
		public SharpQueryConnectCommand() : base()
		{
		}
		
		/// <summary>
		/// Disconnect From a database server
		/// </summary>
		public override void Run()
		{
			(this.sharpQueryNode as SharpQueryNodeConnection).Connect();
		}
	}
	
	
	/// <summary>
	/// Disconnect From a database server
	/// </summary>		
	public class SharpQueryExecuteCommand : AbstractSharpQueryCommand	
	{		
		public SharpQueryExecuteCommand() : base()
		{
		}
		
		/// <summary>
		/// Disconnect From a database server
		/// </summary>
		public override void Run()
		{
			this.sharpQueryNode.Execute(0);
		}
	}
	
	
}
