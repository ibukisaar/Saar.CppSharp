using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp.MacroParse {
	public class UnaryExpr : IExpr {
		public OperatorType Operator { get; internal set; }
		public IExpr Expr { get; internal set; }
	}
}
