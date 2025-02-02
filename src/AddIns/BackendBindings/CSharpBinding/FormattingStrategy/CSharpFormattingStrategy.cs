// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Text;

using SharpDevelop.Internal.Parser;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Actions;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.Core.Properties;
using ICSharpCode.Core.Services;
using ICSharpCode.SharpDevelop.Services;

namespace CSharpBinding.FormattingStrategy
{
	/// <summary>
	/// This class handles the auto and smart indenting in the textbuffer while
	/// you type.
	/// </summary>
	public class CSharpFormattingStrategy : DefaultFormattingStrategy
	{
		public CSharpFormattingStrategy()
		{
		}
		
		#region SmartIndentLine
		/// <summary>
		/// Define CSharp specific smart indenting for a line :)
		/// </summary>
		protected override int SmartIndentLine(TextArea textArea, int lineNr)
		{
			if (lineNr <= 0) return AutoIndentLine(textArea, lineNr);
			//Console.WriteLine("Indent line #{0}", lineNr + 1);
			
			string oldText = textArea.Document.GetText(textArea.Document.GetLineSegment(lineNr));
			
			DocumentAccessor acc = new DocumentAccessor(textArea.Document, lineNr, lineNr);
			
			IndentationSettings set = new IndentationSettings();
			set.IndentString = Tab.GetIndentationString(textArea.Document);
			IndentationReformatter r = new IndentationReformatter();
			
			r.Reformat(acc, set);
			
			if (acc.ChangedLines > 0)
				textArea.Document.UndoStack.UndoLast(2);
			
			string t = acc.Text;
			if (t.Length == 0) {
				// use AutoIndentation for new lines in comments / verbatim strings.
				return AutoIndentLine(textArea, lineNr);
			} else {
				int newIndentLength = t.Length - t.TrimStart().Length;
				int oldIndentLength = oldText.Length - oldText.TrimStart().Length;
				if (oldIndentLength != newIndentLength && lineNr == textArea.Caret.Position.Y) {
					// fix cursor position if indentation was changed
					int newX = textArea.Caret.Position.X - oldIndentLength + newIndentLength;
					textArea.Caret.Position = new Point(Math.Max(newX, 0), lineNr);
				}
				return newIndentLength;
			}
		}
		
		/// <summary>
		/// This function sets the indentlevel in a range of lines.
		/// </summary>
		public override void IndentLines(TextArea textArea, int begin, int end)
		{
			if (textArea.Document.TextEditorProperties.IndentStyle != IndentStyle.Smart) {
				base.IndentLines(textArea, begin, end);
				return;
			}
			int cursorPos = textArea.Caret.Position.Y;
			int oldIndentLength = 0;
			
			if (cursorPos >= begin && cursorPos <= end)
				oldIndentLength = GetIndentation(textArea, cursorPos).Length;
			
			IndentationSettings set = new IndentationSettings();
			set.IndentString = Tab.GetIndentationString(textArea.Document);
			IndentationReformatter r = new IndentationReformatter();
			DocumentAccessor acc = new DocumentAccessor(textArea.Document, begin, end);
			r.Reformat(acc, set);
			
			if (cursorPos >= begin && cursorPos <= end) {
				int newIndentLength = GetIndentation(textArea, cursorPos).Length;
				if (oldIndentLength != newIndentLength) {
					// fix cursor position if indentation was changed
					int newX = textArea.Caret.Position.X - oldIndentLength + newIndentLength;
					textArea.Caret.Position = new Point(Math.Max(newX, 0), cursorPos);
				}
			}
			
			if (acc.ChangedLines > 0)
				textArea.Document.UndoStack.UndoLast(acc.ChangedLines);
		}
		#endregion
		
