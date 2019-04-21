using CppSharp.AST;
using Saar.CppSharp.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp.Processors {
	public class ASTProcessor {
		private readonly Dictionary<string, IDefinition> _units = new Dictionary<string, IDefinition>();

		public MacroProcessor MacroProcessor { get; }
		public EnumerationProcessor EnumerationProcessor { get; }
		public StructureProcessor StructureProcessor { get; }
		public FunctionProcessor FunctionProcessor { get; }
		public HashSet<string> IgnoreUnitNames { get; }
		public IReadOnlyCollection<IDefinition> Units => _units.Values;
		public IDictionary<string, IDefinition> UnitsMap => _units;
		public IDictionary<string, FunctionExport> FunctionExportMap { get; } = new Dictionary<string, FunctionExport>();
		internal TypeVisitor TypeVisitor { get; set; }

		public ASTProcessor() {
			FunctionProcessor = new FunctionProcessor(this);
			StructureProcessor = new StructureProcessor(this);
			EnumerationProcessor = new EnumerationProcessor(this);
			MacroProcessor = new MacroProcessor(this);
			IgnoreUnitNames = new HashSet<string>();
		}

		public bool IsKnownUnitName(string name) {
			return _units.ContainsKey(name);
		}

		public void AddUnit(IDefinition definition) {
			if (IgnoreUnitNames.Contains(definition.Name)) return;
			_units[definition.Name] = definition;
		}

		public void IgnoreUnit(IDefinition definition) {
			IgnoreUnitNames.Add(definition.Name);
			_units.Remove(definition.Name);
		}

		public void Process(IEnumerable<TranslationUnit> units) {
			foreach (var translationUnit in units) {
				MacroProcessor.Process(translationUnit);
				EnumerationProcessor.Process(translationUnit);
				StructureProcessor.Process(translationUnit);
				FunctionProcessor.Process(translationUnit);
			}
		}
	}
}
