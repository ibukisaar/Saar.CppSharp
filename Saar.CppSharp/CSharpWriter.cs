using Saar.CppSharp.MacroParse;
using Saar.CppSharp.Processors;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Security;

namespace Saar.CppSharp {
	public class CSharpWriter : IDisposable {
		private IDisposable namespaceBlock;

		public IndentedTextWriter Writer { get; }

		public CSharpWriter(string file) : this(file, CSharpWriterConfig.Default) {
		}

		public CSharpWriter(string file, CSharpWriterConfig config) {
			Writer = new IndentedTextWriter(new StreamWriter(File.Open(file, FileMode.Create, FileAccess.Write, FileShare.None)));

			foreach (var @using in config.UsingNamespaces) {
				Writer.WriteLine($"using {@using};");
			}
			Writer.WriteLine();
			if (!string.IsNullOrEmpty(config.Namespace)) {
				namespaceBlock = BeginBlock($"namespace {config.Namespace} ");
			}
		}

		public IDisposable BeginBlock(string text = "", bool inline = false) {
			Writer.Write(text);
			Writer.WriteLine("{");
			Writer.Indent++;
			return new End(() => {
				Writer.Indent--;
				if (inline) {
					Writer.Write("}");
				} else {
					Writer.WriteLine("}");
				}
			});
		}

		public IDisposable BeginClass(string className)
			=> BeginBlock($"unsafe public static partial class {className} ");

		public void WriteMacro(Definitions.MacroDefinition macro) {
			// WriteSummary(macro);
			if (macro.IsConst) {
				Writer.WriteLine($"public const {macro.TypeName} {macro.CSharpName} = {macro.CSharpExpr};");
			} else if (macro.TypeName != null && macro.CSharpExpr != null) {
				Writer.WriteLine($"public readonly static {macro.TypeName} {macro.CSharpName} = {macro.CSharpExpr};");
			} else {
				Writer.WriteLine($"// public readonly static int {macro.CSharpName} = {macro.ExprString};");
			}
		}

		public void WriteEnum(Definitions.EnumDefinition @enum) {
			WriteSummary(@enum);
			if (@enum.IsFlags) Writer.WriteLine("[Flags]");
			using (BeginBlock($"public enum {@enum.CSharpName} : {@enum.TypeName} ")) {
				foreach (var item in @enum.Items) {
					WriteSummary(item);
					Writer.WriteLine($"{item.CSharpName} = {item.Value},");
				}
			}
		}