		#region Private functions
		bool NeedCurlyBracket(string text)
		{
			int curlyCounter = 0;
			
			bool inString = false;
			bool inChar   = false;
			bool verbatim = false;
			
			bool lineComment  = false;
			bool blockComment = false;
			
			for (int i = 0; i < text.Length; ++i) {
				switch (text[i]) {
					case '\r':
					case '\n':
						lineComment = false;
						inChar = false;
						if (!verbatim) inString = false;
						break;
					case '/':
						if (blockComment) {
							Debug.Assert(i > 0);
							if (text[i - 1] == '*') {
								blockComment = false;
							}
						}
						if (!inString && !inChar && i + 1 < text.Length) {
							if (!blockComment && text[i + 1] == '/') {
								lineComment = true;
							}
							if (!lineComment && text[i + 1] == '*') {
								blockComment = true;
							}
						}
						break;
					case '"':
						if (!(inChar || lineComment || blockComment)) {
							if (inString && verbatim) {
								if (i + 1 < text.Length && text[i + 1] == '"') {
									++i; // skip escaped quote
									inString = false; // let the string go on
								} else {
									verbatim = false;
								}
							} else if (!inString && i > 0 && text[i - 1] == '@') {
								verbatim = true;
							}
							inString = !inString;
						}
						break;
					case '\'':
						if (!(inString || lineComment || blockComment)) {
							inChar = !inChar;
						}
						break;
					case '{':
						if (!(inString || inChar || lineComment || blockComment)) {
							++curlyCounter;
						}
						break;
					case '}':
						if (!(inString || inChar || lineComment || blockComment)) {
							--curlyCounter;
						}
						break;
					case '\\':
						if ((inString && !verbatim) || inChar)
							++i; // skip next character
						break;
				}
			}
			return curlyCounter > 0;
		}
		
		bool IsInsideStringOrComment(TextArea textArea, LineSegment curLine, int cursorOffset)
		{
			// scan cur line if it is inside a string or single line comment (//)
			bool insideString  = false;
			char stringstart = ' ';
			bool verbatim = false; // true if the current string is verbatim (@-string)
			char c = ' ';
			char lastchar;
			for (int i = curLine.Offset; i < cursorOffset; ++i) {
				lastchar = c;
				c = textArea.Document.GetCharAt(i);
				if (insideString) {
					if (c == stringstart) {
						if (verbatim && i + 1 < cursorOffset && textArea.Document.GetCharAt(i + 1) == '"')
							++i; // skip escaped character
						else
							insideString = false;
					} else if (c == '\\' && !verbatim) {
						++i; // skip escaped character
					}
				} else if (c == '/' && i + 1 < cursorOffset && textArea.Document.GetCharAt(i + 1) == '/') {
					return true;
				} else if (c == '"' || c == '\'') {
					stringstart = c;
					insideString = true;
					verbatim = (c == '"') && (lastchar == '@');
				}
			}
			return insideString;
		}
		
		bool IsInsideDocumentationComment(TextArea textArea, LineSegment curLine, int cursorOffset)
		{
			for (int i = curLine.Offset; i < cursorOffset; ++i) {
				char ch = textArea.Document.GetCharAt(i);
				if (ch == '"') {
					// parsing strings correctly is too complicated (see above),
					// but I don't now any case where a doc comment is after a string...
					return false;
				}
				if (ch == '/' && i + 2 < cursorOffset && textArea.Document.GetCharAt(i + 1) == '/' && textArea.Document.GetCharAt(i + 2) == '/') {
					return true;
				}
			}
			return false;
		}
		
		IParserService parserService = (IParserService)ICSharpCode.Core.Services.ServiceManager.Services.GetService(typeof(IParserService));
		
		bool IsBeforeRegion(TextArea textArea, IRegion region, int lineNr)
		{
			if (region == null) {
				return false;
			}
			return region.BeginLine - 2 <= lineNr && lineNr <= region.BeginLine;
		}
		
		object GetClassMember(TextArea textArea, int lineNr, IClass c)
		{
			if (IsBeforeRegion(textArea, c.Region, lineNr)) {
				return c;
			}
			
			foreach (IClass inner in c.InnerClasses) {
				object o = GetClassMember(textArea, lineNr, inner);
				if (o != null) {
					return o;
				}
			}
			
			foreach (IField f in c.Fields) {
				if (IsBeforeRegion(textArea, f.Region, lineNr)) {
					return f;
				}
			}
			foreach (IProperty p in c.Properties) {
				if (IsBeforeRegion(textArea, p.Region, lineNr)) {
					return p;
				}
			}
			foreach (IIndexer i in c.Indexer) {
				if (IsBeforeRegion(textArea, i.Region, lineNr)) {
					return i;
				}
			}
			foreach (IEvent e in c.Events) {
				if (IsBeforeRegion(textArea, e.Region, lineNr)) {
					return e;
				}
			}
			foreach (IMethod m in c.Methods) {
				if (IsBeforeRegion(textArea, m.Region, lineNr)) {
					return m;
				}
			}
			return null;
		}
		
