using CppSharp.AST;
using CppSharp.AST.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Saar.CppSharp.Processors {
	public class EnumerationProcessor {
		public delegate bool FilterHandler(Enumeration @enum);
		public event FilterHandler Filter;

		private ASTProcessor context;

		public EnumerationProcessor(ASTProcessor context) {
			this.context = context;
		}

		private static IEnumerable<Enumeration> GetEnumerations(TranslationUnit translationUnit) {
			foreach (var enumeration in translationUnit.Enums) {
				if (!enumeration.Type.IsPrimitiveType()) continue;
				if (string.IsNullOrEmpty(enumeration.Name)) continue;

				yield return enumeration;
			}
		}

		public void Process(TranslationUnit translationUnit) {
			var enums = GetEnumerations(translationUnit);
			if (Filter != null) {
				var filters = Array.ConvertAll(Filter.GetInvocationList(), filter => (FilterHandler)filter);
				enums = enums.Where(e => !filters.Any(filter => filter(e)));
			}

			foreach (var e in enums) {
				MakeDefinition(e, null);
			}
		}

		public Definitions.EnumDefinition MakeDefinition(Enumeration enumeration, string name) {
			name = string.IsNullOrEmpty(enumeration.Name) ? name : enumeration.Name;
			var result = new Definitions.EnumDefinition { Name = name };
			if (context.IsKnownUnitName(name)) return result;
			context.AddUnit(result);

			result.TypeName = TypeHelper.GetTypeName(enumeration.Type);
			result.Content = enumeration.Comment?.BriefText;
			result.Items = enumeration.Items
				.Select(x =>
					new Definitions.EnumItem {
						Name = x.Name,
						Value = EnumValueToString(result.TypeName, x.Value),
						Content = x.Comment?.BriefText,
						DetailedContent = x.Comment?.Text
					})
				.ToArray();
			return result;
		}

		private static string EnumValueToString(string type, ulong value) {
			return type switch
			{
				"int" => ((int)value).ToString(),
				"uint" => ((uint)value).ToString(),
				"long" => ((long)value).ToString(),
				"ulong" => value.ToString(),
				_ => throw new NotSupportedException(),
			};
		}
	}
}