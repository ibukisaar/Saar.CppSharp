using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Saar.CppSharp.Definitions {
	[DebuggerDisplay("{" + nameof(Name) + "}={" + nameof(ExprString) + "}, parse {(Expr != null ? \"Success\" : \"Fail\")}")]
	public class MacroDefinition : IDefinition, ICanGenerateXmlDoc {
		public string Name { get; set; }
		public string CSharpName { get; set; }
		public string TypeName { get; set; }
		public string ExprString { get; set; }
		public MacroParse.IExpr Expr { get; set; }
		public string CSharpExpr { get; set; }
		public bool IsConst { get; set; }
		public string Content { get; set; }
		public string DetailedContent { get; set; }
	}
}
