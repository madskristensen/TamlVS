using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

using TamlTokenizer;

namespace TamlVS
{
    /// <summary>
    /// Provides dropdown navigation bars for TAML files, showing a hierarchical view of keys.
    /// </summary>
    internal sealed class DropdownBars : TypeAndMemberDropdownBars, IDisposable
    {
        private readonly LanguageService _languageService;
        private readonly IWpfTextView _textView;
        private readonly Document _document;
        private bool _disposed;
        private bool _hasBufferChanged;

        public DropdownBars(IVsTextView textView, LanguageService languageService) : base(languageService)
        {
            _languageService = languageService;
            _textView = textView.ToIWpfTextView();
            _document = _textView.TextBuffer.GetDocument();
            _document.Parsed += OnDocumentParsed;

            InitializeAsync(textView).FireAndForget();
        }

        /// <summary>
        /// Moves the caret to trigger initial dropdown load.
        /// </summary>
        private Task InitializeAsync(IVsTextView textView)
        {
            return ThreadHelper.JoinableTaskFactory.StartOnIdle(() =>
            {
                _ = textView.SendExplicitFocus();
                _ = _textView.Caret.MoveToNextCaretPosition();
                _textView.Caret.PositionChanged += CaretPositionChanged;
                _ = _textView.Caret.MoveToPreviousCaretPosition();
            }).Task;
        }

        private void CaretPositionChanged(object sender, CaretPositionChangedEventArgs e) => SynchronizeDropdowns();

        private void OnDocumentParsed(Document document)
        {
            _hasBufferChanged = true;
            SynchronizeDropdowns();
        }

        private void SynchronizeDropdowns()
        {
            if (_document.IsParsing)
            {
                return;
            }

            _ = ThreadHelper.JoinableTaskFactory.StartOnIdle(_languageService.SynchronizeDropdowns, VsTaskRunContext.UIThreadIdlePriority);
        }

        public override bool OnSynchronizeDropdowns(LanguageService languageService, IVsTextView textView, int line, int col, ArrayList dropDownTypes, ArrayList dropDownMembers, ref int selectedType, ref int selectedMember)
        {
            if (_hasBufferChanged || dropDownMembers.Count == 0)
            {
                dropDownMembers.Clear();

                if (_document.Result != null)
                {
                    List<KeyInfo> keys = BuildKeyHierarchy(_document.Result.Tokens);

                    foreach (KeyInfo key in keys)
                    {
                        AddKeyWithChildren(key, textView, dropDownMembers, 0);
                    }
                }
            }

            if (dropDownTypes.Count == 0)
            {
                var thisExt = $"{Vsix.Name} ({Vsix.Version})";
                var poweredBy = "   Powered by TAML Tokenizer";
                _ = dropDownTypes.Add(new DropDownMember(thisExt, new TextSpan(), 126, DROPDOWNFONTATTR.FONTATTR_GRAY));
                _ = dropDownTypes.Add(new DropDownMember(poweredBy, new TextSpan(), 126, DROPDOWNFONTATTR.FONTATTR_GRAY));
            }

            DropDownMember currentDropDown = dropDownMembers
                .OfType<DropDownMember>()
                .Where(d => d.Span.iStartLine <= line)
                .LastOrDefault();

            selectedMember = currentDropDown != null ? dropDownMembers.IndexOf(currentDropDown) : dropDownMembers.Count > 0 ? 0 : -1;
            selectedType = 0;
            _hasBufferChanged = false;

            return true;
        }

        /// <summary>
        /// Builds a hierarchical structure of keys from the flat token list.
        /// </summary>
        private static List<KeyInfo> BuildKeyHierarchy(IReadOnlyList<TamlToken> tokens)
        {
            List<KeyInfo> rootKeys = [];
            Stack<List<KeyInfo>> stack = new();
            stack.Push(rootKeys);

            for (var i = 0; i < tokens.Count; i++)
            {
                TamlToken token = tokens[i];

                if (token.Type == TamlTokenType.Key)
                {
                    var keyInfo = new KeyInfo
                    {
                        Name = token.Value,
                        StartPosition = token.Position,
                        EndPosition = FindKeyEndPosition(tokens, i),
                        Line = token.Line,
                        Column = token.Column,
                        HasChildren = HasChildKeys(tokens, i)
                    };

                    stack.Peek().Add(keyInfo);
                }
                else if (token.Type == TamlTokenType.Indent)
                {
                    // Get the last key at current level and push its children list
                    List<KeyInfo> currentLevel = stack.Peek();
                    if (currentLevel.Count > 0)
                    {
                        KeyInfo lastKey = currentLevel[currentLevel.Count - 1];
                        stack.Push(lastKey.Children);
                    }
                    else
                    {
                        // No key at this level, push an empty list to maintain depth
                        stack.Push([]);
                    }
                }
                else if (token.Type == TamlTokenType.Dedent)
                {
                    if (stack.Count > 1)
                    {
                        stack.Pop();
                    }
                }
            }

            return rootKeys;
        }

