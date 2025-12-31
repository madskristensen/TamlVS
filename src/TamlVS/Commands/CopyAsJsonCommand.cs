using System.Windows;
using TamlTokenizer;

namespace TamlVS.Commands
{
    [Command(PackageIds.CopyAsJson)]
    internal sealed class CopyAsJsonCommand : BaseCommand<CopyAsJsonCommand>
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
                var tamlText = docView.TextBuffer.CurrentSnapshot.GetText();
                var json = TamlConverter.ToJson(tamlText, indented: true);
                Clipboard.SetText(json);
                await VS.StatusBar.ShowMessageAsync("TAML copied as JSON to clipboard");
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowMessageAsync($"Failed to convert to JSON: {ex.Message}");
            }
        }
    }
}