		object GetMember(TextArea textArea, int lineNr)
		{
			string fileName = textArea.MotherTextEditorControl.FileName;
			if (fileName != null && fileName.Length > 0 ) {
				string fullPath = Path.GetFullPath(fileName);
				IParseInformation parseInfo = parserService.GetParseInformation(fullPath);
				if (parseInfo != null) {
					ICompilationUnit currentCompilationUnit = (ICompilationUnit)parseInfo.BestCompilationUnit;
					if (currentCompilationUnit != null) {
						foreach (IClass c in currentCompilationUnit.Classes) {
							object o = GetClassMember(textArea, lineNr, c);
							if (o != null) {
								return o;
							}
						}
					}
				}
			}
			return null;
		}
		#endregion
		
		#region FormatLine
		public override int FormatLine(TextArea textArea, int lineNr, int cursorOffset, char ch) // used for comment tag formater/inserter
		{
			LineSegment curLine   = textArea.Document.GetLineSegment(lineNr);
			LineSegment lineAbove = lineNr > 0 ? textArea.Document.GetLineSegment(lineNr - 1) : null;
			
			//// local string for curLine segment
			string curLineText="";
			if (ch == '/') {
				curLineText   = textArea.Document.GetText(curLine);
				string lineAboveText = lineAbove == null ? "" : textArea.Document.GetText(lineAbove);
				if (curLineText != null && curLineText.EndsWith("///") && (lineAboveText == null || !lineAboveText.Trim().StartsWith("///"))) {
					string indentation = base.GetIndentation(textArea, lineNr);
					object member = GetMember(textArea, lineNr);
					if (member != null) {
						StringBuilder sb = new StringBuilder();
						sb.Append(" <summary>\n");
						sb.Append(indentation);
						sb.Append("/// \n");
						sb.Append(indentation);
						sb.Append("/// </summary>");
						
						if (member is IMethod) {
							IMethod method = (IMethod)member;
							if (method.Parameters != null && method.Parameters.Count > 0) {
								for (int i = 0; i < method.Parameters.Count; ++i) {
									sb.Append("\n");
									sb.Append(indentation);
									sb.Append("/// <param name=\"");
									sb.Append(method.Parameters[i].Name);
									sb.Append("\"></param>");
								}
							}
							if (method.ReturnType != null && method.ReturnType.FullyQualifiedName != "System.Void") {
								sb.Append("\n");
								sb.Append(indentation);
								sb.Append("/// <returns></returns>");
							}
						}
						textArea.Document.Insert(cursorOffset, sb.ToString());
						
						textArea.Refresh();
						textArea.Caret.Position = textArea.Document.OffsetToPosition(cursorOffset + indentation.Length + "/// ".Length + " <summary>\n".Length);
						return 0;
					}
				}
				return 0;
			}
			if (ch != '\n' && ch != '>') {
				if (IsInsideStringOrComment(textArea, curLine, cursorOffset)) {
					return 0;
				}
			}
			
			switch (ch) {
				case '>':
					if (IsInsideDocumentationComment(textArea, curLine, cursorOffset)) {
						curLineText  = textArea.Document.GetText(curLine);
						int column = textArea.Caret.Offset - curLine.Offset;
						int index = Math.Min(column - 1, curLineText.Length - 1);
						
						while (index >= 0 && curLineText[index] != '<') {
							--index;
							if(curLineText[index] == '/')
								return 0; // the tag was an end tag or already
						}
						
						if (index > 0) {
							StringBuilder commentBuilder = new StringBuilder("");
							for (int i = index; i < curLineText.Length && i < column && !Char.IsWhiteSpace(curLineText[i]); ++i) {
								commentBuilder.Append(curLineText[ i]);
							}
							string tag = commentBuilder.ToString().Trim();
							if (!tag.EndsWith(">")) {
								tag += ">";
							}
							if (!tag.StartsWith("/")) {
								textArea.Document.Insert(textArea.Caret.Offset, String.Concat("</", tag.Substring(1)));
							}
						}
					}
					break;
				case ':':
				case ')':
				case ']':
					
				case '}':
				case '{':
					return textArea.Document.FormattingStrategy.IndentLine(textArea, lineNr);
				case '\n':
					if (lineNr <= 0) {
						return IndentLine(textArea, lineNr);
					}
					
					string  lineAboveText = lineAbove == null ? "" : textArea.Document.GetText(lineAbove);
					//// curLine might have some text which should be added to indentation
					curLineText = "";
					if (curLine.Length > 0) {
						curLineText = textArea.Document.GetText(curLine);
					}
					
					LineSegment nextLine      = lineNr + 1 < textArea.Document.TotalNumberOfLines ? textArea.Document.GetLineSegment(lineNr + 1) : null;
					string      nextLineText  = lineNr + 1 < textArea.Document.TotalNumberOfLines ? textArea.Document.GetText(nextLine) : "";
					
					int addCursorOffset = 0;
					
					if (lineAbove.HighlightSpanStack != null && lineAbove.HighlightSpanStack.Count > 0) {
						if (!((Span)lineAbove.HighlightSpanStack.Peek()).StopEOL) {	// case for /* style comments
							int index = lineAboveText.IndexOf("/*");
							if (index > 0) {
								StringBuilder indentation = new StringBuilder(GetIndentation(textArea, lineNr - 1));
								for (int i = indentation.Length; i < index; ++ i) {
									indentation.Append(' ');
								}
								//// adding curline text
								textArea.Document.Replace(curLine.Offset, curLine.Length, String.Concat(indentation.ToString(), " * ", curLineText));
								return indentation.Length + 3 + curLineText.Length;
							}
							
							index = lineAboveText.IndexOf("*");
							if (index > 0) {
								StringBuilder indentation = new StringBuilder(GetIndentation(textArea, lineNr - 1));
								for (int i = indentation.Length; i < index; ++ i) {
									indentation.Append(' ');
								}
								//// adding curline if present
								textArea.Document.Replace(curLine.Offset, curLine.Length, String.Concat(indentation.ToString(), "* ", curLineText));
								return indentation.Length + 2 + curLineText.Length;
							}
						} else { // don't handle // lines, because they're only one lined comments
							int indexAbove = lineAboveText.IndexOf("///");
							int indexNext  = nextLineText.IndexOf("///");
							if (indexAbove > 0 && (indexNext != -1 || indexAbove + 4 < lineAbove.Length)) {
								StringBuilder indentation = new StringBuilder(GetIndentation(textArea, lineNr - 1));
								for (int i = indentation.Length; i < indexAbove; ++ i) {
									indentation.Append(' ');
								}
								//// adding curline text if present
								textArea.Document.Replace(curLine.Offset, curLine.Length, String.Concat(indentation.ToString(), "/// ", curLineText));
								textArea.Document.UndoStack.UndoLast(2);
								return indentation.Length + 4 /*+ curLineText.Length*/;
							}
							
							if (IsInString(lineAboveText, curLineText)) {
								textArea.Document.Insert(lineAbove.Offset + lineAbove.Length,
								                         "\" +");
								curLine = textArea.Document.GetLineSegment(lineNr);
								textArea.Document.Insert(curLine.Offset, "\"");
								textArea.Document.UndoStack.UndoLast(3);
								addCursorOffset = 1;
							}
						}
					}
					int result = IndentLine(textArea, lineNr) + addCursorOffset;
					if (textArea.TextEditorProperties.AutoInsertCurlyBracket) {
						string oldLineText = TextUtilities.GetLineAsString(textArea.Document, lineNr - 1);
						if (oldLineText.EndsWith("{")) {
							if (NeedCurlyBracket(textArea.Document.TextContent)) {
								textArea.Document.Insert(curLine.Offset + curLine.Length, "\n}");
								IndentLine(textArea, lineNr + 1);
							}
						}
					}
					return result;
			}
			return 0;
		}
		
