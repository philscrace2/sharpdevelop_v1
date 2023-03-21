// created on 11/11/2003 at 11:20

using  System;
using  System.Collections;

using ICSharpCode.Core.Services;
using ICSharpCode.SharpDevelop.Services;

namespace SharpQuery.Collections {
	
	///<summary>List dictionnary.
	/// <param name='key'> this a string is defining the key</param>
	/// <param name='value'> this a  <see cref=".SharpQuerySchemaClassCollection"></see> is defining the value</param>
	/// </summary>

	public class SharpQueryListDictionary : DictionaryBase  {
	
	readonly StringParserService stringParserService = (StringParserService)ServiceManager.Services.GetService(typeof(StringParserService));
	
	public SharpQuerySchemaClassCollection this[ string key ]  {
	      get  {
	         return( (SharpQuerySchemaClassCollection) Dictionary[key] );
	      }
	      set  {
	         Dictionary[key] = value;
	      }
	   }
	
	   public ICollection Keys  {
	      get  {
	         return( Dictionary.Keys );
	      }
	   }
	
	   public ICollection Values  {
	      get  {
	         return( Dictionary.Values );
	      }
	   }
	
	   public void Add( string key, SharpQuerySchemaClassCollection value )  {
	      Dictionary.Add( key, value );
	   }
	
	   public bool Contains( string key )  {
	      return( Dictionary.Contains( key ) );
	   }
	
	   public void Remove( string key )  {
	      Dictionary.Remove( key );
	   }
	
	   protected override void OnInsert( object key, object value )  {
	      StringParserService stringParserService = (StringParserService)ServiceManager.Services.GetService(typeof(StringParserService));
	      if ( !(key is string) )
	         throw new ArgumentException( stringParserService.Parse("${res:SharpQuery.Error.WrongKeyType}"), "key" );
	
	      if ( !(value is SharpQuerySchemaClassCollection) )
	         throw new ArgumentException( stringParserService.Parse("${res:SharpQuery.Error.WrongValueType}"), "value" );
	   }
	
	   protected override void OnRemove( object key, object value )  {
		StringParserService stringParserService = (StringParserService)ServiceManager.Services.GetService(typeof(StringParserService));
	      if ( !(key is string) )
	         throw new ArgumentException( stringParserService.Parse("${res:SharpQuery.Error.WrongKeyType}"), "key" );
	      }
	
	   protected override void OnSet( object key, object oldValue, object newValue )  {
		  StringParserService stringParserService = (StringParserService)ServiceManager.Services.GetService(typeof(StringParserService));	 
	 	  if (!(key is string) )
	         throw new ArgumentException( stringParserService.Parse("${res:SharpQuery.Error.WrongKeyType}"), "key" );
	
	      if ( !(newValue is SharpQuerySchemaClassCollection) )
	         throw new ArgumentException(  stringParserService.Parse("${res:SharpQuery.Error.WrongValueType}"), "newValue" );
	   }
	
	   protected override void OnValidate( object key, object value )  {
		  StringParserService stringParserService = (StringParserService)ServiceManager.Services.GetService(typeof(StringParserService));	   	
	      if ( !(key is string) )
	         throw new ArgumentException( stringParserService.Parse("${res:SharpQuery.Error.WrongKeyType}"), "key" );
	   	
	      if ( !(value is SharpQuerySchemaClassCollection) )
	         throw new ArgumentException(  stringParserService.Parse("${res:SharpQuery.Error.WrongValueType}"), "value" );
	   }
	
	}
}
