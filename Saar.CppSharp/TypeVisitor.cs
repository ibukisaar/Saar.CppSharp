using Saar.CppSharp.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp {
	public class TypeVisitor {
		public ASTProcessor Processor { get; }

		public TypeVisitor(ASTProcessor processor) {
			Processor = processor;
		}

		public virtual Definitions.TypeDefinition Visit(global::CppSharp.AST.Type type) => null;
	}
}
