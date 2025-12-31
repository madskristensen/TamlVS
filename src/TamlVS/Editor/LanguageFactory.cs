using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

using System.Runtime.InteropServices;

namespace TamlVS
{
    [ComVisible(true)]
    [Guid(TamlLanguage.LanguageGuidString)]
    internal sealed class TamlLanguage : LanguageBase
    {
        public const string LanguageGuidString = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";

        public TamlLanguage(object site) : base(site)
        {
        }

        public override string Name => Constants.LanguageName;

        public override string[] FileExtensions { get; } = new[] { Constants.FileExtension };

        public override void SetDefaultPreferences(LanguagePreferences preferences)
        {
            preferences.EnableCodeSense = false;
            preferences.EnableMatchBraces = true;
            preferences.EnableMatchBracesAtCaret = true;
            preferences.EnableShowMatchingBrace = true;
            preferences.EnableCommenting = true;
            preferences.HighlightMatchingBraceFlags = _HighlightMatchingBraceFlags.HMB_USERECTANGLEBRACES;
            preferences.LineNumbers = true;
            preferences.MaxErrorMessages = 100;
            preferences.AutoOutlining = true;
            preferences.MaxRegionTime = 2000;
            preferences.InsertTabs = true;
            preferences.IndentSize = 1;
            preferences.IndentStyle = IndentingStyle.Smart;
            preferences.ShowNavigationBar = false;

            preferences.WordWrap = false;
            preferences.WordWrapGlyphs = false;

            preferences.AutoListMembers = false;
            preferences.EnableQuickInfo = true;
            preferences.ParameterInformation = false;
        }
    }
}
