using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Saar.CppSharp.Definitions;
using Saar.CppSharp.Processors;

namespace LLVM.CodeGen {
	public class LLVMVisitor : Saar.CppSharp.Visitor {
		static readonly Regex hasWordRegex = new Regex(@"[a-z\d]", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
		static readonly Regex getFunctionRegex = new Regex(@"^LLVM(Get|Parse|Create|Find)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture);
		static readonly Regex outParamRegex = new Regex(@"^Out[A-Z]", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture);
		static readonly Regex errorRegex = new Regex(@"^(Out|Error)Message|ErrMsg|OutError$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture);
		static readonly Regex nameRegex = new Regex(@"(Name|Str)(?=[A-Z]|$)|^ModuleID$|^Path$|^Ident$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture);
		static readonly Regex ignoreEnumRegex = new Regex(@"^[a-z\d_]+$", RegexOptions.Compiled | RegexOptions.Singleline);

		public LLVMVisitor(ASTProcessor processor) : base(processor) {
		}

		protected override void Visit(IDefinition definition) {
			base.Visit(definition);

			if (definition is ICanGenerateXmlDoc xmlDoc && xmlDoc.Content != null && !hasWordRegex.IsMatch(xmlDoc.Content)) {
				xmlDoc.Content = null;
			}
		}

		private static void VisitFunction(IFunctionDefinition function) {
			if (getFunctionRegex.IsMatch(function.Name)) {
				foreach (var param in function.Params) {
					if (outParamRegex.IsMatch(param.Name) && param.Type.Modifies.Count >= 1) {
						param.Modify = ParamDefinition.ModifyType.Out;
						param.Type.RemoveModify(param.Type.Modifies.Count - 1);
					}
				}
			}
			if (function.ReturnType.Name == "bool") {
				function.ReturnAttrs = new[] { "MarshalAs(UnmanagedType.Bool)" };
				return;
			}
			if (function.Name == "LLVMCreateMessage") {
				function.ReturnType = new TypeDefinition { Name = "NativeString" };
				return;
			}
			if (function.ReturnType.Name == "byte"
				&& function.ReturnType.Modifies.Count == 1
				&& function.ReturnType.Modifies[0] == TypeDefinition.ModifyType.ConstPointer) {
				function.ReturnType = new TypeDefinition { Name = "string" };
				function.ReturnAttrs = new[] { "MarshalAs(UnmanagedType.LPStr)" };
				return;
			}
		}

		private static void VisitFunctionParam(IFunctionDefinition function, ParamDefinition param) {
			if (errorRegex.IsMatch(param.Name)
				&& param.Type.Name == "byte"
				&& param.Type.Modifies.Count == 2
				&& param.Type.Modifies[0] == TypeDefinition.ModifyType.Pointer
				&& param.Type.Modifies[1] == TypeDefinition.ModifyType.Pointer
				&& param.Modify == ParamDefinition.ModifyType.None) {
				param.Type = new TypeDefinition { Name = "NativeString" };
				param.Modify = ParamDefinition.ModifyType.Out;
				return;
			}
			if (param.Type.Name == "byte"
				&& param.Type.Modifies.Count == 1
				&& param.Type.Modifies[0] == TypeDefinition.ModifyType.ConstPointer
				&& nameRegex.IsMatch(param.Name)
				&& param.Modify == ParamDefinition.ModifyType.None) {
				param.Type = new TypeDefinition { Name = "string" };
				param.Attrs = new[] { "MarshalAs(UnmanagedType.LPStr)" };
				return;
			}
			if (param.Type.Name == "bool") {
				param.Attrs = new[] { "MarshalAs(UnmanagedType.Bool)" };
				return;
			}
			if (function.Name == "LLVMCreateMessage") {
				if (param.Type.Name == "byte"
				&& param.Type.Modifies.Count == 1
				&& param.Type.Modifies[0] == TypeDefinition.ModifyType.ConstPointer
				&& param.Modify == ParamDefinition.ModifyType.None) {
					param.Type = new TypeDefinition { Name = "string" };
					param.Attrs = new[] { "MarshalAs(UnmanagedType.LPStr)" };
				}
				return;
			}
			if (function.Name == "LLVMDisposeMessage") {
				if (param.Type.Name == "byte"
				&& param.Type.Modifies.Count == 1
				&& param.Type.Modifies[0] == TypeDefinition.ModifyType.Pointer
				&& param.Modify == ParamDefinition.ModifyType.None) {
					param.Type = new TypeDefinition { Name = "NativeString" };
				}
				return;
			}
		}

		protected override void VisitFunction(FunctionDefinition function) {
			base.VisitFunction(function);
			VisitFunction(function);
		}

		protected override void VisitFunctionParam(FunctionDefinition function, ParamDefinition param) {
			base.VisitFunctionParam(function, param);
			VisitFunctionParam(function, param);
		}

		protected override void VisitDelegate(DelegateDefinition @delegate) {
			base.VisitDelegate(@delegate);
			VisitFunction(@delegate);
		}

		protected override void VisitDelegateParam(DelegateDefinition @delegate, ParamDefinition param) {
			base.VisitDelegateParam(@delegate, param);
			VisitFunctionParam(@delegate, param);
		}

		protected override void VisitStruct(StructDefinition @struct) {
			base.VisitStruct(@struct);

			if (@struct.Name.StartsWith("LLVM") && @struct.Name.EndsWith("Ref")) {
				@struct.AddAttr(@"System.Diagnostics.DebuggerDisplay(""{Pointer}"")");
			}
		}

		protected override void VisitStructField(StructDefinition @struct, StructField field) {
			base.VisitStructField(@struct, field);

			if (field.FieldType.Name == "bool") {
				field.FieldType.Name = "bool_t";
			}
		}
	}
}
