using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp.MacroParse {
	public class CastExpr : IExpr {
		public string TargetType { get; internal set; }
		public IExpr Expr { get; internal set; }
	}
}
