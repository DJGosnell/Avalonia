using System.IO;
using Avalonia;
using JetBrains.Profiler.SelfApi;

namespace RenderBenchmark
{
    public class Program
    {
        public static readonly string BasePath = @"C:\Users\mepengadmin\Documents\Projects\AvaloniaTraces\";
        public static readonly string BenchmarkType = @"baseline";
        public static readonly string FullTracePath = Path.Combine(BasePath, "trace", BenchmarkType);
        //public static readonly string FullMemoryPath = Path.Combine(BasePath, "memory", BenchmarkType);
        static void Main(string[] args)
        {
            JetBrains.Profiler.SelfApi.DotTrace.EnsurePrerequisite();
            //JetBrains.Profiler.SelfApi.DotMemory.EnsurePrerequisite();

            if (!Directory.Exists(FullTracePath))
                Directory.CreateDirectory(FullTracePath);

            //if (!Directory.Exists(FullMemoryPath))
            //    Directory.CreateDirectory(FullMemoryPath);

            var traceConfig = new DotTrace.Config();
            traceConfig.SaveToDir(FullTracePath);
            traceConfig.UseLogFile(Path.Combine(FullTracePath, "trace.log")).UseTimelineProfilingType(false);

            JetBrains.Profiler.SelfApi.DotTrace.Attach(traceConfig);
            JetBrains.Profiler.SelfApi.DotTrace.StartCollectingData();

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
    }
}
