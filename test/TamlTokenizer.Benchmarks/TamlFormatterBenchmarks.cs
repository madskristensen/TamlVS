using BenchmarkDotNet.Attributes;

namespace TamlTokenizer.Benchmarks;

/// <summary>
/// Benchmarks for the <see cref="TamlFormatter"/> formatting performance.
/// </summary>
[MemoryDiagnoser]
public class TamlFormatterBenchmarks
{
    private TamlFormatter _defaultFormatter = null!;
    private TamlFormatter _aligningFormatter = null!;
    private string _unformattedSmall = null!;
    private string _unformattedMedium = null!;
    private string _unformattedLarge = null!;

    [GlobalSetup]
    public void Setup()
    {
        _defaultFormatter = new TamlFormatter();
        _aligningFormatter = new TamlFormatter(new TamlFormatterOptions { AlignValues = true });

        _unformattedSmall = GenerateUnformattedSmall();
        _unformattedMedium = GenerateUnformattedMedium();
        _unformattedLarge = GenerateUnformattedLarge();
    }

    [Benchmark(Description = "Format small (~20 lines)")]
    public string Format_Small()
    {
        return _defaultFormatter.Format(_unformattedSmall);
    }

    [Benchmark(Baseline = true, Description = "Format medium (~200 lines)")]
    public string Format_Medium()
    {
        return _defaultFormatter.Format(_unformattedMedium);
    }

    [Benchmark(Description = "Format large (~1000 lines)")]
    public string Format_Large()
    {
        return _defaultFormatter.Format(_unformattedLarge);
    }

    [Benchmark(Description = "Format medium with alignment")]
    public string Format_Medium_WithAlignment()
    {
        return _aligningFormatter.Format(_unformattedMedium);
    }

    /// <summary>
    /// Generates a small unformatted TAML document.
    /// </summary>
    private static string GenerateUnformattedSmall()
    {
        return """
            # Config
            server
            	host	localhost
            	port	8080
            database
            	host	db.local
            	port	5432
            	name	app_db
            	credentials
            		user	admin
            		password	secret
            logging
            	level	debug
            	output	console
            """;
    }

    /// <summary>
    /// Generates a medium unformatted TAML document with inconsistent spacing.
    /// </summary>
    private static string GenerateUnformattedMedium()
    {
        var lines = new List<string>(220);
        lines.Add("# Medium document for formatting");

        for (var i = 0; i < 30; i++)
        {
            lines.Add($"module{i}");
            lines.Add($"\tname\tModule {i}");
            lines.Add($"\tversion\t{i}.0.0");
            lines.Add($"\tactive\ttrue");
            lines.Add($"\tdependencies");
            lines.Add($"\t\tcore\t1.0");
            lines.Add($"\t\tutils\t2.0");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Generates a large unformatted TAML document.
    /// </summary>
    private static string GenerateUnformattedLarge()
    {
        var lines = new List<string>(1100);
        lines.Add("# Large document");

        for (var i = 0; i < 100; i++)
        {
            lines.Add($"record{i}");
            lines.Add($"\tid\t{i}");
            lines.Add($"\ttitle\tRecord Title {i}");
            lines.Add($"\tdescription\tA longer description for record number {i}");
            lines.Add($"\tmetadata");
            lines.Add($"\t\tcreated\t2024-01-01");
            lines.Add($"\t\tmodified\t2024-06-15");
            lines.Add($"\t\tauthor\tuser{i % 10}");
            lines.Add($"\ttags");
            lines.Add($"\t\ttag1\timportant");
            lines.Add($"\t\ttag2\treview");
        }

        return string.Join("\n", lines);
    }
}
