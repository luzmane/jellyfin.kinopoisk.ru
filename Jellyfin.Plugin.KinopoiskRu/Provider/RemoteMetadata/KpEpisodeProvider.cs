using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.KinopoiskRu.Provider.RemoteMetadata;

/// <inheritdoc/>
public class KpEpisodeProvider : IRemoteMetadataProvider<Episode, EpisodeInfo>
{
    private readonly ILogger<KpEpisodeProvider> _logger;
    private readonly Plugin _pluginInstance;

    /// <summary>
    /// Initializes a new instance of the <see cref="KpEpisodeProvider"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger&lt;KpEpisodeProvider&gt;"/> interface.</param>
    public KpEpisodeProvider(ILogger<KpEpisodeProvider> logger)
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
    public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
    {
        if (info == null)
        {
            _logger.LogError("EpisodeInfo is null");
            return new MetadataResult<Episode>() { HasMetadata = false };
        }

        _logger.LogInformation("GetMetadata by EpisodeInfo:'{InfoName}', '{InfoYear}'", info.Name, info.Year);
        return await _pluginInstance.GetKinopoiskService().GetMetadata(info, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
    {
        if (searchInfo == null)
        {
            _logger.LogError("EpisodeInfo is null");
            throw new NotImplementedException();
        }

        _logger.LogInformation("GetSearchResults by EpisodeInfo:'{SearchInfoName}', '{SearchInfoYear}'", searchInfo.Name, searchInfo.Year);
        throw new NotImplementedException();
    }
}
