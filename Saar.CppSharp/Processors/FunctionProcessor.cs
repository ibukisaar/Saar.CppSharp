using CppSharp.AST;
using System;
using System.Linq;
using Type = CppSharp.AST.Type;
using TypeModifyType = Saar.CppSharp.Definitions.TypeDefinition.ModifyType;

namespace Saar.CppSharp.Processors {
	public class FunctionProcessor {
		public delegate bool FilterHandler(Function func);
		public event FilterHandler Filter;

		private ASTProcessor context;

		public FunctionProcessor(ASTProcessor context) {
			this.context = context;
		}


		public void Process(TranslationUnit translationUnit) {
			var funcs = translationUnit.Functions.Where(x => !x.IsInline);
			if (Filter != null) {
				var filters = Array.ConvertAll(Filter.GetInvocationList(), filter => (FilterHandler)filter);
				funcs = funcs.Where(func => !filters.Any(filter => filter(func)));
			}

			foreach (var function in funcs) {
				var functionName = function.Name;
				if (!context.FunctionExportMap.TryGetValue(functionName, out FunctionExport export)) {
					Console.WriteLine($"Export not found. Skipping {functionName} function.");
					continue;
				}

				var functionDefinition = new Definitions.FunctionDefinition {
					Name = functionName,
					ReturnType = GetTypeDefinition(function.ReturnType.Type),
					Content = function.Comment?.BriefText,
					DetailedContent = function.Comment?.Text,
					LibraryName = export.LibraryName,
					IsConstLibraryName = export.IsConstLibraryName,
					Params = function.Parameters.Select((x, i) => GetParameter(function, x, i)).ToArray(),
					IsObsolete = IsObsolete(function),
					ObsoleteMessage = GetObsoleteMessage(function),
					CallingConvention = GetCallingConvention(function.CallingConvention),
				};
				context.AddUnit(functionDefinition);
			}
		}

		private static System.Runtime.InteropServices.CallingConvention GetCallingConvention(CallingConvention callingConvention) {
			return callingConvention switch
			{
				CallingConvention.C => System.Runtime.InteropServices.CallingConvention.Cdecl,
				CallingConvention.StdCall => System.Runtime.InteropServices.CallingConvention.StdCall,
				CallingConvention.ThisCall => System.Runtime.InteropServices.CallingConvention.ThisCall,
				CallingConvention.FastCall => System.Runtime.InteropServices.CallingConvention.FastCall,
				_ => System.Runtime.InteropServices.CallingConvention.Cdecl,
			};
		}

		internal Definitions.TypeDefinition GetDelegateType(FunctionType functionType, string name, bool inStruct) {
			var @delegate = new Definitions.DelegateDefinition {
				Name = name,
				ReturnType = GetTypeDefinition(functionType.ReturnType.Type),
				Params = functionType.Parameters.Select((p, i) => GetParameter(p, i, string.IsNullOrEmpty(p.Name) ? $"{name}_p{i}" : $"{name}_{p.Name}")).ToArray(),
				InStruct = inStruct,
				CallingConvention = GetCallingConvention(functionType.CallingConvention),
			};
			context.AddUnit(@delegate);
			return @delegate;
		}

		private Definitions.ParamDefinition GetParameter(Parameter parameter, int position, string delegateName) {
			var name = string.IsNullOrEmpty(parameter.Name) ? $"p{position}" : parameter.Name;
			return new Definitions.ParamDefinition {
				Name = name,
				Type = GetTypeDefinition(parameter.Type, delegateName)
			};
		}

		private Definitions.ParamDefinition GetParameter(Function function, Parameter parameter, int position) {
			var result = GetParameter(parameter, position, string.IsNullOrEmpty(parameter.Name) ? $"{function.Name}_p{position}" : $"{function.Name}_{parameter.Name}");
			result.Content = GetParamComment(function, parameter.Name);
			return result;
		}

		private Definitions.TypeDefinition GetTypeDefinition(Type type, string name = null) {
			var visitType = context.TypeVisitor.Visit(type);
			if (visitType != null) return visitType;

			if (type is PointerType pointerType &&
				pointerType.QualifiedPointee.Qualifiers.IsConst &&
				pointerType.Pointee is BuiltinType builtinType) {
				return TypeHelper.GetType(builtinType).AddModify(TypeModifyType.ConstPointer);
			}

			{
				// edge case when type is array of pointers to none builtin type (type[]* -> type**)
				if (type is ArrayType arrayType &&
					arrayType.SizeType == ArrayType.ArraySize.Incomplete &&
					arrayType.Type is PointerType arrayPointerType &&
					!(arrayPointerType.Pointee is BuiltinType || arrayPointerType.Pointee is TypedefType typedefType && typedefType.Declaration.Type is BuiltinType)) {
					return TypeHelper.GetType(arrayPointerType).AddModify(TypeModifyType.Array);
				}
			}

			var result = context.StructureProcessor.GetTypeDefinition(type, name, false);
			// 如果函数参数类型是数组类型，那么转成指针
			if (result.Modifies.Count == 0 && context.UnitsMap.TryGetValue(result.Name, out var def) && def is Definitions.FixedArrayDefinition fixedArray) {
				result = fixedArray.ElementType.Clone().AddModify(TypeModifyType.Pointer);
			}
			return result;
		}

		internal static bool IsObsolete(Declaration decl) {
			return decl.PreprocessedEntities.OfType<MacroExpansion>().Any(x => x.Text == "attribute_deprecated");
		}

		internal static string GetObsoleteMessage(Declaration decl) {
			var lines = decl.Comment?.FullComment?.Blocks
				.OfType<BlockCommandComment>()
				.Where(x => x.CommandKind == CommentCommandKind.Deprecated)
				.SelectMany(x => x.ParagraphComment.Content.OfType<TextComment>().Select(c => c.Text.Trim()));
			var obsoleteMessage = lines == null ? string.Empty : string.Join(" ", lines);
			return obsoleteMessage;
		}

		private static string GetParamComment(Function function, string parameterName) {
			var comment = function?.Comment?.FullComment.Blocks
				.OfType<ParamCommandComment>()
				.FirstOrDefault(x => x.Arguments.Count == 1 && x.Arguments[0].Text == parameterName);
			return comment == null ? null : string.Join(" ", comment.ParagraphComment.Content.OfType<TextComment>().Select(x => x.Text.Trim()));
		}
	}
}