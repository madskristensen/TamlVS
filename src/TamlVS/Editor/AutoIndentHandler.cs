using System.ComponentModel.Composition;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace TamlVS.Editor
{
    [Export(typeof(ICommandHandler))]
    [Name(nameof(AutoIndentHandler))]
    [ContentType(Constants.LanguageName)]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    internal class AutoIndentHandler : ICommandHandler<ReturnKeyCommandArgs>
    {
        public string DisplayName => "TAML Auto-Indent Handler";

        public bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext executionContext)
        {
            ITextView textView = args.TextView;
            ITextBuffer buffer = args.SubjectBuffer;

            // Get the current line
            SnapshotPoint caretPosition = textView.Caret.Position.BufferPosition;
            ITextSnapshotLine currentLine = caretPosition.GetContainingLine();
            string lineText = currentLine.GetText();

            // Count leading tabs on current line
            var leadingTabs = 0;
            foreach (char c in lineText)
            {
                if (c == '\t')
                {
                    leadingTabs++;
                }
                else
                {
                    break;
                }
            }

            // If line is empty or whitespace only, no indentation
            if (string.IsNullOrWhiteSpace(lineText))
            {
                leadingTabs = 0;
            }

            // Build the new line text with indentation
            string indent = new string('\t', leadingTabs);
            string newLineText = Environment.NewLine + indent;

            // Insert the new line with indentation
            using (ITextEdit edit = buffer.CreateEdit())
            {
                edit.Insert(caretPosition.Position, newLineText);
                edit.Apply();
            }

            // Move caret to end of inserted text (after the indentation)
            SnapshotPoint newCaretPosition = new(buffer.CurrentSnapshot, caretPosition.Position + newLineText.Length);
            textView.Caret.MoveTo(newCaretPosition);

            return true; // Command handled
        }

        public CommandState GetCommandState(ReturnKeyCommandArgs args)
        {
            return CommandState.Available;
        }
    }
}
