using Jellyfin.Plugin.KinopoiskRu.Api;
using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskApiUnofficial;
using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskDev;
using Jellyfin.Plugin.KinopoiskRu.Configuration;

using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.KinopoiskRu;

/// <summary>
/// The main plugin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    internal const string PluginKey = "KinopoiskRu";
    internal const string PluginName = "Кинопоиск";
    internal const string PluginGuid = "0417364b-5a93-4ad0-a5f0-b8756957cf80";

    private readonly ILogger<Plugin> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IActivityManager _activityManager;
    private readonly Dictionary<string, IKinopoiskRuService> _kinopoiskServiciesDictionary = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
    /// <param name="activityManager">Instance of the <see cref="IActivityManager"/> interface.</param>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public Plugin(
        ILoggerFactory loggerFactory,
        IActivityManager activityManager,
        IApplicationPaths applicationPaths,
        IHttpClientFactory httpClientFactory,
        IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        if (httpClientFactory == null)
        {
            throw new ArgumentNullException(nameof(httpClientFactory));
        }

        Instance = this;
        HttpClient = httpClientFactory.CreateClient(NamedClient.Default);
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger<Plugin>();
        _activityManager = activityManager;
    }

    /// <inheritdoc />
    public override string Name => PluginName;

    /// <inheritdoc />
    public override Guid Id => Guid.Parse(PluginGuid);

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    internal HttpClient HttpClient { get; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "kinopoiskru",
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.kinopoiskru.html",
            },
            new PluginPageInfo
            {
                Name = "kinopoiskrujs",
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.kinopoiskru.js"
            }
        };
    }

    internal IKinopoiskRuService GetKinopoiskService()
    {
        if (PluginConfiguration.KinopoiskDev.Equals(Configuration.ApiType, StringComparison.Ordinal))
        {
            _logger.LogInformation($"Fetching {PluginConfiguration.KinopoiskDev} service");
            if (!_kinopoiskServiciesDictionary.TryGetValue("KinopoiskDev", out IKinopoiskRuService? result))
            {
                result = new KinopoiskDevService(_loggerFactory, _activityManager);
                _kinopoiskServiciesDictionary.Add("KinopoiskDev", result);
            }

            return result;
        }

        if (PluginConfiguration.KinopoiskAPIUnofficialTech.Equals(Configuration.ApiType, StringComparison.Ordinal))
        {
            _logger.LogInformation($"Fetching {PluginConfiguration.KinopoiskAPIUnofficialTech} service");
            if (!_kinopoiskServiciesDictionary.TryGetValue("KinopoiskUnofficial", out IKinopoiskRuService? result))
            {
                result = new KinopoiskUnofficialService(_loggerFactory, _activityManager);
                _kinopoiskServiciesDictionary.Add("KinopoiskUnofficial", result);
            }

            return result;
        }

        throw new Exception($"Unable to recognize provided API type '{Configuration.ApiType}'");
    }
}
