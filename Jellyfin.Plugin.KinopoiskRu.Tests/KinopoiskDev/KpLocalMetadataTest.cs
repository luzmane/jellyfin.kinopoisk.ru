using FluentAssertions;

using Jellyfin.Plugin.KinopoiskRu.Provider.LocalMetadata;
using Jellyfin.Plugin.KinopoiskRu.Tests.Utils;

using MediaBrowser.Controller.Entities.TV;

using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.KinopoiskRu.Tests;

[Collection("Sequential")]
public class KpLocalMetadataTest : BaseTest
{
    private static readonly NLog.ILogger Logger = NLog.LogManager.GetCurrentClassLogger();

    [Fact]
    public async void KpSeriesLocalMetadata_WithKpInName()
    {
        Logger.Info($"Start '{nameof(KpSeriesLocalMetadata_WithKpInName)}'");

        var itemInfo = new ItemInfo(new Series()
        {
            Path = "/jellyfin/movie_library/kp326_Побег из Шоушенка.mkv",
            Container = "mkv",
            IsInMixedFolder = false,
            Id = new Guid(),
            Name = "Побег из Шоушенка"
        });
        var seriesLocalMetadata = new KpSeriesLocalMetadata(new JellyfinLogger<KpSeriesLocalMetadata>());
        using var cancellationTokenSource = new CancellationTokenSource();
        MetadataResult<Series> result = await seriesLocalMetadata.GetMetadata(itemInfo, _directoryService.Object, cancellationTokenSource.Token);

        seriesLocalMetadata.Name.Should().Be(Plugin.PluginName, "the name is hardcoded");

        result.HasMetadata.Should().BeTrue("that mean the item was found");
        result.Item.Should().NotBeNull("that mean the item was found");
        result.Item.ProviderIds.TryGetValue(Plugin.PluginKey, out var providerId);

        providerId.Should().NotBeNull("that mean the item was found");
        providerId.Should().Be("326", "id of the requested item");

        VerifyNoOtherCalls();

        Logger.Info($"Finish '{nameof(KpSeriesLocalMetadata_WithKpInName)}'");
    }
}
