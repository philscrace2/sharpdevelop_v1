using System;

using ICSharpCode.Core.Services;
using ICSharpCode.SharpDevelop.Services;

using SharpQuery.SchemaClass;

namespace SharpQuery.Exceptions
{
	public class ConnectionStringException : Exception
	{				
		public ConnectionStringException( ) : base( ((StringParserService)ServiceManager.Services.GetService(typeof(StringParserService))).Parse("${res:SharpQuery.Error.WrongConnectionString}") )
		{			
		}
		
		public ConnectionStringException( ISchemaClass schema ) : base( ((StringParserService)ServiceManager.Services.GetService(typeof(StringParserService))).Parse("${res:SharpQuery.Error.WrongConnectionString}") 
		                                                               + "\n\r"
		                                                               + "-----------------"
		                                                               + "\n\r"
		                                                               + "(" + schema.Connection.ConnectionString + ")"
		                                                               + "\n\r"
		                                                               + "(" + schema.Connection.Name + ")"
		                                                               )
		{
		}		
		
		public ConnectionStringException( string message ) : base( ((StringParserService)ServiceManager.Services.GetService(typeof(StringParserService))).Parse("${res:SharpQuery.Error.WrongConnectionString}") 
		                                                               + "\n\r"
		                                                               + "-----------------"
		                                                               + "\n\r"
		                                                               + message )
		{
		}					
	}
	
}
