using System;
using System.Collections.Generic;

namespace TamlTokenizer;

/// <summary>
/// Lexical analyzer for the TAML language. Converts source text into tokens.
/// </summary>
/// <remarks>
/// The lexer handles TAML's tab-based syntax:
/// - Tabs for indentation (hierarchy)
/// - Tabs for key-value separation
/// - Comments starting with #
/// - Null values (~)
/// - Empty strings ("")
/// </remarks>
public sealed class TamlLexer
{
    // Cached tab strings to avoid allocations for common cases (extended for deeper nesting)
    private static readonly string[] _cachedTabs = ["", "\t", "\t\t", "\t\t\t", "\t\t\t\t", "\t\t\t\t\t", "\t\t\t\t\t\t", "\t\t\t\t\t\t\t", "\t\t\t\t\t\t\t\t"];

    // Cached common values to avoid repeated allocations
    private const string CachedTrue = "true";
    private const string CachedFalse = "false";
    private const string CachedNewline = "\n";
    private const string CachedEmpty = "";
    private const string CachedNull = "~";
    private const string CachedEmptyString = "\"\"";

    private readonly string _source;
    private readonly TamlParserOptions _options;
    private readonly List<TamlError> _errors;

    // Array-based stacks to avoid allocation overhead of Stack<T>
    private readonly int[] _indentLevels;
    private int _indentStackTop;
    private readonly bool[] _listContexts;
    private int _listContextTop;

    // Array-based pending tokens (replaces Queue<TamlToken>)
    private readonly TamlToken[] _pendingTokens;
    private int _pendingTokenStart;
    private int _pendingTokenEnd;

    private int _position;
    private int _line;
    private int _column;
    private int _tokenCount;
    private bool _atLineStart;
    private int _currentIndentLevel;
    private bool _afterTabSeparator;
    private bool _lastKeyHadValue; // tracks if the most recent key had a tab-separated value

    /// <summary>
    /// Creates a new TAML lexer with default options.
    /// </summary>
    /// <param name="source">The TAML source text to tokenize.</param>
    public TamlLexer(string source) : this(source, TamlParserOptions.Default)
    {
    }

    /// <summary>
    /// Creates a new TAML lexer with custom options.
    /// </summary>
    /// <param name="source">The TAML source text to tokenize.</param>
    /// <param name="options">The parser options to use.</param>
    public TamlLexer(string source, TamlParserOptions? options)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _options = options ?? TamlParserOptions.Default;
        _errors = new List<TamlError>();

        // Pre-allocate arrays based on max nesting depth (plus buffer)
        var maxDepth = _options.MaxNestingDepth + 2;
        _indentLevels = new int[maxDepth];
        _indentLevels[0] = 0;
        _indentStackTop = 0;

        _listContexts = new bool[maxDepth];
        _listContexts[0] = false; // root level is not a list context
        _listContextTop = 0;

        // Pre-allocate pending tokens array (dedents rarely exceed nesting depth)
        _pendingTokens = new TamlToken[maxDepth];
        _pendingTokenStart = 0;
        _pendingTokenEnd = 0;

