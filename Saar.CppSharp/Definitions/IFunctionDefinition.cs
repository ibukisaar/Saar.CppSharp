using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp.Definitions {
	public interface IFunctionDefinition : IDefinition, ICanGenerateXmlDoc {
		TypeDefinition ReturnType { get; set; }
		ParamDefinition[] Params { get; set; }
		string[] ReturnAttrs { get; set; }
		CallingConvention CallingConvention { get; set; }
	}
}
