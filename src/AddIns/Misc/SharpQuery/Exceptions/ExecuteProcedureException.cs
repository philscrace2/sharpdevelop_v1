using System;

using ICSharpCode.Core.Services;
using ICSharpCode.SharpDevelop.Services;

using SharpQuery.SchemaClass;

namespace SharpQuery.Exceptions
{
	public class ExecuteProcedureException : Exception
	{								
		public ExecuteProcedureException( ) : base( ((StringParserService)ServiceManager.Services.GetService(typeof(StringParserService))).Parse("${res:SharpQuery.Error.ProcedureExecution}") )
		{			
		}
		
		public ExecuteProcedureException( ISchemaClass schema ) : base( ((StringParserService)ServiceManager.Services.GetService(typeof(StringParserService))).Parse("${res:SharpQuery.Error.ProcedureExecution}") 
		                                                               + "\n\r"
		                                                               + "-----------------"
		                                                               + "\n\r"
		                                                               + "(" + schema.Connection.ConnectionString + ")"
		                                                               + "\n\r"
		                                                               + "(" + schema.Connection.Name + ")"
		                                                               )
		{
		}		
		
		public ExecuteProcedureException( string message ) : base( ((StringParserService)ServiceManager.Services.GetService(typeof(StringParserService))).Parse("${res:SharpQuery.Error.ProcedureExecution}") 
		                                                               + "\n\r"
		                                                               + "-----------------"
		                                                               + "\n\r"
		                                                               + message )			
       {		                                                               	
       }
	}
	
}
