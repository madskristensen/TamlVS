using Community.VisualStudio.Toolkit;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace TamlVS
{
    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class GeneralOptionsPage : BaseOptionPage<GeneralOptions> { }
    }

    /// <summary>
    /// General options for the TAML extension.
    /// </summary>
    public class GeneralOptions : BaseOptionModel<GeneralOptions>, IRatingConfig
    {
        // Validation settings
        [Category("Validation")]
        [DisplayName("Enable strict mode")]
        [Description("Report warnings for non-standard TAML syntax such as mixed indentation or spaces.")]
        [DefaultValue(false)]
        public bool StrictMode { get; set; } = false;

        // Formatting settings
        [Category("Formatting")]
        [DisplayName("Align values")]
        [Description("Align values at the same indentation level to the same column when formatting.")]
        [DefaultValue(true)]
        public bool AlignValues { get; set; } = true;

        [Category("Formatting")]
        [DisplayName("Trim trailing whitespace")]
        [Description("Remove trailing whitespace from lines when formatting.")]
        [DefaultValue(true)]
        public bool TrimTrailingWhitespace { get; set; } = true;

        [Category("Formatting")]
        [DisplayName("Ensure trailing newline")]
        [Description("Ensure the document ends with a single newline when formatting.")]
        [DefaultValue(true)]
        public bool EnsureTrailingNewline { get; set; } = true;

        [Category("Formatting")]
        [DisplayName("Preserve blank lines")]
        [Description("Keep blank lines in the document when formatting.")]
        [DefaultValue(true)]
        public bool PreserveBlankLines { get; set; } = true;

        [Category("Formatting")]
        [DisplayName("Tab size")]
        [Description("The visual width of a tab character for alignment calculations (2-8).")]
        [DefaultValue(4)]
        [TypeConverter(typeof(TabSizeConverter))]
        public int TabSize { get; set; } = 4;

        // IRatingConfig implementation
        [Browsable(false)]
        public int RatingRequests { get; set; }
    }

    /// <summary>
    /// Converts tab size values and constrains them to valid range.
    /// </summary>
    public class TabSizeConverter : Int32Converter
    {
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            var result = (int)base.ConvertFrom(context, culture, value);
            return Math.Max(2, Math.Min(8, result));
        }
    }
}
