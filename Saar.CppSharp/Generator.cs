using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CppSharp;
using CppSharp.Parser;
using CppSharp.Parser.AST;
using Saar.CppSharp.Processors;
using ClangParser = CppSharp.ClangParser;

namespace Saar.CppSharp {
	public sealed class Generator {
		public bool Verbose { get; set; } = true;
		public LanguageVersion? LanguageVersion { get; set; } = global::CppSharp.Parser.LanguageVersion.C99_GNU;
		public VisualStudioVersion? VisualStudioVersion { get; set; } = global::CppSharp.VisualStudioVersion.Latest;
		public IList<string> Defines { get; set; } = new List<string>();
		public IList<string> IncludeDirs { get; set; } = new List<string>();
		public ASTProcessor Processor { get; }
		public TypeVisitor TypeVisitor { get => Processor.TypeVisitor; set => Processor.TypeVisitor = value; }
		public Visitor Visitor { get; set; }
		public Renamer Renamer { get; set; }

		public Generator() : this(new ASTProcessor()) { }
		public Generator(ASTProcessor processor) {
			Processor = processor;
			TypeVisitor = new TypeVisitor(processor);
			Visitor = new Visitor(processor);
			Renamer = new Renamer(processor);
		}

		public void AddExports(IEnumerable<string> exports, string libraryName, bool constLibraryName = true) {
			foreach (var export in exports) {
				Processor.FunctionExportMap.Add(export, new FunctionExport {
					FunctionName = export,
					LibraryName = libraryName,
					IsConstLibraryName = constLibraryName
				});
			}
		}

		public void Parse(IEnumerable<string> sourceFiles) {
			var parserOptions = new ParserOptions {
				Verbose = Verbose,
				LanguageVersion = LanguageVersion,
			};
			if (VisualStudioVersion is VisualStudioVersion vsVersion) {
				parserOptions.SetupMSVC(vsVersion);
			}
			foreach (var define in Defines) parserOptions.AddDefines(define);
			foreach (var includeDir in IncludeDirs) parserOptions.AddIncludeDirs(includeDir);

			var parser = new ClangParser(new ASTContext());
			parser.SourcesParsed += OnSourceFileParsed;
			parser.ParseSourceFiles(sourceFiles, parserOptions);
			var context = ClangParser.ConvertASTContext(parser.ASTContext);
			Processor.Process(context.TranslationUnits.Where(u => !u.IsSystemHeader));
		}

		public void Parse(params string[] sourceFiles) => Parse((IEnumerable<string>)sourceFiles);

		private static void OnSourceFileParsed(IEnumerable<string> files, ParserResult result) {
			bool hasError = false;
			switch (result.Kind) {
				case ParserResultKind.Success:
					Diagnostics.Message("Parsed '{0}'", string.Join(", ", files));
					break;
				case ParserResultKind.Error:
					Console.ForegroundColor = ConsoleColor.Red;
					Diagnostics.Error("Error parsing '{0}'", string.Join(", ", files));
					hasError = true;
					break;
				case ParserResultKind.FileNotFound:
					Diagnostics.Error("A file from '{0}' was not found", string.Join(",", files));
					break;
			}
			for (uint i = 0; i < result.DiagnosticsCount; ++i) {
				var diagnostics = result.GetDiagnostics(i);

				var message = $"{diagnostics.FileName}({diagnostics.LineNumber},{diagnostics.ColumnNumber}): " +
							  $"{diagnostics.Level.ToString().ToLower()}: {diagnostics.Message}";
				Diagnostics.Message(message);
			}
			if (hasError) {
				Console.ResetColor();
				throw new Exception("解析错误，程序终止。");
			}
		}

		public void ParseFinal() {
			Visitor.VisitAll();
			Renamer.RenameAll();
		}
	}
}
