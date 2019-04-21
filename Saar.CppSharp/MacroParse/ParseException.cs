using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp.MacroParse {
	public class ParseException : Exception {
		public string Expr { get; }
		public int Index { get; }

		public ParseException(string expr, int index) : base($"位置({index})解析错误。") {
			Expr = expr;
			Index = index;
		}

		public ParseException(string expr, int index, string message) : base($"位置({index})解析错误，{message}。") {
			Expr = expr;
			Index = index;
		}
	}
}
