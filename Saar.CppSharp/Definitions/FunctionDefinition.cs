using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp.Definitions {
	public class FunctionDefinition : NamedDefinition {
		public string LibraryName { get; set; }
		public bool IsConstLibraryName { get; set; }
		public TypeDefinition ReturnType { get; set; }
		public ParamDefinition[] Params { get; set; }
		public bool IsObsolete { get; set; }
		public string ObsoleteMessage { get; set; }
		public CallingConvention CallingConvention { get; set; } = CallingConvention.Cdecl;
		public string[] ReturnAttrs { get; set; } = Array.Empty<string>();
	}
}
