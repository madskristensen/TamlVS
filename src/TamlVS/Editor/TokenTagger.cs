using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using TamlTokenizer;

namespace TamlVS
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(TokenTag))]
    [ContentType(Constants.LanguageName)]
    [Name(Constants.LanguageName)]
    internal sealed class TokenTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag =>
            buffer.Properties.GetOrCreateSingletonProperty(() => new TokenTagger(buffer)) as ITagger<T>;
    }

    internal class TokenTagger : TokenTaggerBase, IDisposable
    {
        private readonly Document _document;
        private static readonly ImageId _errorIcon = KnownMonikers.StatusWarning.ToImageId();
        private bool _isDisposed;

        internal TokenTagger(ITextBuffer buffer) : base(buffer)
        {
            _document = buffer.GetDocument();
            _document.Parsed += ReParse;
        }

        private void ReParse(Document document)
        {
            _ = TokenizeAsync();
        }

        public override Task TokenizeAsync()
        {
            // Make sure this is running on a background thread.
            ThreadHelper.ThrowIfOnUIThread();

            List<ITagSpan<TokenTag>> list = [];

            if (_document.Result != null)
            {
                TagAll(list);

                if (list.Any())
                {
                    CreateErrorListItems(list);
                    OnTagsUpdated(list);
                }
            }

            return Task.CompletedTask;
        }

        private void TagAll(List<ITagSpan<TokenTag>> list)
        {
            ITextSnapshot snapshot = Buffer.CurrentSnapshot;
            IReadOnlyList<TamlToken> tokens = _document.Result.Tokens;

            for (var i = 0; i < tokens.Count; i++)
            {
                TamlToken token = tokens[i];
                var tokenSpanData = token.ToSpan();

                // Validate span is within snapshot bounds
                if (tokenSpanData.End > snapshot.Length)
                {
                    continue;
                }

                // Check if this key has nested keys for outlining
                if (token.Type == TamlTokenType.Key)
                {
                    var endPosition = FindOutliningEnd(tokens, i, snapshot, out var hasNestedKeys);

                    if (hasNestedKeys && endPosition > tokenSpanData.End)
                    {
                        // Create outlining span using Whitespace type so it doesn't affect colorization
                        var outliningSpan = new SnapshotSpan(snapshot, Span.FromBounds(tokenSpanData.Start, endPosition));
                        TokenTag outliningTag = CreateToken(TamlTokenType.Whitespace, true, true, null);
                        list.Add(new TagSpan<TokenTag>(outliningSpan, outliningTag));
                    }
                }

                // Add token for colorization
                var span = new SnapshotSpan(snapshot, tokenSpanData);
                TokenTag tag = CreateToken(token.Type, true, false, null);
                list.Add(new TagSpan<TokenTag>(span, tag));
            }
        }

        /// <summary>
        /// Finds the end position for an outlining region starting at the given Key token.
        /// Also determines if the direct children contain nested keys (not just values).
        /// Returns the position at the end of the last content on the last child's line.
        /// </summary>
        private static int FindOutliningEnd(IReadOnlyList<TamlToken> tokens, int keyIndex, ITextSnapshot snapshot, out bool hasNestedKeys)
        {
            var depth = 0;
            var lastContentLine = tokens[keyIndex].Line;
            var lastContentEndInLine = tokens[keyIndex].EndPosition;
            hasNestedKeys = false;
            var foundIndent = false;

            for (var i = keyIndex + 1; i < tokens.Count; i++)
            {
                TamlToken token = tokens[i];

                if (token.Type == TamlTokenType.Indent)
                {
                    if (depth == 0)
                    {
                        foundIndent = true;
                    }
                    depth++;
                }
                else if (token.Type == TamlTokenType.Dedent)
                {
                    depth--;
                    if (depth <= 0)
                    {
                        if (!foundIndent)
                        {
                            return tokens[keyIndex].EndPosition;
                        }

                        // Return the end position of the last content on its line
                        return Math.Min(lastContentEndInLine, snapshot.Length);
                    }
                }
                else if (token.Type == TamlTokenType.Key)
                {
                    if (depth == 0)
                    {
                        return tokens[keyIndex].EndPosition;
                    }
                    else if (depth == 1 && foundIndent)
                    {
                        hasNestedKeys = true;
                    }
                }

                // Track the last content token's end position
                // Only update if this is a content token (Key, Value, Null, EmptyString, Comment)
                if (depth > 0 && IsContentToken(token.Type))
                {
                    // If this is on a new line, or further in the same line, update
                    if (token.Line > lastContentLine || 
                        (token.Line == lastContentLine && token.EndPosition > lastContentEndInLine))
                    {
                        lastContentLine = token.Line;
                        lastContentEndInLine = token.EndPosition;
                    }
                }
            }

            if (!foundIndent)
            {
                return tokens[keyIndex].EndPosition;
            }

            return Math.Min(lastContentEndInLine, snapshot.Length);
        }

        /// <summary>
        /// Determines if a token type represents actual content (not structural or whitespace).
        /// </summary>
        private static bool IsContentToken(TamlTokenType type)
        {
            return type == TamlTokenType.Key ||
                   type == TamlTokenType.Value ||
                   type == TamlTokenType.Null ||
                   type == TamlTokenType.EmptyString ||
                   type == TamlTokenType.Comment;
        }

        private void CreateErrorListItems(List<ITagSpan<TokenTag>> list)
        {
            foreach (TamlError error in _document.Result.Errors)
            {
                // Find the best matching span for this error, excluding whitespace/outlining tags
                ITagSpan<TokenTag> span =
                    list.FirstOrDefault(s => !Equals(s.Tag.TokenType, TamlTokenType.Whitespace) &&
                                             s.Span.Start <= error.Position &&
                                             s.Span.End >= error.EndPosition) ??
                    list.FirstOrDefault(s => !Equals(s.Tag.TokenType, TamlTokenType.Whitespace) &&
                                             s.Span.Start.GetContainingLineNumber() == error.Line - 1);

                if (span == null)
                {
                    continue;
                }

                span.Tag.Errors =
                [
                    new ErrorListItem
                    {
                        ProjectName = "",
                        FileName = _document.FileName,
                        Message = error.Message,
                        ErrorCategory = PredefinedErrorTypeNames.SyntaxError,
                        Severity = __VSERRORCATEGORY.EC_ERROR,
                        Line = error.Line - 1,
                        Column = error.Column - 1,
                        BuildTool = Vsix.Name,
                        ErrorCode = error.Code ?? ""
                    }
                ];
            }
        }

        public override Task<object> GetTooltipAsync(SnapshotPoint triggerPoint)
        {
            ITagSpan<TokenTag> item = TagsCache.FirstOrDefault(s => s.Tag.Errors.Any() && s.Span.Contains(triggerPoint.Position));

            // Error messages
            if (item != null)
            {
                ContainerElement elm = new(
                    ContainerElementStyle.Wrapped,
                    new ImageElement(_errorIcon),
                    string.Join(Environment.NewLine, item.Tag.Errors.Select(e => e.Message)));

                return Task.FromResult<object>(elm);
            }

            return Task.FromResult<object>(null);
        }

        public override string GetOutliningText(string text)
        {
            var tab = text.IndexOf('\t');
            return tab > 0 ? text.Substring(0, tab) : base.GetOutliningText(text);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _document.Parsed -= ReParse;
                _document.Dispose();
            }

            _isDisposed = true;
        }
    }
}
