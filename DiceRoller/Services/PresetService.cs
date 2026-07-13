using System.Text.Json;
using DiceRoller.Models;
using Microsoft.JSInterop;

namespace DiceRoller.Services;

/// <summary>Persists named attack setups in the browser's localStorage.</summary>
public class PresetService(IJSRuntime js)
{
    private const string StorageKey = "diceroller.presets";

    public async Task<Dictionary<string, List<AttackGroup>>> LoadAllAsync()
    {
        var json = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        if (string.IsNullOrEmpty(json))
        {
            return [];
        }
        try
        {
            var result = new Dictionary<string, List<AttackGroup>>();
            using var doc = JsonDocument.Parse(json);
            foreach (var preset in doc.RootElement.EnumerateObject())
            {
                result[preset.Name] = ParsePreset(preset.Name, preset.Value);
            }
            return result;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    // Presets saved before groups existed hold a flat list of attack rows;
    // wrap those in a single group named after the preset.
    private static List<AttackGroup> ParsePreset(string name, JsonElement value)
    {
        bool isLegacy = value.ValueKind == JsonValueKind.Array
            && value.GetArrayLength() > 0
            && !value[0].TryGetProperty("Attacks", out _);
        if (isLegacy)
        {
            var rows = value.Deserialize<List<AttackRow>>() ?? [];
            return [new AttackGroup { Name = name, Attacks = rows }];
        }
        return value.Deserialize<List<AttackGroup>>() ?? [];
    }

    public async Task SaveAllAsync(Dictionary<string, List<AttackGroup>> presets) =>
        await js.InvokeVoidAsync("localStorage.setItem", StorageKey, JsonSerializer.Serialize(presets));

    /// <summary>Deep copy so live edits never mutate a stored preset (and vice versa).</summary>
    public static List<AttackGroup> DeepCopy(List<AttackGroup> groups) =>
        JsonSerializer.Deserialize<List<AttackGroup>>(JsonSerializer.Serialize(groups))!;
}
