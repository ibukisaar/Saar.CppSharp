using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp.MacroParse {
	public class BinaryExpr : IExpr {
		public OperatorType Operator { get; internal set; }
		public IExpr Left { get; internal set; }
		public IExpr Right { get; internal set; }
	}
}
