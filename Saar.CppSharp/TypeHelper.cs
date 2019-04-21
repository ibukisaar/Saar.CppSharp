using CppSharp.AST;
using Saar.CppSharp.Processors;
using System;
using System.Linq;
using Type = CppSharp.AST.Type;

namespace Saar.CppSharp {
	public static class TypeHelper {
		public static Definitions.TypeDefinition GetType(Type type) {
			return type switch
			{
				PointerType x => GetType(x.QualifiedPointee.Type).AddModify(Definitions.TypeDefinition.ModifyType.Pointer),
				BuiltinType x => new Definitions.TypeDefinition { Name = GetTypeName(x.Type), Type = x },
				TypedefType x => x.Declaration.Type switch
				{
					BuiltinType y => GetType(y),
					PointerType y when !(y.QualifiedPointee.Type is FunctionType) => GetType(y),
					_ => new Definitions.TypeDefinition { Name = x.Declaration.Name, Type = type }
				},
				TagType x => new Definitions.TypeDefinition { Name = x.Declaration.Name, Type = x },
				ArrayType x => GetType(x.Type).AddModify(Definitions.TypeDefinition.ModifyType.Array),
				_ => throw new NotSupportedException()
			};
		}

		public static string GetTypeName(Type type) {
			switch (type) {
				case PointerType x: return GetTypeName(x.QualifiedPointee.Type) + "*";
				case BuiltinType x: return GetTypeName(x.Type);
				case TypedefType x: return GetTypeName(x);
				case TagType x: return x.Declaration.Name;
				case ArrayType x: return GetTypeName(x.Type) + "[]";
				case AttributedType _: return GetTypeName(PrimitiveType.Void);
				default: throw new NotSupportedException();
			}
		}

		private static string GetTypeName(TypedefType type) {
			switch (type.Declaration.Type) {
				case BuiltinType x: return GetTypeName(x);
				case PointerType x when !(x.QualifiedPointee.Type is FunctionType): return GetTypeName(x);
				default: return type.Declaration.Name;
			}
		}

		private static string GetTypeName(PrimitiveType type) {
			return type switch
			{
				PrimitiveType.Void => "void",
				PrimitiveType.Bool => "bool",
				PrimitiveType.Char => "byte",
				PrimitiveType.UChar => "byte",
				PrimitiveType.SChar => "sbyte",
				PrimitiveType.Short => "short",
				PrimitiveType.UShort => "ushort",
				PrimitiveType.Int => "int",
				PrimitiveType.UInt => "uint",
				PrimitiveType.Long => "long",
				PrimitiveType.ULong => "ulong",
				PrimitiveType.LongLong => "long",
				PrimitiveType.ULongLong => "ulong",
				PrimitiveType.Float => "float",
				PrimitiveType.Double => "double",
				PrimitiveType.IntPtr => "IntPtr",
				PrimitiveType.UIntPtr => "UIntPtr",
				_ => throw new NotSupportedException()
			};
		}

		public static string ToCSharpType(string type) {
			return type switch
			{
				"unsigned" => "uint",
				"signed" => "int",
				"char" => "byte",
				"int8_t" => "sbyte",
				"uint8_t" => "byte",
				"int16_t" => "short",
				"uint16_t" => "ushort",
				"int32_t" => "int",
				"uint32_t" => "uint",
				"int64_t" => "long",
				"uint64_t" => "ulong",
				_ => type
			};
		}
	}
}
