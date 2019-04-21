using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp.MacroParse {
	[System.Diagnostics.DebuggerDisplay("{" + nameof(Name) + "}")]
	public class VarExpr : IExpr {
		public string Name { get; internal set; }
	}
}
