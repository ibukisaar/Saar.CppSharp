﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp.Definitions {
	public abstract class NamedDefinition : IDefinition, ICanGenerateXmlDoc {
		public string Name { get; set; }
		public string CSharpName { get; set; }
		public string Content { get; set; }
		public string DetailedContent { get; set; }
	}
}
