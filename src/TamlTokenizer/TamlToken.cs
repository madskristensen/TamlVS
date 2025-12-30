using System;

namespace TamlTokenizer;

/// <summary>
/// Represents a single token in the TAML language with position tracking.
/// </summary>
public sealed class TamlToken : IEquatable<TamlToken>
{
    /// <summary>
    /// The type of this token.
    /// </summary>
    public TamlTokenType Type { get; }

    /// <summary>
    /// The raw text value of this token.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// The line number where this token starts (1-based).
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// The column number where this token starts (1-based).
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// The absolute character position in the source text (0-based).
    /// </summary>
    public int Position { get; }

    /// <summary>
    /// The length of this token in characters.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Creates a new TAML token.
    /// </summary>
    /// <param name="type">The token type.</param>
    /// <param name="value">The raw text value.</param>
    /// <param name="line">The 1-based line number.</param>
    /// <param name="column">The 1-based column number.</param>
    /// <param name="position">The 0-based absolute character position.</param>
    /// <param name="length">The length in characters.</param>
    public TamlToken(TamlTokenType type, string value, int line, int column, int position, int length)
    {
        Type = type;
        Value = value ?? string.Empty;
        Line = line;
        Column = column;
        Position = position;
        Length = length;
    }

    /// <summary>
    /// Gets the end position of this token (exclusive).
    /// </summary>
    public int EndPosition => Position + Length;

    /// <summary>
    /// Determines if this token is a value type (Key, Value, Null, EmptyString).
    /// </summary>
    public bool IsValueToken => Type == TamlTokenType.Key ||
                                 Type == TamlTokenType.Value ||
                                 Type == TamlTokenType.Null ||
                                 Type == TamlTokenType.EmptyString;

    /// <summary>
    /// Determines if this token is structural (Tab, Newline, Indent, Dedent).
    /// </summary>
    public bool IsStructuralToken => Type == TamlTokenType.Tab ||
                                      Type == TamlTokenType.Newline ||
                                      Type == TamlTokenType.Indent ||
                                      Type == TamlTokenType.Dedent;

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Type}({Value}) at {Line}:{Column}";
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is TamlToken other && Equals(other);
    }

    /// <inheritdoc/>
    public bool Equals(TamlToken? other)
    {
        if (other is null) return false;
        return Type == other.Type &&
               Value == other.Value &&
               Line == other.Line &&
               Column == other.Column &&
               Position == other.Position &&
               Length == other.Length;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Type.GetHashCode();
            hash = hash * 31 + (Value?.GetHashCode() ?? 0);
            hash = hash * 31 + Line.GetHashCode();
            hash = hash * 31 + Column.GetHashCode();
            hash = hash * 31 + Position.GetHashCode();
            hash = hash * 31 + Length.GetHashCode();
            return hash;
        }
    }

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(TamlToken? left, TamlToken? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(TamlToken? left, TamlToken? right)
    {
        return !(left == right);
    }
}
