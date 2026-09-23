using System;
using System.IO;
using System.Text.Json;

namespace PoolControl.Helper;

/// <summary>Display preferences only; deliberately separate from hardware configuration.</summary>
public sealed class UiPreferences
{
    public string Surface { get; set; } = "Modern";
    public string Language { get; set; } = "de";
    public string Theme { get; set; } = "System";

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PoolControl", "ui.json");

    public static UiPreferences Load(string? path = null)
    {
        try
        {
            var settings = JsonSerializer.Deserialize<UiPreferences>(File.ReadAllText(path ?? DefaultPath)) ?? new();
            settings.Normalize();
            return settings;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new();
        }
    }

    public void Normalize()
    {
        Surface = Surface == "Classic" ? "Classic" : "Modern";
        Language = Language == "en" ? "en" : "de";
        Theme = Theme is "Light" or "Dark" ? Theme : "System";
    }

    public void Save(string? path = null)
    {
        Normalize();
        var destination = Path.GetFullPath(path ?? DefaultPath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(this));
            File.Move(temporary, destination, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
