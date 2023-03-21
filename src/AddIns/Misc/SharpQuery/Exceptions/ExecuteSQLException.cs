using System;

using ICSharpCode.Core.Services;
using ICSharpCode.SharpDevelop.Services;

using SharpQuery.SchemaClass;

namespace SharpQuery.Exceptions
{
	public class ExecuteSQLException : Exception
	{		
		
		public ExecuteSQLException( ) : base( ((StringParserService)ServiceManager.Services.GetService(typeof(StringParserService))).Parse("${res:SharpQuery.Error.SQLExecution}") )
		{			
		}
		
		public ExecuteSQLException( ISchemaClass schema ) : base( ((StringParserService)ServiceManager.Services.GetService(typeof(StringParserService))).Parse("${res:SharpQuery.Error.SQLExecution}") 
		                                                               + "\n\r"
		                                                               + "-----------------"
		                                                               + "\n\r"
		                                                               + "(" + schema.Connection.ConnectionString + ")"
		                                                               + "\n\r"
		                                                               + "(" + schema.Connection.Name + ")"
		                                                               )
		{
		}		
		
		public ExecuteSQLException( string message ) : base( ((StringParserService)ServiceManager.Services.GetService(typeof(StringParserService))).Parse("${res:SharpQuery.Error.SQLExecution}") 
		                                                               + "\n\r"
		                                                               + "-----------------"
		                                                               + "\n\r"
		                                                               + message )	
       {		                                                               	
       }
		                                                               
	}
	
}
