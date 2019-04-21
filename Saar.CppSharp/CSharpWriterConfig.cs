using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp {
	public sealed class CSharpWriterConfig {
		public readonly static CSharpWriterConfig Default = new CSharpWriterConfig();

		public string[] UsingNamespaces { get; set; } = new string[] { "System" };
		public string Namespace { get; set; }

		public static CSharpWriterConfig GetDefaultEnumConfig(string @namespace)
			=> new CSharpWriterConfig {
				Namespace = @namespace,
				UsingNamespaces = new[] { "System" }
			};

		public static CSharpWriterConfig GetDefaultStructConfig(string @namespace)
			=> new CSharpWriterConfig {
				Namespace = @namespace,
				UsingNamespaces = new[] { "System", "System.Runtime.InteropServices" }
			};

		public static CSharpWriterConfig GetDefaultFunctionConfig(string @namespace)
			=> new CSharpWriterConfig {
				Namespace = @namespace,
				UsingNamespaces = new[] { "System", "System.Runtime.InteropServices" }
			};

		public static CSharpWriterConfig GetDefaultDelegateConfig(string @namespace)
			=> new CSharpWriterConfig {
				Namespace = @namespace,
				UsingNamespaces = new[] { "System", "System.Runtime.InteropServices", "System.Security" }
			};

		public static CSharpWriterConfig GetDefaultFixedArrayConfig(string @namespace)
			=> new CSharpWriterConfig {
				Namespace = @namespace,
				UsingNamespaces = new[] { "System", "System.Runtime.InteropServices", "System.Runtime.CompilerServices" }
			};
	}
}
