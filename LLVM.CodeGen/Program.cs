using Saar.CppSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLVM.CodeGen {
	class Program {
		static void Main(string[] args) {
			var exports = DllReader.ReadFunctions(@"D:\MyDocuments\Documents\Visual Studio 2019\Projects\libLLVM_8.0.0\x64\Release\libLLVM_8.0.0.dll");
			Generator g = new Generator();
			g.TypeVisitor = new LLVMTypeVisitor(g.Processor);
			g.Visitor = new LLVMVisitor(g.Processor);
			g.Renamer = new LLVMRenamer(g.Processor);
			g.AddExports(exports, "libLLVM_8.0.0");
			g.IncludeDirs.Add(@"D:\Develop\LLVM-8.0.0\include");

			foreach (var header in Directory.GetFiles(@"D:\Develop\LLVM-8.0.0\include\llvm-c")) {
				g.Parse($@"llvm-c\{Path.GetFileName(header)}");
			}
			g.ParseFinal();

			//using (var writer = new CSharpWriter(@"Z:\macro.cs", new CSharpWriterConfig { Namespace = "Saar.LLVM" })) {
			//	using (writer.BeginClass("Constant")) {
			//		writer.WriteMacros(g.Processor.Units);
			//	}
			//}
			using (var writer = new CSharpWriter(@"Z:\LLVM.Enums.cs", CSharpWriterConfig.GetDefaultEnumConfig("Saar.LLVM"))) {
				writer.WriteEnums(g.Processor.Units);
			}
			using (var writer = new CSharpWriter(@"Z:\LLVM.Structs.cs", CSharpWriterConfig.GetDefaultStructConfig("Saar.LLVM"))) {
				writer.WriteStructs(g.Processor.Units);
			}
			using (var writer = new CSharpWriter(@"Z:\LLVM.Delegates.cs", CSharpWriterConfig.GetDefaultDelegateConfig("Saar.LLVM"))) {
				writer.WriteDelegates(g.Processor.Units);
			}
			using (var writer = new CSharpWriter(@"Z:\LLVM.Functions.cs", CSharpWriterConfig.GetDefaultFunctionConfig("Saar.LLVM"))) {
				using (writer.BeginClass("LLVM")) {
					writer.WriteFunctions(g.Processor.Units);
				}
			}
			//using (var writer = new CSharpWriter(@"Z:\array.cs", CSharpWriterConfig.GetDefaultFixedArrayConfig("Saar.LLVM"))) {
			//	writer.WriteFixedArrays(g.Processor.Units);
			//}
		}
	}
}
