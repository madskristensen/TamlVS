global using Community.VisualStudio.Toolkit;

global using Microsoft.VisualStudio.Shell;

global using System;

global using Task = System.Threading.Tasks.Task;

using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace TamlVS
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.TamlVSString)]

    [ProvideLanguageService(typeof(TamlLanguage), Constants.LanguageName, 0, ShowHotURLs = false, DefaultToNonHotURLs = true, EnableLineNumbers = true, EnableAsyncCompletion = true, ShowCompletion = false, ShowDropDownOptions = false, MatchBraces = true, MatchBracesAtCaret = true)]
    [ProvideLanguageExtension(typeof(TamlLanguage), Constants.FileExtension)]

    [ProvideEditorFactory(typeof(TamlLanguage), 0, false, CommonPhysicalViewAttributes = (int)__VSPHYSICALVIEWATTRIBUTES.PVA_SupportsPreview, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
    [ProvideEditorLogicalView(typeof(TamlLanguage), VSConstants.LOGVIEWID.TextView_string, IsTrusted = true)]
    [ProvideEditorExtension(typeof(TamlLanguage), Constants.FileExtension, 1000)]

    [ProvideFileIcon(Constants.FileExtension, "KnownMonikers.Settings")]
    [ProvideBindingPath()]
    public sealed class TamlVSPackage : ToolkitPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            TamlLanguage language = new(this);
            RegisterEditorFactory(language);
            ((IServiceContainer)this).AddService(typeof(TamlLanguage), language, true);

            await this.RegisterCommandsAsync();
        }
    }
}