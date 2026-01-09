using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace TamlVS.Editor
{
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [Name("TAML Suggested Actions")]
    [ContentType(Constants.LanguageName)]
    internal class SuggestedActionsSourceProvider : ISuggestedActionsSourceProvider
    {
        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            return new SuggestedActionsSource(textBuffer);
        }
    }

    internal class SuggestedActionsSource(ITextBuffer textBuffer) : ISuggestedActionsSource2
    {
        public event EventHandler<EventArgs> SuggestedActionsChanged { add { } remove { } }

        public void Dispose() { }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(
            ISuggestedActionCategorySet requestedActionCategories,
            SnapshotSpan range,
            CancellationToken cancellationToken)
        {
            var lineNumber = range.Start.GetContainingLine().LineNumber;

            if (TamlSorter.CanSort(range.Snapshot, lineNumber))
            {
                yield return new SuggestedActionSet(
                    categoryName: PredefinedSuggestedActionCategoryNames.Refactoring,
                    actions: [new SortKeysAction(range.Snapshot, lineNumber, textBuffer)],
                    title: "TAML");
            }
        }

        public Task<ISuggestedActionCategorySet> GetSuggestedActionCategoriesAsync(
            ISuggestedActionCategorySet requestedActionCategories,
            SnapshotSpan range,
            CancellationToken cancellationToken)
        {
            var lineNumber = range.Start.GetContainingLine().LineNumber;

            if (TamlSorter.CanSort(range.Snapshot, lineNumber))
            {
                return Task.FromResult(requestedActionCategories);
            }

            return Task.FromResult<ISuggestedActionCategorySet>(null);
        }

        public Task<bool> HasSuggestedActionsAsync(
            ISuggestedActionCategorySet requestedActionCategories,
            SnapshotSpan range,
            CancellationToken cancellationToken)
        {
            var lineNumber = range.Start.GetContainingLine().LineNumber;
            return Task.FromResult(TamlSorter.CanSort(range.Snapshot, lineNumber));
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }

    /// <summary>
    /// Suggested action that sorts child keys alphabetically.
    /// </summary>
    internal class SortKeysAction(ITextSnapshot snapshot, int cursorLineNumber, ITextBuffer textBuffer) : ISuggestedAction
    {
        public string DisplayText => "Sort keys alphabetically";
        public string IconAutomationText => null;
        public ImageMoniker IconMoniker => KnownMonikers.SortAscending;
        public string InputGestureText => null;
        public bool HasActionSets => false;
        public bool HasPreview => false;

        public void Dispose() { }

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<SuggestedActionSet>>(null);

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
            => Task.FromResult<object>(null);

        public void Invoke(CancellationToken cancellationToken)
        {
            TamlSorter.SortResult? result = TamlSorter.Sort(snapshot, cursorLineNumber);
            if (result == null)
            {
                return;
            }

            TamlSorter.SortResult sortResult = result.Value;
            ITextSnapshotLine firstLine = snapshot.GetLineFromLineNumber(sortResult.StartLine);
            ITextSnapshotLine lastLine = snapshot.GetLineFromLineNumber(sortResult.EndLine);
            var spanToReplace = new Span(firstLine.Start.Position, lastLine.End.Position - firstLine.Start.Position);

            using (ITextEdit edit = textBuffer.CreateEdit())
            {
                edit.Replace(spanToReplace, sortResult.SortedText);
                edit.Apply();
            }
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }
}
