using System;
using System.IO;
using System.Text.Json;

namespace StarsAICopilot.Avalonia.Helper;

public static class ConfigHelper
{
    private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "appdata", "config.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    static ConfigHelper()
    {
        LoadConfig();
    }

    public static ApiConfig CurrentConfig { get; private set; } = new();

    private static void LoadConfig()
    {
        try
        {
            var configDir = Path.GetDirectoryName(ConfigPath);

            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir!);
                SaveConfig();
                return;
            }

            if (!File.Exists(ConfigPath) || new FileInfo(ConfigPath).Length == 0)
            {
                SaveConfig();
                return;
            }

            var json = File.ReadAllText(ConfigPath);

            if (string.IsNullOrWhiteSpace(json))
            {
                SaveConfig();
                return;
            }

            var loadedConfig = JsonSerializer.Deserialize<ApiConfig>(json, Options);
            if (loadedConfig != null) CurrentConfig = loadedConfig;
        }
        catch (Exception ex)
        {

        }
    }

    public static void SaveConfig()
    {
        var json = JsonSerializer.Serialize(CurrentConfig, Options);
        File.WriteAllText(ConfigPath, json);
    }

    public class ApiConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ApiUrl { get; set; } = string.Empty;
        public string Mod { get; set; } = string.Empty;
        public string Theme { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string IsWelcome { get; set; } = string.Empty;
    }
}