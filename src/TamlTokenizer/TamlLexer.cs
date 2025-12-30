using System;
using System.Collections.Generic;
using System.Text;

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
    private readonly string _source;
    private readonly TamlParserOptions _options;
    private readonly List<TamlError> _errors;
    private readonly Stack<int> _indentStack;
    private readonly Queue<TamlToken> _pendingTokens;

    private int _position;
    private int _line;
    private int _column;
    private int _tokenCount;
    private bool _atLineStart;
    private int _currentIndentLevel;
    private bool _afterTabSeparator;

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
        _indentStack = new Stack<int>();
        _indentStack.Push(0);
        _pendingTokens = new Queue<TamlToken>();
        _position = 0;
        _line = 1;
        _column = 1;
        _tokenCount = 0;
        _atLineStart = true;
        _currentIndentLevel = 0;
        _afterTabSeparator = false;
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
            return new List<TamlToken>
            {
                new TamlToken(TamlTokenType.EndOfFile, string.Empty, 1, 1, 0, 0)
            };
        }

        var tokens = new List<TamlToken>();
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
        return tokens;
    }

    /// <summary>
    /// Gets the next token from the source.
    /// </summary>
    /// <returns>The next token.</returns>
    public TamlToken NextToken()
    {
        // Return any pending tokens (indent/dedent)
        if (_pendingTokens.Count > 0)
        {
            return _pendingTokens.Dequeue();
        }

        // Handle end of file
        if (_position >= _source.Length)
        {
            // Emit dedents for remaining indent levels
            if (_indentStack.Count > 1)
            {
                _indentStack.Pop();
                return CreateToken(TamlTokenType.Dedent, string.Empty);
            }
            return CreateToken(TamlTokenType.EndOfFile, string.Empty);
        }

        char current = Current;

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
        int startPos = _position;
        int startColumn = _column;
        int tabCount = 0;
        bool hasSpaces = false;
        bool hasMixedIndentation = false;

        // Count leading tabs and check for spaces
        while (_position < _source.Length)
        {
            char c = Current;
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

        // Check for errors
        if (hasSpaces && _options.StrictMode)
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
        var indentToken = ProcessIndentChange(tabCount, startPos, startColumn);
        if (indentToken != null)
        {
            return indentToken;
        }

        // No indentation change, continue to content
        return NextToken();
    }

    private TamlToken? ProcessIndentChange(int newIndentLevel, int startPos, int startColumn)
    {
        int currentLevel = _indentStack.Peek();

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

            _indentStack.Push(newIndentLevel);
            _currentIndentLevel = newIndentLevel;
            return new TamlToken(TamlTokenType.Indent, new string('\t', newIndentLevel - currentLevel),
                _line, startColumn, startPos, newIndentLevel - currentLevel);
        }
        else if (newIndentLevel < currentLevel)
        {
            // Emit dedent tokens
            while (_indentStack.Count > 1 && _indentStack.Peek() > newIndentLevel)
            {
                _indentStack.Pop();
                _pendingTokens.Enqueue(new TamlToken(TamlTokenType.Dedent, string.Empty,
                    _line, startColumn, startPos, 0));
            }

            _currentIndentLevel = newIndentLevel;

            if (_pendingTokens.Count > 0)
            {
                return _pendingTokens.Dequeue();
            }
        }

        _currentIndentLevel = newIndentLevel;
        return null;
    }

    private TamlToken ConsumeNewline()
    {
        int start = _position;
        int startColumn = _column;
        int startLine = _line;

        if (Current == '\r' && Peek() == '\n')
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

        return new TamlToken(TamlTokenType.Newline, "\n", startLine, startColumn, start, _position - start);
    }

    private TamlToken ConsumeComment()
    {
        int start = _position;
        int startColumn = _column;

        // Skip the # character
        _position++;
        _column++;

        // Read until end of line
        while (_position < _source.Length && Current != '\n' && Current != '\r')
        {
            _position++;
            _column++;
        }

        string value = _source.Substring(start, _position - start);
        return new TamlToken(TamlTokenType.Comment, value, _line, startColumn, start, value.Length);
    }

    private TamlToken ConsumeTab()
    {
        int start = _position;
        int startColumn = _column;
        int tabCount = 0;

        while (_position < _source.Length && Current == '\t')
        {
            tabCount++;
            _position++;
            _column++;
        }

        // After a tab separator, we're in value context
        // This means # should not be treated as a comment start
        _afterTabSeparator = true;

        return new TamlToken(TamlTokenType.Tab, new string('\t', tabCount), _line, startColumn, start, tabCount);
    }

    private TamlToken ConsumeSpace()
    {
        int start = _position;
        int startColumn = _column;

        while (_position < _source.Length && Current == ' ')
        {
            _position++;
            _column++;
        }

        string value = _source.Substring(start, _position - start);
        return new TamlToken(TamlTokenType.Whitespace, value, _line, startColumn, start, value.Length);
    }

    private TamlToken ConsumeNull()
    {
        int start = _position;
        int startColumn = _column;

        _position++;
        _column++;

        return new TamlToken(TamlTokenType.Null, "~", _line, startColumn, start, 1);
    }

    private TamlToken ConsumeEmptyString()
    {
        int start = _position;
        int startColumn = _column;

        _position += 2;
        _column += 2;

        return new TamlToken(TamlTokenType.EmptyString, "\"\"", _line, startColumn, start, 2);
    }

    private TamlToken ConsumeText()
    {
        int start = _position;
        int startColumn = _column;
        var sb = new StringBuilder();

        while (_position < _source.Length)
        {
            char c = Current;

            // Stop at tab, newline, or end
            if (c == '\t' || c == '\n' || c == '\r')
            {
                break;
            }

            // Check for embedded tab (error)
            if (c == '\t')
            {
                _errors.Add(new TamlError(
                    "Tab character found within content",
                    _position, 1, _line, _column,
                    TamlErrorCode.TabInContent));
                break;
            }

            sb.Append(c);
            _position++;
            _column++;

            // Check string length
            if (sb.Length > _options.MaxStringLength)
            {
                _errors.Add(new TamlError(
                    $"String length ({sb.Length:N0}) exceeds maximum allowed ({_options.MaxStringLength:N0})",
                    start, sb.Length, _line, startColumn,
                    TamlErrorCode.InputSizeExceeded));
                break;
            }
        }

        string value = sb.ToString();

        // Determine if this is a key or value based on context
        // A key is followed by tab(s) OR is at the start of a line (parent key or list item)
        // A value is after a tab separator
        bool isKey = _position < _source.Length && Current == '\t';

        // Also consider it a key if it's at the start position of a line (after indentation)
        // This handles parent keys and list items which don't have tab-separated values
        if (!isKey && startColumn == _currentIndentLevel + 1)
        {
            isKey = true;
        }

        TamlTokenType type = isKey ? TamlTokenType.Key : TamlTokenType.Value;

        return new TamlToken(type, value, _line, startColumn, start, value.Length);
    }

    private TamlToken CreateToken(TamlTokenType type, string value)
    {
        return new TamlToken(type, value, _line, _column, _position, value.Length);
    }

    private char Current => _position < _source.Length ? _source[_position] : '\0';

    private char Peek(int offset = 1)
    {
        int pos = _position + offset;
        return pos < _source.Length ? _source[pos] : '\0';
    }

    private bool IsValueTerminator(char c)
    {
        return c == '\0' || c == '\t' || c == '\n' || c == '\r' || c == '#';
    }
}
