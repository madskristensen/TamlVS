using System.Collections.Generic;

namespace TamlTokenizer;

/// <summary>
/// Represents the result of tokenizing a TAML document.
/// </summary>
public sealed class TamlParseResult
{
    /// <summary>
    /// List of tokens generated during lexical analysis.
    /// Allows consumers to access the token stream for syntax highlighting, IDE features, etc.
    /// </summary>
    public IReadOnlyList<TamlToken> Tokens { get; }

    /// <summary>
    /// List of errors encountered during tokenization. Empty list means successful tokenization.
    /// </summary>
    public IReadOnlyList<TamlError> Errors { get; }

    /// <summary>
    /// Indicates whether the tokenization was successful (no errors).
    /// </summary>
    public bool IsSuccess => Errors.Count == 0;

    /// <summary>
    /// Indicates whether tokenization encountered any errors.
    /// </summary>
    public bool HasErrors => Errors.Count > 0;

    /// <summary>
    /// Creates a new TamlParseResult.
    /// </summary>
    /// <param name="tokens">The list of tokens.</param>
    /// <param name="errors">The list of errors.</param>
    public TamlParseResult(IReadOnlyList<TamlToken> tokens, IReadOnlyList<TamlError> errors)
    {
        Tokens = tokens ?? new List<TamlToken>();
        Errors = errors ?? new List<TamlError>();
    }

    /// <summary>
    /// Creates a successful parse result with tokens and no errors.
    /// </summary>
    /// <param name="tokens">The list of tokens.</param>
    public static TamlParseResult Success(IReadOnlyList<TamlToken> tokens)
    {
        return new TamlParseResult(tokens, new List<TamlError>());
    }

    /// <summary>
    /// Creates a parse result with both tokens and errors.
    /// Used for resilient parsing where errors don't stop the tokenization.
    /// </summary>
    /// <param name="tokens">The list of tokens.</param>
    /// <param name="errors">The list of errors.</param>
    public static TamlParseResult Partial(IReadOnlyList<TamlToken> tokens, IReadOnlyList<TamlError> errors)
    {
        return new TamlParseResult(tokens, errors);
    }

    /// <summary>
    /// Creates a failed parse result with errors and no tokens.
    /// Used only for catastrophic failures.
    /// </summary>
    /// <param name="errors">The list of errors.</param>
    public static TamlParseResult Failure(IReadOnlyList<TamlError> errors)
    {
        return new TamlParseResult(new List<TamlToken>(), errors);
    }

    /// <summary>
    /// Creates a failed parse result with a single error and no tokens.
    /// Used only for catastrophic failures.
    /// </summary>
    /// <param name="error">The error.</param>
    public static TamlParseResult Failure(TamlError error)
    {
        return new TamlParseResult(new List<TamlToken>(), new List<TamlError> { error });
    }
}
