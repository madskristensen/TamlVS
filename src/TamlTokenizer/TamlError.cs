using System;

namespace TamlTokenizer;

/// <summary>
/// Represents a parsing or validation error with span information.
/// </summary>
public sealed class TamlError
{
    /// <summary>
    /// The error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// The error code for programmatic error handling (e.g., "TAML1001").
    /// See <see cref="TamlErrorCode"/> for standard error codes.
    /// </summary>
    public string? Code { get; }

    /// <summary>
    /// The starting position (0-based index) of the error in the source string.
    /// </summary>
    public int Position { get; }

    /// <summary>
    /// The length of the span that contains the error.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// The line number (1-based) where the error occurs.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// The column number (1-based) where the error occurs.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Creates a new TAML error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="position">The 0-based position in the source.</param>
    /// <param name="length">The length of the error span.</param>
    /// <param name="line">The 1-based line number.</param>
    /// <param name="column">The 1-based column number.</param>
    /// <param name="code">Optional error code.</param>
    public TamlError(string message, int position, int length, int line, int column, string? code = null)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Position = position;
        Length = length;
        Line = line;
        Column = column;
        Code = code;
    }

    /// <summary>
    /// Gets the end position of the error span.
    /// </summary>
    public int EndPosition => Position + Length;

    /// <summary>
    /// Returns a string representation of the error with location information.
    /// </summary>
    public override string ToString()
    {
        var codePrefix = !string.IsNullOrEmpty(Code) ? $"[{Code}] " : "";
        return $"{codePrefix}{Message} (line {Line}, column {Column}, position {Position}, length {Length})";
    }
}
