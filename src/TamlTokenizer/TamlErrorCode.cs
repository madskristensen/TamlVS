namespace TamlTokenizer;

/// <summary>
/// Defines standard error codes for TAML parsing and validation.
/// </summary>
/// <remarks>
/// Error codes are organized by category:
/// - 1xxx: Lexer/tokenization errors
/// - 2xxx: Parser structural errors
/// - 3xxx: Validation errors (indentation, hierarchy)
/// - 9xxx: Internal/system errors
/// </remarks>
public static class TamlErrorCode
{
    // Lexer errors (1xxx)

    /// <summary>
    /// Invalid character encountered that cannot be tokenized.
    /// </summary>
    public const string InvalidCharacter = "TAML1001";

    /// <summary>
    /// Tab character found within a key or value (not allowed).
    /// TAML spec: Keys and values cannot contain tab characters.
    /// </summary>
    public const string TabInContent = "TAML1002";

    /// <summary>
    /// Unexpected end of input during tokenization.
    /// </summary>
    public const string UnexpectedEndOfInput = "TAML1003";

    // Parser structural errors (2xxx)

    /// <summary>
    /// Expected a key but found something else.
    /// </summary>
    public const string ExpectedKey = "TAML2001";

    /// <summary>
    /// Expected a value but found something else.
    /// </summary>
    public const string ExpectedValue = "TAML2002";

    /// <summary>
    /// Expected a tab separator between key and value.
    /// </summary>
    public const string ExpectedTabSeparator = "TAML2003";

    /// <summary>
    /// Expected a newline at end of line.
    /// </summary>
    public const string ExpectedNewline = "TAML2004";

    /// <summary>
    /// Empty key found (line has no key content).
    /// </summary>
    public const string EmptyKey = "TAML2005";

    /// <summary>
    /// Parent key cannot have a value on the same line when it has children.
    /// </summary>
    public const string ParentWithValue = "TAML2006";

    // Indentation errors (3xxx)

    /// <summary>
    /// Spaces used for indentation instead of tabs.
    /// TAML spec: Only tab characters may be used for indentation.
    /// </summary>
    public const string SpaceIndentation = "TAML3001";

    /// <summary>
    /// Mixed spaces and tabs in indentation.
    /// TAML spec: Indentation must be pure tabs.
    /// </summary>
    public const string MixedIndentation = "TAML3002";

    /// <summary>
    /// Inconsistent indentation level (skipped levels).
    /// TAML spec: Each nesting level must increase by exactly one tab.
    /// </summary>
    public const string InconsistentIndentation = "TAML3003";

    /// <summary>
    /// Orphaned line (indented but has no parent).
    /// TAML spec: Indented lines must have a parent.
    /// </summary>
    public const string OrphanedLine = "TAML3004";

    /// <summary>
    /// Invalid indentation after key-value pair.
    /// Cannot increase indentation after a line that has a value.
    /// </summary>
    public const string InvalidIndentationAfterValue = "TAML3005";

    // Internal errors (9xxx)

    /// <summary>
    /// Infinite loop detected in lexer or parser (safety check).
    /// This indicates a bug in the implementation.
    /// </summary>
    public const string InfiniteLoopDetected = "TAML9001";

    /// <summary>
    /// Token count exceeded maximum limit.
    /// </summary>
    public const string TokenCountExceeded = "TAML9002";

    /// <summary>
    /// Input size exceeded maximum limit.
    /// </summary>
    public const string InputSizeExceeded = "TAML9003";

    /// <summary>
    /// Nesting depth exceeded maximum limit.
    /// </summary>
    public const string NestingDepthExceeded = "TAML9004";
}
