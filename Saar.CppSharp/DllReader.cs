using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saar.CppSharp {
	public static class DllReader {
		private static int ToFileOffset((int VirtualSize, int VirtualAddress, int SizeOfRawData, int PointerToRawData) section, int rva) {
			return rva - section.VirtualAddress + section.PointerToRawData;
		}

		private static string ReadString(BinaryReader reader) {
			StringBuilder buffer = new StringBuilder();
			while (true) {
				char c = reader.ReadChar();
				if (c == 0) return buffer.ToString();
				buffer.Append(c);
			}
		}

		public static string[] ReadFunctions(string fileName) {
			using (var reader = new BinaryReader(File.OpenRead(fileName))) {
				// MZ标志
				if (reader.ReadUInt16() != 0x5a4d) {
					throw new BadImageFormatException($"{fileName} 不是PE文件。");
				}

				// PE头部偏移
				reader.BaseStream.Seek(58, SeekOrigin.Current);
				int peOffset = reader.ReadInt32();
				reader.BaseStream.Seek(peOffset, SeekOrigin.Begin);

				// PE标志
				if (reader.ReadUInt32() != 0x4550) {
					throw new BadImageFormatException($"{fileName} 不是PE文件。");
				}

				bool X64;
				// Machine
				if (reader.ReadUInt16() == 0x8664) {
					X64 = true;
				} else {
					X64 = false;
				}

				int numberOfSections = reader.ReadInt16();
				if (X64) {
					reader.BaseStream.Seek(128, SeekOrigin.Current);
				} else {
					reader.BaseStream.Seek(112, SeekOrigin.Current);
				}

				var exportDict = (VirtualAddress: reader.ReadInt32(), Size: reader.ReadInt32());
				if (exportDict.VirtualAddress == 0) return Array.Empty<string>();
				reader.BaseStream.Seek(15 * 8, SeekOrigin.Current);

				// 所有IMAGE_SECTION_HEADER
				var sections = new (int VirtualSize, int VirtualAddress, int SizeOfRawData, int PointerToRawData)[numberOfSections];
				for (int i = 0; i < numberOfSections; i++) {
					reader.BaseStream.Seek(8, SeekOrigin.Current);
					sections[i] = (reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
					reader.BaseStream.Seek(16, SeekOrigin.Current);
				}

				int exportSectionIndex = -1;

				for (int i = 0; i < numberOfSections; i++) {
					var section = sections[i];
					if (exportDict.VirtualAddress >= section.VirtualAddress && exportDict.VirtualAddress < section.VirtualAddress + Math.Max(section.VirtualSize, section.SizeOfRawData)) {
						exportSectionIndex = i;
						break;
					}
				}

				if (exportSectionIndex < 0) throw new BadImageFormatException("不能找到IMAGE_EXPORT_DIRECTORY文件偏移");

				var exportSection = sections[exportSectionIndex];
				int exportDictFileOffset = ToFileOffset(exportSection, exportDict.VirtualAddress);
				reader.BaseStream.Seek(exportDictFileOffset + 24, SeekOrigin.Begin);
				var numberOfNames = reader.ReadInt32();
				int addressOfFunctions = reader.ReadInt32();
				int addressOfNames = reader.ReadInt32();
				int addressOfNameOrdinals = reader.ReadInt32();
				var functionNamesFileOffset = ToFileOffset(exportSection, addressOfNames);

				reader.BaseStream.Seek(functionNamesFileOffset, SeekOrigin.Begin);
				string[] functionNames = new string[numberOfNames];
				for (int i = 0; i < numberOfNames; i++) {
					var nameRVA = reader.ReadInt32();
					long currPos = reader.BaseStream.Position;
					reader.BaseStream.Seek(ToFileOffset(exportSection, nameRVA), SeekOrigin.Begin);
					functionNames[i] = ReadString(reader);
					reader.BaseStream.Position = currPos;
				}

				return functionNames;
			}
		}

	}
}
