using System;
using System.Text;
using SharpDevelop.Internal.Parser;

namespace VBBinding.Parser
{
	/// <summary>
	/// Description of ExpressionFinder.	
	/// </summary>
	public class ExpressionFinder  : IExpressionFinder
	{
		public string FindExpression(string inText, int offset)
		{
			this.text = FilterComments(inText, ref offset);
			this.text   = text;
			this.offset = this.lastAccept = offset;
			this.state  = START;
			if (this.text == null) {
				return null;
			}
			while (state != ERROR) {
				ReadNextToken();
				//Console.WriteLine("cur state {0} got token {1}/{3} going to {2}", GetStateName(state), GetTokenName(curTokenType), GetStateName(stateTable[state, curTokenType]), curTokenType);
				state = stateTable[state, curTokenType];
				
				if (state == ACCEPT || state == ACCEPT2) {
					lastAccept = this.offset;
				}
				if (state == ACCEPTNOMORE) {
					return this.text.Substring(this.offset + 1, offset - this.offset);
				}
			}
			return this.text.Substring(this.lastAccept + 1, offset - this.lastAccept);
		}
		
		#region Comment Filter and 'inside string watcher'
		int initialOffset;
		public string FilterComments(string text, ref int offset)
		{
			this.initialOffset = offset;
			StringBuilder outText = new StringBuilder();
			int curOffset = 0;
			while (curOffset <= initialOffset) {
				char ch = text[curOffset];
				
				switch (ch) {
					case '@':
						if (curOffset + 1 < text.Length && text[curOffset + 1] == '"') {
							outText.Append(text[curOffset++]); // @
							outText.Append(text[curOffset++]); // "
							if (!ReadVerbatimString(outText, text, ref curOffset)) {
								return null;
							}
						}else{
							outText.Append(ch);
							++curOffset;
						}
						break;
					case '"':
						outText.Append(ch);
						curOffset++;
						if (!ReadString(outText, text, ref curOffset)) {
							return null;
						}
						break;
					case '\'':
						offset    -= 1;
						curOffset += 1;
						if (!ReadToEOL(text, ref curOffset, ref offset)) {
							return null;
						}
						break;
					default:
						outText.Append(ch);
						++curOffset;
						break;
				}
			}
			
			return outText.ToString();
		}
		
		bool ReadToEOL(string text, ref int curOffset, ref int offset)
		{
			while (curOffset <= initialOffset) {
				char ch = text[curOffset++];
				--offset;
				if (ch == '\n') {
					return true;
				}
			}
			return false;
		}
		
		bool ReadString(StringBuilder outText, string text, ref int curOffset)
		{
			while (curOffset <= initialOffset) {
				char ch = text[curOffset++];
				outText.Append(ch);
				if (ch == '"') {
					return true;
				}
			}
			return false;
		}
		
		bool ReadVerbatimString(StringBuilder outText, string text, ref int curOffset)
		{
			while (curOffset <= initialOffset) {
				char ch = text[curOffset++];
				outText.Append(ch);
				if (ch == '"') {
					if (curOffset < text.Length && text[curOffset] == '"') {
						outText.Append(text[curOffset++]);
					} else {
						return true;
					}
				}
			}
			return false;
		}
		
		bool ReadMultiLineComment(string text, ref int curOffset, ref int offset)
		{
			while (curOffset <= initialOffset) {
				char ch = text[curOffset++];
				--offset;
				if (ch == '*') {
					if (curOffset < text.Length && text[curOffset] == '/') {
						++curOffset;
						--offset;
						return true;
					}
				}
			}
			return false;
		}
		#endregion
		
		#region mini backward lexer
		string text;
		int    offset;
		
		char GetNext()
		{
			if (offset >= 0) {
				return text[offset--];
			}
			return '\0';
		}
		
		char Peek()
		{
			if (offset >= 0) {
				return text[offset];
			}
			return '\0';
		}
		
		void UnGet()
		{
			++offset;
		}
		
		// tokens for our lexer
		static int Err     = 0;
		static int Dot     = 1;
		static int StrLit  = 2;
		static int Ident   = 3;
		static int New     = 4;
//		static int Bracket = 5;
		static int Parent  = 6;
		static int Curly   = 7;
		static int Using   = 8;
		int curTokenType;
		
		readonly static string[] tokenStateName = new string[] {
			"Err", "Dot", "StrLit", "Ident", "New", "Bracket", "Paren", "Curly", "Using"
		};
		string GetTokenName(int state)
		{
			return tokenStateName[state];
		}
		
