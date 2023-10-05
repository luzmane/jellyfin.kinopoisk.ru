using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.KinopoiskRu.Provider.RemoteMetadata;

/// <inheritdoc/>
public class KpPersonProvider : IRemoteMetadataProvider<Person, PersonLookupInfo>
{
    private readonly ILogger<KpPersonProvider> _logger;
    private readonly Plugin _pluginInstance;

    /// <summary>
    /// Initializes a new instance of the <see cref="KpPersonProvider"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger&lt;KpPersonProvider&gt;"/> interface.</param>
    public KpPersonProvider(ILogger<KpPersonProvider> logger)
    {
        _logger = logger;
        _pluginInstance = Plugin.Instance!;
    }

    /// <inheritdoc/>
    public string Name => Plugin.PluginName;

    /// <inheritdoc/>
    public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await _pluginInstance.HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<MetadataResult<Person>> GetMetadata(PersonLookupInfo info, CancellationToken cancellationToken)
    {
        if (info == null)
        {
            _logger.LogError("PersonLookupInfo is null");
            return new MetadataResult<Person>() { HasMetadata = false };
        }

        _logger.LogInformation("GetMetadata by PersonLookupInfo:'{InfoName}', '{InfoYear}'", info.Name, info.Year);
        return await _pluginInstance.GetKinopoiskService().GetMetadata(info, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(PersonLookupInfo searchInfo, CancellationToken cancellationToken)
    {
        if (searchInfo == null)
        {
            _logger.LogError("PersonLookupInfo is null");
            return Array.Empty<RemoteSearchResult>();
        }

        _logger.LogInformation("GetSearchResults by PersonLookupInfo:'{SearchInfoName}', '{SearchInfoYear}'", searchInfo.Name, searchInfo.Year);
        return await _pluginInstance.GetKinopoiskService().GetSearchResults(searchInfo, cancellationToken).ConfigureAwait(false);
    }
}
