using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

namespace TamlVS
{
    [ComVisible(true)]
    [Guid(TamlLanguage.LanguageGuidString)]
    internal sealed class TamlLanguage(object site) : LanguageBase(site)
    {
        public const string LanguageGuidString = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";

        private DropdownBars _dropdownBars;

        public override string Name => Constants.LanguageName;

        public override string[] FileExtensions { get; } = [Constants.FileExtension];

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
            preferences.ShowNavigationBar = true;

            preferences.WordWrap = false;
            preferences.WordWrapGlyphs = false;

            preferences.AutoListMembers = false;
            preferences.EnableQuickInfo = true;
            preferences.ParameterInformation = false;
        }

        public override TypeAndMemberDropdownBars CreateDropDownHelper(IVsTextView textView)
        {
            _dropdownBars?.Dispose();
            _dropdownBars = new DropdownBars(textView, this);

            return _dropdownBars;
        }

        public override void Dispose()
        {
            _dropdownBars?.Dispose();
            _dropdownBars = null;
            base.Dispose();
        }
    }
}
