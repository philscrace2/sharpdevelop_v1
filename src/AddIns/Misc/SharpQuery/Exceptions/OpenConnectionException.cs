using System;

using ICSharpCode.Core.Services;
using ICSharpCode.SharpDevelop.Services;

using SharpQuery.SchemaClass;


namespace SharpQuery.Exceptions
{
	public class OpenConnectionException : Exception
	{								
		public OpenConnectionException( ) : base( ((StringParserService)ServiceManager.Services.GetService(typeof(StringParserService))).Parse("${res:SharpQuery.Error.OpenError}") )
		{			
		}
		
		public OpenConnectionException( ISchemaClass schema ) : base( ((StringParserService)ServiceManager.Services.GetService(typeof(StringParserService))).Parse("${res:SharpQuery.Error.OpenError}") 
		                                                               + "\n\r"
		                                                               + "-----------------"
		                                                               + "\n\r"
		                                                               + "(" + schema.Connection.ConnectionString + ")"
		                                                               + "\n\r"
		                                                               + "(" + schema.Connection.Name + ")"
		                                                               )
		{
		}		
		
		public OpenConnectionException( string message ) : base( ((StringParserService)ServiceManager.Services.GetService(typeof(StringParserService))).Parse("${res:SharpQuery.Error.OpenError}") 
		                                                               + "\n\r"
		                                                               + "-----------------"
		                                                               + "\n\r"
		                                                               + message )
       {		                                                               	
       }
		                                                               

	}
	
}
