using System.Text.RegularExpressions;

using Jellyfin.Plugin.KinopoiskRu.Helper;

using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.KinopoiskRu.Provider.LocalMetadata;

/// <inheritdoc/>
public class KpMovieLocalMetadata : KpBaseLocalMetadata<KpMovieLocalMetadata, Movie>
{
    private static readonly Regex NotAlphaNumeric = new("[^0-9ЁA-ZА-Я-]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex MultiSpaces = new(" {2,}", RegexOptions.Compiled);

    private readonly ILogger<KpMovieLocalMetadata> _logger;

    private readonly Plugin _pluginInstance;

    /// <summary>
    /// Initializes a new instance of the <see cref="KpMovieLocalMetadata"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger&lt;KpMovieLocalMetadata&gt;"/> interface.</param>
    public KpMovieLocalMetadata(ILogger<KpMovieLocalMetadata> logger) : base(logger)
    {
        _logger = logger;
        _pluginInstance = Plugin.Instance!;
    }

    /// <inheritdoc/>
    public override async Task<MetadataResult<Movie>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
    {
        if (info == null)
        {
            throw new ArgumentNullException(nameof(info));
        }

        MetadataResult<Movie> result = await base.GetMetadata(info, directoryService, cancellationToken).ConfigureAwait(false);
        if (result.HasMetadata)
        {
            var providerId = result.Item.ProviderIds[Plugin.PluginKey];
            _logger.LogInformation("Movie has kp id: {ProviderId}", providerId);
            return result;
        }

        var movieName = Path.GetFileName(info.Path);
        movieName = movieName[..movieName.LastIndexOf(".", StringComparison.InvariantCulture)];
        _logger.LogInformation("info.Name - {MovieName}", movieName);
        movieName = MultiSpaces.Replace(NotAlphaNumeric.Replace(movieName, " "), " ");
        var year = string.IsNullOrWhiteSpace(movieName) ? null : KpHelper.DetectYearFromMoviePath(info.Path, movieName);
        _logger.LogInformation("Searching movie by name - '{MovieName}' and year - {Year}", movieName, year);
        List<Movie> movies = await _pluginInstance.GetKinopoiskService().GetMoviesByOriginalNameAndYear(movieName, year, cancellationToken).ConfigureAwait(false);
        if (movies.Count == 0)
        {
            _logger.LogInformation("Nothing found for movie name '{MovieName}'", movieName);
        }
        else if (movies.Count == 1)
        {
            result.Item = movies[0];
            result.HasMetadata = true;
            var movieProviderId = movies[0].GetProviderId(Plugin.PluginKey);
            _logger.LogInformation("For movie name '{MovieName}' found movie with KP id = '{MovieProviderId}'", movieName, movieProviderId);
        }
        else
        {
            Movie? movieWithHighestRating = movies
                .Where(m => m.CommunityRating != null)
                .OrderByDescending(m => m.CommunityRating)
                .FirstOrDefault();
            if (movieWithHighestRating != null)
            {
                result.Item = movieWithHighestRating;
                var movieProviderId = result.Item.GetProviderId(Plugin.PluginKey);
                _logger.LogInformation(
                    "Found {MoviesCount} movies. Taking the first one with highest rating in KP. Choose movie with KP id = '{MovieProviderId}' for '{MovieName}'",
                    movies.Count,
                    movieProviderId,
                    movieName);
            }
            else
            { // all films without KP rating
                result.Item = movies[0];
                var movieProviderId = result.Item.GetProviderId(Plugin.PluginKey);
                _logger.LogInformation(
                    "Found {MoviesCount} movies. Taking the first one. Choose movie with KP id = '{MovieProviderId}' for '{MovieName}'",
                    movies.Count,
                    movieProviderId,
                    movieName);
            }

            result.HasMetadata = true;
        }

        return result;
    }
}
