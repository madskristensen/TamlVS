namespace TamlTokenizer;

/// <summary>
/// Defines all token types in the TAML (Tab Annotated Markup Language) language.
/// </summary>
/// <remarks>
/// Tokens are the atomic lexical units produced by the lexer before parsing.
/// TAML uses tabs for hierarchy and key-value separation, making it simpler than YAML.
/// </remarks>
public enum TamlTokenType
{
    /// <summary>A key (property name) in a key-value pair.</summary>
    Key,

    /// <summary>A value in a key-value pair (non-empty string).</summary>
    Value,

    /// <summary>A tab character used as indentation (one tab = one level).</summary>
    Tab,

    /// <summary>A newline character (LF or CRLF).</summary>
    Newline,

    /// <summary>A null value represented by tilde (~).</summary>
    Null,

    /// <summary>An empty string value represented by two double-quotes ("").</summary>
    EmptyString,

    /// <summary>A comment line starting with #.</summary>
    Comment,

    /// <summary>Virtual token indicating indentation increase (one tab deeper).</summary>
    Indent,

    /// <summary>Virtual token indicating indentation decrease (returning to parent level).</summary>
    Dedent,

    /// <summary>Whitespace characters (spaces) - invalid for indentation but may appear in values.</summary>
    Whitespace,

    /// <summary>End of file marker (virtual token).</summary>
    EndOfFile,

    /// <summary>Invalid or unrecognized token.</summary>
    Invalid,

    /// <summary>A boolean value (true or false).</summary>
    Boolean,

    /// <summary>A numeric value (integer or decimal).</summary>
    Number
}
