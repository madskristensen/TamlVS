using System;
using System.Collections.Generic;

namespace TamlTokenizer;

/// <summary>
/// Main entry point for TAML tokenization operations.
/// Provides static methods for tokenizing TAML documents.
/// </summary>
/// <example>
/// <code>
/// // Basic tokenization
/// var result = Taml.Tokenize(tamlSource);
/// if (result.IsSuccess)
/// {
///     foreach (var token in result.Tokens)
///     {
///         Console.WriteLine($"{token.Type}: {token.Value}");
///     }
/// }
/// 
/// // With custom options
/// var options = new TamlParserOptions { StrictMode = true };
/// var result = Taml.Tokenize(tamlSource, options);
/// </code>
/// </example>
public static class Taml
{
    /// <summary>
    /// Tokenizes a TAML document with default options.
    /// </summary>
    /// <param name="source">The TAML source text to tokenize.</param>
    /// <returns>A TamlParseResult containing tokens and any errors.</returns>
    /// <exception cref="ArgumentNullException">source is null.</exception>
    public static TamlParseResult Tokenize(string source)
    {
        return Tokenize(source, TamlParserOptions.Default);
    }

    /// <summary>
    /// Tokenizes a TAML document with custom options.
    /// </summary>
    /// <param name="source">The TAML source text to tokenize.</param>
    /// <param name="options">The parser options to use.</param>
    /// <returns>A TamlParseResult containing tokens and any errors.</returns>
    /// <exception cref="ArgumentNullException">source is null.</exception>
    public static TamlParseResult Tokenize(string source, TamlParserOptions? options)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        var lexer = new TamlLexer(source, options);
        var tokens = lexer.Tokenize();
        var errors = new List<TamlError>(lexer.Errors);

        if (errors.Count > 0)
        {
            return TamlParseResult.Partial(tokens, errors);
        }

        return TamlParseResult.Success(tokens);
    }

    /// <summary>
    /// Tokenizes a TAML document in strict mode.
    /// Rejects invalid TAML immediately with detailed error messages.
    /// </summary>
    /// <param name="source">The TAML source text to tokenize.</param>
    /// <returns>A TamlParseResult containing tokens and any errors.</returns>
    /// <exception cref="ArgumentNullException">source is null.</exception>
    public static TamlParseResult TokenizeStrict(string source)
    {
        var options = new TamlParserOptions { StrictMode = true };
        return Tokenize(source, options);
    }

    /// <summary>
    /// Gets all tokens from a TAML document, ignoring errors.
    /// Useful for IDE features like syntax highlighting where partial results are acceptable.
    /// </summary>
    /// <param name="source">The TAML source text to tokenize.</param>
    /// <returns>A list of tokens.</returns>
    /// <exception cref="ArgumentNullException">source is null.</exception>
    public static IReadOnlyList<TamlToken> GetTokens(string source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        var lexer = new TamlLexer(source, TamlParserOptions.Default);
        return lexer.Tokenize();
    }

    /// <summary>
    /// Validates a TAML document and returns any errors.
    /// </summary>
    /// <param name="source">The TAML source text to validate.</param>
    /// <returns>A list of validation errors. Empty if the document is valid.</returns>
    /// <exception cref="ArgumentNullException">source is null.</exception>
    public static IReadOnlyList<TamlError> Validate(string source)
    {
        var result = TokenizeStrict(source);
        return result.Errors;
    }

    /// <summary>
    /// Checks if a TAML document is valid.
    /// </summary>
    /// <param name="source">The TAML source text to check.</param>
    /// <returns>true if the document is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">source is null.</exception>
    public static bool IsValid(string source)
    {
        return Validate(source).Count == 0;
    }

    /// <summary>
    /// Formats a TAML document with default options.
    /// Aligns values at the same indentation level to the same column.
    /// </summary>
    /// <param name="source">The TAML source text to format.</param>
    /// <returns>The formatted TAML text.</returns>
    /// <exception cref="ArgumentNullException">source is null.</exception>
    /// <example>
    /// <code>
    /// var formatted = Taml.Format(tamlSource);
    /// </code>
    /// </example>
    public static string Format(string source)
    {
        return Format(source, TamlFormatterOptions.Default);
    }

    /// <summary>
    /// Formats a TAML document with custom options.
    /// </summary>
    /// <param name="source">The TAML source text to format.</param>
    /// <param name="options">The formatter options to use.</param>
    /// <returns>The formatted TAML text.</returns>
    /// <exception cref="ArgumentNullException">source is null.</exception>
    public static string Format(string source, TamlFormatterOptions? options)
    {
        var formatter = new TamlFormatter(options);
        return formatter.Format(source);
    }
}
