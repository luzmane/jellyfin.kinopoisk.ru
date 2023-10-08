using Jellyfin.Plugin.KinopoiskRu.Provider.ExternalId;

using FluentAssertions;

using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.KinopoiskRu.Tests.Common;

public class PersonExternalIdProviderTest
{
    private readonly NLog.ILogger _logger = NLog.LogManager.GetLogger(nameof(PersonExternalIdProviderTest));

    [Fact]
    public void PersonExternalIdProvider_ForCodeCoverage()
    {
        _logger.Info($"Start '{nameof(PersonExternalIdProvider_ForCodeCoverage)}'");

        PersonExternalIdProvider personExternalIdProvider = new();

        personExternalIdProvider.ProviderName.Should().Be(Plugin.PluginName, "has value of Plugin.PluginName");
        personExternalIdProvider.Key.Should().Be(Plugin.PluginKey, "has value of Plugin.PluginKey");
        personExternalIdProvider.UrlFormatString.Should().Be("https://www.kinopoisk.ru/name/{0}/", "this is constant");

        personExternalIdProvider.Supports(new Person()).Should().BeTrue("this provider supports only Person");
        personExternalIdProvider.Supports(new Series()).Should().BeFalse("this provider supports only Person");

        _logger.Info($"Finished '{nameof(PersonExternalIdProvider_ForCodeCoverage)}'");
    }

}
