using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Saar.CppSharp.Definitions;
using Saar.CppSharp.Processors;

namespace LLVM.CodeGen {
	public class LLVMVisitor : Saar.CppSharp.Visitor {
		public LLVMVisitor(ASTProcessor processor) : base(processor) {
		}
	}
}
