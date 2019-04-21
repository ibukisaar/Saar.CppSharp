using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Saar.CppSharp {
	public static class NamedTool {
		static readonly HashSet<string> keyWords = new HashSet<string> {
			"abstract",     "as",           "base",         "bool",
			"break",        "byte",         "case",         "catch",
			"char",         "checked",      "class",        "const",
			"continue",     "decimal",      "default",      "delegate",
			"do",           "double",       "else",         "enum",
			"event",        "explicit",     "extern",       "false",
			"finally",      "fixed",        "float",        "for",
			"foreach",      "goto",         "if",           "implicit",
			"in",           "int",          "interface",    "internal",
			"is",           "lock",         "long",         "namespace",
			"new",          "null",         "object",       "operator",
			"out",          "override",     "params",       "private",
			"protected",    "public",       "readonly",     "ref",
			"return",       "sbyte",        "sealed",       "short",
			"sizeof",       "stackalloc",   "static",       "string",
			"struct",       "switch",       "this",         "throw",
			"true",         "try",          "typeof",       "uint",
			"ulong",        "unchecked",    "unsafe",       "ushort",
			"using",        "virtual",      "void",         "volatile",
			"while",
		};

		static readonly Regex splitWordRegex = new Regex(@"([A-Z\d][A-Z\d]*|[A-Z][a-z\d]*|[a-z\d][a-z\d]*)(?=[A-Z]|$|_)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture);

		public static bool IsKeyWord(string word) => keyWords.Contains(word);

		public static string VarName(string word) => IsKeyWord(word) ? $"@{word}" : word;

		public static string[] SplitWord(string name) {
			return splitWordRegex.Matches(name).Cast<Match>().Select(m => m.Value).ToArray();
		}

		public static string ToCamelNamed(IEnumerable<string> words) {
			return string.Concat(words.Select(FirstUppercase));
		}

		public static string ToCamelNamed(string name, int skipWords = 0) {
			return ToCamelNamed(SplitWord(name).Skip(skipWords));
		}

		unsafe private static string FirstUppercase(string word) {
			if (string.IsNullOrEmpty(word)) return string.Empty;
			char* chars = stackalloc char[word.Length];
			chars[0] = char.ToUpper(word[0]);
			for (int i = 1; i < word.Length; i++) {
				chars[i] = char.ToLower(word[i]);
			}
			return new string(chars, 0, word.Length);
		}
	}
}