		void ReadNextToken()
		{
			char ch = GetNext();
				
			curTokenType = Err;
			if (ch == '\0' || ch == '\n' || ch == '\r') {
				return;
			}
			while (Char.IsWhiteSpace(ch)) {
				ch = GetNext();
				if (ch == '\n' || ch == '\r') {
					return;
				}
			}
			
			switch (ch) {
				case '}':
					if (ReadBracket('{', '}')) {
						curTokenType = Curly;
					}
					break;
				case ')':
					if (ReadBracket('(', ')')) {
						curTokenType = Parent;
					}
					break;
				case ']':
					if (ReadBracket('[', ']')) {
						curTokenType = Ident;
					}
					break;
				case '.':
					curTokenType = Dot;
					break;
				case '\'':
				case '"':
					if (ReadStringLiteral(ch)) {
						curTokenType = StrLit;
					}
					break;
				default:
					if (IsIdentifierPart(ch)) {
						string ident = ReadIdentifier(ch);
						if (ident != null) {
							switch (ident.ToLower()) {
								case "new":
									curTokenType = New;
									break;
								case "imports":
									curTokenType = Using;
									break;
								default:
									curTokenType = Ident;
									break;
							}
						}
					}
					break;
			}
		}
		
		bool ReadStringLiteral(char litStart)
		{
			while (true) {
				char ch = GetNext();
				if (ch == '\0') {
					return false;
				}
				if (ch == litStart) {
					if (Peek() == '@' && litStart == '"') {
						GetNext();
					}
					return true;
				}
			}
		}
		
		bool ReadBracket(char openBracket, char closingBracket)
		{
			int curlyBraceLevel    = 0;
			int squareBracketLevel = 0;
			int parenthesisLevel   = 0;
			switch (openBracket) {
				case '(':
					parenthesisLevel++;
					break;
				case '[':
					squareBracketLevel++;
					break;
				case '{':
					curlyBraceLevel++;
					break;
			}
			
			while (parenthesisLevel != 0 || squareBracketLevel != 0 || curlyBraceLevel != 0) {
				char ch = GetNext();
				if (ch == '\0') {
					return false;
				}
				switch (ch) {
					case '(':
						parenthesisLevel--;
						break;
					case '[':
						squareBracketLevel--;
						break;
					case '{':
						curlyBraceLevel--;
						break;
					case ')':
						parenthesisLevel++;
						break;
					case ']':
						squareBracketLevel++;
						break;
					case '}':
						curlyBraceLevel++;
						break;
				}
			}
			return true;
		}
		
		string ReadIdentifier(char ch)
		{
			string identifier = ch.ToString();
			while (IsIdentifierPart(Peek())) {
				identifier = GetNext() + identifier;
			}
			return identifier;
		}
		
		bool IsIdentifierPart(char ch)
		{
			return Char.IsLetterOrDigit(ch) || ch == '_';
		}
		#endregion
		
		#region finite state machine 
		readonly static int ERROR  = 0;
		readonly static int START  = 1;
		readonly static int DOT    = 2;
		readonly static int MORE   = 3;
		readonly static int CURLY  = 4;
		readonly static int CURLY2 = 5;
		readonly static int CURLY3 = 6;
		
		readonly static int ACCEPT = 7;
		readonly static int ACCEPTNOMORE = 8;
		readonly static int ACCEPT2 = 9;
		
		readonly static string[] stateName = new string[] {
			"ERROR",
			"START",
			"DOT",
			"MORE",
			"CURLY",
			"CURLY2",
			"CURLY3",
			"ACCEPT",
			"ACCEPTNOMORE",
			"ACCEPT2"
		};
			
		string GetStateName(int state)
		{
			return stateName[state];
		}
		
		int state = 0;
		int lastAccept = 0;
		static int[,] stateTable = new int[,] {
			//                   Err,     Dot,     Str,      ID,         New,     Brk,     Par,     Cur,   Using
			/*ERROR*/        { ERROR,   ERROR,   ERROR,   ERROR,        ERROR,  ERROR,   ERROR,   ERROR,   ERROR},
			/*START*/        { ERROR,     DOT,  ACCEPT,  ACCEPT,        ERROR,   MORE, ACCEPT2,   CURLY,   ACCEPTNOMORE},
			/*DOT*/          { ERROR,   ERROR,  ACCEPT,  ACCEPT,        ERROR,   MORE,  ACCEPT,   CURLY,   ERROR},
			/*MORE*/         { ERROR,   ERROR,  ACCEPT,  ACCEPT,        ERROR,   MORE, ACCEPT2,   CURLY,   ERROR},
			/*CURLY*/        { ERROR,   ERROR,   ERROR,   ERROR,        ERROR, CURLY2,   ERROR,   ERROR,   ERROR},
			/*CURLY2*/       { ERROR,   ERROR,   ERROR,  CURLY3,        ERROR,  ERROR,   ERROR,   ERROR,   ERROR},
			/*CURLY3*/       { ERROR,   ERROR,   ERROR,   ERROR, ACCEPTNOMORE,  ERROR,   ERROR,   ERROR,   ERROR},
			/*ACCEPT*/       { ERROR,    MORE,   ERROR,   ERROR,       ACCEPT,  ERROR,   ERROR,   ERROR,   ACCEPTNOMORE},
			/*ACCEPTNOMORE*/ { ERROR,   ERROR,   ERROR,   ERROR,        ERROR,  ERROR,   ERROR,   ERROR,   ERROR},
			/*ACCEPT2*/      { ERROR,    MORE,   ERROR,  ACCEPT,       ACCEPT,  ERROR,   ERROR,   ERROR,   ERROR},
		};
		#endregion 
	}
}
