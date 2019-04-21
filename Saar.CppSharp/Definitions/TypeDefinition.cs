using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp.Definitions {
	public class TypeDefinition : IDefinition {
		public enum ModifyType {
			Pointer = 0,
			Array = 1,
			ConstPointer = 2,
			ConstArray = 3,
		}

		public string Name { get; set; }
		public string CSharpName { get; set; } = null;
		public global::CppSharp.AST.Type Type { get; set; }
		public IList<ModifyType> Modifies { get; private set; } = Array.Empty<ModifyType>();

		public TypeDefinition AddModify(ModifyType modify) {
			if (Modifies.IsReadOnly) Modifies = new List<ModifyType>(Modifies);
			Modifies.Add(modify);
			return this;
		}

		public TypeDefinition RemoveModify(int index) {
			if (Modifies.IsReadOnly) Modifies = new List<ModifyType>(Modifies);
			Modifies.RemoveAt(index);
			return this;
		}

		public TypeDefinition Clone() => new TypeDefinition {
			Name = Name,
			Type = Type,
			Modifies = Modifies.Count > 0 ? new List<ModifyType>(Modifies) : (IList<ModifyType>)Array.Empty<ModifyType>(),
		};

	}
}
