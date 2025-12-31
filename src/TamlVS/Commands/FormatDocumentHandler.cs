using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

using System.ComponentModel.Composition;

using TamlTokenizer;

namespace TamlVS.Commands
{
    [Export(typeof(ICommandHandler))]
    [ContentType(Constants.LanguageName)]
    [Name(nameof(FormatDocumentHandler))]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    internal class FormatDocumentHandler : ICommandHandler<FormatDocumentCommandArgs>
    {
        public string DisplayName => nameof(FormatDocumentHandler);

        public CommandState GetCommandState(FormatDocumentCommandArgs args)
        {
            return CommandState.Available;
        }

        public bool ExecuteCommand(FormatDocumentCommandArgs args, CommandExecutionContext executionContext)
        {
            ITextView textView = args.TextView;
            ITextBuffer buffer = args.SubjectBuffer;

            string originalText = buffer.CurrentSnapshot.GetText();

            if (string.IsNullOrEmpty(originalText))
            {
                return true;
            }

            try
            {
                string formattedText = Taml.Format(originalText);

                // Only update if there are changes
                if (formattedText != originalText)
                {
                    using (ITextEdit edit = buffer.CreateEdit())
                    {
                        edit.Replace(new Span(0, originalText.Length), formattedText);
                        edit.Apply();
                    }
                }

                return true;
            }
            catch (Exception)
            {
                // If formatting fails, don't make any changes
                return false;
            }
        }
    }
}
