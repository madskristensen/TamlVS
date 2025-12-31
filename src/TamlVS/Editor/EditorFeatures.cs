using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using TamlTokenizer;

namespace TamlVS
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IClassificationTag))]
    [ContentType(Constants.LanguageName)]
    public class SyntaxHighlighting : TokenClassificationTaggerBase
    {
        public override Dictionary<object, string> ClassificationMap { get; } = new Dictionary<object, string>
        {
            { TamlTokenType.Key, PredefinedClassificationTypeNames.SymbolDefinition },
            { TamlTokenType.Value, PredefinedClassificationTypeNames.String },
            { TamlTokenType.Tab, PredefinedClassificationTypeNames.WhiteSpace },
            { TamlTokenType.Newline, PredefinedClassificationTypeNames.WhiteSpace },
            { TamlTokenType.Null, PredefinedClassificationTypeNames.MarkupAttribute },
            { TamlTokenType.Boolean, PredefinedClassificationTypeNames.MarkupAttribute },
            { TamlTokenType.Number, PredefinedClassificationTypeNames.Number },
            { TamlTokenType.EmptyString, PredefinedClassificationTypeNames.String },
            { TamlTokenType.Comment, PredefinedClassificationTypeNames.Comment },
            { TamlTokenType.Indent, PredefinedClassificationTypeNames.WhiteSpace },
            { TamlTokenType.Dedent, PredefinedClassificationTypeNames.WhiteSpace },
            { TamlTokenType.Whitespace, PredefinedClassificationTypeNames.WhiteSpace },
            { TamlTokenType.EndOfFile, PredefinedClassificationTypeNames.WhiteSpace },
            { TamlTokenType.Invalid, PredefinedClassificationTypeNames.ExcludedCode },
        };
    }

    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IStructureTag))]
    [ContentType(Constants.LanguageName)]
    public class Outlining : TokenOutliningTaggerBase
    { }

    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IErrorTag))]
    [ContentType(Constants.LanguageName)]
    public class ErrorSquigglies : TokenErrorTaggerBase
    { }

    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [ContentType(Constants.LanguageName)]
    internal sealed class Tooltips : TokenQuickInfoBase
    { }

    //[Export(typeof(IAsyncCompletionCommitManagerProvider))]
    //[ContentType(Constants.LanguageName)]
    //internal sealed class CompletionCommitManager : CompletionCommitManagerBase
    //{
    //    public override IEnumerable<char> CommitChars => ['\t', '\n'];
    //}

    //[Export(typeof(IViewTaggerProvider))]
    //[TagType(typeof(TextMarkerTag))]
    //[ContentType(Constants.LanguageName)]
    //internal sealed class BraceMatchingTaggerProvider : BraceMatchingBase
    //{
    //}

    [Export(typeof(IViewTaggerProvider))]
    [ContentType(Constants.LanguageName)]
    [TagType(typeof(TextMarkerTag))]
    public class SameWordHighlighter : SameWordHighlighterBase
    { }

    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType(Constants.LanguageName)]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    public class UserRatings : WpfTextViewCreationListener
    {
        private DateTime _openedDate;
        private RatingPrompt _rating;

        protected override void Created(DocumentView docView)
        {
            _openedDate = DateTime.Now;
            _rating = new RatingPrompt(Constants.MarketplaceId, Vsix.Name, GeneralOptions.Instance, 5);
        }

        protected override void Closed(IWpfTextView textView)
        {
            if (_openedDate.AddMinutes(2) < DateTime.Now)
            {
                _rating.RegisterSuccessfulUsage();
            }
        }
    }

}
