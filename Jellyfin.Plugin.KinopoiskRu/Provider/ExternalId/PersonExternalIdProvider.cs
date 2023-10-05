using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.KinopoiskRu.Provider.ExternalId;

/// <summary>
/// Add link on kinopoisk page to metadate of the Person.
/// </summary>
public class PersonExternalIdProvider : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => Plugin.PluginName;

    /// <inheritdoc />
    public string Key => Plugin.PluginKey;

    /// <inheritdoc />
    public ExternalIdMediaType? Type => ExternalIdMediaType.Person;

    /// <inheritdoc />
    public string? UrlFormatString => "https://www.kinopoisk.ru/name/{0}/";

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item)
    {
        return item is Person;
    }
}
