using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp.MacroParse {
	public class SizeOfExpr : IExpr {
		public IExpr Expr { get; internal set; }
	}
}
