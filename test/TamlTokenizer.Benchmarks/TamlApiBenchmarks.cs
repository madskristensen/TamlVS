using BenchmarkDotNet.Attributes;

namespace TamlTokenizer.Benchmarks;

/// <summary>
/// Benchmarks for the high-level <see cref="Taml"/> API with different parser modes.
/// </summary>
[MemoryDiagnoser]
public class TamlApiBenchmarks
{
    private string _validInput = null!;
    private string _inputWithErrors = null!;
    private TamlParserOptions _defaultOptions = null!;
    private TamlParserOptions _strictOptions = null!;

    [GlobalSetup]
    public void Setup()
    {
        _validInput = GenerateValidTaml();
        _inputWithErrors = GenerateTamlWithErrors();
        _defaultOptions = TamlParserOptions.Default;
        _strictOptions = new TamlParserOptions { StrictMode = true };
    }

    [Benchmark(Baseline = true, Description = "Tokenize valid TAML (default)")]
    public TamlParseResult Tokenize_Valid_DefaultMode()
    {
        return Taml.Tokenize(_validInput, _defaultOptions);
    }

    [Benchmark(Description = "Tokenize valid TAML (strict)")]
    public TamlParseResult Tokenize_Valid_StrictMode()
    {
        return Taml.Tokenize(_validInput, _strictOptions);
    }

    [Benchmark(Description = "Tokenize TAML with errors (default)")]
    public TamlParseResult Tokenize_WithErrors_DefaultMode()
    {
        return Taml.Tokenize(_inputWithErrors, _defaultOptions);
    }

    [Benchmark(Description = "Tokenize TAML with errors (strict)")]
    public TamlParseResult Tokenize_WithErrors_StrictMode()
    {
        return Taml.Tokenize(_inputWithErrors, _strictOptions);
    }

    /// <summary>
    /// Generates valid TAML content for benchmarking.
    /// </summary>
    private static string GenerateValidTaml()
    {
        var lines = new List<string>(550)
        {
            "# Valid TAML document"
        };

        for (var i = 0; i < 50; i++)
        {
            lines.Add($"item{i}");
            lines.Add($"\tname\tItem {i}");
            lines.Add($"\tvalue\t{i * 100}");
            lines.Add($"\tenabled\t{(i % 2 == 0 ? "true" : "false")}");
            lines.Add($"\tconfig");
            lines.Add($"\t\toption_a\tvalue_a_{i}");
            lines.Add($"\t\toption_b\tvalue_b_{i}");
            lines.Add($"\t\tnested");
            lines.Add($"\t\t\tdeep_key\tdeep_value_{i}");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Generates TAML content with intentional structure variations for error recovery testing.
    /// </summary>
    private static string GenerateTamlWithErrors()
    {
        var lines = new List<string>(300)
        {
            "# TAML with some issues"
        };

        for (var i = 0; i < 30; i++)
        {
            lines.Add($"section{i}");
            lines.Add($"\tkey{i}\tvalue{i}");

            // Add some comments
            if (i % 5 == 0)
            {
                lines.Add($"\t# Comment in section {i}");
            }

            lines.Add($"\tnested");
            lines.Add($"\t\tinner_key\tinner_value");

            // Add blank lines occasionally
            if (i % 7 == 0)
            {
                lines.Add("");
            }
        }

        return string.Join("\n", lines);
    }
}