        _position = 0;
        _line = 1;
        _column = 1;
        _tokenCount = 0;
        _atLineStart = true;
        _currentIndentLevel = 0;
        _afterTabSeparator = false;
        _lastKeyHadValue = false;
    }

    /// <summary>
    /// Gets the list of errors encountered during tokenization.
    /// </summary>
    public IReadOnlyList<TamlError> Errors => _errors;


    /// <summary>
    /// Tokenizes the entire source and returns all tokens.
    /// </summary>
    /// <returns>A list of all tokens in the source.</returns>
    public List<TamlToken> Tokenize()
    {
        // Check input size
        if (_source.Length > _options.MaxInputSize)
        {
            _errors.Add(new TamlError(
                $"Input size ({_source.Length:N0} characters) exceeds maximum allowed ({_options.MaxInputSize:N0}).",
                0, 0, 1, 1, TamlErrorCode.InputSizeExceeded));
            return
            [
                new(TamlTokenType.EndOfFile, string.Empty, 1, 1, 0, 0)
            ];
        }

        // Pre-allocate capacity: rough estimate of 1 token per 5 characters
        var tokens = new List<TamlToken>(Math.Max(16, _source.Length / 5));
        TamlToken token;

        while ((token = NextToken()).Type != TamlTokenType.EndOfFile)
        {
            tokens.Add(token);

            // Check token count limit
            _tokenCount++;
            if (_tokenCount > _options.MaxTokenCount)
            {
                _errors.Add(new TamlError(
                    $"Token count ({_tokenCount:N0}) exceeds maximum allowed ({_options.MaxTokenCount:N0}).",
                    token.Position, token.Length, token.Line, token.Column,
                    TamlErrorCode.TokenCountExceeded));
                break;
            }
        }

        tokens.Add(token); // Add EOF token

        // Post-process: Reclassify list item value-type tokens that have children as Keys
        // A list item (value token at start of line) followed by an Indent is actually a parent key
        ReclassifyListItemsWithChildrenAsKeys(tokens);

        return tokens;
    }

    /// <summary>
    /// Reclassifies list item value-type tokens (Value, Boolean, Number) that are followed by Indent tokens as Key tokens.
    /// Only affects list items (values at start of line), not values in key-value pairs.
    /// </summary>
    private static void ReclassifyListItemsWithChildrenAsKeys(List<TamlToken> tokens)
    {
        for (var i = 0; i < tokens.Count - 1; i++)
        {
            TamlTokenType tokenType = tokens[i].Type;
            // Check if this is a value-type token that could be a list item
            if (tokenType == TamlTokenType.Value ||
                tokenType == TamlTokenType.Boolean ||
                tokenType == TamlTokenType.Number)
            {
                // Check if this is a list item (not preceded by a Tab, i.e., not a key-value value)
                // A list item is a value at the start of a line, which means it's preceded by
                // either Indent/Dedent or Newline, not by Tab
                var isListItem = false;
                if (i > 0)
                {
                    TamlTokenType prevTokenType = tokens[i - 1].Type;
                    // List items are preceded by Indent, Newline, or are at the start
                    // Key-value values are preceded by Tab
                    isListItem = prevTokenType != TamlTokenType.Tab;
                }
                else
                {
                    // First token could be a list item if we ever support that
                    isListItem = true;
                }

                if (!isListItem)
                {
                    continue;
                }

                // Look ahead for the next meaningful token (skip newlines)
                for (var j = i + 1; j < tokens.Count; j++)
                {
                    TamlToken nextToken = tokens[j];
                    if (nextToken.Type == TamlTokenType.Newline)
                    {
                        continue;
                    }

                    if (nextToken.Type == TamlTokenType.Indent)
                    {
                        // This list item has children, so it's actually a Key
                        TamlToken originalToken = tokens[i];
                        tokens[i] = new TamlToken(
                            TamlTokenType.Key,
                            originalToken.Value,
                            originalToken.Line,
                            originalToken.Column,
                            originalToken.Position,
                            originalToken.Length);
                    }
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Gets the next token from the source.
    /// </summary>
    /// <returns>The next token.</returns>
    public TamlToken NextToken()
    {
        // Return any pending tokens (indent/dedent)
        if (_pendingTokenStart < _pendingTokenEnd)
        {
            return _pendingTokens[_pendingTokenStart++];
        }

        // Handle end of file
        if (_position >= _source.Length)
        {
            // Emit dedents for remaining indent levels
            if (_indentStackTop > 0)
            {
                _indentStackTop--;
                return CreateToken(TamlTokenType.Dedent, CachedEmpty);
            }
            return CreateToken(TamlTokenType.EndOfFile, CachedEmpty);
        }

        var current = Current;

        // Handle line start (indentation processing)
        if (_atLineStart)
        {
            return ProcessLineStart();
        }

        // Handle newlines
        if (current == '\n' || current == '\r')
        {
            return ConsumeNewline();
        }

        // Handle comments (# only at start of line, not after tab separator)
        // TAML spec: "Lines starting with # are ignored. Mid-line comments are not supported."
        if (current == '#' && !_afterTabSeparator)
        {
            return ConsumeComment();
        }

        // Handle tabs (key-value separator)
        if (current == '\t')
        {
            return ConsumeTab();
        }

        // Handle spaces (may be invalid or part of value)
        if (current == ' ')
        {
            return ConsumeSpace();
        }

        // Handle null value (~)
        if (current == '~' && IsValueTerminator(Peek()))
        {
            return ConsumeNull();
        }

        // Handle empty string ("")
        if (current == '"' && Peek() == '"' && IsValueTerminator(Peek(2)))
        {
            return ConsumeEmptyString();
        }

        // Handle key or value (text content)
        return ConsumeText();
    }

    private TamlToken ProcessLineStart()
    {
        var startPos = _position;
        var startColumn = _column;
        var tabCount = 0;
        var hasSpaces = false;
        var hasMixedIndentation = false;

        // Count leading tabs and check for spaces
        while (_position < _source.Length)
        {
            var c = Current;
            if (c == '\t')
            {
                if (hasSpaces)
                {
                    hasMixedIndentation = true;
                }
                tabCount++;
                _position++;
                _column++;
            }
            else if (c == ' ')
            {
                hasSpaces = true;
                _position++;
                _column++;
            }
            else
            {
                break;
            }
        }

        // Check for errors - spaces in indentation are always invalid in TAML
        if (hasSpaces)
        {
            if (hasMixedIndentation)
            {
                _errors.Add(new TamlError(
                    "Mixed spaces and tabs in indentation",
                    startPos, _position - startPos, _line, startColumn,
                    TamlErrorCode.MixedIndentation));
            }
            else
            {
                _errors.Add(new TamlError(
                    "Indentation must use tabs, not spaces",
                    startPos, _position - startPos, _line, startColumn,
                    TamlErrorCode.SpaceIndentation));
            }
        }

        _atLineStart = false;

        // Handle blank line or comment-only line
        if (_position >= _source.Length || Current == '\n' || Current == '\r')
        {
            // Blank line - just return newline
            if (_position < _source.Length)
            {
                return ConsumeNewline();
            }
            return CreateToken(TamlTokenType.EndOfFile, string.Empty);
        }

        if (Current == '#')
        {
            // Comment line - process indent first if needed, then comment
            return ProcessIndentChange(tabCount, startPos, startColumn) ?? ConsumeComment();
        }

        // Process indentation changes
        TamlToken? indentToken = ProcessIndentChange(tabCount, startPos, startColumn);
        if (indentToken != null)
        {
            return indentToken;
        }

        // No indentation change, continue to content
        return NextToken();
    }

    private TamlToken? ProcessIndentChange(int newIndentLevel, int startPos, int startColumn)
    {
        var currentLevel = _indentLevels[_indentStackTop];

        if (newIndentLevel > currentLevel)
        {
            // Check for skipped indent levels
            if (newIndentLevel > currentLevel + 1)
            {
                _errors.Add(new TamlError(
                    $"Invalid indentation level (expected {currentLevel + 1} tabs, found {newIndentLevel})",
                    startPos, newIndentLevel, _line, startColumn,
                    TamlErrorCode.InconsistentIndentation));
            }

            // Check nesting depth
            if (newIndentLevel > _options.MaxNestingDepth)
            {
                _errors.Add(new TamlError(
                    $"Nesting depth ({newIndentLevel}) exceeds maximum allowed ({_options.MaxNestingDepth})",
                    startPos, newIndentLevel, _line, startColumn,
                    TamlErrorCode.NestingDepthExceeded));
            }

            _indentStackTop++;
            _indentLevels[_indentStackTop] = newIndentLevel;
            // If the last key had no value, this is a list context
            _listContextTop++;
            _listContexts[_listContextTop] = !_lastKeyHadValue;
            _currentIndentLevel = newIndentLevel;

            // Use cached tab string if available
            var tabDiff = newIndentLevel - currentLevel;
            var tabValue = tabDiff < _cachedTabs.Length ? _cachedTabs[tabDiff] : new string('\t', tabDiff);
            return new TamlToken(TamlTokenType.Indent, tabValue,
                _line, startColumn, startPos, tabDiff);
        }
        else if (newIndentLevel < currentLevel)
        {
            // Reset pending tokens buffer
            _pendingTokenStart = 0;
            _pendingTokenEnd = 0;

            // Emit dedent tokens
            while (_indentStackTop > 0 && _indentLevels[_indentStackTop] > newIndentLevel)
            {
                _indentStackTop--;
                if (_listContextTop > 0)
                {
                    _listContextTop--;
                }
                _pendingTokens[_pendingTokenEnd++] = new TamlToken(TamlTokenType.Dedent, CachedEmpty,
                    _line, startColumn, startPos, 0);
            }

            _currentIndentLevel = newIndentLevel;

            if (_pendingTokenStart < _pendingTokenEnd)
            {
                return _pendingTokens[_pendingTokenStart++];
            }
        }

        _currentIndentLevel = newIndentLevel;
        return null;
    }

    private TamlToken ConsumeNewline()
    {
        var start = _position;
        var startColumn = _column;
        var startLine = _line;
        var sourceLength = _source.Length;

        // Check for CRLF vs LF - inline the character access for performance
        if (_position < sourceLength && _source[_position] == '\r' &&
            _position + 1 < sourceLength && _source[_position + 1] == '\n')
        {
            _position += 2;
        }
        else
        {
            _position++;
        }

        _line++;
        _column = 1;
        _atLineStart = true;
        _afterTabSeparator = false;

        // Check line count
        if (_line > _options.MaxLineCount)
        {
            _errors.Add(new TamlError(
                $"Line count ({_line:N0}) exceeds maximum allowed ({_options.MaxLineCount:N0})",
                start, 1, startLine, startColumn,
                TamlErrorCode.InputSizeExceeded));
        }

        return new TamlToken(TamlTokenType.Newline, CachedNewline, startLine, startColumn, start, _position - start);
    }

    private TamlToken ConsumeComment()
    {
        var start = _position;
        var startColumn = _column;

        // Skip the # character
        _position++;
        _column++;

        // Read until end of line
        while (_position < _source.Length && Current != '\n' && Current != '\r')
        {
            _position++;
            _column++;
        }

        var value = _source.Substring(start, _position - start);
        return new TamlToken(TamlTokenType.Comment, value, _line, startColumn, start, value.Length);
    }

    private TamlToken ConsumeTab()
    {
        var start = _position;
        var startColumn = _column;
        var tabCount = 0;

        while (_position < _source.Length && Current == '\t')
        {
            tabCount++;
            _position++;
            _column++;
        }

        // Check for multiple tab separators on the same line (multiple values)
        // TAML only allows one key-value pair per line: key<TAB>value
        // Only report error if there's actual content after these tabs
        // (not end of line, end of file, or trailing whitespace)
        var hasContentAfter = _position < _source.Length &&
                              Current != '\n' &&
                              Current != '\r' &&
                              Current != ' ' &&
                              Current != '\t';

        if (_afterTabSeparator && hasContentAfter)
        {
            _errors.Add(new TamlError(
                "Only one value allowed per line",
                start, tabCount, _line, startColumn,
                TamlErrorCode.MultipleValuesOnLine));
        }

        // After a tab separator, we're in value context
        // This means # should not be treated as a comment start
        _afterTabSeparator = true;

        // Use cached tab strings for common cases to avoid allocations
        var tabValue = tabCount < _cachedTabs.Length ? _cachedTabs[tabCount] : new string('\t', tabCount);
        return new TamlToken(TamlTokenType.Tab, tabValue, _line, startColumn, start, tabCount);
    }

    private TamlToken ConsumeSpace()
    {
        var start = _position;
        var startColumn = _column;

        while (_position < _source.Length && Current == ' ')
        {
            _position++;
            _column++;
        }

        var value = _source.Substring(start, _position - start);

        // Spaces as standalone tokens mid-line are always invalid in TAML
        // - Before tab separator: spaces used instead of tabs to separate key from value
        // - After tab separator: spaces before value content (should use tabs for alignment)
        // Note: Spaces WITHIN values are valid and are consumed by ConsumeText, not here
        // Only skip error for trailing spaces at end of line (harmless whitespace)
        var isTrailingWhitespace = _position >= _source.Length ||
                                   Current == '\n' ||
                                   Current == '\r';

        if (!isTrailingWhitespace)
        {
            _errors.Add(new TamlError(
                "Use tabs, not spaces, to separate keys from values",
                start, value.Length, _line, startColumn,
                TamlErrorCode.SpaceSeparator));
        }

        return new TamlToken(TamlTokenType.Whitespace, value, _line, startColumn, start, value.Length);
    }

    private TamlToken ConsumeNull()
    {
        var start = _position;
        var startColumn = _column;

        _position++;
        _column++;

        return new TamlToken(TamlTokenType.Null, CachedNull, _line, startColumn, start, 1);
    }

    private TamlToken ConsumeEmptyString()
    {
        var start = _position;
        var startColumn = _column;

        _position += 2;
        _column += 2;

        return new TamlToken(TamlTokenType.EmptyString, CachedEmptyString, _line, startColumn, start, 2);
    }

    private TamlToken ConsumeText()
    {
        var start = _position;
        var startColumn = _column;

        // Scan forward to find end of text (tab, newline, or end of source)
        var end = start;
        var sourceLength = _source.Length;
        var maxStringLength = _options.MaxStringLength;

        while (end < sourceLength)
        {
            var c = _source[end];

            // Stop at tab, newline, or end
            if (c == '\t' || c == '\n' || c == '\r')
            {
                break;
            }

            end++;

            // Check string length limit
            if (end - start > maxStringLength)
            {
                _errors.Add(new TamlError(
                    "String length (" + (end - start).ToString("N0") + ") exceeds maximum allowed (" + maxStringLength.ToString("N0") + ")",
                    start, end - start, _line, startColumn,
                    TamlErrorCode.InputSizeExceeded));
                break;
            }
        }

        var length = end - start;
        _position = end;
        _column = startColumn + length;

        var value = _source.Substring(start, length);

        // Determine if this is a key or value based on context
        // A key is followed by tab(s) OR is at the start of a line (parent key or list item)
        // A value is after a tab separator
        var isKeyWithValue = _position < sourceLength && _source[_position] == '\t';

        // Use interned strings for common boolean values to reduce allocations
        if (length == 4 && _source[start] == 't' && value == CachedTrue)
        {
            value = CachedTrue;
        }
        else if (length == 5 && _source[start] == 'f' && value == CachedFalse)
        {
            value = CachedFalse;
        }

        // Check for spaces used as separator (key followed by space then more text before tab/newline)
        // This catches "key value" where space is incorrectly used instead of tab
        if (isKeyWithValue)
        {
            var spaceIndex = value.IndexOf(' ');
            if (spaceIndex >= 0)
            {
                _errors.Add(new TamlError(
                    "Use tabs, not spaces, to separate keys from values",
                    start + spaceIndex, 1, _line, startColumn + spaceIndex,
                    TamlErrorCode.SpaceSeparator));
            }
        }

        // Also consider it a key if it's at the start position of a line (after indentation)
        // This handles parent keys and list items which don't have tab-separated values
        var isKey = isKeyWithValue;
        var isLineStart = startColumn == _currentIndentLevel + 1;
        if (!isKey && isLineStart)
        {
            isKey = true;
        }

        TamlTokenType type;
        if (isKey)
        {
            // Track whether this key has a value (for list context detection)
            _lastKeyHadValue = isKeyWithValue;


            // Check if we're in a list context (parent had no value)
            // List items are indented values without a tab separator
            var isListContext = _listContextTop >= 0 && _listContexts[_listContextTop];
            if (isListContext && !isKeyWithValue)
            {
                // This is a list item - classify by value type for proper highlighting
                // List items are semantically values, so they get the same colorization
                type = ClassifyValueType(value);
            }
            else
            {
                type = TamlTokenType.Key;
            }
        }
        else
        {
            // Classify the value type for syntax highlighting
            type = ClassifyValueType(value);
        }

        return new TamlToken(type, value, _line, startColumn, start, length);
    }

    private TamlToken CreateToken(TamlTokenType type, string value)
    {
        return new TamlToken(type, value, _line, _column, _position, value.Length);
    }

    /// <summary>
    /// Classifies a value string into its specific type (Boolean, Number, or Value).
    /// </summary>
    private static TamlTokenType ClassifyValueType(string value)
    {
        // Check for boolean
        if (value == "true" || value == "false")
        {
            return TamlTokenType.Boolean;
        }

        // Check for number (integer or decimal)
        if (value.Length > 0 && IsNumeric(value))
        {
            return TamlTokenType.Number;
        }

        return TamlTokenType.Value;
    }

    /// <summary>
    /// Checks if the string represents a valid number (integer or decimal).
    /// </summary>
    private static bool IsNumeric(string value)
    {
        var startIndex = 0;
        var hasDecimalPoint = false;

        // Handle optional leading sign
        if (value[0] == '-' || value[0] == '+')
        {
            startIndex = 1;
            if (value.Length == 1)
            {
                return false;
            }
        }

        for (var i = startIndex; i < value.Length; i++)
        {
            var c = value[i];

            if (c == '.')
            {
                if (hasDecimalPoint)
                {
                    return false; // Multiple decimal points
                }
                hasDecimalPoint = true;
            }
            else if (c < '0' || c > '9')
            {
                return false;
            }
        }

        // Ensure there's at least one digit
        return value.Length > startIndex && (value.Length > startIndex + (hasDecimalPoint ? 1 : 0));
    }

    private char Current => _position < _source.Length ? _source[_position] : '\0';

    private char Peek(int offset = 1)
    {
        var pos = _position + offset;
        return pos < _source.Length ? _source[pos] : '\0';
    }

    private static bool IsValueTerminator(char c)
    {
        return c == '\0' || c == '\t' || c == '\n' || c == '\r' || c == '#';
    }
}