		public void WriteStruct(Definitions.StructDefinition @struct) {
			WriteSummary(@struct);
			Writer.WriteLine("[StructLayout(LayoutKind.Sequential)]");
			using (BeginBlock($"unsafe public struct {@struct.CSharpName} ")) {
				foreach (var field in @struct.Fields) {
					WriteSummary(field);
					foreach (var attr in field.Attrs) {
						Writer.WriteLine($"[{attr}]");
					}
					if (field.IsObsolete) {
						Writer.WriteLine($@"[Obsolete(@""{field.ObsoleteMessage?.Replace("\"", "\"\"") ?? string.Empty}"")]");
					}
					if (field.FixedSize == 0) {
						Writer.WriteLine($"public {field.FieldType.CSharpName} {field.CSharpName};");
					} else {
						Writer.WriteLine($"public fixed {field.FieldType.CSharpName} {field.CSharpName}[{field.FixedSize}];");
					}
				}
			}
		}

		static string GetParamString(Definitions.ParamDefinition p, int i) {
			string modify = p.Modify switch
			{
				Definitions.ParamDefinition.ModifyType.None => "",
				Definitions.ParamDefinition.ModifyType.Out => "out ",
				Definitions.ParamDefinition.ModifyType.Ref => "ref ",
				Definitions.ParamDefinition.ModifyType.In => "in ",
				_ => throw new InvalidOperationException(),
			};
			var sb = new StringBuilder();
			if (p.Attrs.Length > 0) {
				sb.Append($"[{string.Join(", ", p.Attrs)}] ");
			}
			sb.Append(modify).Append(p.Type.CSharpName).Append(' ').Append(NamedTool.VarName(p.CSharpName));
			return sb.ToString();
		}

		static string GetArgString(Definitions.ParamDefinition p, int i) {
			string modify = p.Modify switch
			{
				Definitions.ParamDefinition.ModifyType.None => "",
				Definitions.ParamDefinition.ModifyType.Out => "out ",
				Definitions.ParamDefinition.ModifyType.Ref => "ref ",
				Definitions.ParamDefinition.ModifyType.In => "in ",
				_ => throw new InvalidOperationException(),
			};
			string paramName = !string.IsNullOrEmpty(p.CSharpName) ? NamedTool.VarName(p.CSharpName) : "p" + i;
			return modify + paramName;
		}

		public void WriteDelegate(Definitions.DelegateDefinition @delegate) {
			void WriteAttrs() {
				Writer.WriteLine("[SuppressUnmanagedCodeSecurity]");
				Writer.WriteLine($"[UnmanagedFunctionPointer(CallingConvention.{@delegate.CallingConvention})]");
				if (@delegate.ReturnAttrs.Length > 0) Writer.WriteLine($"return: [{string.Join(", ", @delegate.ReturnAttrs)}]");
			}

			WriteAttrs();
			if (!@delegate.InStruct) {
				Writer.WriteLine($"unsafe public delegate {@delegate.ReturnType.CSharpName} {@delegate.CSharpName}({string.Join(", ", @delegate.Params.Select(GetParamString))});");
			} else {
				Writer.WriteLine($"unsafe public delegate {@delegate.ReturnType.CSharpName} {@delegate.CSharpName}_Func({string.Join(", ", @delegate.Params.Select(GetParamString))});");
				using (BeginBlock($"unsafe public struct {@delegate.CSharpName} ")) {
					Writer.WriteLine("public IntPtr DelegatePointer;");
					Writer.WriteLine($"public {@delegate.ReturnType.CSharpName} Invoke({string.Join(", ", @delegate.Params.Select(GetParamString))}) => Marshal.GetDelegateForFunctionPointer<{@delegate.CSharpName}_Func>(DelegatePointer)({string.Join(", ", @delegate.Params.Select(GetArgString))});");
				}
			}
			Writer.WriteLine();
		}

		public void WriteFunction(Definitions.FunctionDefinition function) {
			WriteSummary(function);
			foreach (var p in function.Params) {
				if (string.IsNullOrEmpty(p.Content)) continue;
				Writer.WriteLine($@"/// <param name=""{p.CSharpName}"">{SecurityElement.Escape(p.Content.Trim())}</param>");
			}
			string libraryName = function.IsConstLibraryName ? $@"""{function.LibraryName}""" : function.LibraryName;
			Writer.Write($@"[DllImport({libraryName}, CallingConvention = CallingConvention.{function.CallingConvention}");
			if (function.CSharpName != function.Name) {
				Writer.WriteLine($@", EntryPoint = ""{function.Name}"")]");
			} else {
				Writer.WriteLine(")]");
			}
			if (function.IsObsolete) {
				Writer.WriteLine($@"[Obsolete(@""{function.ObsoleteMessage?.Replace("\"", "\"\"") ?? string.Empty}"")]");
			}
			if (function.ReturnAttrs.Length > 0) Writer.WriteLine($"[return: {string.Join(", ", function.ReturnAttrs)}]");
			Writer.WriteLine($"public static extern {function.ReturnType.CSharpName} {function.CSharpName}({string.Join(", ", function.Params.Select(GetParamString))});");
			Writer.WriteLine();
		}

		public void WriteFixedArray(Definitions.FixedArrayDefinition fixedArray) {
			Writer.WriteLine("[StructLayout(LayoutKind.Sequential)]");
			using (BeginBlock($"unsafe public struct {fixedArray.CSharpName} ")) {
				Writer.WriteLine($"public const int Size = {fixedArray.Size};");
				Writer.WriteLine($"public readonly static Type ElementType = typeof({fixedArray.ElementType.CSharpName});");
				Writer.WriteLine();
				if (fixedArray.IsPrimitive) {
					Writer.WriteLine($"public fixed {fixedArray.ElementType.CSharpName} _[{fixedArray.Size}];");
				} else {
					Writer.WriteLine($"public {fixedArray.ElementType.CSharpName} {string.Join(", ", Enumerable.Range(0, fixedArray.Size).Select(i => "_" + i))};");
				}
			}
			Writer.WriteLine();
		}

		public void WriteMacros(IEnumerable<Definitions.IDefinition> defs) {
			foreach (var m in defs.OfType<Definitions.MacroDefinition>()) WriteMacro(m);
		}

		public void WriteEnums(IEnumerable<Definitions.IDefinition> defs) {
			foreach (var e in defs.OfType<Definitions.EnumDefinition>()) WriteEnum(e);
		}

		public void WriteStructs(IEnumerable<Definitions.IDefinition> defs) {
			foreach (var s in defs.OfType<Definitions.StructDefinition>()) WriteStruct(s);
		}

		public void WriteDelegates(IEnumerable<Definitions.IDefinition> defs) {
			foreach (var d in defs.OfType<Definitions.DelegateDefinition>()) WriteDelegate(d);
		}

		public void WriteFunctions(IEnumerable<Definitions.IDefinition> defs) {
			foreach (var f in defs.OfType<Definitions.FunctionDefinition>()) WriteFunction(f);
		}

		public void WriteFixedArrays(IEnumerable<Definitions.IDefinition> defs) {
			foreach (var fa in defs.OfType<Definitions.FixedArrayDefinition>()) WriteFixedArray(fa);
		}

		private void WriteSummary(Definitions.ICanGenerateXmlDoc value) {
			string summary = null;
			if (!string.IsNullOrWhiteSpace(value.Content)) {
				summary = SecurityElement.Escape(value.Content.Trim());
			}
			if (summary == null) return;
			var ss = summary.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
			ss = Array.ConvertAll(ss, s => $"/// {s.Trim()}");
			Writer.WriteLine("/// <summary>");
			foreach (var s in ss) {
				Writer.WriteLine(s);
			}
			Writer.WriteLine("/// </summary>");
		}

		void IDisposable.Dispose() {
			if (namespaceBlock != null) {
				namespaceBlock.Dispose();
				namespaceBlock = null;
			}
			Writer.Flush();
			Writer.Dispose();
		}


		private class End : IDisposable {
			private readonly Action action;

			public End(Action action) => this.action = action;

			public void Dispose() => action();
		}
	}
}
