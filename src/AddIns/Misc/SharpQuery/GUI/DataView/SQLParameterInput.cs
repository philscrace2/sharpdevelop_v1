using System;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;
using ICSharpCode.SharpDevelop.Gui;
using SharpQuery.SchemaClass;
using SharpQuery.Collections;
using System.ComponentModel;

using ICSharpCode.Core.Services;

using ICSharpCode.XmlForms;
using ICSharpCode.SharpDevelop.Gui.XmlForms;


namespace SharpQuery.Gui.DataView
{
	public class SQLParameterInput : XmlForm
	{
		private DataGrid _dataGrid = null;

		public DataGrid dataGrid
		{
			get
			{
				if ( this._dataGrid == null )
				{
					this._dataGrid = this.ControlDictionary["dataGrid"] as DataGrid;
				}
				return this._dataGrid;
			}
		}

		private void ResetClick( object sender, EventArgs e )
		{

		}

		protected void FillParameters( SharpQueryParameterCollection parameters )
		{
			SharpQueryParameter par = null;
			for( int i = 0; i < parameters.Count; i++)
			{
				par = parameters[i];
				if ( par.Type == ParameterDirection.ReturnValue )
				{
					i--;
					parameters.Remove( par );
				}
			}
			this.dataGrid.CaptionVisible = true;
			this.dataGrid.DataSource = parameters;
	 		this.dataGrid.DataMember = null;
	 		this.dataGrid.AllowNavigation = false;
		}

		static PropertyService propertyService = (PropertyService)ServiceManager.Services.GetService(typeof(PropertyService));
		public SQLParameterInput() : base(propertyService.DataDirectory + @"\resources\dialogs\SharpQuery\SqlParametersInput.xfrm")
		{
		}

		public SQLParameterInput( SharpQueryParameterCollection parameters ) : this()
		{
			this.FillParameters( parameters );
		}

		protected override void SetupXmlLoader()
		{
			xmlLoader.StringValueFilter    = new SharpDevelopStringValueFilter();
			xmlLoader.PropertyValueCreator = new SharpDevelopPropertyValueCreator();
		}
	}
}

