using System.Text.RegularExpressions;

using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.KinopoiskRu.Provider.LocalMetadata;

/// <inheritdoc/>
public abstract class KpBaseLocalMetadata<TProvider, T> : ILocalMetadataProvider<T>
    where T : BaseItem, IHasProviderIds, new()
{
    private static readonly Regex KinopoiskIdRegex = new("kp-?(?<kinopoiskId>\\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly ILogger<TProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KpBaseLocalMetadata&lt;TProvider, T&gt;"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger&lt;TProvider&gt;"/> interface.</param>
    protected KpBaseLocalMetadata(ILogger<TProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public string Name => Plugin.PluginName;

    /// <inheritdoc/>
    public virtual Task<MetadataResult<T>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
    {
        if (info == null)
        {
            throw new ArgumentNullException(nameof(info));
        }

        _logger.LogInformation("GetMetadata by ItemInfo: '{InfoPath}'", info.Path);
        var result = new MetadataResult<T>();

        if (!string.IsNullOrEmpty(info.Path))
        {
            var match = KinopoiskIdRegex.Match(info.Path);
            if (match.Success && int.TryParse(match.Groups["kinopoiskId"].Value, out var kinopoiskId))
            {
                _logger.LogInformation("Detected kinopoisk id '{KinopoiskId}' for file '{InfoPath}'", kinopoiskId, info.Path);
                var item = new T();
                item.SetProviderId(Plugin.PluginKey, match.Groups["kinopoiskId"].Value);

                result.Item = item;
                result.HasMetadata = true;
            }
        }

        return Task.FromResult(result);
    }
}
