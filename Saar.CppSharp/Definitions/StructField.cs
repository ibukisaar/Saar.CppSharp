using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp.Definitions {
	public class StructField : IField, ICanGenerateXmlDoc {
		public string Name { get; set; }
		public string CSharpName { get; set; }
		public TypeDefinition FieldType { get; set; }
		public string Content { get; set; }
		public string DetailedContent { get; set; }
		public string[] Attrs { get; set; } = Array.Empty<string>();
		public int FixedSize { get; set; }
		public bool IsObsolete { get; set; }
		public string ObsoleteMessage { get; set; }
	}
}
