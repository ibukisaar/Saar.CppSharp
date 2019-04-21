using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp.Definitions {
	public interface IDefinition {
		string Name { get; }
		string CSharpName { get; }
	}
}
