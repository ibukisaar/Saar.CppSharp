using CppSharp.AST;
using Saar.CppSharp.MacroParse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Saar.CppSharp.Processors {
	public class MacroProcessor {
		static readonly Regex clearMacroNewLineRegex = new Regex(@"\s*?\\(\r?\n)\s*", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		public delegate bool FilterHandler(MacroDefinition macro);
		public event FilterHandler Filter;

		private ASTProcessor context;

		public MacroProcessor(ASTProcessor context) {
			this.context = context;
		}

		public void Process(TranslationUnit translationUnit) {
			var macros = translationUnit.PreprocessedEntities
				.OfType<MacroDefinition>()
				.Where(x => !string.IsNullOrWhiteSpace(x.Expression));

			if (Filter != null) {
				var filters = Array.ConvertAll(Filter.GetInvocationList(), f => (FilterHandler)f);
				macros = macros.Where(m => !filters.Any(f => f(m)));
			}

			foreach (var macro in macros) {
				string expression = clearMacroNewLineRegex.Replace(macro.Expression, " ");
				var macroDefinition = new Definitions.MacroDefinition {
					Name = macro.Name,
					ExprString = expression,
					Content = $"{macro.Name} = {expression}",
				};
				try {
					macroDefinition.Expr = MacroParse.MacroParser.Parse(expression);
					macroDefinition.TypeName = GetMacroType(macroDefinition.Expr);
				} catch {

				}
				context.AddUnit(macroDefinition);
			}

			foreach (var m in context.Units.OfType<Definitions.MacroDefinition>()) {
				if (m.Expr != null && m.TypeName != null) {
					m.IsConst = IsConstMacro(m.Expr);
					m.CSharpExpr = RewriteMacro(m.Expr);
				}
			}
		}

		readonly static HashSet<string> sizeofConstTypes = new HashSet<string> { "byte", "sbyte", "ushort", "short", "uint", "int", "ulong", "long", "float", "double" };

		private bool IsConstMacro(IExpr expression) {
			switch (expression) {
				case BinaryExpr e: return IsConstMacro(e.Left) && IsConstMacro(e.Right);
				case UnaryExpr e: return IsConstMacro(e.Expr);
				case CondExpr e: return IsConstMacro(e.Cond) && IsConstMacro(e.TrueExpr) && IsConstMacro(e.FalseExpr);
				case MacroParse.CastExpr e: return IsConstMacro(e.Expr);
				case SizeOfExpr e: return e.Expr is ConstExpr || (e.Expr is VarExpr ve && sizeofConstTypes.Contains(ve.Name));
				case ConstExpr _: return true;
				case VarExpr e: return context.UnitsMap.TryGetValue(e.Name, out var m) && m is Definitions.MacroDefinition { Expr: IExpr e2 } && IsConstMacro(e2);
				case MacroParse.CallExpr _: return false;
				default: throw new InvalidOperationException();
			}
		}

		private string GetMacroType(IExpr expression) {
			return expression switch
			{
				BinaryExpr e => GetBinaryType(e.Operator, GetMacroType(e.Left), GetMacroType(e.Right)),
				UnaryExpr e => GetUnaryType(e, GetMacroType(e.Expr)),
				CondExpr e => GetMacroType(e.FalseExpr),
				MacroParse.CastExpr e => TypeHelper.ToCSharpType(e.TargetType),
				SizeOfExpr _ => "int",
				ConstExpr e => GetValueType(e.Value),
				VarExpr e when context.UnitsMap.TryGetValue(e.Name, out var m) && m is Definitions.MacroDefinition { Expr: IExpr e2 } => GetMacroType(e2),
				_ => throw new InvalidOperationException()
			};
		}

		static readonly HashSet<string> toIntTypeSet = new HashSet<string> {
			"byte", "char", "short", "ushort", "int"
		};

		private string GetBinaryType(OperatorType op, string left, string right) {
			switch (op) {
				case OperatorType.LessThan:
				case OperatorType.LessThanOrEqual:
				case OperatorType.GreatThan:
				case OperatorType.GreatThanOrEqual:
				case OperatorType.NotEqual:
				case OperatorType.Equal:
					return "bool";
			}

			if (toIntTypeSet.Contains(left) && toIntTypeSet.Contains(right)) return "int";
			if (left == "string" || right == "string") return "string";
			if (left == "double" || right == "double") return "double";
			if (left == "float" || right == "float") return "float";
			if (right == "ulong" || right == "uint") (left, right) = (right, left);

			if (left == "int" || left == "long") {
				return "long";
			} else if (left == "uint") {
				return right == "uint" ? "uint" :
					right == "long" ? "long" :
					right == "ulong" ? "ulong" :
					throw new InvalidOperationException();
			} else if (left == "ulong") {
				return "ulong";
			} else {
				throw new InvalidOperationException();
			}
		}

		private string GetUnaryType(UnaryExpr e, string exprType) {
			switch (e.Operator) {
				case OperatorType.Not: return "bool";
				case OperatorType.BitNot:
				case OperatorType.Neg:
				case OperatorType.Pos: return exprType;
				default: throw new InvalidOperationException();
			}
		}

		private static string GetValueType(object value) {
			return value switch
			{
				byte _ => "byte",
				ushort _ => "ushort",
				short _ => "short",
				int _ => "int",
				uint _ => "uint",
				long _ => "long",
				ulong _ => "ulong",
				double _ => "double",
				float _ => "float",
				char _ => "char",
				bool _ => "bool",
				string _ => "string",
				_ => throw new InvalidOperationException(),
			};
		}

		public static string RewriteMacro(IExpr expression) {
			var rewriter = new MacroRewriter { sb = new StringBuilder() };
			rewriter.MacroRewrite(expression, null);
			return rewriter.sb.ToString();
		}
	}
}