using CppSharp.AST;
using Saar.CppSharp.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Saar.CppSharp {
	public class Visitor {
		protected ASTProcessor Processor { get; }

		public Visitor(ASTProcessor processor) {
			Processor = processor;
		}

		internal void VisitAll() {
			foreach (var u in Processor.Units)
				Visit(u);
		}

		protected virtual void Visit(Definitions.IDefinition definition) {
			switch (definition) {
				case Definitions.MacroDefinition m: VisitMacro(m); break;
				case Definitions.EnumDefinition e: VisitEnum(e); break;
				case Definitions.StructDefinition s: VisitStruct(s); break;
				case Definitions.FunctionDefinition f: VisitFunction(f); break;
				case Definitions.DelegateDefinition d: VisitDelegate(d); break;
				case Definitions.TypeDefinition t: VisitType(t); break;
				case Definitions.FixedArrayDefinition fa: VisitFixedArray(fa); break;
			}
		}

		protected virtual void VisitMacro(Definitions.MacroDefinition macro) {

		}

		static readonly Regex enumFlagsRegex = new Regex(@"((?<=_)(flags?|FLAGS?)|(?<=_|[a-z\d])Flags?)\d*$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.RightToLeft);

		protected virtual void VisitEnum(Definitions.EnumDefinition @enum) {
			if (enumFlagsRegex.IsMatch(@enum.Name)) @enum.IsFlags = true;
			foreach (var item in @enum.Items) {
				VisitEnumItem(@enum, item);
			}
		}

		protected virtual void VisitEnumItem(Definitions.EnumDefinition @enum, Definitions.EnumItem item) {

		}

		protected virtual void VisitStruct(Definitions.StructDefinition @struct) {
			foreach (var field in @struct.Fields) {
				VisitStructField(@struct, field);
			}
		}

		protected virtual void VisitStructField(Definitions.StructDefinition @struct, Definitions.StructField field) {
			if (field.FieldType.Modifies.Count == 0 && Processor.UnitsMap.TryGetValue(field.FieldType.Name, out var def)) {
				if (def is Definitions.FixedArrayDefinition fixedArray && fixedArray.IsPrimitive) {
					field.FieldType = fixedArray.ElementType;
					field.FixedSize = fixedArray.Size;
				}
			}
		}

		protected virtual void VisitFunction(Definitions.FunctionDefinition function) {
			foreach (var p in function.Params) {
				VisitFunctionParam(function, p);
			}
		}

		protected virtual void VisitFunctionParam(Definitions.FunctionDefinition function, Definitions.ParamDefinition param) {
			
		}

		protected virtual void VisitDelegate(Definitions.DelegateDefinition @delegate) {
			foreach (var p in @delegate.Params) {
				VisitDelegateParam(@delegate, p);
			}
		}

		protected virtual void VisitDelegateParam(Definitions.DelegateDefinition @delegate, Definitions.ParamDefinition param) {
			
		}

		protected virtual void VisitType(Definitions.TypeDefinition type) {

		}

		protected virtual void VisitFixedArray(Definitions.FixedArrayDefinition fixedArray) {

		}
	}
}