        /// <summary>
        /// Determines if a key has child keys (not just values).
        /// </summary>
        private static bool HasChildKeys(IReadOnlyList<TamlToken> tokens, int keyIndex)
        {
            var depth = 0;
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
                        return false;
                    }
                }
                else if (token.Type == TamlTokenType.Key)
                {
                    if (depth == 0)
                    {
                        return false;
                    }
                    else if (depth == 1 && foundIndent)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Finds the end position of a key's content (including all children).
        /// </summary>
        private static int FindKeyEndPosition(IReadOnlyList<TamlToken> tokens, int keyIndex)
        {
            var depth = 0;
            var lastPosition = tokens[keyIndex].EndPosition;
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
                        return foundIndent ? lastPosition : tokens[keyIndex].EndPosition;
                    }
                }
                else if (token.Type == TamlTokenType.Key && depth == 0)
                {
                    return tokens[keyIndex].EndPosition;
                }

                if (depth > 0 && token.IsValueToken && token.EndPosition > lastPosition)
                {
                    lastPosition = token.EndPosition;
                }
            }

            return foundIndent ? lastPosition : tokens[keyIndex].EndPosition;
        }

        /// <summary>
        /// Recursively adds a key and its children to the dropdown members list.
        /// </summary>
        private static void AddKeyWithChildren(KeyInfo key, IVsTextView textView, ArrayList dropDownMembers, int depth)
        {
            DropDownMember member = CreateDropDownMember(key, textView, depth);
            _ = dropDownMembers.Add(member);

            if (key.HasChildren)
            {
                foreach (KeyInfo child in key.Children)
                {
                    if (child.HasChildren)
                    {
                        AddKeyWithChildren(child, textView, dropDownMembers, depth + 1);
                    }
                }
            }
        }

        private static DropDownMember CreateDropDownMember(KeyInfo key, IVsTextView textView, int depth)
        {
            TextSpan textSpan = GetTextSpan(key, textView);
            var headingText = GetKeyName(key, depth, out DROPDOWNFONTATTR format);

            return new DropDownMember(headingText, textSpan, 126, format);
        }

        private static string GetKeyName(KeyInfo key, int depth, out DROPDOWNFONTATTR format)
        {
            format = depth == 0 ? DROPDOWNFONTATTR.FONTATTR_BOLD : DROPDOWNFONTATTR.FONTATTR_PLAIN;

            var indent = new string(' ', depth * 2);
            return indent + key.Name;
        }

        private static TextSpan GetTextSpan(KeyInfo key, IVsTextView textView)
        {
            TextSpan textSpan = new();

            var hrStart = textView.GetLineAndColumn(key.StartPosition, out textSpan.iStartLine, out textSpan.iStartIndex);
            var hrEnd = textView.GetLineAndColumn(key.EndPosition, out textSpan.iEndLine, out textSpan.iEndIndex);

            if (hrStart != 0 || hrEnd != 0)
            {
                return new TextSpan();
            }

            return textSpan;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _textView.Caret.PositionChanged -= CaretPositionChanged;
            _document.Parsed -= OnDocumentParsed;
        }

        /// <summary>
        /// Represents a key in the TAML document with its position and children.
        /// </summary>
        private sealed class KeyInfo
        {
            public string Name { get; set; }
            public int StartPosition { get; set; }
            public int EndPosition { get; set; }
            public int Line { get; set; }
            public int Column { get; set; }
            public bool HasChildren { get; set; }
            public List<KeyInfo> Children { get; } = [];
        }
    }
}
