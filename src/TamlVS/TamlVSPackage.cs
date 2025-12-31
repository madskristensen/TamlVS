global using System;
global using System.Runtime.InteropServices;
global using System.Threading;
global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using Microsoft.VisualStudio.Shell.Interop;
global using TamlVS.Commands;
global using Task = System.Threading.Tasks.Task;
using System.ComponentModel.Design;
using Microsoft.VisualStudio;

namespace TamlVS
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.TamlVSString)]

    [ProvideOptionPage(typeof(OptionsProvider.GeneralOptionsPage), Constants.LanguageName, "General", 0, 0, true, SupportsProfiles = true)]

    [ProvideLanguageService(typeof(TamlLanguage), Constants.LanguageName, 0, ShowHotURLs = true, DefaultToNonHotURLs = false, EnableLineNumbers = true, EnableAsyncCompletion = true, ShowCompletion = false, ShowDropDownOptions = true, MatchBraces = true, MatchBracesAtCaret = true)]
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

            await FormatDocumentHandler.RegisterAsync();
        }
    }
}