using Saar.CppSharp.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp {
	public class Renamer {
		public ASTProcessor Processor { get; }

		public Renamer(ASTProcessor processor) {
			Processor = processor;
		}

		internal void RenameAll() {
			foreach (var u in Processor.Units) {
				RenameDefinition(u);
			}
		}

		protected void RenameDefinition(Definitions.IDefinition u) {
			switch (u) {
				case Definitions.MacroDefinition m: RenameMacro(m); break;
				case Definitions.EnumDefinition e: RenameEnum(e); break;
				case Definitions.StructDefinition s: RenameStruct(s); break;
				case Definitions.FunctionDefinition f: RenameFunction(f); break;
				case Definitions.DelegateDefinition d: RenameDelegate(d); break;
				case Definitions.FixedArrayDefinition fa: RenameFixedArray(fa); break;
			}
		}

		protected virtual void RenameMacro(Definitions.MacroDefinition macro) {
			if (macro.CSharpName != null) return;
			macro.CSharpName = macro.Name;
		}

		protected virtual void RenameEnum(Definitions.EnumDefinition @enum) {
			if (@enum.CSharpName != null) return;
			@enum.CSharpName = @enum.Name;
			if (@enum.Items.Count == 0) return;
			var fieldNames = @enum.Items.Select(item => NamedTool.SplitWord(item.Name)).ToArray();
			int commonCount = 0;
			while (true) {
				if (commonCount >= fieldNames[0].Length) break;
				if (fieldNames.Skip(1).Any(names => commonCount >= names.Length || names[commonCount] != fieldNames[0][commonCount])) break;
				commonCount++;
			}
			for (int i = 0; i < @enum.Items.Count; i++) {
				var csharpName = NamedTool.ToCamelNamed(fieldNames[i].Skip(commonCount));
				if (char.IsDigit(csharpName[0])) csharpName = "_" + csharpName;
				@enum.Items[i].CSharpName = csharpName;
			}

			foreach (var item in @enum.Items) {
				RenameEnumItem(@enum, item);
			}
		}

		protected virtual void RenameEnumItem(Definitions.EnumDefinition @enum, Definitions.EnumItem item) {

		}

		protected virtual void RenameStruct(Definitions.StructDefinition @struct) {
			if (@struct.CSharpName != null) return;
			@struct.CSharpName = @struct.Name;
			foreach (var field in @struct.Fields) {
				RenameType(field.FieldType);
				RenameStructField(@struct, field);
			}
		}

		protected virtual void RenameStructField(Definitions.StructDefinition @struct, Definitions.StructField field) {
			field.CSharpName = NamedTool.ToCamelNamed(field.Name);
		}

		protected virtual void RenameFunction(Definitions.FunctionDefinition function) {
			if (function.CSharpName != null) return;
			function.CSharpName = function.Name;
			RenameType(function.ReturnType);
			foreach (var p in function.Params) {
				RenameType(p.Type);
				p.CSharpName = p.Name;
			}
		}

		protected virtual void RenameDelegate(Definitions.DelegateDefinition @delegate) {
			if (@delegate.CSharpName != null) return;
			@delegate.CSharpName = @delegate.Name;
			RenameType(@delegate.ReturnType);
			foreach (var p in @delegate.Params) {
				RenameType(p.Type);
				p.CSharpName = p.Name;
			}
		}

		protected virtual void RenameType(Definitions.TypeDefinition type) {
			if (type.CSharpName == null) {
				if (Processor.UnitsMap.TryGetValue(type.Name, out var def)) {
					if (def.CSharpName == null) {
						RenameDefinition(def);
					}
					type.CSharpName = def.CSharpName + new string('*', type.Modifies.Count);
				} else {
					type.CSharpName = type.Name + new string('*', type.Modifies.Count);
				}
			}
		}

		protected virtual void RenameFixedArray(Definitions.FixedArrayDefinition fixedArray) {
			fixedArray.CSharpName = fixedArray.Name;
			RenameType(fixedArray.ElementType);
		}
	}
}
