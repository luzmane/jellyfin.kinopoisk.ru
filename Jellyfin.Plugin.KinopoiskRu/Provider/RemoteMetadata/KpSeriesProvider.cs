using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.KinopoiskRu.Provider.RemoteMetadata;

/// <inheritdoc/>
public class KpSeriesProvider : IRemoteMetadataProvider<Series, SeriesInfo>
{
    private readonly ILogger<KpSeriesProvider> _logger;
    private readonly Plugin _pluginInstance;

    /// <summary>
    /// Initializes a new instance of the <see cref="KpSeriesProvider"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger&lt;KpSeriesProvider&gt;"/> interface.</param>
    public KpSeriesProvider(ILogger<KpSeriesProvider> logger)
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
    public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
    {
        if (info == null)
        {
            _logger.LogError("SeriesInfo is null");
            return new MetadataResult<Series>() { HasMetadata = false };
        }

        _logger.LogInformation("GetMetadata by SeriesInfo:'{InfoName}', '{InfoYear}'", info.Name, info.Year);
        return await _pluginInstance.GetKinopoiskService().GetMetadata(info, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
    {
        if (searchInfo == null)
        {
            _logger.LogError("SeriesInfo is null");
            return Array.Empty<RemoteSearchResult>();
        }

        _logger.LogInformation("GetSearchResults by SeriesInfo:'{SearchInfoName}', '{SearchInfoYear}'", searchInfo.Name, searchInfo.Year);
        return await _pluginInstance.GetKinopoiskService().GetSearchResults(searchInfo, cancellationToken).ConfigureAwait(false);
    }
}