		bool IsInString(string start, string end)
		{
			bool inString = false;
			for (int i = 0; i < start.Length; ++i) {
				char c = start[i];
				if (c == '"') {
					if (!inString && i > 0 && start[i - 1] == '@')
						return false; // no string line break for verbatim strings
					inString = !inString;
				}
				if (!inString && i > 0 && start[i - 1] == '/' && (c == '/' || c == '*'))
					return false;
				if (inString && start[i] == '\\')
					++i;
			}
			if (!inString) return false;
			// we are possibly in a string, or a multiline string has just ended here
			// check if the closing double quote is in end.
			for (int i = 0; i < end.Length; ++i) {
				char c = end[i];
				if (c == '"') {
					if (!inString && i > 0 && start[i - 1] == '@')
						return false; // no string line break for verbatim strings
					inString = !inString;
				}
				if (!inString && i > 0 && start[i - 1] == '/' && (c == '/' || c == '*'))
					break;
				if (inString && start[i] == '\\')
					++i;
			}
			// return true if string was closed properly
			return !inString;
		}
		#endregion
		
		#region SearchBracket helper functions
		static int ScanLineStart(IDocument document, int offset)
		{
			for (int i = offset - 1; i > 0; --i) {
				if (document.GetCharAt(i) == '\n')
					return i + 1;
			}
			return 0;
		}
		
