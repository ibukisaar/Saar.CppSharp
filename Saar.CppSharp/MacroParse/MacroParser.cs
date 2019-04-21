using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Saar.CppSharp.MacroParse {
	public class MacroParser {
		private static readonly Regex idRegex = new Regex(@"^[_a-z]\w*", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
		private static readonly Regex sizeofTypeRegex = new Regex(@"^\(\s*((?<unsigned>(?<i8>char))|(((?<unsigned>unsigned)|signed)\s+)?((?<i8>char)|(?<i16>short)|(?<i32>int)|(?<i64>long\s+long)|(?<i32>long))|(?<i32>(?<unsigned>unsigned)|signed)|(?<f>float)|(?<d>double)|struct\s+(?<struct>[_a-zA-Z]\w*)|enum\s+(?<enum>[_a-zA-Z]\w*))\s*\)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture);
		private static readonly Regex constRegex = new Regex(@"^((?<float>(\d*\.\d+|\d+\.)(e[+\-]?\d+)?|\d+e[+\-]?\d+)(?<float_postfix>f)?|(0(?<oct>[0-7]+)|0b(?<bin>[10]+)|0x(?<hex>[\da-f]+)|(?<int>[1-9]\d*|0))(?<int_postfix>ull|llu|lu|ll|ul|u|l)?|'(?<char>\\(?-i:[\\'abfnrtv])|\\\d{1,3}|\\x[\da-f]{1,2}|[^\\'])'|(?<str>""(\\(?-i:[\\'abfnrtv])|\\\d{1,3}|\\x[\da-f]{1,2}|[^\\'])*""))", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

		private readonly string expr;
		private int i = 0;
		private readonly Func<IExpr> orParse, andParse, bitOrParse, bitXorParse, bitAndParse, equalParse, relationalParse, shiftParse, addParse, mulParse;

		private MacroParser(string expr) {
			this.expr = expr;
			mulParse = BinaryParseGenerate(Cast, new[] { ("*", OperatorType.Mul), ("/", OperatorType.Div), ("%", OperatorType.Mod) });
			addParse = BinaryParseGenerate(mulParse, new[] { ("+", OperatorType.Add), ("-", OperatorType.Sub) });
			shiftParse = BinaryParseGenerate(addParse, new[] { ("<<", OperatorType.LeftShift), (">>", OperatorType.RightShift) });
			relationalParse = BinaryParseGenerate(shiftParse, new[] { ("<", OperatorType.LessThan), ("<=", OperatorType.LessThanOrEqual), (">", OperatorType.GreatThan), (">=", OperatorType.GreatThanOrEqual) });
			equalParse = BinaryParseGenerate(relationalParse, new[] { ("==", OperatorType.Equal), ("!=", OperatorType.NotEqual) });
			bitAndParse = BinaryParseGenerate(equalParse, new[] { ("&", OperatorType.BitAnd) });
			bitXorParse = BinaryParseGenerate(bitAndParse, new[] { ("^", OperatorType.BitXor) });
			bitOrParse = BinaryParseGenerate(bitXorParse, new[] { ("|", OperatorType.BitOr) });
			andParse = BinaryParseGenerate(bitOrParse, new[] { ("&&", OperatorType.And) });
			orParse = BinaryParseGenerate(andParse, new[] { ("||", OperatorType.Or) });
		}

		public static IExpr Parse(string expr) {
			MacroParser macroParser = new MacroParser(expr);
			var result = macroParser.Cond();
			macroParser.SkipWhiteSpace();
			if (macroParser.i != expr.Length) throw new ParseException(expr, macroParser.i, "未能解析所有表达式");
			return result;
		}

		private void SkipWhiteSpace() {
			while (i < expr.Length && char.IsWhiteSpace(expr[i])) i++;
		}

		private bool ExprEqual(char c) {
			SkipWhiteSpace();
			if (i >= expr.Length) return false;
			if (expr[i] == c) {
				i++;
				return true;
			} else {
				return false;
			}
		}

		private bool ExprEqual(string other) {
			SkipWhiteSpace();
			if (i + other.Length > expr.Length) return false;
			if (expr.AsSpan(i, other.Length).SequenceEqual(other.AsSpan())) {
				i += other.Length;
				return true;
			} else {
				return false;
			}
		}

		private IExpr Cond() {
			var cond = orParse();
			if (!ExprEqual('?')) return cond;
			var result = new CondExpr { Cond = cond };
			result.TrueExpr = Cond();
			if (!ExprEqual(':')) throw new ParseException(expr, i, "期望：':'");
			result.FalseExpr = Cond();
			return result;
		}

		private Func<IExpr> BinaryParseGenerate(Func<IExpr> nextParse, (string OpStr, OperatorType Op)[] ops) {
			return () => {
				IExpr root = nextParse();
				while (true) {
					int findIndex = Array.FindIndex(ops, p => ExprEqual(p.OpStr));
					if (findIndex < 0) break;
					root = new BinaryExpr { Left = root, Operator = ops[findIndex].Op, Right = nextParse() };
				}
				return root;
			};
		}

		private IExpr Cast() {
			string targetType = null;
			int oldIndex = i;
			if (ExprEqual('(')) {
				SkipWhiteSpace();
				var m = idRegex.Match(expr, i, expr.Length - i);
				if (m.Success) {
					i += m.Length;
					if (ExprEqual(')')) {
						targetType = m.Value;
					} else {
						i = oldIndex;
					}
				} else {
					i = oldIndex;
				}
			}
			try {
				var unary = Unary();
				return targetType != null ? new CastExpr { TargetType = targetType, Expr = unary } : unary;
			} catch (ParseException) when (targetType != null) {
				return new VarExpr { Name = targetType };
			}
		}

		private IExpr Unary() {
			SkipWhiteSpace();
			int oldIndex = i;
			var m = idRegex.Match(expr, i, expr.Length - i);
			if (m.Success) {
				i += m.Length;
				var funcName = m.Value;
				if (funcName != "sizeof") {
					if (ExprEqual('(')) {
						var callExpr = new CallExpr { FunctionName = funcName, Args = Array.Empty<IExpr>() };
						if (ExprEqual(')')) return callExpr;
						var args = new List<IExpr> { Cond() };
						while (ExprEqual(',')) {
							args.Add(Cond());
						}
						if (!ExprEqual(')')) throw new ParseException(expr, i, "期望：')'");
						callExpr.Args = args.ToArray();
						return callExpr;

					}
				} else {
					SkipWhiteSpace();
					m = sizeofTypeRegex.Match(expr, i, expr.Length - i);
					if (m.Success) {
						i += m.Length;
						bool unsigned = m.Groups["unsigned"].Success;
						if (m.Groups["i8"].Success) return new SizeOfExpr { Expr = new VarExpr { Name = unsigned ? "byte" : "sbyte" } };
						if (m.Groups["i16"].Success) return new SizeOfExpr { Expr = new VarExpr { Name = unsigned ? "ushort" : "short" } };
						if (m.Groups["i32"].Success) return new SizeOfExpr { Expr = new VarExpr { Name = unsigned ? "uint" : "int" } };
						if (m.Groups["i64"].Success) return new SizeOfExpr { Expr = new VarExpr { Name = unsigned ? "ulong" : "long" } };
						if (m.Groups["f"].Success) return new SizeOfExpr { Expr = new VarExpr { Name = "float" } };
						if (m.Groups["d"].Success) return new SizeOfExpr { Expr = new VarExpr { Name = "double" } };
						if (m.Groups["struct"].Success) return new SizeOfExpr { Expr = new VarExpr { Name = m.Groups["struct"].Value } };
						if (m.Groups["enum"].Success) return new SizeOfExpr { Expr = new VarExpr { Name = m.Groups["enum"].Value } };
						throw null;
					} else {
						return new SizeOfExpr { Expr = Unary() };
					}
				}
			}
			i = oldIndex;
			OperatorType? unaryOp = null;
			if (ExprEqual('~')) unaryOp = OperatorType.BitNot;
			else if (ExprEqual('!')) unaryOp = OperatorType.Not;
			else if (ExprEqual('-')) unaryOp = OperatorType.Neg;
			else if (ExprEqual('+')) unaryOp = OperatorType.Pos;
			if (unaryOp is OperatorType op) {
				return new UnaryExpr { Operator = op, Expr = Unary() };
			} else {
				return Atom();
			}
		}

		private IExpr Atom() {
			SkipWhiteSpace();
			var m = idRegex.Match(expr, i, expr.Length - i);
			if (m.Success) {
				i += m.Length;
				return new VarExpr { Name = m.Value };
			} else {
				m = constRegex.Match(expr, i, expr.Length - i);
				if (m.Success) {
					i += m.Length;
					if (m.Groups["int_postfix"].Success) {
						switch (m.Groups["int_postfix"].Value.ToLowerInvariant()) {
							case "u":
							case "ul":
							case "lu":
								if (m.Groups["oct"].Success) return new ConstExpr($"{Convert.ToUInt32(m.Groups["oct"].Value, 8)}U", Convert.ToUInt32(m.Groups["oct"].Value, 8));
								if (m.Groups["bin"].Success) return new ConstExpr($"0b{m.Groups["bin"].Value}U", Convert.ToUInt32(m.Groups["bin"].Value, 2));
								if (m.Groups["hex"].Success) return new ConstExpr($"0x{m.Groups["hex"].Value}U", Convert.ToUInt32(m.Groups["hex"].Value, 16));
								if (m.Groups["int"].Success) return new ConstExpr($"{m.Groups["int"].Value}U", Convert.ToUInt32(m.Groups["int"].Value));
								break; // 不可能到达
							case "ull":
							case "llu":
								if (m.Groups["oct"].Success) return new ConstExpr($"{Convert.ToUInt64(m.Groups["oct"].Value, 8)}UL", Convert.ToUInt64(m.Groups["oct"].Value, 8));
								if (m.Groups["bin"].Success) return new ConstExpr($"0b{m.Groups["bin"].Value}UL", Convert.ToUInt64(m.Groups["bin"].Value, 2));
								if (m.Groups["hex"].Success) return new ConstExpr($"0x{m.Groups["hex"].Value}UL", Convert.ToUInt64(m.Groups["hex"].Value, 16));
								if (m.Groups["int"].Success) return new ConstExpr($"{m.Groups["int"].Value}UL", Convert.ToUInt64(m.Groups["int"].Value));
								break; // 不可能到达
							case "ll":
								if (m.Groups["oct"].Success) return new ConstExpr($"{Convert.ToInt64(m.Groups["oct"].Value, 8)}L", Convert.ToInt64(m.Groups["oct"].Value, 8));
								if (m.Groups["bin"].Success) return new ConstExpr($"0b{m.Groups["bin"].Value}L", Convert.ToInt64(m.Groups["bin"].Value, 2));
								if (m.Groups["hex"].Success) return new ConstExpr($"0x{m.Groups["hex"].Value}L", Convert.ToInt64(m.Groups["hex"].Value, 16));
								if (m.Groups["int"].Success) return new ConstExpr($"{m.Groups["int"].Value}L", Convert.ToInt64(m.Groups["int"].Value));
								break; // 不可能到达
							case "l":
								if (m.Groups["oct"].Success) return new ConstExpr($"{Convert.ToInt32(m.Groups["oct"].Value, 8)}", Convert.ToInt32(m.Groups["oct"].Value, 8));
								if (m.Groups["bin"].Success) return new ConstExpr($"0b{m.Groups["bin"].Value}", Convert.ToInt32(m.Groups["bin"].Value, 2));
								if (m.Groups["hex"].Success) return new ConstExpr($"0x{m.Groups["hex"].Value}", Convert.ToInt32(m.Groups["hex"].Value, 16));
								if (m.Groups["int"].Success) return new ConstExpr($"{m.Groups["int"].Value}", Convert.ToInt32(m.Groups["int"].Value));
								break; // 不可能到达
						}
					}

					if (m.Groups["oct"].Success) return AutoConst(m.Groups["oct"].Value, "oct");
					if (m.Groups["bin"].Success) return AutoConst(m.Groups["bin"].Value, "bin");
					if (m.Groups["hex"].Success) return AutoConst(m.Groups["hex"].Value, "hex");
					if (m.Groups["int"].Success) return AutoConst(m.Groups["int"].Value, "int");

					if (m.Groups["float"].Success) {
						if (m.Groups["float_postfix"].Success) {
							return new ConstExpr($"{m.Groups["float"].Value}f", Convert.ToSingle(m.Groups["float"].Value));
						} else {
							return new ConstExpr($"{m.Groups["float"].Value}", Convert.ToDouble(m.Groups["float"].Value));
						}
					}

					if (m.Groups["char"].Success) {
						string chars = m.Groups["char"].Value;
						if (chars[0] == '\\') {
							switch (chars[1]) {
								case '\\': return new ConstExpr(@"'\\'", '\\');
								case '\'': return new ConstExpr(@"'\''", '\'');
								case 'a': return new ConstExpr(@"'\a'", '\a');
								case 'n': return new ConstExpr(@"'\n'", '\n');
								case 't': return new ConstExpr(@"'\t'", '\t');
								case 'r': return new ConstExpr(@"'\r'", '\r');
								case 'b': return new ConstExpr(@"'\b'", '\b');
								case 'f': return new ConstExpr(@"'\f'", '\f');
								case 'v': return new ConstExpr(@"'\v'", '\v');
							}
							char c = char.ToLowerInvariant(chars[1]) == 'x'
								? (char)Convert.ToByte(chars.Substring(2), 16)
								: (char)Convert.ToByte(chars.Substring(1), 8);
							return new ConstExpr($@"'\x{(byte)c:x}'", c);
						} else {
							return new ConstExpr($"'{chars}'", chars[0]);
						}
					}

					if (m.Groups["str"].Success) {
						string str = m.Groups["str"].Value;
						return new ConstExpr(str, str);
					}

					throw null; // 不可能到达
				} else if (ExprEqual('(')) {
					var result = Cond();
					if (!ExprEqual(')')) throw new ParseException(expr, i, "期望：')'");
					return result;
				} else {
					throw new ParseException(expr, i, "期望：常量或'('");
				}
			}
		}

		static ConstExpr AutoConst(string expr, string format) {
			static bool IsInt32(long x) => int.MinValue <= x && x <= int.MaxValue;

			long val;
			switch (format) {
				case "int":
					val = Convert.ToInt64(expr);
					if (IsInt32(val)) {
						return new ConstExpr(expr, (int)val);
					} else {
						return new ConstExpr($"{expr}L", val);
					}
				case "bin":
					val = Convert.ToInt64(expr, 2);
					if (IsInt32(val)) {
						return new ConstExpr($"0b{expr}", (int)val);
					} else {
						return new ConstExpr($"0b{expr}L", val);
					}
				case "oct":
					val = Convert.ToInt64(expr, 8);
					if (IsInt32(val)) {
						return new ConstExpr($"{val}", (int)val);
					} else {
						return new ConstExpr($"{val}L", val);
					}
				case "hex":
					val = Convert.ToInt64(expr, 16);
					if (IsInt32(val)) {
						return new ConstExpr($"0x{expr}", (int)val);
					} else {
						return new ConstExpr($"0x{expr}L", val);
					}
				default:
					throw new InvalidOperationException();
			}
		}
	}
}
