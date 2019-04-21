using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp.Definitions {
	public interface IField {
		string Name { get; set; }
		string CSharpName { get; set; }
	}
}
