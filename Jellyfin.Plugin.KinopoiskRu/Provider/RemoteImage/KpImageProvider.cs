using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.KinopoiskRu.Provider.RemoteImage;

/// <inheritdoc/>
public class KpImageProvider : IRemoteImageProvider
{
    private readonly Plugin _pluginInstance;

    /// <summary>
    /// Initializes a new instance of the <see cref="KpImageProvider"/> class.
    /// </summary>
    public KpImageProvider()
    {
        _pluginInstance = Plugin.Instance!;
    }

    /// <inheritdoc/>
    public string Name => Plugin.PluginName;

    /// <inheritdoc/>
    public bool Supports(BaseItem item)
    {
        return item is Movie || item is Series;
    }

    /// <inheritdoc/>
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        return new List<ImageType>
            {
                ImageType.Primary,
                ImageType.Backdrop,
                ImageType.Logo,
            };
    }

    /// <inheritdoc/>
    public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await _pluginInstance.HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        return await _pluginInstance.GetKinopoiskService().GetImages(item, cancellationToken).ConfigureAwait(false);
    }
}
