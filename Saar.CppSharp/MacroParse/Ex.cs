using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp.MacroParse {
	public static class Ex {
		public static string ToOperatorString(this OperatorType operatorType) {
			return operatorType switch
			{
				OperatorType.Pos => "+",
				OperatorType.Neg => "-",
				OperatorType.Not => "!",
				OperatorType.BitNot => "~",
				OperatorType.Mul => "*",
				OperatorType.Div => "/",
				OperatorType.Mod => "%",
				OperatorType.Add => "+",
				OperatorType.Sub => "-",
				OperatorType.LeftShift => "<<",
				OperatorType.RightShift => ">>",
				OperatorType.LessThan => "<",
				OperatorType.LessThanOrEqual => "<=",
				OperatorType.GreatThan => ">",
				OperatorType.GreatThanOrEqual => ">=",
				OperatorType.Equal => "==",
				OperatorType.NotEqual => "!=",
				OperatorType.BitAnd => "&",
				OperatorType.BitXor => "^",
				OperatorType.BitOr => "|",
				OperatorType.And => "&&",
				OperatorType.Or => "||",
				_ => throw new InvalidOperationException()
			};
		}
	}
}
