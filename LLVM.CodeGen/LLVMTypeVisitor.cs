using CppSharp.AST;
using Saar.CppSharp;
using Saar.CppSharp.Definitions;
using Saar.CppSharp.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLVM.CodeGen {
	public class LLVMTypeVisitor : TypeVisitor {
		public LLVMTypeVisitor(ASTProcessor processor) : base(processor) {
		}

		public override TypeDefinition Visit(CppSharp.AST.Type type) {
			if (type is TypedefType typedef) {
				if (typedef.Declaration.Name.StartsWith("LLVM")
				&& typedef.Declaration.Name.EndsWith("Ref")
				&& typedef.Declaration.Type is PointerType pointerType
				&& pointerType.Pointee is TagType tagType
				&& tagType.Declaration is Class) {
					if (!Processor.UnitsMap.ContainsKey(typedef.Declaration.Name)) {
						Processor.AddUnit(new StructDefinition {
							Name = typedef.Declaration.Name,
							Fields = new[] {
								new StructField {
									Name = "Pointer",
									FieldType = new TypeDefinition {
										Name = "void",
									}.AddModify(TypeDefinition.ModifyType.Pointer),
								}
							},
							IsComplete = true,
						});
					}
					return new TypeDefinition { Name = typedef.Declaration.Name };
				}
				if (typedef.Declaration.Name == "LLVMBool") {
					return new TypeDefinition { Name = "bool" };
				}
			}
			return null;
		}
	}
}
