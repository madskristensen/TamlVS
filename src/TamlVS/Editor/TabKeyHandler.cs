using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace TamlVS.Editor
{
    /// <summary>
    /// Handles Tab key presses in TAML files to insert actual tab characters
    /// instead of spaces (which is Visual Studio's default behavior).
    /// </summary>
    [Export(typeof(ICommandHandler))]
    [Name(nameof(TabKeyHandler))]
    [ContentType(Constants.LanguageName)]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    internal class TabKeyHandler : ICommandHandler<TabKeyCommandArgs>
    {
        public string DisplayName => "TAML Tab Key Handler";

        public bool ExecuteCommand(TabKeyCommandArgs args, CommandExecutionContext executionContext)
        {
            ITextView textView = args.TextView;
            ITextBuffer buffer = args.SubjectBuffer;

            // If there's a selection, replace it with a tab; otherwise insert at caret
            SnapshotSpan selection = textView.Selection.SelectedSpans.Count > 0
                ? textView.Selection.SelectedSpans[0]
                : new SnapshotSpan(textView.Caret.Position.BufferPosition, 0);

            using (ITextEdit edit = buffer.CreateEdit())
            {
                edit.Replace(selection, "\t");
                edit.Apply();
            }

            // Move caret after the inserted tab
            SnapshotPoint newCaretPosition = new(buffer.CurrentSnapshot, selection.Start.Position + 1);
            textView.Caret.MoveTo(newCaretPosition);
            textView.Selection.Clear();

            return true;
        }

        public CommandState GetCommandState(TabKeyCommandArgs args)
        {
            return CommandState.Available;
        }
    }
}
