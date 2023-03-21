// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;

namespace ICSharpCode.AssemblyAnalyser.Rules
{
	/// <summary>
	/// Description of AvoidNamespacesWithFewMembers.	
	/// </summary>
	public class AvoidNamespacesWithFewMembers : AbstractReflectionRule, INamespaceRule
	{
		public override string Description {
			get {
				return "${res:ICSharpCode.AssemblyAnalyser.Rules.AvoidNamespacesWithFewMembers.Description}";
			}
		}
		
		public override string Details {
			get {
				return "${res:ICSharpCode.AssemblyAnalyser.Rules.AvoidNamespacesWithFewMembers.Details}";
			}
		}
		
		public AvoidNamespacesWithFewMembers()
		{
			base.certainty     = 50;
			base.priorityLevel = PriorityLevel.Warning;
		}
		
		public Resolution Check(string namespaceName, ICollection types)
		{
			if (namespaceName != null && namespaceName.Length > 0 && types.Count < 5) {
				return new Resolution(this, "${res:ICSharpCode.AssemblyAnalyser.Rules.AvoidNamespacesWithFewMembers.Resolution}", namespaceName, new string[,]{{"NamespaceName", namespaceName}});
			}
			return null;
		}
	}
}
#region Unit Test
#if TEST
namespace ICSharpCode.AssemblyAnalyser.Rules
{
	using NUnit.Framework;

	[TestFixture]
	public class AvoidNamespacesWithFewMembersTest
	{
		[Test]
		public void TestCorrectNamespaces()
		{
			AvoidNamespacesWithFewMembers rule = new AvoidNamespacesWithFewMembers();
			Assertion.AssertNull(rule.Check("MyNamespace", new Type[] {typeof(System.Object),
			                                                             typeof(System.Object),
			                                                             typeof(System.Object),
			                                                             typeof(System.Object),
			                                                             typeof(System.Object),
			                                                             typeof(System.Object)}));
		}
		
		[Test]
		public void TestIncorrectAttribute()
		{
			AvoidNamespacesWithFewMembers rule = new AvoidNamespacesWithFewMembers();
			Assertion.AssertNotNull(rule.Check("a", new Type[] {}));
		}
	}
}
#endif
#endregion
