using BenchmarkDotNet.Attributes;

namespace TamlTokenizer.Benchmarks;

/// <summary>
/// Benchmarks for the <see cref="TamlLexer"/> tokenization performance.
/// </summary>
[MemoryDiagnoser]
public class TamlLexerBenchmarks
{
    private string _smallInput = null!;
    private string _mediumInput = null!;
    private string _largeInput = null!;
    private string _deeplyNestedInput = null!;
    private string _wideInput = null!;

    [GlobalSetup]
    public void Setup()
    {
        _smallInput = GenerateSmallTaml();
        _mediumInput = GenerateMediumTaml();
        _largeInput = GenerateLargeTaml();
        _deeplyNestedInput = GenerateDeeplyNestedTaml();
        _wideInput = GenerateWideTaml();
    }

    [Benchmark(Description = "Small TAML (~20 lines)")]
    public List<TamlToken> Tokenize_Small()
    {
        var lexer = new TamlLexer(_smallInput);
        return lexer.Tokenize();
    }

    [Benchmark(Description = "Medium TAML (~200 lines)")]
    public List<TamlToken> Tokenize_Medium()
    {
        var lexer = new TamlLexer(_mediumInput);
        return lexer.Tokenize();
    }

    [Benchmark(Baseline = true, Description = "Large TAML (~2000 lines)")]
    public List<TamlToken> Tokenize_Large()
    {
        var lexer = new TamlLexer(_largeInput);
        return lexer.Tokenize();
    }

    [Benchmark(Description = "Deeply nested (20 levels)")]
    public List<TamlToken> Tokenize_DeeplyNested()
    {
        var lexer = new TamlLexer(_deeplyNestedInput);
        return lexer.Tokenize();
    }

    [Benchmark(Description = "Wide TAML (many siblings)")]
    public List<TamlToken> Tokenize_Wide()
    {
        var lexer = new TamlLexer(_wideInput);
        return lexer.Tokenize();
    }

    /// <summary>
    /// Generates a small TAML document (~20 lines).
    /// </summary>
    private static string GenerateSmallTaml()
    {
        return """
            # Configuration file
            server
            	host	localhost
            	port	8080
            	ssl	true
            database
            	connection
            		host	db.example.com
            		port	5432
            		name	myapp
            	pool
            		min	5
            		max	100
            logging
            	level	info
            	file	/var/log/app.log
            	console	true
            features
            	auth	enabled
            	cache	enabled
            """;
    }

    /// <summary>
    /// Generates a medium TAML document (~200 lines).
    /// </summary>
    private static string GenerateMediumTaml()
    {
        var lines = new List<string>(220);
        lines.Add("# Medium configuration");

        for (var i = 0; i < 20; i++)
        {
            lines.Add($"section{i}");
            lines.Add($"\tname\tSection {i}");
            lines.Add($"\tenabled\ttrue");
            lines.Add($"\tpriority\t{i * 10}");
            lines.Add($"\tsubsection");
            lines.Add($"\t\tkey1\tvalue{i}_1");
            lines.Add($"\t\tkey2\tvalue{i}_2");
            lines.Add($"\t\tkey3\tvalue{i}_3");
            lines.Add($"\tmetadata");
            lines.Add($"\t\tcreated\t2024-01-{i + 1:D2}");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Generates a large TAML document (~2000 lines).
    /// </summary>
    private static string GenerateLargeTaml()
    {
        var lines = new List<string>(2100);
        lines.Add("# Large configuration");

        for (var i = 0; i < 200; i++)
        {
            lines.Add($"entity{i}");
            lines.Add($"\tid\t{i}");
            lines.Add($"\tname\tEntity Number {i}");
            lines.Add($"\tdescription\tThis is a detailed description for entity {i}");
            lines.Add($"\tactive\t{(i % 2 == 0 ? "true" : "false")}");
            lines.Add($"\tproperties");
            lines.Add($"\t\tprop_a\t{i * 100}");
            lines.Add($"\t\tprop_b\t{i * 200}");
            lines.Add($"\t\tprop_c\tsomething_{i}");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Generates a deeply nested TAML document (20 levels deep).
    /// </summary>
    private static string GenerateDeeplyNestedTaml()
    {
        var lines = new List<string>(100);
        lines.Add("# Deeply nested structure");
        lines.Add("root");

        for (var depth = 1; depth <= 20; depth++)
        {
            var indent = new string('\t', depth);
            lines.Add($"{indent}level{depth}");
            lines.Add($"{indent}\tname\tLevel {depth}");
            lines.Add($"{indent}\tvalue\t{depth * 100}");
        }

        // Add some siblings at various depths
        for (var depth = 18; depth >= 1; depth -= 3)
        {
            var indent = new string('\t', depth);
            lines.Add($"{indent}sibling{depth}");
            lines.Add($"{indent}\tdata\tSibling at depth {depth}");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Generates a wide TAML document with many siblings at the same level.
    /// </summary>
    private static string GenerateWideTaml()
    {
        var lines = new List<string>(1100);
        lines.Add("# Wide structure with many siblings");
        lines.Add("items");

        for (var i = 0; i < 500; i++)
        {
            lines.Add($"\titem{i}\tvalue_{i}");
        }

        lines.Add("categories");
        for (var i = 0; i < 100; i++)
        {
            lines.Add($"\tcategory{i}");
            lines.Add($"\t\tlabel\tCategory {i}");
        }

        return string.Join("\n", lines);
    }
}
