// Field.cs
// Copyright (C) 2003 Mike Krueger
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System;
using System.IO;

namespace ICSharpCode.SharpAssembly.Metadata.Rows {
	
	public class ENCLog : AbstractRow
	{
		public static readonly int TABLE_ID = 0x1E;
		
		uint token;
		uint funcCode;
		
		public uint Token {
			get {
				return token;
			}
			set {
				token = value;
			}
		}
		public uint FuncCode {
			get {
				return funcCode;
			}
			set {
				funcCode = value;
			}
		}
		
		public override void LoadRow()
		{
			token    = binaryReader.ReadUInt32();
			funcCode = binaryReader.ReadUInt32();
		}
	}
}
