using System.Text.Json;
using DiceRoller.Models;
using Microsoft.JSInterop;

namespace DiceRoller.Services;

/// <summary>Persists named attack setups in the browser's localStorage.</summary>
public class PresetService(IJSRuntime js)
{
    private const string StorageKey = "diceroller.presets";

    public async Task<Dictionary<string, List<AttackRow>>> LoadAllAsync()
    {
        var json = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        if (string.IsNullOrEmpty(json))
        {
            return [];
        }
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, List<AttackRow>>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public async Task SaveAllAsync(Dictionary<string, List<AttackRow>> presets) =>
        await js.InvokeVoidAsync("localStorage.setItem", StorageKey, JsonSerializer.Serialize(presets));

    /// <summary>Deep copy so live edits never mutate a stored preset (and vice versa).</summary>
    public static List<AttackRow> DeepCopy(List<AttackRow> rows) =>
        JsonSerializer.Deserialize<List<AttackRow>>(JsonSerializer.Serialize(rows))!;
}
