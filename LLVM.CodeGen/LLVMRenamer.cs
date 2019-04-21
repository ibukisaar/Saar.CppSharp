using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Saar.CppSharp.Definitions;
using Saar.CppSharp.Processors;

namespace LLVM.CodeGen {
	public class LLVMRenamer : Saar.CppSharp.Renamer {
		public LLVMRenamer(ASTProcessor processor) : base(processor) {
		}

		protected override void RenameFunction(FunctionDefinition function) {
			if (function.CSharpName != null) return;
			base.RenameFunction(function);

			if (function.CSharpName.StartsWith("LLVM")) {
				function.CSharpName = function.CSharpName.Substring(4);
			}
		}

		protected override void RenameEnum(EnumDefinition @enum) {
			if (@enum.Items.All(item => item.Name.StartsWith("LLVM"))) {
				foreach (var item in @enum.Items) {
					item.Name = item.Name.Substring(4);
				}
			}

			base.RenameEnum(@enum);
		}
	}
}
