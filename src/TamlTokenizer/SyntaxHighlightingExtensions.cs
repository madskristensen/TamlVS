using System.Collections.Generic;
using System.Linq;

namespace TamlTokenizer;

/// <summary>
/// Provides utility methods for syntax highlighting and IDE integration.
/// </summary>
public static class SyntaxHighlightingExtensions
{
    /// <summary>
    /// Gets all tokens on a specific line.
    /// </summary>
    /// <param name="tokens">The list of tokens to search.</param>
    /// <param name="line">The 1-based line number.</param>
    /// <returns>A list of tokens on the specified line.</returns>
    public static List<TamlToken> GetTokensOnLine(this IReadOnlyList<TamlToken> tokens, int line)
    {
        return tokens.Where(t => t.Line == line).ToList();
    }

    /// <summary>
    /// Gets all tokens in a range of lines (inclusive).
    /// </summary>
    /// <param name="tokens">The list of tokens to search.</param>
    /// <param name="startLine">The 1-based start line number.</param>
    /// <param name="endLine">The 1-based end line number.</param>
    /// <returns>A list of tokens in the specified range.</returns>
    public static List<TamlToken> GetTokensInRange(this IReadOnlyList<TamlToken> tokens, int startLine, int endLine)
    {
        return tokens.Where(t => t.Line >= startLine && t.Line <= endLine).ToList();
    }

    /// <summary>
    /// Gets the token at a specific position (line and column).
    /// </summary>
    /// <param name="tokens">The list of tokens to search.</param>
    /// <param name="line">The 1-based line number.</param>
    /// <param name="column">The 1-based column number.</param>
    /// <returns>The token at that position, or null if no token found.</returns>
    /// <example>
    /// <code>
    /// var result = Taml.Tokenize(source);
    /// var token = result.Tokens.GetTokenAt(line: 5, column: 10);
    /// if (token != null)
    /// {
    ///     Console.WriteLine($"Token at 5:10 is {token.Type}: {token.Value}");
    /// }
    /// </code>
    /// </example>
    public static TamlToken? GetTokenAt(this IReadOnlyList<TamlToken> tokens, int line, int column)
    {
        return tokens.FirstOrDefault(t =>
            t.Line == line &&
            column >= t.Column &&
            column < t.Column + t.Length);
    }

    /// <summary>
    /// Gets all tokens of a specific type.
    /// </summary>
    /// <param name="tokens">The list of tokens to search.</param>
    /// <param name="type">The token type to filter by.</param>
    /// <returns>A list of tokens with the specified type.</returns>
    public static List<TamlToken> GetTokensByType(this IReadOnlyList<TamlToken> tokens, TamlTokenType type)
    {
        return tokens.Where(t => t.Type == type).ToList();
    }

    /// <summary>
    /// Gets all key tokens.
    /// </summary>
    /// <param name="tokens">The list of tokens to search.</param>
    /// <returns>A list of key tokens.</returns>
    public static List<TamlToken> GetKeys(this IReadOnlyList<TamlToken> tokens)
    {
        return tokens.GetTokensByType(TamlTokenType.Key);
    }

    /// <summary>
    /// Gets all value tokens (including Null and EmptyString).
    /// </summary>
    /// <param name="tokens">The list of tokens to search.</param>
    /// <returns>A list of value tokens.</returns>
    public static List<TamlToken> GetValues(this IReadOnlyList<TamlToken> tokens)
    {
        return tokens.Where(t =>
            t.Type == TamlTokenType.Value ||
            t.Type == TamlTokenType.Null ||
            t.Type == TamlTokenType.EmptyString).ToList();
    }

    /// <summary>
    /// Gets all comment tokens.
    /// </summary>
    /// <param name="tokens">The list of tokens to search.</param>
    /// <returns>A list of comment tokens.</returns>
    public static List<TamlToken> GetComments(this IReadOnlyList<TamlToken> tokens)
    {
        return tokens.GetTokensByType(TamlTokenType.Comment);
    }

    /// <summary>
    /// Determines if a token represents a keyword-like value (null).
    /// </summary>
    /// <param name="token">The token to check.</param>
    /// <returns>true if the token is a keyword; otherwise, false.</returns>
    public static bool IsKeyword(this TamlToken token)
    {
        return token.Type == TamlTokenType.Null;
    }

    /// <summary>
    /// Determines if a token represents a structural element (Tab, Newline, Indent, Dedent).
    /// </summary>
    /// <param name="token">The token to check.</param>
    /// <returns>true if the token is structural; otherwise, false.</returns>
    public static bool IsStructural(this TamlToken token)
    {
        return token.Type == TamlTokenType.Tab ||
               token.Type == TamlTokenType.Newline ||
               token.Type == TamlTokenType.Indent ||
               token.Type == TamlTokenType.Dedent;
    }

    /// <summary>
    /// Determines if a token represents a value (Value, Null, EmptyString).
    /// </summary>
    /// <param name="token">The token to check.</param>
    /// <returns>true if the token is a value; otherwise, false.</returns>
    public static bool IsValue(this TamlToken token)
    {
        return token.Type == TamlTokenType.Value ||
               token.Type == TamlTokenType.Null ||
               token.Type == TamlTokenType.EmptyString;
    }

    /// <summary>
    /// Gets the indentation level at a specific line.
    /// </summary>
    /// <param name="tokens">The list of tokens to search.</param>
    /// <param name="line">The 1-based line number.</param>
    /// <returns>The indentation level (number of tabs) at the line.</returns>
    public static int GetIndentationLevel(this IReadOnlyList<TamlToken> tokens, int line)
    {
        List<TamlToken> lineTokens = tokens.GetTokensOnLine(line);
        var indentLevel = 0;

        foreach (TamlToken token in lineTokens)
        {
            if (token.Type == TamlTokenType.Indent)
            {
                indentLevel++;
            }
            else if (token.Type == TamlTokenType.Dedent)
            {
                indentLevel--;
            }
        }

        return indentLevel;
    }

    /// <summary>
    /// Gets the error on a specific line, if any.
    /// </summary>
    /// <param name="errors">The list of errors to search.</param>
    /// <param name="line">The 1-based line number.</param>
    /// <returns>The first error on the line, or null if none.</returns>
    public static TamlError? GetErrorOnLine(this IReadOnlyList<TamlError> errors, int line)
    {
        return errors.FirstOrDefault(e => e.Line == line);
    }

    /// <summary>
    /// Gets all errors in a range of lines (inclusive).
    /// </summary>
    /// <param name="errors">The list of errors to search.</param>
    /// <param name="startLine">The 1-based start line number.</param>
    /// <param name="endLine">The 1-based end line number.</param>
    /// <returns>A list of errors in the specified range.</returns>
    public static List<TamlError> GetErrorsInRange(this IReadOnlyList<TamlError> errors, int startLine, int endLine)
    {
        return errors.Where(e => e.Line >= startLine && e.Line <= endLine).ToList();
    }
}
