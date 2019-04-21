using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp.Definitions {
	public interface ICanGenerateXmlDoc {
		string Content { get; set; }
		string DetailedContent { get; set; }
	}
}
