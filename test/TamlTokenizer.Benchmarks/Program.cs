using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using TamlTokenizer.Benchmarks;

// Run all benchmarks in Release mode
// Usage: dotnet run -c Release
// Or run specific benchmark: dotnet run -c Release -- --filter "*Lexer*"

IConfig config = DefaultConfig.Instance;

#if DEBUG
Console.WriteLine("WARNING: Running benchmarks in DEBUG mode. Results will not be accurate.");
Console.WriteLine("Run with: dotnet run -c Release");
Console.WriteLine();
#endif

BenchmarkSwitcher.FromAssembly(typeof(TamlLexerBenchmarks).Assembly).Run(args, config);
