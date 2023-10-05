using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.KinopoiskRu.Provider.LocalMetadata;

/// <inheritdoc/>
public class KpSeriesLocalMetadata : KpBaseLocalMetadata<KpSeriesLocalMetadata, Series>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KpSeriesLocalMetadata"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger&lt;KpSeriesLocalMetadata&gt;"/> interface.</param>
    public KpSeriesLocalMetadata(ILogger<KpSeriesLocalMetadata> logger) : base(logger)
    {
    }
}
