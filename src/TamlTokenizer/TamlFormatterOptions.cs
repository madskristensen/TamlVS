using System;

namespace TamlTokenizer;

/// <summary>
/// Configuration options for the TAML formatter.
/// </summary>
public sealed class TamlFormatterOptions
{
    /// <summary>
    /// Gets the default formatter options.
    /// </summary>
    public static TamlFormatterOptions Default { get; } = new TamlFormatterOptions();

    /// <summary>
    /// Gets or sets the minimum number of tabs between a key and its value.
    /// </summary>
    /// <value>Default is 1.</value>
    public int MinTabsBetweenKeyAndValue { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether to align values at the same indentation level to the same column.
    /// </summary>
    /// <value>Default is true.</value>
    public bool AlignValues { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to remove trailing whitespace from lines.
    /// </summary>
    /// <value>Default is true.</value>
    public bool TrimTrailingWhitespace { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to ensure a single newline at the end of the document.
    /// </summary>
    /// <value>Default is true.</value>
    public bool EnsureTrailingNewline { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to normalize line endings to LF only.
    /// </summary>
    /// <value>Default is true.</value>
    public bool NormalizeLineEndings { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to preserve blank lines in the output.
    /// </summary>
    /// <value>Default is true.</value>
    public bool PreserveBlankLines { get; set; } = true;

    /// <summary>
    /// Gets or sets the tab size for calculating column alignment.
    /// </summary>
    /// <value>Default is 4.</value>
    public int TabSize { get; set; } = 4;

    /// <summary>
    /// Creates a copy of the current options.
    /// </summary>
    public TamlFormatterOptions Clone()
    {
        return new TamlFormatterOptions
        {
            MinTabsBetweenKeyAndValue = MinTabsBetweenKeyAndValue,
            AlignValues = AlignValues,
            TrimTrailingWhitespace = TrimTrailingWhitespace,
            EnsureTrailingNewline = EnsureTrailingNewline,
            NormalizeLineEndings = NormalizeLineEndings,
            PreserveBlankLines = PreserveBlankLines,
            TabSize = TabSize
        };
    }
}
