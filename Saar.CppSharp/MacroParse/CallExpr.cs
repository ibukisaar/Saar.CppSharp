using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp.MacroParse {
	public class CallExpr : IExpr {
		public string FunctionName { get; internal set; }
		public IExpr[] Args { get; internal set; }
	}
}
