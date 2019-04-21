using CppSharp.AST;
using CppSharp.AST.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Type = CppSharp.AST.Type;

namespace Saar.CppSharp.Processors {
	public class StructureProcessor {
		public delegate bool FilterHandler(Class @class);
		public event FilterHandler Filter;

		private ASTProcessor context;

		public StructureProcessor(ASTProcessor context) {
			this.context = context;
		}

		private static IEnumerable<Class> GetStructs(TranslationUnit translationUnit) {
			foreach (var typedef in translationUnit.Typedefs) {
				if (!typedef.Type.TryGetClass(out Class @class))
					continue;
				yield return @class;
			}
		}

		public void Process(TranslationUnit translationUnit) {
			var structs = GetStructs(translationUnit);
			if (Filter != null) {
				var filters = Array.ConvertAll(Filter.GetInvocationList(), filter => (FilterHandler)filter);
				structs = structs.Where(s => !filters.Any(filter => filter(s)));
			}

			foreach (var s in structs) {
				MakeDefinition(s, null);
			}
		}

		internal Definitions.TypeDefinition GetTypeDefinition(Type type, string name, bool inStruct) {
			if (name != null && context.UnitsMap.TryGetValue(name, out var def) && def is Definitions.TypeDefinition t) return t;
			var visitType = context.TypeVisitor.Visit(type);
			if (visitType != null) {
				return visitType;
			}

			switch (type) {
				case TypedefType declaration when declaration.Declaration.Name == "size_t":
					return new Definitions.TypeDefinition { Name = "size_t" };
				case TypedefType declaration:
					return GetTypeDefinition(declaration.Declaration.Type, declaration.Declaration.Name, inStruct);
				case ArrayType arrayType when arrayType.SizeType == ArrayType.ArraySize.Constant:
					return GetFieldTypeForFixedArray(arrayType);
				case TagType tagType:
					return GetFieldTypeForNestedDeclaration(tagType.Declaration, name);
				case PointerType pointerType:
					return GetTypeDefinitionForPointer(pointerType, name, inStruct);
				case FunctionType functionType:
					return context.FunctionProcessor.GetDelegateType(functionType, name, inStruct);
				default:
					return new Definitions.TypeDefinition { Name = TypeHelper.GetTypeName(type) };
			}
		}

		private void MakeDefinition(Class @class, string name) {
			name = string.IsNullOrEmpty(@class.Name) ? name : @class.Name;
			if (string.IsNullOrEmpty(name)) return;

			Definitions.StructDefinition definition = null;
			if (!(context.UnitsMap.TryGetValue(name, out var idef) && idef is Definitions.StructDefinition structDefinition)) {
				definition = new Definitions.StructDefinition { Name = name };
				context.AddUnit(definition);
			} else {
				definition = structDefinition;
			}

			if (!@class.IsIncomplete && !definition.IsComplete) {
				definition.IsComplete = true;

				var bitFieldNames = new List<string>();
				var bitFieldComments = new List<string>();
				long bitCounter = 0;
				var fields = new List<Definitions.StructField>();
				void FlushBitFields() {
					fields.Add(GetBitField(bitFieldNames, bitCounter, bitFieldComments));
					bitFieldNames.Clear();
					bitFieldComments.Clear();
					bitCounter = 0;
				}

				foreach (var field in @class.Fields) {
					if (field.IsBitField) {
						bitFieldNames.Add($"{field.Name}{field.BitWidth}");
						bitFieldComments.Add(field.Comment?.BriefText ?? string.Empty);
						bitCounter += field.BitWidth;
						continue;
					} else if (bitCounter > 0) {
						FlushBitFields();
					}

					var typeName = field.Class.Name + "_" + NamedTool.ToCamelNamed(field.Name);
					fields.Add(new Definitions.StructField {
						Name = field.Name,
						FieldType = GetTypeDefinition(field.Type, typeName, true),
						Content = field.Comment?.BriefText,
						DetailedContent = field.Comment?.Text,
						IsObsolete = FunctionProcessor.IsObsolete(field),
						ObsoleteMessage = FunctionProcessor.GetObsoleteMessage(field),
					});
				}

				if (bitCounter > 0) FlushBitFields();

				definition.Fields = fields.ToArray();
				definition.Content = @class.Comment?.BriefText;
				definition.DetailedContent = @class.Comment?.Text;
			}
		}

		private static Definitions.StructField GetBitField(IEnumerable<string> names, long bitCounter, List<string> comments) {
			var fieldName = string.Join("_", names);
			string fieldType = (bitCounter + 7) / 8 switch
			{
				1 => "byte",
				2 => "short",
				3 => "int",
				4 => "long",
				_ => throw new NotSupportedException()
			};
			return new Definitions.StructField {
				Name = fieldName,
				FieldType = new Definitions.TypeDefinition { Name = fieldType },
				Content = string.Join(" ", comments.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()))
			};
		}

		private Definitions.TypeDefinition GetTypeDefinitionForPointer(PointerType pointerType, string name, bool inStruct) {
			var pointee = pointerType.Pointee;
			var pointerTypeDefinition = GetTypeDefinition(pointee, name, inStruct);
			if (!(pointerTypeDefinition is Definitions.DelegateDefinition)) {
				pointerTypeDefinition.AddModify(pointerType.QualifiedPointee.Qualifiers.IsConst
					? Definitions.TypeDefinition.ModifyType.ConstPointer
					: Definitions.TypeDefinition.ModifyType.Pointer);
			}
			return pointerTypeDefinition;
		}

		private Definitions.TypeDefinition GetFieldTypeForNestedDeclaration(Declaration declaration, string name) {
			var typeName = string.IsNullOrEmpty(declaration.Name) ? name : declaration.Name;
			if (declaration is Class @class) {
				MakeDefinition(@class, typeName);
				return new Definitions.TypeDefinition { Name = typeName };
			}
			if (declaration is Enumeration @enum) {
				context.EnumerationProcessor.MakeDefinition(@enum, typeName);
				return new Definitions.TypeDefinition { Name = typeName };
			}
			throw new NotSupportedException();
		}


		private Definitions.TypeDefinition GetFieldTypeForFixedArray(ArrayType arrayType) {
			var fixedSize = (int)arrayType.Size;

			var elementType = arrayType.Type;
			var elementTypeDefinition = GetTypeDefinition(elementType, null, true);

			var name = elementTypeDefinition.Name + "_array" + fixedSize;
			if (elementType.IsPointer()) name = TypeHelper.GetTypeName(elementType.GetPointee()) + "_ptrArray" + fixedSize;
			if (elementType is ArrayType elementArrayType) {
				fixedSize /= (int)elementArrayType.Size;
				name = TypeHelper.GetTypeName(elementArrayType.Type) + "_arrayOfArray" + fixedSize;
			}

			if (!context.IsKnownUnitName(name)) {
				var fixedArray = new Definitions.FixedArrayDefinition {
					Name = name,
					Size = fixedSize,
					ElementType = elementTypeDefinition,
					IsPrimitive = elementType.IsPrimitiveType()
				};
				context.AddUnit(fixedArray);
			}

			return new Definitions.TypeDefinition { Name = name, Type = arrayType };
		}
	}
}