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

			using (var writer = new CSharpWriter(@"Z:\macro.cs")) {
				using (writer.BeginClass("Constant")) {
					writer.WriteMacros(g.Processor.Units);
				}
			}
			using (var writer = new CSharpWriter(@"Z:\enum.cs", CSharpWriterConfig.GetDefaultEnumConfig("Saar"))) {
				writer.WriteEnums(g.Processor.Units);
			}
			using (var writer = new CSharpWriter(@"Z:\struct.cs", CSharpWriterConfig.GetDefaultStructConfig("Saar"))) {
				writer.WriteStructs(g.Processor.Units);
			}
			using (var writer = new CSharpWriter(@"Z:\delegate.cs", CSharpWriterConfig.GetDefaultDelegateConfig("Saar"))) {
				writer.WriteDelegates(g.Processor.Units);
			}
			using (var writer = new CSharpWriter(@"Z:\function.cs", CSharpWriterConfig.GetDefaultFunctionConfig("Saar"))) {
				using (writer.BeginClass("LLVM")) {
					writer.WriteFunctions(g.Processor.Units);
				}
			}
			using (var writer = new CSharpWriter(@"Z:\array.cs", CSharpWriterConfig.GetDefaultFixedArrayConfig("Saar"))) {
				writer.WriteFixedArrays(g.Processor.Units);
			}
		}
	}
}
