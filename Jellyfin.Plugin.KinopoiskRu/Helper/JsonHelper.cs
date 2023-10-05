using System.Text.Json;

namespace Jellyfin.Plugin.KinopoiskRu.Helper;

/// <summary>
/// Helper methods for API.
/// </summary>
internal static class JsonHelper
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, JsonSerializerOptions);
    }
}
