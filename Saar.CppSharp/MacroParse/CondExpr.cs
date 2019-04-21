using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp.MacroParse {
	public class CondExpr : IExpr {
		public IExpr Cond { get; internal set; }
		public IExpr TrueExpr { get; internal set; }
		public IExpr FalseExpr { get; internal set; }
	}
}