		/// <summary>
		/// Gets the type of code at offset.<br/>
		/// 0 = Code,<br/>
		/// 1 = Comment,<br/>
		/// 2 = String<br/>
		/// Block comments and multiline strings are not supported.
		/// </summary>
		static int GetStartType(IDocument document, int linestart, int offset)
		{
			bool inString = false;
			bool inChar = false;
			bool verbatim = false;
			for(int i = linestart; i < offset; i++) {
				switch (document.GetCharAt(i)) {
					case '/':
						if (!inString && !inChar && i + 1 < document.TextLength) {
							if (document.GetCharAt(i + 1) == '/') {
								return 1;
							}
						}
						break;
					case '"':
						if (!inChar) {
							if (inString && verbatim) {
								if (i + 1 < document.TextLength && document.GetCharAt(i + 1) == '"') {
									++i; // skip escaped quote
									inString = false; // let the string go on
								} else {
									verbatim = false;
								}
							} else if (!inString && i > 0 && document.GetCharAt(i - 1) == '@') {
								verbatim = true;
							}
							inString = !inString;
						}
						break;
					case '\'':
						if (!inString) inChar = !inChar;
						break;
					case '\\':
						if ((inString && !verbatim) || inChar)
							++i; // skip next character
						break;
				}
			}
			return (inString || inChar) ? 2 : 0;
		}
		#endregion
		
		#region SearchBracketBackward
		public override int SearchBracketBackward(IDocument document, int offset, char openBracket, char closingBracket)
		{
			if (offset + 1 >= document.TextLength) return -1;
			// this method parses a c# document backwards to find the matching bracket
			
			// first try "quick find" - find the matching bracket if there is no string/comment in the way
			int quickResult = base.SearchBracketBackward(document, offset, openBracket, closingBracket);
			if (quickResult >= 0) return quickResult;
			
			// we need to parse the line from the beginning, so get the line start position
			int linestart = ScanLineStart(document, offset + 1);
			
			// we need to know where offset is - in a string/comment or in normal code?
			// ignore cases where offset is in a block comment
			int starttype = GetStartType(document, linestart, offset + 1);
			if (starttype != 0) {
				return -1; // start position is in a comment/string
			}
			
			// I don't see any possibility to parse a C# document backwards...
			// We have to do it forwards and push all bracket positions on a stack.
			Stack bracketStack = new Stack();
			bool  blockComment = false;
			bool  lineComment  = false;
			bool  inChar       = false;
			bool  inString     = false;
			bool  verbatim     = false;
			
			for(int i = 0; i <= offset; ++i) {
				char ch = document.GetCharAt(i);
				switch (ch) {
					case '\r':
					case '\n':
						lineComment = false;
						inChar = false;
						if (!verbatim) inString = false;
						break;
					case '/':
						if (blockComment) {
							Debug.Assert(i > 0);
							if (document.GetCharAt(i - 1) == '*') {
								blockComment = false;
							}
						}
						if (!inString && !inChar && i + 1 < document.TextLength) {
							if (!blockComment && document.GetCharAt(i + 1) == '/') {
								lineComment = true;
							}
							if (!lineComment && document.GetCharAt(i + 1) == '*') {
								blockComment = true;
							}
						}
						break;
					case '"':
						if (!(inChar || lineComment || blockComment)) {
							if (inString && verbatim) {
								if (i + 1 < document.TextLength && document.GetCharAt(i + 1) == '"') {
									++i; // skip escaped quote
									inString = false; // let the string go
								} else {
									verbatim = false;
								}
							} else if (!inString && offset > 0 && document.GetCharAt(i - 1) == '@') {
								verbatim = true;
							}
							inString = !inString;
						}
						break;
					case '\'':
						if (!(inString || lineComment || blockComment)) {
							inChar = !inChar;
						}
						break;
					case '\\':
						if ((inString && !verbatim) || inChar)
							++i; // skip next character
						break;
						default :
							if (ch == openBracket) {
							if (!(inString || inChar || lineComment || blockComment)) {
								bracketStack.Push(i);
							}
						} else if (ch == closingBracket) {
							if (!(inString || inChar || lineComment || blockComment)) {
								if (bracketStack.Count > 0)
									bracketStack.Pop();
							}
						}
						break;
				}
			}
			if (bracketStack.Count > 0) return (int)bracketStack.Pop();
			return -1;
		}
		#endregion
		
