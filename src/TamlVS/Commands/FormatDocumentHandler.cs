using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

using TamlTokenizer;

namespace TamlVS.Commands
{
    internal class FormatDocumentHandler
    {
        public static async Task RegisterAsync()
        {
            // Intercept Edit.FormatDocument command (Ctrl+K, Ctrl+D)
            await VS.Commands.InterceptAsync(
                KnownCommands.Edit_FormatDocument.Guid,
                KnownCommands.Edit_FormatDocument.ID,
                () => ExecuteFormat(formatSelection: false));

            // Intercept Edit.FormatSelection command (Ctrl+K, Ctrl+F)
            await VS.Commands.InterceptAsync(
                KnownCommands.Edit_FormatSelection.Guid,
                KnownCommands.Edit_FormatSelection.ID,
                () => ExecuteFormat(formatSelection: true));
        }

        private static CommandProgression ExecuteFormat(bool formatSelection)
        {
            return ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                DocumentView docView = await VS.Documents.GetActiveDocumentViewAsync();

                if (docView?.TextBuffer == null || docView.TextBuffer.ContentType.TypeName != Constants.LanguageName)
                {
                    return CommandProgression.Continue;
                }

                ITextBuffer buffer = docView.TextBuffer;
                ITextView textView = docView.TextView;

                try
                {
                    if (formatSelection && !textView.Selection.IsEmpty)
                    {
                        return FormatSelection(buffer, textView);
                    }
                    else
                    {
                        return FormatDocument(buffer);
                    }
                }
                catch
                {
                    return CommandProgression.Continue;
                }
            });
        }

        private static CommandProgression FormatDocument(ITextBuffer buffer)
        {
            var originalText = buffer.CurrentSnapshot.GetText();

            if (string.IsNullOrEmpty(originalText))
            {
                return CommandProgression.Stop;
            }

            var formattedText = Taml.Format(originalText, GetFormatterOptions());

            if (formattedText != originalText)
            {
                using (ITextEdit edit = buffer.CreateEdit())
                {
                    edit.Replace(new Span(0, originalText.Length), formattedText);
                    edit.Apply();
                }
            }

            return CommandProgression.Stop;
        }

        private static CommandProgression FormatSelection(ITextBuffer buffer, ITextView textView)
        {
            SnapshotSpan selectionSpan = textView.Selection.SelectedSpans[0];

            // Expand selection to full lines for proper formatting
            ITextSnapshotLine startLine = selectionSpan.Start.GetContainingLine();
            ITextSnapshotLine endLine = selectionSpan.End.GetContainingLine();

            // If selection ends at the start of a line, use the previous line
            if (selectionSpan.End == endLine.Start && endLine.LineNumber > startLine.LineNumber)
            {
                endLine = buffer.CurrentSnapshot.GetLineFromLineNumber(endLine.LineNumber - 1);
            }

            var startPos = startLine.Start.Position;
            var endPos = endLine.End.Position;
            var selectedText = buffer.CurrentSnapshot.GetText(startPos, endPos - startPos);

            if (string.IsNullOrEmpty(selectedText))
            {
                return CommandProgression.Stop;
            }

            var formattedText = Taml.Format(selectedText, GetFormatterOptions());

            if (formattedText != selectedText)
            {
                using (ITextEdit edit = buffer.CreateEdit())
                {
                    edit.Replace(new Span(startPos, selectedText.Length), formattedText);
                    edit.Apply();
                }
            }

            return CommandProgression.Stop;
        }

        private static TamlFormatterOptions GetFormatterOptions()
        {
            var options = GeneralOptions.Instance;
            return new TamlFormatterOptions
            {
                AlignValues = options.AlignValues,
                TrimTrailingWhitespace = options.TrimTrailingWhitespace,
                EnsureTrailingNewline = options.EnsureTrailingNewline,
                PreserveBlankLines = options.PreserveBlankLines,
                TabSize = options.TabSize
            };
        }
    }
}
