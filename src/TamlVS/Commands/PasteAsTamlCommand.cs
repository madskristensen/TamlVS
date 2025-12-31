using System.Windows;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using TamlTokenizer;

namespace TamlVS.Commands
{
    [Command(PackageIds.PasteAsTaml)]
    internal sealed class PasteAsTamlCommand : BaseCommand<PasteAsTamlCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            DocumentView docView = await VS.Documents.GetActiveDocumentViewAsync();

            if (docView?.TextBuffer == null)
            {
                return;
            }

            try
            {
                if (!Clipboard.ContainsText())
                {
                    await VS.StatusBar.ShowMessageAsync("Clipboard does not contain text");
                    return;
                }

                var jsonText = Clipboard.GetText();
                var taml = TamlConverter.FromJson(jsonText);

                // Get the current selection or cursor position
                IWpfTextView textView = docView.TextView;
                ITextSelection selection = textView.Selection;

                using (ITextEdit edit = docView.TextBuffer.CreateEdit())
                {
                    if (!selection.IsEmpty)
                    {
                        // Replace selection
                        foreach (SnapshotSpan span in selection.SelectedSpans)
                        {
                            edit.Replace(span, taml);
                        }
                    }
                    else
                    {
                        // Insert at cursor
                        var caretPosition = textView.Caret.Position.BufferPosition.Position;
                        edit.Insert(caretPosition, taml);
                    }
                    edit.Apply();
                }

                await VS.StatusBar.ShowMessageAsync("JSON pasted as TAML");
            }
            catch (FormatException ex)
            {
                await VS.StatusBar.ShowMessageAsync($"Invalid JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowMessageAsync($"Failed to convert from JSON: {ex.Message}");
            }
        }
    }
}
