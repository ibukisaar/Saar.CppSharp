using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp.Definitions {
	public class DelegateDefinition : TypeDefinition, IFunctionDefinition, ICanGenerateXmlDoc {
		public TypeDefinition ReturnType { get; set; }
		public ParamDefinition[] Params { get; set; }
		public string Content { get; set; }
		public string DetailedContent { get; set; }
		public bool InStruct { get; set; }
		public CallingConvention CallingConvention { get; set; } = CallingConvention.Cdecl;
		public string[] ReturnAttrs { get; set; } = Array.Empty<string>();
	}
}
