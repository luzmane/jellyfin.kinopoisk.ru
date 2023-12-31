using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.KinopoiskRu.Helper;

internal static class ProviderIdExtensions
{
    public static string? GetSeriesProviderId(this EpisodeInfo instance, string name)
    {
        if (instance.SeriesProviderIds == null)
        {
            return null;
        }

        _ = instance.SeriesProviderIds.TryGetValue(name, out var value);
        return value;
    }
}
