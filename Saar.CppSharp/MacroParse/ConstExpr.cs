using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp.MacroParse {
	[System.Diagnostics.DebuggerDisplay("{" + nameof(Value) + "}")]
	public class ConstExpr : IExpr {
		public string ValueExpr { get; internal set; }
		public object Value { get; internal set; }

		public ConstExpr() { }

		public ConstExpr(string valueExpr, object value) {
			ValueExpr = valueExpr;
			Value = value;
		}
	}
}
