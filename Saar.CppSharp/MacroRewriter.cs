using Saar.CppSharp.MacroParse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp {
	public ref struct MacroRewriter {
		public StringBuilder sb;

		readonly static Dictionary<OperatorType, int> precedenceMap = new Dictionary<OperatorType, int>() {
			[OperatorType.Or] = 0,
			[OperatorType.And] = 1,
			[OperatorType.BitOr] = 2,
			[OperatorType.BitXor] = 3,
			[OperatorType.BitAnd] = 4,
			[OperatorType.Equal] = 5,
			[OperatorType.NotEqual] = 5,
			[OperatorType.LessThan] = 6,
			[OperatorType.LessThanOrEqual] = 6,
			[OperatorType.GreatThan] = 6,
			[OperatorType.GreatThanOrEqual] = 6,
			[OperatorType.LeftShift] = 7,
			[OperatorType.RightShift] = 7,
			[OperatorType.Add] = 8,
			[OperatorType.Sub] = 8,
			[OperatorType.Mul] = 9,
			[OperatorType.Div] = 9,
			[OperatorType.Mod] = 9,
		};

		readonly static Dictionary<System.Type, string> typeMap = new Dictionary<System.Type, string>() {
			[typeof(int)] = "int",
			[typeof(uint)] = "uint",
			[typeof(long)] = "long",
			[typeof(ulong)] = "ulong",
			[typeof(float)] = "float",
			[typeof(double)] = "double",
			[typeof(char)] = "byte",
		};

		public void MacroRewrite(IExpr expression, IExpr parent) {
			bool needBrackets;
			switch (expression) {
				case BinaryExpr e:
					needBrackets = parent is UnaryExpr || parent is SizeOfExpr || parent is MacroParse.CastExpr;
					if (!needBrackets && parent is BinaryExpr parentBinary) {
						if (ReferenceEquals(parentBinary.Left, e) ? precedenceMap[e.Operator] < precedenceMap[parentBinary.Operator] : precedenceMap[e.Operator] <= precedenceMap[parentBinary.Operator]) {
							needBrackets = true;
						}
					}
					if (needBrackets) sb.Append('(');
					MacroRewrite(e.Left, e);
					sb.Append(' ').Append(e.Operator.ToOperatorString()).Append(' ');
					MacroRewrite(e.Right, e);
					if (needBrackets) sb.Append(')');
					break;
				case UnaryExpr e:
					sb.Append(e.Operator.ToOperatorString());
					MacroRewrite(e.Expr, e);
					break;
				case MacroParse.CastExpr e:
					sb.Append('(').Append(TypeHelper.ToCSharpType(e.TargetType)).Append(')');
					MacroRewrite(e.Expr, e);
					break;
				case CondExpr e:
					needBrackets = parent != null;
					if (needBrackets) sb.Append('(');
					MacroRewrite(e.Cond, e);
					sb.Append(" ? ");
					MacroRewrite(e.TrueExpr, e);
					sb.Append(" : ");
					MacroRewrite(e.FalseExpr, e);
					if (needBrackets) sb.Append(')');
					break;
				case SizeOfExpr e:
					sb.Append("sizeof(");
					if (e.Expr is ConstExpr constExpr) {
						sb.Append(typeMap[constExpr.Value.GetType()]);
					} else {
						MacroRewrite(e.Expr, e);
					}
					sb.Append(')');
					break;
				case MacroParse.CallExpr e:
					sb.Append(e.FunctionName).Append('(');
					if (e.Args.Length > 0) {
						MacroRewrite(e.Args[0], e);
						for (int i = 1; i < e.Args.Length; i++) {
							sb.Append(", ");
							MacroRewrite(e.Args[i], e);
						}
					}
					sb.Append(')');
					break;
				case ConstExpr e:
					sb.Append(e.ValueExpr);
					break;
				case VarExpr e:
					sb.Append(e.Name);
					break;
			}
		}
	}
}
