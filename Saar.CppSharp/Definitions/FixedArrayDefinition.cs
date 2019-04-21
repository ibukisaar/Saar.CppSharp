using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp.Definitions {
	public class FixedArrayDefinition : NamedDefinition {
		public TypeDefinition ElementType { get; set; }
		public int Size { get; set; }
		public bool IsPrimitive { get; set; }
	}
}
