using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;

namespace TamlVS.Editor
{
    /// <summary>
    /// Provides functionality to sort TAML keys alphabetically within a document.
    /// </summary>
    internal static class TamlSorter
    {
        /// <summary>
        /// Represents a sortable section of TAML content (a key and its nested children).
        /// </summary>
        private readonly struct Section(int startLine, int endLine, string key)
        {
            public int StartLine { get; } = startLine;
            public int EndLine { get; } = endLine;
            public string Key { get; } = key;
        }

        /// <summary>
        /// Result of a sort operation containing the line range and sorted text.
        /// </summary>
        public readonly struct SortResult(int startLine, int endLine, string sortedText)
        {
            public int StartLine { get; } = startLine;
            public int EndLine { get; } = endLine;
            public string SortedText { get; } = sortedText;
        }

        /// <summary>
        /// Determines if the keys at the specified cursor position can be sorted.
        /// Returns true if there are at least 2 child keys that are not already sorted.
        /// </summary>
        public static bool CanSort(ITextSnapshot snapshot, int cursorLineNumber)
        {
            if (cursorLineNumber >= snapshot.LineCount)
            {
                return false;
            }

            var parentText = snapshot.GetLineFromLineNumber(cursorLineNumber).GetText();
            if (string.IsNullOrWhiteSpace(parentText))
            {
                return false;
            }

            var parentIndent = GetIndent(parentText);
            var childIndent = -1;
            var keys = new List<string>();

            for (var i = cursorLineNumber + 1; i < snapshot.LineCount; i++)
            {
                var lineText = snapshot.GetLineFromLineNumber(i).GetText();
                if (string.IsNullOrWhiteSpace(lineText))
                {
                    break;
                }

                var indent = GetIndent(lineText);
                if (indent <= parentIndent)
                {
                    break;
                }

                if (childIndent < 0)
                {
                    childIndent = indent;
                }

                if (indent == childIndent)
                {
                    keys.Add(ExtractKey(lineText.TrimStart()));
                }
            }

            if (keys.Count < 2)
            {
                return false;
            }

            // Check if already sorted
            var sortedKeys = keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
            for (var i = 0; i < keys.Count; i++)
            {
                if (!string.Equals(keys[i], sortedKeys[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true; // Not sorted, can sort
                }
            }

            return false; // Already sorted
        }

        /// <summary>
        /// Sorts the child keys at the specified cursor position alphabetically.
        /// Returns null if sorting is not possible or keys are already sorted.
        /// </summary>
        public static SortResult? Sort(ITextSnapshot snapshot, int cursorLineNumber)
        {
            var parentText = snapshot.GetLineFromLineNumber(cursorLineNumber).GetText();
            var parentIndent = GetIndent(parentText);

            // Find the range of child lines
            (var firstChildLine, var lastChildLine, var childIndent) = FindChildRange(snapshot, cursorLineNumber, parentIndent);
            if (firstChildLine < 0)
            {
                return null;
            }

            // Collect sections (each key with its nested content)
            List<Section> sections = CollectSections(snapshot, firstChildLine, lastChildLine, childIndent);
            if (sections.Count <= 1)
            {
                return null;
            }

            // Sort sections alphabetically by key
            var sortedSections = sections.OrderBy(s => s.Key, StringComparer.OrdinalIgnoreCase).ToList();

            // Check if already sorted
            if (IsAlreadySorted(sections, sortedSections))
            {
                return null;
            }

            // Build the sorted text
            var sortedText = BuildSortedText(snapshot, sortedSections);

            return new SortResult(firstChildLine, lastChildLine, sortedText);
        }

        private static (int firstLine, int lastLine, int childIndent) FindChildRange(
            ITextSnapshot snapshot, int cursorLineNumber, int parentIndent)
        {
            var firstChildLine = -1;
            var childIndent = -1;

            for (var i = cursorLineNumber + 1; i < snapshot.LineCount; i++)
            {
                var lineText = snapshot.GetLineFromLineNumber(i).GetText();
                if (string.IsNullOrWhiteSpace(lineText))
                {
                    break;
                }

                var indent = GetIndent(lineText);
                if (indent <= parentIndent)
                {
                    break;
                }

                firstChildLine = i;
                childIndent = indent;
                break;
            }

            if (firstChildLine < 0)
            {
                return (-1, -1, -1);
            }

            // Find the last line
            var lastChildLine = firstChildLine;
            for (var i = firstChildLine + 1; i < snapshot.LineCount; i++)
            {
                var lineText = snapshot.GetLineFromLineNumber(i).GetText();
                if (string.IsNullOrWhiteSpace(lineText))
                {
                    break;
                }

                var indent = GetIndent(lineText);
                if (indent <= parentIndent)
                {
                    break;
                }

                lastChildLine = i;
            }

            return (firstChildLine, lastChildLine, childIndent);
        }

        private static List<Section> CollectSections(
            ITextSnapshot snapshot, int firstLine, int lastLine, int childIndent)
        {
            var sections = new List<Section>();
            var currentStart = -1;
            string currentKey = null;

            for (var i = firstLine; i <= lastLine; i++)
            {
                var lineText = snapshot.GetLineFromLineNumber(i).GetText();
                var trimmed = lineText.TrimStart();
                var indent = GetIndent(lineText);

                if (indent == childIndent)
                {
                    if (currentStart >= 0)
                    {
                        sections.Add(new Section(currentStart, i - 1, currentKey));
                    }
                    currentStart = i;
                    currentKey = ExtractKey(trimmed);
                }
            }

            if (currentStart >= 0)
            {
                sections.Add(new Section(currentStart, lastLine, currentKey));
            }

            return sections;
        }

        private static bool IsAlreadySorted(List<Section> original, List<Section> sorted)
        {
            for (var i = 0; i < original.Count; i++)
            {
                if (original[i].Key != sorted[i].Key)
                {
                    return false;
                }
            }
            return true;
        }

        private static string BuildSortedText(ITextSnapshot snapshot, List<Section> sortedSections)
        {
            var sb = new StringBuilder();

            foreach (Section section in sortedSections)
            {
                for (var i = section.StartLine; i <= section.EndLine; i++)
                {
                    ITextSnapshotLine line = snapshot.GetLineFromLineNumber(i);
                    sb.Append(line.GetTextIncludingLineBreak());
                }
            }

            return sb.ToString().TrimEnd('\r', '\n');
        }

        /// <summary>
        /// Extracts the key from a trimmed line (text before the first tab, or the entire line).
        /// </summary>
        private static string ExtractKey(string trimmedLine)
        {
            var tabIndex = trimmedLine.IndexOf('\t');
            return tabIndex > 0 ? trimmedLine.Substring(0, tabIndex) : trimmedLine;
        }

        /// <summary>
        /// Gets the indentation level (number of leading whitespace characters).
        /// </summary>
        private static int GetIndent(string line)
        {
            return line.Length - line.TrimStart().Length;
        }
    }
}
