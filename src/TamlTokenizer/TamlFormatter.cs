using System;
using System.Collections.Generic;
using System.Text;

namespace TamlTokenizer;

/// <summary>
/// Formats TAML documents with consistent indentation and value alignment.
/// </summary>
/// <remarks>
/// The formatter ensures:
/// - Proper tab-based indentation for hierarchy
/// - Values at the same indentation level aligned to the same column
/// - Consistent line endings
/// - Optional trailing newline
/// </remarks>
public sealed class TamlFormatter
{
    private readonly TamlFormatterOptions _options;

    /// <summary>
    /// Creates a new TAML formatter with default options.
    /// </summary>
    public TamlFormatter() : this(TamlFormatterOptions.Default)
    {
    }

    /// <summary>
    /// Creates a new TAML formatter with custom options.
    /// </summary>
    /// <param name="options">The formatter options to use.</param>
    public TamlFormatter(TamlFormatterOptions? options)
    {
        _options = options ?? TamlFormatterOptions.Default;
    }

    /// <summary>
    /// Formats a TAML document.
    /// </summary>
    /// <param name="source">The TAML source text to format.</param>
    /// <returns>The formatted TAML text.</returns>
    /// <exception cref="ArgumentNullException">source is null.</exception>
    public string Format(string source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        if (string.IsNullOrWhiteSpace(source))
            return _options.EnsureTrailingNewline ? "\n" : string.Empty;

        // Parse into lines with structure info
        var lines = ParseLines(source);

        // Calculate alignment for each indentation level
        if (_options.AlignValues)
        {
            CalculateAlignment(lines);
        }

        // Build formatted output
        return BuildOutput(lines);
    }

    private List<TamlLine> ParseLines(string source)
    {
        var rawLines = source.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        // Pre-allocate list capacity
        var lines = new List<TamlLine>(rawLines.Length);

        foreach (var rawLine in rawLines)
        {
            lines.Add(ParseLine(rawLine));
        }

        return lines;
    }

    private TamlLine ParseLine(string line)
    {
        var result = new TamlLine();

        if (string.IsNullOrEmpty(line))
        {
            result.IsBlank = true;
            return result;
        }

        int pos = 0;

        // Count leading tabs (indentation)
        while (pos < line.Length && line[pos] == '\t')
        {
            result.IndentLevel++;
            pos++;
        }

        // Count leading spaces and convert to tabs (4 spaces = 1 tab level)
        int spaceCount = 0;
        while (pos < line.Length && line[pos] == ' ')
        {
            spaceCount++;
            pos++;
        }
        result.IndentLevel += spaceCount / _options.TabSize;

        if (pos >= line.Length)
        {
            result.IsBlank = true;
            return result;
        }

        // Check for comment
        if (line[pos] == '#')
        {
            result.IsComment = true;
            result.Content = line.Substring(pos);
            return result;
        }

        // Parse key
        int keyStart = pos;
        while (pos < line.Length && line[pos] != '\t')
        {
            pos++;
        }

        result.Key = line.Substring(keyStart, pos - keyStart);

        // Check for value after tab(s)
        if (pos < line.Length && line[pos] == '\t')
        {
            // Skip all tabs between key and value
            while (pos < line.Length && line[pos] == '\t')
            {
                pos++;
            }

            // Get value (rest of line)
            if (pos < line.Length)
            {
                result.Value = _options.TrimTrailingWhitespace
                    ? line.Substring(pos).TrimEnd()
                    : line.Substring(pos);
                result.HasValue = true;
            }
        }

        return result;
    }

    private void CalculateAlignment(List<TamlLine> lines)
    {
        // Group lines by indentation level only
        // All lines at the same indent level get aligned together
        var levelGroups = new Dictionary<int, List<TamlLine>>();

        foreach (var line in lines)
        {
            if (line.IsBlank || line.IsComment || !line.HasValue)
                continue;

            if (!levelGroups.TryGetValue(line.IndentLevel, out var group))
            {
                group = new List<TamlLine>();
                levelGroups[line.IndentLevel] = group;
            }

            group.Add(line);
        }

        // Calculate max key length for each indent level and set alignment
        foreach (var kvp in levelGroups)
        {
            var group = kvp.Value;
            int maxKeyLength = 0;

            foreach (var line in group)
            {
                if (!string.IsNullOrEmpty(line.Key))
                {
                    maxKeyLength = Math.Max(maxKeyLength, line.Key!.Length);
                }
            }

            // Set alignment column for all lines in group
            foreach (var line in group)
            {
                line.AlignmentColumn = maxKeyLength;
            }
        }
    }

