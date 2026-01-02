using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace TamlVS.Editor
{
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [ContentType(Constants.LanguageName)]
    [Name("TAML Completion")]
    internal class CompletionSourceProvider : IAsyncCompletionSourceProvider
    {
        public IAsyncCompletionSource GetOrCreate(ITextView textView)
        {
            return textView.Properties.GetOrCreateSingletonProperty(() => new CompletionSource());
        }
    }

    internal class CompletionSource() : IAsyncCompletionSource
    {
        private static readonly ImageId _keywordIcon = KnownMonikers.SelectObject.ToImageId();
        private static readonly ImageId _valueIcon = KnownMonikers.SelectObject.ToImageId();

        public Task<CompletionContext> GetCompletionContextAsync(
            IAsyncCompletionSession session,
            CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableToSpan,
            CancellationToken token)
        {
            // Only show completions in value position (after a tab following a key)
            if (!IsInValuePosition(triggerLocation))
            {
                return Task.FromResult(CompletionContext.Empty);
            }

            var items = ImmutableArray.Create(
                new CompletionItem("true", this, new ImageElement(_keywordIcon, "Boolean")),
                new CompletionItem("false", this, new ImageElement(_keywordIcon, "Boolean")),
                new CompletionItem("~", this, new ImageElement(_valueIcon, "Null")),
                new CompletionItem("\"\"", this, new ImageElement(_valueIcon, "Empty String"))
            );

            return Task.FromResult(new CompletionContext(items));
        }

        public Task<object> GetDescriptionAsync(
            IAsyncCompletionSession session,
            CompletionItem item,
            CancellationToken token)
        {
            // Return description based on the item
            var description = item.DisplayText switch
            {
                "true" => "Boolean true value",
                "false" => "Boolean false value",
                "~" => "Null value (tilde represents null in TAML)",
                "\"\"" => "Empty string value",
                _ => string.Empty
            };

            return Task.FromResult<object>(description);
        }

        public CompletionStartData InitializeCompletion(
            CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            CancellationToken token)
        {
            // Don't trigger on deletion
            if (trigger.Reason == CompletionTriggerReason.Deletion)
            {
                return CompletionStartData.DoesNotParticipateInCompletion;
            }

            // Check if we're in a value position
            if (!IsInValuePosition(triggerLocation))
            {
                return CompletionStartData.DoesNotParticipateInCompletion;
            }

            // Find the applicable span (current word being typed)
            SnapshotSpan applicableToSpan = GetApplicableToSpan(triggerLocation);
            return new CompletionStartData(CompletionParticipation.ProvidesItems, applicableToSpan);
        }

        /// <summary>
        /// Determines if the cursor is in a value position and completions should be shown.
        /// Only shows completions if the value is empty or partially matches one of our items.
        /// </summary>
        private static bool IsInValuePosition(SnapshotPoint point)
        {
            ITextSnapshotLine line = point.GetContainingLine();
            var lineText = line.GetText();

            // Empty line or comment line - no completions
            if (string.IsNullOrWhiteSpace(lineText) || lineText.TrimStart().StartsWith("#"))
            {
                return false;
            }

            // Skip leading tabs (indentation)
            var i = 0;
            while (i < lineText.Length && lineText[i] == '\t')
            {
                i++;
            }

            // Must have some key content after indentation
            if (i >= lineText.Length)
            {
                return false;
            }

            // Find the key-value separator tab (first tab after key content)
            while (i < lineText.Length && lineText[i] != '\t')
            {
                i++;
            }

            // Must have a separator tab
            if (i >= lineText.Length)
            {
                return false;
            }

            // Skip the separator tab
            i++;

            // Extract the current value (everything after the separator tab)
            var currentValue = i < lineText.Length ? lineText.Substring(i).Trim() : string.Empty;

            // Show completions if value is empty or partially matches one of our items
            if (string.IsNullOrEmpty(currentValue))
            {
                return true;
            }

            // Check if current value is a prefix of any completion item
            return "true".StartsWith(currentValue, StringComparison.OrdinalIgnoreCase)
                || "false".StartsWith(currentValue, StringComparison.OrdinalIgnoreCase)
                || "~".StartsWith(currentValue, StringComparison.Ordinal)
                || "\"\"".StartsWith(currentValue, StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets the span of text that the completion will replace.
        /// </summary>
        private static SnapshotSpan GetApplicableToSpan(SnapshotPoint point)
        {
            ITextSnapshotLine line = point.GetContainingLine();
            var lineText = line.GetText();
            var positionInLine = point.Position - line.Start.Position;

            // Find the start of the current word (scan backwards from cursor)
            var wordStart = positionInLine;
            while (wordStart > 0 && !char.IsWhiteSpace(lineText[wordStart - 1]))
            {
                wordStart--;
            }

            // Find the end of the current word (scan forwards from cursor)
            var wordEnd = positionInLine;
            while (wordEnd < lineText.Length && !char.IsWhiteSpace(lineText[wordEnd]))
            {
                wordEnd++;
            }

            var start = line.Start.Position + wordStart;
            var end = line.Start.Position + wordEnd;

            return new SnapshotSpan(point.Snapshot, start, end - start);
        }
    }
}
