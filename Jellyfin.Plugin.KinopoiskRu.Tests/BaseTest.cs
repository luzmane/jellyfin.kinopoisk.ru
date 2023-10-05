
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.KinopoiskRu.Tests;

public abstract class BaseTest
{
    private const string KINOPOISK_DEV_TOKEN = "8DA0EV2-KTP4A5Q-G67QP3K-S2VFBX7";
    private const string KINOPOISK_UNOFFICIAL_TOKEN = "0f162131-81c1-4979-b46c-3eea4263fb11";

    private readonly NLog.ILogger _logger;

    #region Mock
    protected readonly Mock<IDirectoryService> _directoryService = new();

    #endregion

    #region Not Mock

    #endregion

    protected void VerifyNoOtherCalls()
    {
        _directoryService.VerifyNoOtherCalls();
        // _fileSystem.VerifyNoOtherCalls();
        // _applicationPaths.VerifyNoOtherCalls();
        // _xmlSerializer.VerifyNoOtherCalls();
        // _activityManager.VerifyNoOtherCalls();
        // _libraryManager.VerifyNoOtherCalls();
        // _collectionManager.VerifyNoOtherCalls();
        // _localizationManager.VerifyNoOtherCalls();
        // _serverConfigurationManager.VerifyNoOtherCalls();
        // _serverApplicationHost.VerifyNoOtherCalls();
    }

}