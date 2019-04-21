using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp.Definitions {
	[DebuggerDisplay("struct {Name}, Field Count:{Fields.Length}")]
	public class StructDefinition : NamedDefinition {
		public StructField[] Fields { get; set; } = Array.Empty<StructField>();
		public bool IsComplete { get; set; }
		public IList<string> Attrs { get; set; } = Array.Empty<string>();

		public void AddAttr(string attr) {
			if (Attrs.IsReadOnly) Attrs = new List<string>(Attrs);
			Attrs.Add(attr);
		}
	}
}
