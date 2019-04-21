using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Saar.CppSharp.Definitions {
	[DebuggerDisplay("enum {Name}, Item Count:{Items.Length}")]
	public class EnumDefinition : NamedDefinition {
		public string TypeName { get; set; }
		public IList<EnumItem> Items { get; set; } = Array.Empty<EnumItem>();
		public bool IsFlags { get; set; } = false;

		public void AddItem(EnumItem item) {
			if (Items is EnumItem[] array) Items = new List<EnumItem>(array);
			Items.Add(item);
		}
	}
}
