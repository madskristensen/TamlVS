namespace TamlVS.Commands
{
    [Command(PackageIds.OpenSettings)]
    internal sealed class OpenSettingsCommand : BaseCommand<OpenSettingsCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await VS.Settings.OpenAsync<OptionsProvider.GeneralOptionsPage>();
        }
    }
}
