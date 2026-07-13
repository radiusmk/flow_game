using System.IO;
using System.Text.Json;
using FlowGame.Core;

namespace FlowGame.Wpf;

public sealed class SettingsStore
{
    private readonly string _settingsPath;

    public SettingsStore()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FlowGame");
        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "settings.json");
    }

    public GameSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return new GameSettings();
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            return Normalize(JsonSerializer.Deserialize<GameSettings>(json) ?? new GameSettings());
        }
        catch
        {
            return new GameSettings();
        }
    }

    public void Save(GameSettings settings)
    {
        settings.SelectedStartLevel = Math.Max(1, settings.SelectedStartLevel);
        settings.LastUnlockedLevel = Math.Max(1, settings.LastUnlockedLevel);
        settings.HintCredits = Math.Max(0, settings.HintCredits);
        settings.NextHintCreditScore = Math.Max(2500, settings.NextHintCreditScore);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsPath, json);
    }

    private static GameSettings Normalize(GameSettings settings)
    {
        settings.SelectedStartLevel = Math.Max(1, settings.SelectedStartLevel);
        settings.LastUnlockedLevel = Math.Max(1, settings.LastUnlockedLevel);

        if (settings.HintCredits == 0 && settings.NextHintCreditScore == 0 && settings.HintCreditsEarned == 0)
        {
            settings.HintCredits = 3;
            settings.NextHintCreditScore = 2500;
        }

        settings.NextHintCreditScore = Math.Max(2500, settings.NextHintCreditScore);
        return settings;
    }
}
