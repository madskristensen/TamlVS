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

        options ??= TamlParserOptions.Default;
        var lexer = new TamlLexer(source, options);
        var tokens = lexer.Tokenize();
        var errors = new List<TamlError>(lexer.Errors);

        // Perform structural validation in strict mode
        if (options.StrictMode)
        {
            ValidateStructure(tokens, errors);
        }

        if (errors.Count > 0)
        {
            return TamlParseResult.Partial(tokens, errors);
        }

        return TamlParseResult.Success(tokens);
    }



    /// <summary>
    /// Validates the structural integrity of a TAML document.
    /// Checks for orphaned lines, parent-with-value errors, and empty keys.
    /// </summary>
    private static void ValidateStructure(IReadOnlyList<TamlToken> tokens, List<TamlError> errors)
    {
        // Track the previous line's state to detect orphaned lines
        var previousLineHadValue = false;
        var previousLineIndentLevel = 0;
        var currentIndentLevel = 0;

        // Track keys that have values to detect parent-with-value errors
        // Key: encoded as (line << 16) | indent, Value: token info for error reporting
        var keysWithValues = new Dictionary<long, TamlToken>();

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            switch (token.Type)
            {
                case TamlTokenType.Indent:
                    currentIndentLevel++;

                    // Check for orphaned line: indentation increased after a key-value pair
                    if (previousLineHadValue && currentIndentLevel > previousLineIndentLevel)
                    {
                        errors.Add(new TamlError(
                            "Indented line has no parent (previous line has a value)",
                            token.Position, token.Length, token.Line, token.Column,
                            TamlErrorCode.OrphanedLine));
                    }

                    // Check for parent-with-value: a key with a value now has children
                    // Find the parent key at previousLineIndentLevel
                    var parentKeyLookup = MakeKey(token.Line - 1, previousLineIndentLevel);
                    if (keysWithValues.TryGetValue(parentKeyLookup, out var parentKey))
                    {
                        errors.Add(new TamlError(
                            "Key '" + parentKey.Value + "' has a value but also has children",
                            parentKey.Position, parentKey.Length, parentKey.Line, parentKey.Column,
                            TamlErrorCode.ParentWithValue));
                        keysWithValues.Remove(parentKeyLookup);
                    }
                    break;

                case TamlTokenType.Dedent:
                    currentIndentLevel--;
                    break;

                case TamlTokenType.Key:
                    // Check for empty key
                    if (string.IsNullOrWhiteSpace(token.Value))
                    {
                        errors.Add(new TamlError(
                            "Empty key",
                            token.Position, Math.Max(1, token.Length), token.Line, token.Column,
                            TamlErrorCode.EmptyKey));
                    }

                    // Check if this key is followed by a value (tab then value)
                    for (var j = i + 1; j < tokens.Count; j++)
                    {
                        var nextToken = tokens[j];
                        if (nextToken.Type == TamlTokenType.Tab)
                        {
                            // Look for value after tab
                            for (var k = j + 1; k < tokens.Count; k++)
                            {
                                var valueToken = tokens[k];
                                if (valueToken.Type == TamlTokenType.Value ||
                                    valueToken.Type == TamlTokenType.Null ||
                                    valueToken.Type == TamlTokenType.EmptyString)
                                {
                                    keysWithValues[MakeKey(token.Line, currentIndentLevel)] = token;
                                    break;
                                }
                                if (valueToken.Type != TamlTokenType.Tab &&
                                    valueToken.Type != TamlTokenType.Whitespace)
                                {
                                    break;
                                }
                            }
                            break;
                        }
                        if (nextToken.Type == TamlTokenType.Newline ||
                            nextToken.Type == TamlTokenType.EndOfFile)
                        {
                            break;
                        }
                    }
                    break;

                case TamlTokenType.Newline:
                    // Update previous line state
                    previousLineHadValue = keysWithValues.ContainsKey(MakeKey(token.Line, currentIndentLevel));
                    previousLineIndentLevel = currentIndentLevel;
                    break;
            }
        }
    }

    /// <summary>
    /// Creates a dictionary key from line and indent level.
    /// Uses bit packing to avoid string allocation.
    /// </summary>
    private static long MakeKey(int line, int indent)
    {
        return ((long)line << 16) | (uint)indent;
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
