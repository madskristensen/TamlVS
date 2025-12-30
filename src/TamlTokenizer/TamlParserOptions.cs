using System;

namespace TamlTokenizer;

/// <summary>
/// Configuration options for the TAML lexer and parser with security limits.
/// </summary>
public sealed class TamlParserOptions
{
    /// <summary>Default maximum input size: 10 MB.</summary>
    public const int DefaultMaxInputSize = 10 * 1024 * 1024;

    /// <summary>Default maximum nesting depth: 64 levels.</summary>
    public const int DefaultMaxNestingDepth = 64;

    /// <summary>Default maximum token count: 1,000,000 tokens.</summary>
    public const int DefaultMaxTokenCount = 1_000_000;

    /// <summary>Default maximum string length: 64 KB.</summary>
    public const int DefaultMaxStringLength = 64 * 1024;

    /// <summary>Default maximum line count: 100,000 lines.</summary>
    public const int DefaultMaxLineCount = 100_000;

    private int _maxInputSize = DefaultMaxInputSize;
    private int _maxNestingDepth = DefaultMaxNestingDepth;
    private int _maxTokenCount = DefaultMaxTokenCount;
    private int _maxStringLength = DefaultMaxStringLength;
    private int _maxLineCount = DefaultMaxLineCount;

    /// <summary>
    /// Gets the default parser options with standard security limits.
    /// </summary>
    public static TamlParserOptions Default { get; } = new TamlParserOptions();

    /// <summary>
    /// Gets or sets the maximum input size in bytes.
    /// Protects against memory exhaustion from extremely large inputs.
    /// </summary>
    /// <value>Default is 10 MB.</value>
    /// <exception cref="ArgumentOutOfRangeException">Value is less than 1.</exception>
    public int MaxInputSize
    {
        get => _maxInputSize;
        set
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value), "MaxInputSize must be at least 1");
            _maxInputSize = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum nesting depth.
    /// Protects against stack overflow from deeply nested structures.
    /// </summary>
    /// <value>Default is 64 levels.</value>
    /// <exception cref="ArgumentOutOfRangeException">Value is less than 1.</exception>
    public int MaxNestingDepth
    {
        get => _maxNestingDepth;
        set
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value), "MaxNestingDepth must be at least 1");
            _maxNestingDepth = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of tokens.
    /// Protects against algorithmic complexity attacks.
    /// </summary>
    /// <value>Default is 1,000,000 tokens.</value>
    /// <exception cref="ArgumentOutOfRangeException">Value is less than 1.</exception>
    public int MaxTokenCount
    {
        get => _maxTokenCount;
        set
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value), "MaxTokenCount must be at least 1");
            _maxTokenCount = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum string length in characters.
    /// Protects against memory exhaustion from extremely long strings.
    /// </summary>
    /// <value>Default is 64 KB.</value>
    /// <exception cref="ArgumentOutOfRangeException">Value is less than 1.</exception>
    public int MaxStringLength
    {
        get => _maxStringLength;
        set
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value), "MaxStringLength must be at least 1");
            _maxStringLength = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of lines.
    /// Protects against extremely long documents.
    /// </summary>
    /// <value>Default is 100,000 lines.</value>
    /// <exception cref="ArgumentOutOfRangeException">Value is less than 1.</exception>
    public int MaxLineCount
    {
        get => _maxLineCount;
        set
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value), "MaxLineCount must be at least 1");
            _maxLineCount = value;
        }
    }

    /// <summary>
    /// Gets or sets whether to use strict validation mode.
    /// When true, the parser rejects invalid TAML immediately.
    /// When false (lenient mode), it attempts to recover and continue.
    /// </summary>
    /// <value>Default is false (lenient mode).</value>
    public bool StrictMode { get; set; } = false;

    /// <summary>
    /// Creates a new instance with default security limits.
    /// </summary>
    public TamlParserOptions()
    {
    }

    /// <summary>
    /// Creates a custom parser options instance with specified limits.
    /// </summary>
    public TamlParserOptions(
        int maxInputSize = DefaultMaxInputSize,
        int maxNestingDepth = DefaultMaxNestingDepth,
        int maxTokenCount = DefaultMaxTokenCount,
        int maxStringLength = DefaultMaxStringLength,
        int maxLineCount = DefaultMaxLineCount,
        bool strictMode = false)
    {
        MaxInputSize = maxInputSize;
        MaxNestingDepth = maxNestingDepth;
        MaxTokenCount = maxTokenCount;
        MaxStringLength = maxStringLength;
        MaxLineCount = maxLineCount;
        StrictMode = strictMode;
    }

    /// <summary>
    /// Creates a copy of the current options.
    /// </summary>
    public TamlParserOptions Clone()
    {
        return new TamlParserOptions(
            maxInputSize: MaxInputSize,
            maxNestingDepth: MaxNestingDepth,
            maxTokenCount: MaxTokenCount,
            maxStringLength: MaxStringLength,
            maxLineCount: MaxLineCount,
            strictMode: StrictMode);
    }

    /// <summary>
    /// Returns a string representation of the current limits.
    /// </summary>
    public override string ToString()
    {
        return $"TamlParserOptions {{ " +
               $"MaxInputSize={FormatBytes(MaxInputSize)}, " +
               $"MaxNestingDepth={MaxNestingDepth}, " +
               $"MaxTokenCount={MaxTokenCount:N0}, " +
               $"MaxStringLength={MaxStringLength:N0}, " +
               $"MaxLineCount={MaxLineCount:N0}, " +
               $"StrictMode={StrictMode} }}";
    }

    private static string FormatBytes(int bytes)
    {
        if (bytes >= 1024 * 1024)
            return $"{bytes / (1024.0 * 1024):F1} MB";
        if (bytes >= 1024)
            return $"{bytes / 1024.0:F1} KB";
        return $"{bytes} bytes";
    }
}