		#region SearchBracketForward
		public override int SearchBracketForward(IDocument document, int offset, char openBracket, char closingBracket)
		{
			bool inString = false;
			bool inChar   = false;
			bool verbatim = false;
			
			bool lineComment  = false;
			bool blockComment = false;
			
			if (offset < 0) return -1;
			
			// first try "quick find" - find the matching bracket if there is no string/comment in the way
			int quickResult = base.SearchBracketForward(document, offset, openBracket, closingBracket);
			if (quickResult >= 0) return quickResult;
			
			// we need to parse the line from the beginning, so get the line start position
			int linestart = ScanLineStart(document, offset);
			
			// we need to know where offset is - in a string/comment or in normal code?
			// ignore cases where offset is in a block comment
			int starttype = GetStartType(document, linestart, offset);
			if (starttype != 0) return -1; // start position is in a comment/string
			
			int brackets = 1;
			
			while (offset < document.TextLength) {
				char ch = document.GetCharAt(offset);
				switch (ch) {
					case '\r':
					case '\n':
						lineComment = false;
						inChar = false;
						if (!verbatim) inString = false;
						break;
					case '/':
						if (blockComment) {
							Debug.Assert(offset > 0);
							if (document.GetCharAt(offset - 1) == '*') {
								blockComment = false;
							}
						}
						if (!inString && !inChar && offset + 1 < document.TextLength) {
							if (!blockComment && document.GetCharAt(offset + 1) == '/') {
								lineComment = true;
							}
							if (!lineComment && document.GetCharAt(offset + 1) == '*') {
								blockComment = true;
							}
						}
						break;
					case '"':
						if (!(inChar || lineComment || blockComment)) {
							if (inString && verbatim) {
								if (offset + 1 < document.TextLength && document.GetCharAt(offset + 1) == '"') {
									++offset; // skip escaped quote
									inString = false; // let the string go
								} else {
									verbatim = false;
								}
							} else if (!inString && offset > 0 && document.GetCharAt(offset - 1) == '@') {
								verbatim = true;
							}
							inString = !inString;
						}
						break;
					case '\'':
						if (!(inString || lineComment || blockComment)) {
							inChar = !inChar;
						}
						break;
					case '\\':
						if ((inString && !verbatim) || inChar)
							++offset; // skip next character
						break;
						default :
							if (ch == openBracket) {
							if (!(inString || inChar || lineComment || blockComment)) {
								++brackets;
							}
						} else if (ch == closingBracket) {
							if (!(inString || inChar || lineComment || blockComment)) {
								--brackets;
								if (brackets == 0) {
									return offset;
								}
							}
						}
						break;
				}
				++offset;
			}
			return -1;
		}
		#endregion
	}
}
