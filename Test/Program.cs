using Saar.CppSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Test {
	class Program {

		static void Main(string[] args) {
			var avutilExports = DllReader.ReadFunctions(@"D:\Develop\ffmpeg-4.1-win64-shared\bin\avutil-56.dll");
			var avcodecExports = DllReader.ReadFunctions(@"D:\Develop\ffmpeg-4.1-win64-shared\bin\avcodec-58.dll");
			var g = new Generator();
			g.AddExports(avutilExports, "avutil-56");
			g.AddExports(avcodecExports, "avcodec-58");
			g.Processor.MacroProcessor.Filter += MacroProcessor_Filter;

			g.IncludeDirs.Add(@"D:\Develop\ffmpeg-4.1-win64-dev\include");
			g.Defines.Add("__STDC_CONSTANT_MACROS");

			g.Parse("libavutil/avutil.h");
			g.Parse("libavutil/audio_fifo.h");
			g.Parse("libavutil/channel_layout.h");
			g.Parse("libavutil/cpu.h");
			g.Parse("libavutil/frame.h");
			g.Parse("libavutil/opt.h");
			g.Parse("libavutil/imgutils.h");
			g.Parse("libavutil/timecode.h");
			g.Parse("libavutil/hwcontext.h");
			g.Parse("libavutil/hwcontext_dxva2.h");
			g.Parse("libavutil/hwcontext_d3d11va.h");

			g.Parse("libswresample/swresample.h");

			g.Parse("libpostproc/postprocess.h");

			g.Parse("libswscale/swscale.h");

			g.Parse("libavcodec/avcodec.h");

			g.Parse("libavformat/avformat.h");

			g.Parse("libavfilter/avfilter.h");
			g.Parse("libavfilter/buffersrc.h");
			g.Parse("libavfilter/buffersink.h");

			g.Parse("libavdevice/avdevice.h");

			g.ParseFinal();

			using (var writer = new CSharpWriter(@"Z:\macro.cs")) {
				using (writer.BeginClass("Constant")) {
					writer.WriteMacros(g.Processor.Units);
				}
			}
			using (var writer = new CSharpWriter(@"Z:\enum.cs", CSharpWriterConfig.GetDefaultEnumConfig("Saar"))) {
				writer.WriteEnums(g.Processor.Units);
			}
			using (var writer = new CSharpWriter(@"Z:\struct.cs", CSharpWriterConfig.GetDefaultStructConfig("Saar"))) {
				writer.WriteStructs(g.Processor.Units);
			}
			using (var writer = new CSharpWriter(@"Z:\delegate.cs", CSharpWriterConfig.GetDefaultDelegateConfig("Saar"))) {
				writer.WriteDelegates(g.Processor.Units);
			}
			using (var writer = new CSharpWriter(@"Z:\function.cs", CSharpWriterConfig.GetDefaultFunctionConfig("Saar"))) {
				using (writer.BeginClass("FFmpeg")) {
					writer.WriteFunctions(g.Processor.Units);
				}
			}
			using (var writer = new CSharpWriter(@"Z:\array.cs", CSharpWriterConfig.GetDefaultFixedArrayConfig("Saar"))) {
				writer.WriteFixedArrays(g.Processor.Units);
			}
		}

		static readonly Regex macroFilterRegex = new Regex(@"^[a-z\d_]+$", RegexOptions.Singleline | RegexOptions.Compiled);

		private static bool MacroProcessor_Filter(CppSharp.AST.MacroDefinition macro) {
			return macroFilterRegex.IsMatch(macro.Name);
		}
	}
}