    private string BuildOutput(List<TamlLine> lines)
    {
        var sb = new StringBuilder();

        // Remove trailing blank lines from input
        int lastContentIndex = lines.Count - 1;
        while (lastContentIndex >= 0 && lines[lastContentIndex].IsBlank)
        {
            lastContentIndex--;
        }

        bool lastWasBlank = false;

        for (int i = 0; i <= lastContentIndex; i++)
        {
            var line = lines[i];

            // Handle blank lines
            if (line.IsBlank)
            {
                if (_options.PreserveBlankLines && !lastWasBlank)
                {
                    sb.Append(_options.NormalizeLineEndings ? "\n" : Environment.NewLine);
                    lastWasBlank = true;
                }
                continue;
            }

            lastWasBlank = false;

            // Build the line
            // Add indentation
            for (int t = 0; t < line.IndentLevel; t++)
            {
                sb.Append('\t');
            }

            if (line.IsComment)
            {
                sb.Append(line.Content);
            }
            else if (!string.IsNullOrEmpty(line.Key))
            {
                sb.Append(line.Key);

                if (line.HasValue)
                {
                    // Calculate tabs needed for alignment
                    int tabsNeeded = CalculateTabsForAlignment(line.Key!.Length, line.AlignmentColumn);
                    tabsNeeded = Math.Max(tabsNeeded, _options.MinTabsBetweenKeyAndValue);

                    for (int t = 0; t < tabsNeeded; t++)
                    {
                        sb.Append('\t');
                    }

                    sb.Append(line.Value);
                }
            }

            // Add newline
            sb.Append(_options.NormalizeLineEndings ? "\n" : Environment.NewLine);
        }

        string result = sb.ToString();

        // Ensure trailing newline if requested, or remove if not
        if (_options.EnsureTrailingNewline)
        {
            if (!result.EndsWith("\n", StringComparison.Ordinal) && result.Length > 0)
            {
                result += _options.NormalizeLineEndings ? "\n" : Environment.NewLine;
            }
        }
        else
        {
            // Remove trailing newline
            while (result.EndsWith("\n", StringComparison.Ordinal) || result.EndsWith("\r", StringComparison.Ordinal))
            {
                result = result.TrimEnd('\n', '\r');
            }
        }

        return result;
    }

    private int CalculateTabsForAlignment(int keyLength, int alignmentColumn)
    {
        if (alignmentColumn == 0)
            return _options.MinTabsBetweenKeyAndValue;

        // Simple approach: calculate tabs needed based on alignment column
        // We want all values to start at the same visual column
        // alignmentColumn is the length of the longest key in the group

        // Calculate tabs needed to reach just past the longest key
        int targetColumn = alignmentColumn + _options.TabSize;

        // How many tabs do we need from this key to reach that column?
        int tabsNeeded = 1;
        int currentPos = keyLength;

        // Advance through tab stops until we're at or past target
        while (currentPos + _options.TabSize <= targetColumn)
        {
            // Move to next tab stop
            currentPos = ((currentPos / _options.TabSize) + 1) * _options.TabSize;
            if (currentPos < targetColumn)
            {
                tabsNeeded++;
            }
        }

        return Math.Max(tabsNeeded, _options.MinTabsBetweenKeyAndValue);
    }

    /// <summary>
    /// Represents a parsed TAML line for formatting.
    /// </summary>
    private sealed class TamlLine
    {
        public int IndentLevel { get; set; }
        public string? Key { get; set; }
        public string? Value { get; set; }
        public bool HasValue { get; set; }
        public bool IsComment { get; set; }
        public bool IsBlank { get; set; }
        public string? Content { get; set; }
        public int AlignmentColumn { get; set; }
    }
}
