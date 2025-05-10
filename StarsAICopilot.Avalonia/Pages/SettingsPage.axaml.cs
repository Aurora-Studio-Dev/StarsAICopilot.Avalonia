using Avalonia.Controls;
using Avalonia.Interactivity;
using StarsAICopilot.Avalonia.Helper;

namespace StarsAICopilot.Avalonia.Pages;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent(); 
        LoadSettings();
    }

    private void SaveConfig_API_OnClick(object sender, RoutedEventArgs e)
    {
        ConfigHelper.CurrentConfig.ApiKey = ApiKey.Text;
        ConfigHelper.CurrentConfig.ApiUrl = ApiUrl.Text;
        ConfigHelper.CurrentConfig.Mod = Mod.Text;
        ConfigHelper.SaveConfig();
    }

    private void LoadSettings()
    {
        ApiUrl.Text = ConfigHelper.CurrentConfig.ApiUrl;
        ApiKey.Text = ConfigHelper.CurrentConfig.ApiKey;
        Mod.Text = ConfigHelper.CurrentConfig.Mod;
    }
}
