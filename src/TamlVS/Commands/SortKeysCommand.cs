using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.Text;

namespace TamlVS.Commands
{
    [Command(PackageIds.SortKeys)]
    internal sealed class SortKeysCommand : BaseCommand<SortKeysCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            DocumentView docView = await VS.Documents.GetActiveDocumentViewAsync();
            if (docView?.TextBuffer == null)
            {
                return;
            }

            ITextSnapshot snapshot = docView.TextBuffer.CurrentSnapshot;
            var caretPosition = docView.TextView.Caret.Position.BufferPosition.Position;
            var cursorLineNumber = snapshot.GetLineFromPosition(caretPosition).LineNumber;

            (int startLine, int endLine, string sortedText)? result = GetSortedSectionAtLine(snapshot, cursorLineNumber);
            if (result == null)
            {
                await VS.StatusBar.ShowMessageAsync("Nothing to sort - place cursor on a key with child keys");
                return;
            }


            (var startLine, var endLine, var sortedText) = result.Value;

            ITextSnapshotLine firstLine = snapshot.GetLineFromLineNumber(startLine);
            ITextSnapshotLine lastLine = snapshot.GetLineFromLineNumber(endLine);
            var spanToReplace = new Span(firstLine.Start.Position, lastLine.End.Position - firstLine.Start.Position);

            using (ITextEdit edit = docView.TextBuffer.CreateEdit())
            {
                edit.Replace(spanToReplace, sortedText);
                edit.Apply();
            }

            await VS.StatusBar.ShowMessageAsync("Keys sorted");
        }

        /// <summary>
        /// Returns the sorted section info: (startLine, endLine, sortedText) where lines are 0-based.
        /// Returns null if no sorting is needed.
        /// </summary>
        private static (int startLine, int endLine, string sortedText)? GetSortedSectionAtLine(ITextSnapshot snapshot, int cursorLineNumber)
        {
            if (cursorLineNumber >= snapshot.LineCount)
            {
                return null;
            }

            var parentLine = snapshot.GetLineFromLineNumber(cursorLineNumber);
            var parentText = parentLine.GetText();
            if (string.IsNullOrWhiteSpace(parentText))
            {
                return null;
            }

            var parentIndent = parentText.Length - parentText.TrimStart().Length;

            // Find first child line and its indent
            var firstChildLine = -1;
            var childIndent = -1;
            for (var i = cursorLineNumber + 1; i < snapshot.LineCount; i++)
            {
                var lineText = snapshot.GetLineFromLineNumber(i).GetText();
                if (string.IsNullOrWhiteSpace(lineText))
                {
                    break; // Stop at empty line
                }

                var indent = lineText.Length - lineText.TrimStart().Length;
                if (indent <= parentIndent)
                {
                    break; // Back to parent level
                }

                firstChildLine = i;
                childIndent = indent;
                break;
            }

            if (firstChildLine < 0)
            {
                return null;
            }

            // Find last line to include (stop at empty line or parent-level line)
            var lastChildLine = firstChildLine;
            for (var i = firstChildLine + 1; i < snapshot.LineCount; i++)
            {
                var lineText = snapshot.GetLineFromLineNumber(i).GetText();
                if (string.IsNullOrWhiteSpace(lineText))
                {
                    break; // Stop at empty line
                }

                var indent = lineText.Length - lineText.TrimStart().Length;
                if (indent <= parentIndent)
                {
                    break; // Back to parent level
                }

                lastChildLine = i;
            }

            // Collect sections: each section starts at childIndent
            var sections = new List<(int startLine, int endLine, string key)>();
            var currentStart = -1;
            string currentKey = null;

            for (var i = firstChildLine; i <= lastChildLine; i++)
            {
                var lineText = snapshot.GetLineFromLineNumber(i).GetText();
                var trimmed = lineText.TrimStart();
                var indent = lineText.Length - trimmed.Length;

                if (indent == childIndent)
                {
                    // New section starts
                    if (currentStart >= 0)
                    {
                        sections.Add((currentStart, i - 1, currentKey));
                    }
                    currentStart = i;
                    var tabIndex = trimmed.IndexOf('\t');
                    currentKey = tabIndex > 0 ? trimmed.Substring(0, tabIndex) : trimmed;
                }
            }

            // Add last section
            if (currentStart >= 0)
            {
                sections.Add((currentStart, lastChildLine, currentKey));
            }

            if (sections.Count <= 1)
            {
                return null;
            }

            var sortedSections = sections.OrderBy(s => s.key, StringComparer.OrdinalIgnoreCase).ToList();

            // Check if already sorted
            var alreadySorted = true;
            for (var i = 0; i < sections.Count; i++)
            {
                if (sections[i].key != sortedSections[i].key)
                {
                    alreadySorted = false;
                    break;
                }
            }

            if (alreadySorted)
            {
                return null;
            }

            // Build sorted text
            var sb = new StringBuilder();
            foreach (var section in sortedSections)
            {
                for (var i = section.startLine; i <= section.endLine; i++)
                {
                    var line = snapshot.GetLineFromLineNumber(i);
                    sb.Append(line.GetTextIncludingLineBreak());
                }
            }

            // Remove trailing line break
            var result = sb.ToString().TrimEnd('\r', '\n');

            return (firstChildLine, lastChildLine, result);
        }
    }
}
