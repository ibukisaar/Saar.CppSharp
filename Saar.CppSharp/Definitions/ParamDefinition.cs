using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp.Definitions {
	public class ParamDefinition : NamedDefinition {
		public enum ModifyType {
			None, Ref, Out, In
		}

		public TypeDefinition Type { get; set; }
		public ModifyType Modify { get; set; } = ModifyType.None;
		public string[] Attrs { get; set; } = Array.Empty<string>();
	}
}
