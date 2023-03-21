using System;
using System.Collections;


using ICSharpCode.Core.AddIns.Conditions;
using ICSharpCode.Core.AddIns;

using ICSharpCode.Core.AddIns.Codons;

namespace SharpQuery.Codons
{
	[CodonName("SharpQueryConnection")]	
	public class SharpQueryConnectionCodon : AbstractCodon
	{			
		[XmlMemberAttributeAttribute("schema", IsRequired=true)]
		string pSchema = null;
		[XmlMemberAttributeAttribute("node", IsRequired=true)]
		string pNode   = null;
		[XmlMemberAttributeAttribute("showUnsuported", IsRequired=true)]
		string pshowUnsuported   = "True";		
		
		
		public string Schema{
			get{
				return pSchema;
			}
			
			set{
				pSchema = value;
			}
		}
		
		public string Node{
			get{
				return pNode;
			}
			
			set{
				pNode = value;
			}
		}		
		
		public string ShowUnsuported{
			get{
				return pshowUnsuported;
			}
			
			set{
				pshowUnsuported = value;
			}
		}		
				
		public override bool HandleConditions {
			get {
				return true;
			}
		}				
				
		/// <summary>
		/// Creates an item with the specified sub items. And the current
		/// Condition status for this item.
		/// </summary>
		public override object BuildItem(object owner, ArrayList subItems, ConditionCollection conditions)
		{
			return this.Class;
		}

	}
}
