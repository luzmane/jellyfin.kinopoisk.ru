using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.KinopoiskRu.Provider.ExternalId;

/// <summary>
/// Add link on kinopoisk page to metadate of the Movie.
/// </summary>
public class MovieExternalIdProvider : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => Plugin.PluginName;

    /// <inheritdoc />
    public string Key => Plugin.PluginKey;

    /// <inheritdoc />
    public ExternalIdMediaType? Type => ExternalIdMediaType.Movie;

    /// <inheritdoc />
    public string? UrlFormatString => "https://www.kinopoisk.ru/film/{0}/";

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item)
    {
        return item is Movie;
    }
}
