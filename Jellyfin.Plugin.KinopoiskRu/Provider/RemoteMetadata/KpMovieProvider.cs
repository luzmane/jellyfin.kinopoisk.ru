using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.KinopoiskRu.Provider.RemoteMetadata;

/// <inheritdoc/>
public class KpMovieProvider : IRemoteMetadataProvider<Movie, MovieInfo>
{
    private readonly ILogger<KpMovieProvider> _logger;
    private readonly Plugin _pluginInstance;

    /// <summary>
    /// Initializes a new instance of the <see cref="KpMovieProvider"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger&lt;KpMovieProvider&gt;"/> interface.</param>
    public KpMovieProvider(ILogger<KpMovieProvider> logger)
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
    public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
    {
        if (info == null)
        {
            _logger.LogError("MovieInfo is null");
            return new MetadataResult<Movie>() { HasMetadata = false };
        }

        _logger.LogInformation("GetMetadata by MovieInfo:'{InfoName}', '{InfoYear}'", info.Name, info.Year);
        return await _pluginInstance.GetKinopoiskService().GetMetadata(info, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
    {
        if (searchInfo == null)
        {
            _logger.LogError("MovieInfo is null");
            return Array.Empty<RemoteSearchResult>();
        }

        _logger.LogInformation("GetSearchResults by MovieInfo:'{SearchInfoName}', '{SearchInfoYear}'", searchInfo.Name, searchInfo.Year);
        return await _pluginInstance.GetKinopoiskService().GetSearchResults(searchInfo, cancellationToken).ConfigureAwait(false);
    }
}
