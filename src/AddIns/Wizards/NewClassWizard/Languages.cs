//
// -*- C# -*-
//
// Author:         Roman Taranchenko
// Copyright:      (c) 2004 Roman Taranchenko
// Copying Policy: GNU General Public License
//

namespace NewClassWizard
{
	using System.CodeDom.Compiler;
	using Microsoft.CSharp;
	using Microsoft.VisualBasic;
	
	public abstract class Language
	{
		public abstract ICodeGenerator GetDeclarationCodeGenerator();
		
		public abstract ICodeGenerator GetImplementationCodeGenerator();
	
		public abstract string GetDeclarationFileExt();
		
		public abstract string GetImplementationFileExt();	
		
		public abstract string GetName();
		
		public abstract new string ToString();

		// factory method		
		public static Language Create(string name)
		{
			switch (name)
			{
				case "C#":
					return new CSharp();
				case "VBNET":
					return new VisualBasic();
				case "C++.NET":
					return new CPlusPlus();
				default:
					throw new UnknownLanguageException("The language " + name + " is not supported by this wizard!");
			}
		}
		
		// common behavior
		public bool CanGenerateDeclaration()
		{
			return GetDeclarationCodeGenerator() != null;
		}
		
		public bool CanGenerateImplementation()
		{
			return GetImplementationCodeGenerator() != null;
		}
		
		public bool IsValidIdentifier(string identifier) {
			bool result = false;
			if (CanGenerateDeclaration()) {
				result = GetDeclarationCodeGenerator().IsValidIdentifier(identifier);
			}
			else if (CanGenerateImplementation()) {
				result = GetImplementationCodeGenerator().IsValidIdentifier(identifier);
			}
			return result;
		}

	}
	
	internal class CSharp : Language
	{
		public override ICodeGenerator GetDeclarationCodeGenerator()
		{
			return null;	
		}
		
		public override ICodeGenerator GetImplementationCodeGenerator()
		{
			return new CSharpCodeProvider().CreateGenerator();
		}
	
		public override string GetDeclarationFileExt()
		{
			return null;	
		}
		
		public override string GetImplementationFileExt()
		{
			return ".cs";	
		}
		
		public override string GetName()
		{
			return "C#";
		}
		
		public override string ToString()
		{
			return "C#";
		}
	}
	
	internal class VisualBasic : Language
	{
		public override ICodeGenerator GetDeclarationCodeGenerator()
		{
			return null;	
		}
		
		public override ICodeGenerator GetImplementationCodeGenerator()
		{
			return new VBCodeProvider().CreateGenerator();
		}
	
		public override string GetDeclarationFileExt()
		{
			return null;
		}
		
		public override string GetImplementationFileExt()
		{
			return ".vb";	
		}
		
		public override string GetName()
		{
			return "Visual Basic";
		}
		
		public override string ToString()
		{
			return "VBNET";
		}
		
	}

	internal class CPlusPlus : Language
	{
		public override ICodeGenerator GetDeclarationCodeGenerator()
		{
			return new CppCodeGenerator();
		}
		
		public override ICodeGenerator GetImplementationCodeGenerator()
		{
			return null;
		}
	
		public override string GetDeclarationFileExt()
		{
			return ".h";
		}
		
		public override string GetImplementationFileExt()
		{
			return null;
		}
		
		public override string GetName()
		{
			return "C++.NET";
		}
		
		public override string ToString()
		{
			return "C++.NET";
		}
	}
}
