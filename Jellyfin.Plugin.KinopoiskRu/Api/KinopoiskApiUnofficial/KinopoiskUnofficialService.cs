using System.Globalization;

using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskApiUnofficial.Model;
using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskApiUnofficial.Model.Film;
using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskApiUnofficial.Model.Person;
using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskApiUnofficial.Model.Season;
using Jellyfin.Plugin.KinopoiskRu.Helper;

using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskApiUnofficial;

internal sealed class KinopoiskUnofficialService : IKinopoiskRuService
{
    private readonly ILogger<KinopoiskUnofficialService> _logger;
    private readonly KinopoiskUnofficialApi _api;
    private readonly Plugin _pluginInstance;

    internal KinopoiskUnofficialService(ILoggerFactory loggerFactory, IActivityManager activityManager)
    {
        _logger = loggerFactory.CreateLogger<KinopoiskUnofficialService>();
        _api = new KinopoiskUnofficialApi(loggerFactory, activityManager);
        _pluginInstance = Plugin.Instance!;
    }

    #region MovieProvider
    public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<Movie>()
        {
            ResultLanguage = "ru"
        };

        if (string.IsNullOrWhiteSpace(_pluginInstance.Configuration.GetCurrentToken()))
        {
            _logger.LogWarning($"The Token for {Plugin.PluginName} is empty");
            return result;
        }

        KpFilm? movie = await GetKpFilmByProviderId(info, cancellationToken).ConfigureAwait(false);
        if (movie != null)
        {
            _logger.LogInformation("Movie found by provider ID, Kinopoisk ID: '{MovieKinopoiskId}'", movie.KinopoiskId);
            await CreateMovie(result, movie, cancellationToken).ConfigureAwait(false);
            return result;
        }

        _logger.LogInformation("Movie not found by provider IDs");

        // no name cleanup - search 'as is', otherwise doesn't work
        _logger.LogInformation("Searching movies by name '{InfoName}' and year '{InfoYear}'", info.Name, info.Year);
        KpSearchResult<KpFilm> movies = await _api.GetFilmsByNameAndYear(info.Name, info.Year, cancellationToken).ConfigureAwait(false);
        List<KpFilm> relevantMovies = FilterRelevantItems(movies.Items, info.Name, info.Year);
        if (relevantMovies.Count != 1)
        {
            _logger.LogError("Found {RelevantMoviesCount} movies, skipping movie update", relevantMovies.Count);
            return result;
        }

        KpFilm? film = await _api.GetFilmById(relevantMovies[0].KinopoiskId.ToString(CultureInfo.InvariantCulture), cancellationToken).ConfigureAwait(false);
        if (film != null)
        {
            await CreateMovie(result, film, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
    {
        var result = new List<RemoteSearchResult>();

        if (string.IsNullOrWhiteSpace(_pluginInstance.Configuration.GetCurrentToken()))
        {
            _logger.LogWarning($"The Token for {Plugin.PluginName} is empty");
            return result;
        }

        KpFilm? movie = await GetKpFilmByProviderId(searchInfo, cancellationToken).ConfigureAwait(false);
        if (movie != null)
        {
            var imageUrl = (movie.PosterUrlPreview ?? movie.PosterUrl) ?? string.Empty;
            var item = new RemoteSearchResult()
            {
                Name = movie.NameRu,
                ImageUrl = imageUrl,
                SearchProviderName = Plugin.PluginKey,
                ProductionYear = movie.Year,
                Overview = movie.Description,
            };
            item.SetProviderId(Plugin.PluginKey, movie.KinopoiskId.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(movie.ImdbId))
            {
                item.SetProviderId(MetadataProvider.Imdb, movie.ImdbId);
            }

            result.Add(item);
            _logger.LogInformation("Found a movie with name '{MovieNameRu}' and id {MovieKinopoiskId}", movie.NameRu, movie.KinopoiskId);
            return result;
        }

        _logger.LogInformation("Movie not found by provider IDs");

        // no name cleanup - search 'as is', otherwise doesn't work
        _logger.LogInformation("Searching movies by name '{SearchInfoName}' and year '{SearchInfoYear}'", searchInfo.Name, searchInfo.Year);
        KpSearchResult<KpFilm> movies = await _api.GetFilmsByNameAndYear(searchInfo.Name, searchInfo.Year, cancellationToken).ConfigureAwait(false);
        foreach (KpFilm m in movies.Items)
        {
            var imageUrl = (m.PosterUrlPreview ?? m.PosterUrl) ?? string.Empty;
            var item = new RemoteSearchResult()
            {
                Name = m.NameRu,
                ImageUrl = imageUrl,
                SearchProviderName = Plugin.PluginKey,
                ProductionYear = m.Year,
                Overview = m.Description,
            };
            item.SetProviderId(Plugin.PluginKey, m.KinopoiskId.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(m.ImdbId))
            {
                item.SetProviderId(MetadataProvider.Imdb, m.ImdbId);
            }

            result.Add(item);
        }

        _logger.LogInformation("By name '{SearchInfoName}' found {ResultCount} movies", searchInfo.Name, result.Count);
        return result;
    }

    public async Task<List<Movie>> GetMoviesByOriginalNameAndYear(string name, int? year, CancellationToken cancellationToken)
    {
        var result = new List<Movie>();

        if (string.IsNullOrWhiteSpace(_pluginInstance.Configuration.GetCurrentToken()))
        {
            _logger.LogWarning($"The Token for {Plugin.PluginName} is empty");
            return result;
        }

        // no name cleanup - search 'as is', otherwise doesn't work
        _logger.LogInformation("Searching movies by name '{Name}' and year '{Year}'", name, year);
        KpSearchResult<KpFilm> movies = await _api.GetFilmsByNameAndYear(name, year, cancellationToken).ConfigureAwait(false);
        List<KpFilm> relevantMovies = FilterRelevantItems(movies.Items, name, year);
        foreach (KpFilm movie in relevantMovies)
        {
            KpFilm? film = await _api.GetFilmById(movie.KinopoiskId.ToString(CultureInfo.InvariantCulture), cancellationToken).ConfigureAwait(false);
            if (film != null)
            {
                result.Add(CreateMovieFromKpFilm(film));
            }
        }

        _logger.LogInformation("By keywords '{Name}' found {ResultCount} movies", name, result.Count);
        return result;
    }

    private async Task CreateMovie(MetadataResult<Movie> result, KpFilm movie, CancellationToken cancellationToken)
    {
        result.Item = CreateMovieFromKpFilm(movie);
        result.HasMetadata = true;

        var movieId = movie.KinopoiskId.ToString(CultureInfo.InvariantCulture);

        List<KpFilmStaff>? staffList = await _api.GetStaffByFilmId(movieId, cancellationToken).ConfigureAwait(false);
        if (staffList != null && staffList.Count > 0)
        {
            UpdatePersonsList(result, staffList, movie.NameRu);
        }

        KpSearchResult<KpVideo>? videosList = await _api.GetVideosByFilmId(movieId, cancellationToken).ConfigureAwait(false);
        if (result.HasMetadata && videosList != null && videosList.Items.Count > 0)
        {
            videosList.Items
                .Where(i => !string.IsNullOrWhiteSpace(i.Url) && i.Url.Contains("youtube", StringComparison.InvariantCultureIgnoreCase))
                .Select(i => i.Url!
                    .Replace("https://www.youtube.com/embed/", "https://www.youtube.com/watch?v=", StringComparison.InvariantCultureIgnoreCase)
                    .Replace("https://www.youtube.com/v/", "https://www.youtube.com/watch?v=", StringComparison.InvariantCultureIgnoreCase))
                .Reverse()
                .ToList()
                .ForEach(v => result.Item.AddTrailerUrl(v));
        }
    }

    private Movie CreateMovieFromKpFilm(KpFilm movie)
    {
        _logger.LogInformation(
            "Movie '{MovieNameRu}' with {PluginPluginName} id '{MovieKinopoiskId}' found",
            movie.NameRu,
            Plugin.PluginName,
            movie.KinopoiskId);

        var movieId = movie.KinopoiskId.ToString(CultureInfo.InvariantCulture);
        var toReturn = new Movie()
        {
            CommunityRating = movie.RatingKinopoisk,
            ExternalId = movieId,
            Name = movie.NameRu,
            OfficialRating = movie.RatingMpaa,
            OriginalTitle = movie.NameOriginal,
            Overview = movie.Description,
            ProductionLocations = movie.Countries?.Select(i => i.Country).ToArray(),
            ProductionYear = movie.Year,
            SortName = string.IsNullOrWhiteSpace(movie.NameRu) ? movie.NameOriginal : movie.NameRu,
            Tagline = movie.Slogan
        };

        toReturn.SetProviderId(Plugin.PluginKey, movieId);
        if (!string.IsNullOrWhiteSpace(movie.ImdbId))
        {
            toReturn.SetProviderId(MetadataProvider.Imdb, movie.ImdbId);
        }

        if (long.TryParse(movie.FilmLength?.ToString(CultureInfo.InvariantCulture), out var size))
        {
            toReturn.Size = size;
        }

        toReturn.Genres = movie
            .Genres?
            .Where(i => !string.IsNullOrWhiteSpace(i.Genre))
            .Select(i => i.Genre!)
            .ToArray();

        return toReturn;
    }
    #endregion

    #region MovieImagesProvider
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        var result = new List<RemoteImageInfo>();

        if (string.IsNullOrWhiteSpace(_pluginInstance.Configuration.GetCurrentToken()))
        {
            _logger.LogWarning($"The Token for {Plugin.PluginName} is empty");
            return result;
        }

        if (item.HasProviderId(Plugin.PluginKey))
        {
            var movieId = item.GetProviderId(Plugin.PluginKey);
            if (!string.IsNullOrWhiteSpace(movieId))
            {
                _logger.LogInformation("Searching movie by movie id '{MovieId}'", movieId);
                KpFilm? movie = await _api.GetFilmById(movieId, cancellationToken).ConfigureAwait(false);
                if (movie != null)
                {
                    UpdateRemoteImageInfoList(movie, result);
                    return result;
                }

                _logger.LogInformation("Images by movie id '{MovieId}' not found", movieId);
            }
        }

        // no name cleanup - search 'as is', otherwise doesn't work
        _logger.LogInformation("Searching movies by name '{ItemName}' and year '{ItemProductionYear}'", item.Name, item.ProductionYear);
        KpSearchResult<KpFilm> movies = await _api.GetFilmsByNameAndYear(item.Name, item.ProductionYear, cancellationToken).ConfigureAwait(false);
        List<KpFilm> relevantMovies = FilterRelevantItems(movies.Items, item.Name, item.ProductionYear);
        if (relevantMovies.Count != 1)
        {
            _logger.LogError("Found {RelevantMoviesCount} movies, skipping image update", relevantMovies.Count);
            return result;
        }

        KpFilm? film = await _api.GetFilmById(relevantMovies[0].KinopoiskId.ToString(CultureInfo.InvariantCulture), cancellationToken).ConfigureAwait(false);
        if (film != null)
        {
            UpdateRemoteImageInfoList(film, result);
        }

        return result;
    }

    private void UpdateRemoteImageInfoList(KpFilm movie, List<RemoteImageInfo> result)
    {
        if (!string.IsNullOrWhiteSpace(movie.CoverUrl))
        {
            result.Add(new RemoteImageInfo()
            {
                ProviderName = Plugin.PluginKey,
                Url = movie.CoverUrl,
                Language = "ru",
                Type = ImageType.Backdrop
            });
        }

        if (!string.IsNullOrWhiteSpace(movie.PosterUrl))
        {
            result.Add(new RemoteImageInfo()
            {
                ProviderName = Plugin.PluginKey,
                Url = movie.PosterUrl,
                ThumbnailUrl = movie.PosterUrlPreview,
                Language = "ru",
                Type = ImageType.Primary
            });
        }

        if (!string.IsNullOrWhiteSpace(movie.LogoUrl))
        {
            result.Add(new RemoteImageInfo()
            {
                ProviderName = Plugin.PluginKey,
                Url = movie.LogoUrl,
                Language = "ru",
                Type = ImageType.Logo
            });
        }

        var imageTypes = string.Join(", ", result.Select(i => i.Type).ToList());
        _logger.LogInformation("By movie id '{MovieKinopoiskId}' found '{ImageTypes}' image types", movie.KinopoiskId, imageTypes);
    }
    #endregion

    #region SeriesProvider
    public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<Series>()
        {
            ResultLanguage = "ru"
        };

        if (string.IsNullOrWhiteSpace(_pluginInstance.Configuration.GetCurrentToken()))
        {
            _logger.LogWarning($"The Token for {Plugin.PluginName} is empty");
            return result;
        }

        KpFilm? movie = await GetKpFilmByProviderId(info, cancellationToken).ConfigureAwait(false);
        if (movie != null)
        {
            _logger.LogInformation("Series found by provider ID, Kinopoisk ID: '{MovieKinopoiskId}'", movie.KinopoiskId);
            await CreateSeries(result, movie, cancellationToken).ConfigureAwait(false);
            return result;
        }

        _logger.LogInformation("Series was not found by provider ID");

        // no name cleanup - search 'as is', otherwise doesn't work
        _logger.LogInformation("Searching series by name '{InfoName}' and year '{InfoYear}'", info.Name, info.Year);
        KpSearchResult<KpFilm> series = await _api.GetFilmsByNameAndYear(info.Name, info.Year, cancellationToken).ConfigureAwait(false);
        List<KpFilm> relevantSeries = FilterRelevantItems(series.Items, info.Name, info.Year);
        if (relevantSeries.Count != 1)
        {
            _logger.LogError("Found {RelevantSeriesCount} series, skipping series update", relevantSeries.Count);
            return result;
        }

        KpFilm? s = await _api.GetFilmById(relevantSeries[0].KinopoiskId.ToString(CultureInfo.InvariantCulture), cancellationToken).ConfigureAwait(false);
        if (s != null)
        {
            await CreateSeries(result, s, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
    {
        var result = new List<RemoteSearchResult>();

        if (string.IsNullOrWhiteSpace(_pluginInstance.Configuration.GetCurrentToken()))
        {
            _logger.LogWarning($"The Token for {Plugin.PluginName} is empty");
            return result;
        }

        KpFilm? series = await GetKpFilmByProviderId(searchInfo, cancellationToken).ConfigureAwait(false);
        if (series != null)
        {
            var imageUrl = (series.PosterUrlPreview ?? series.PosterUrl) ?? string.Empty;
            var item = new RemoteSearchResult()
            {
                Name = series.NameRu,
                ImageUrl = imageUrl,
                SearchProviderName = Plugin.PluginKey,
                ProductionYear = series.Year,
                Overview = series.Description,
            };
            item.SetProviderId(Plugin.PluginKey, series.KinopoiskId.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(series.ImdbId))
            {
                item.SetProviderId(MetadataProvider.Imdb, series.ImdbId);
            }

            result.Add(item);
            _logger.LogInformation("Found a series with name {SeriesNameRu} and id {SeriesKinopoiskId}", series.NameRu, series.KinopoiskId);
            return result;
        }

        _logger.LogInformation("Series not found by provider ID");

        // no name cleanup - search 'as is', otherwise doesn't work
        _logger.LogInformation("Searching series by name '{SearchInfoName}' and year '{SearchInfoYear}'", searchInfo.Name, searchInfo.Year);
        KpSearchResult<KpFilm> seriesResult = await _api.GetFilmsByNameAndYear(searchInfo.Name, searchInfo.Year, cancellationToken).ConfigureAwait(false);
        foreach (KpFilm s in seriesResult.Items)
        {
            var imageUrl = (s.PosterUrlPreview ?? s.PosterUrl) ?? string.Empty;
            var item = new RemoteSearchResult()
            {
                Name = s.NameRu,
                ImageUrl = imageUrl,
                SearchProviderName = Plugin.PluginKey,
                ProductionYear = s.Year,
                Overview = s.Description,
            };
            item.SetProviderId(Plugin.PluginKey, s.KinopoiskId.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(s.ImdbId))
            {
                item.SetProviderId(MetadataProvider.Imdb, s.ImdbId);
            }

            result.Add(item);
        }

        _logger.LogInformation("By name '{SearchInfoName}' found {ResultCount} series", searchInfo.Name, result.Count);
        return result;
    }

    private async Task CreateSeries(MetadataResult<Series> result, KpFilm film, CancellationToken cancellationToken)
    {
        result.Item = CreateSeriesFromKpFilm(film);
        result.HasMetadata = true;

        var seriesId = film.KinopoiskId.ToString(CultureInfo.InvariantCulture);

        List<KpFilmStaff>? staffList = await _api.GetStaffByFilmId(seriesId, cancellationToken).ConfigureAwait(false);
        if (staffList != null && staffList.Count > 0)
        {
            UpdatePersonsList(result, staffList, film.NameRu);
        }

        KpSearchResult<KpVideo>? videosList = await _api.GetVideosByFilmId(seriesId, cancellationToken).ConfigureAwait(false);
        if (videosList != null && videosList.Items.Count > 0)
        {
            videosList.Items
                .Where(i => !string.IsNullOrWhiteSpace(i.Url) && i.Url.Contains("youtube", StringComparison.InvariantCultureIgnoreCase))
                .Select(i => i.Url!
                    .Replace("https://www.youtube.com/embed/", "https://www.youtube.com/watch?v=", StringComparison.InvariantCultureIgnoreCase)
                    .Replace("https://www.youtube.com/v/", "https://www.youtube.com/watch?v=", StringComparison.InvariantCultureIgnoreCase))
                .Reverse()
                .ToList()
                .ForEach(v => result.Item.AddTrailerUrl(v));
        }
    }

    private Series CreateSeriesFromKpFilm(KpFilm series)
    {
        _logger.LogInformation(
            "Series '{SeriesNameRu}' with {PluginPluginName} id '{SeriesKinopoiskId}' found",
            series.NameRu,
            Plugin.PluginName,
            series.KinopoiskId);

        var seriesId = series.KinopoiskId.ToString(CultureInfo.InvariantCulture);
        var toReturn = new Series()
        {
            CommunityRating = series.RatingKinopoisk,
            ExternalId = seriesId,
            Name = series.NameRu,
            OfficialRating = series.RatingMpaa,
            OriginalTitle = series.NameOriginal,
            Overview = series.Description,
            ProductionLocations = series.Countries?.Select(i => i.Country).ToArray(),
            ProductionYear = series.Year,
            SortName = string.IsNullOrWhiteSpace(series.NameRu) ? series.NameOriginal : series.NameRu,
            Tagline = series.Slogan,
        };

        toReturn.SetProviderId(Plugin.PluginKey, seriesId);
        if (!string.IsNullOrWhiteSpace(series.ImdbId))
        {
            toReturn.SetProviderId(MetadataProvider.Imdb, series.ImdbId);
        }

        if (long.TryParse(series.FilmLength?.ToString(CultureInfo.InvariantCulture), out var size))
        {
            toReturn.Size = size;
        }

        toReturn.Genres = series
            .Genres?
            .Where(i => !string.IsNullOrWhiteSpace(i.Genre))
            .Select(i => i.Genre!)
            .ToArray();

        return toReturn;
    }
    #endregion

    #region EpisodeProvider
    public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<Episode>()
        {
            ResultLanguage = "ru"
        };

        if (string.IsNullOrWhiteSpace(_pluginInstance.Configuration.GetCurrentToken()))
        {
            _logger.LogWarning($"The Token for {Plugin.PluginName} is empty");
            return result;
        }

        var seriesId = info.GetSeriesProviderId(Plugin.PluginKey);
        if (string.IsNullOrWhiteSpace(seriesId))
        {
            _logger.LogDebug("SeriesProviderId not exists for {PluginPluginName}, checking ProviderId", Plugin.PluginName);
            seriesId = info.GetProviderId(Plugin.PluginKey);
        }

        if (string.IsNullOrWhiteSpace(seriesId))
        {
            _logger.LogInformation("The episode doesn't have series id for {PluginPluginName}", Plugin.PluginName);
            return result;
        }

        if (info.IndexNumber == null || info.ParentIndexNumber == null)
        {
            _logger.LogWarning("Not enough parameters. Season index '{InfoParentIndexNumber}', episode index '{InfoIndexNumber}'", info.ParentIndexNumber, info.IndexNumber);
            return result;
        }

        _logger.LogInformation(
            "Searching episode by series id '{SeriesId}', season index '{InfoParentIndexNumber}' and episode index '{InfoIndexNumber}'",
            seriesId,
            info.ParentIndexNumber,
            info.IndexNumber);
        KpSearchResult<KpSeason>? item = await _api.GetEpisodesBySeriesId(seriesId, cancellationToken).ConfigureAwait(false);
        if (item == null)
        {
            _logger.LogInformation("Episodes by series id '{SeriesId}' not found", seriesId);
            return result;
        }

        KpSeason? kpSeason = item.Items.FirstOrDefault(s => s.Number == info.ParentIndexNumber);
        if (kpSeason == null)
        {
            _logger.LogInformation("Season with index '{InfoParentIndexNumber}' not found", info.ParentIndexNumber);
            return result;
        }

        KpEpisode? kpEpisode = kpSeason.Episodes.FirstOrDefault(e =>
            e.EpisodeNumber == info.IndexNumber && e.SeasonNumber == info.ParentIndexNumber);
        if (kpEpisode == null)
        {
            _logger.LogInformation("Episode with index '{InfoIndexNumber}' not found", info.IndexNumber);
            return result;
        }

        _ = DateTime.TryParseExact(
            kpEpisode.ReleaseDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime premiereDate);
        result.Item = new Episode()
        {
            Name = kpEpisode.NameRu,
            OriginalTitle = kpEpisode.NameEn,
            Overview = kpEpisode.Synopsis,
            IndexNumber = info.IndexNumber,
            ParentIndexNumber = info.ParentIndexNumber,
            PremiereDate = premiereDate,
        };
        result.HasMetadata = true;
        _logger.LogInformation(
            "Episode {InfoIndexNumber} of season {InfoParentIndexNumber} of series {SeriesId} updated",
            info.IndexNumber,
            info.ParentIndexNumber,
            seriesId);
        return result;
    }

    #endregion

    #region PersonProvider
    public async Task<MetadataResult<Person>> GetMetadata(PersonLookupInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<Person>()
        {
            ResultLanguage = "ru"
        };

        if (string.IsNullOrWhiteSpace(_pluginInstance.Configuration.GetCurrentToken()))
        {
            _logger.LogWarning($"The Token for {Plugin.PluginName} is empty");
            return result;
        }

        if (info.HasProviderId(Plugin.PluginKey))
        {
            var personId = info.ProviderIds[Plugin.PluginKey];
            if (!string.IsNullOrWhiteSpace(personId))
            {
                _logger.LogInformation("Fetching person by person id '{PersonId}'", personId);
                KpPerson? person = await _api.GetPersonById(personId, cancellationToken).ConfigureAwait(false);
                if (person != null)
                {
                    result.Item = CreatePersonFromKpPerson(person);
                    result.HasMetadata = true;
                    return result;
                }

                _logger.LogInformation("Person by person id '{PersonId}' not found", personId);
            }
        }

        _logger.LogInformation("Searching person by name '{InfoName}'", info.Name);
        KpSearchResult<KpStaff>? persons = await _api.GetPersonsByName(info.Name, cancellationToken).ConfigureAwait(false);
        if (persons == null)
        {
            _logger.LogError("No person found, skipping person update");
            return result;
        }

        persons.Items = FilterPersonsByName(info.Name, persons.Items);
        if (persons.Items.Count != 1)
        {
            _logger.LogError("Found {PersonsItemsCount} persons, skipping person update", persons.Items.Count);
            return result;
        }

        KpPerson? p = await _api.GetPersonById(persons.Items[0].KinopoiskId.ToString(CultureInfo.InvariantCulture), cancellationToken).ConfigureAwait(false);
        if (p != null)
        {
            result.Item = CreatePersonFromKpPerson(p);
            result.HasMetadata = true;
        }

        return result;
    }

    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(PersonLookupInfo searchInfo, CancellationToken cancellationToken)
    {
        var result = new List<RemoteSearchResult>();

        if (string.IsNullOrWhiteSpace(_pluginInstance.Configuration.GetCurrentToken()))
        {
            _logger.LogWarning($"The Token for {Plugin.PluginName} is empty");
            return result;
        }

        if (searchInfo.HasProviderId(Plugin.PluginKey))
        {
            var personId = searchInfo.ProviderIds[Plugin.PluginKey];
            if (!string.IsNullOrWhiteSpace(personId))
            {
                _logger.LogInformation("Searching person by id '{PersonId}'", personId);
                KpPerson? person = await _api.GetPersonById(personId, cancellationToken).ConfigureAwait(false);
                if (person != null)
                {
                    var item = new RemoteSearchResult()
                    {
                        Name = person.NameRu,
                        ImageUrl = person.PosterUrl,
                        SearchProviderName = Plugin.PluginKey
                    };
                    item.SetProviderId(Plugin.PluginKey, personId);
                    result.Add(item);
                    return result;
                }

                _logger.LogInformation("Person by id '{PersonId}' not found", personId);
            }
        }

        _logger.LogInformation("Searching persons by name '{SearchInfoName}'", searchInfo.Name);
        KpSearchResult<KpStaff>? persons = await _api.GetPersonsByName(searchInfo.Name, cancellationToken).ConfigureAwait(false);
        if (persons == null)
        {
            _logger.LogInformation("No person was found");
            return result;
        }

        foreach (KpStaff person in persons.Items)
        {
            var item = new RemoteSearchResult()
            {
                Name = person.NameRu,
                ImageUrl = person.PosterUrl,
                SearchProviderName = Plugin.PluginKey
            };
            item.SetProviderId(Plugin.PluginKey, person.KinopoiskId.ToString(CultureInfo.InvariantCulture));
            result.Add(item);
        }

        _logger.LogInformation("By name '{SearchInfoName}' found {ResultCount} persons", searchInfo.Name, result.Count);
        return result;
    }

    private Person CreatePersonFromKpPerson(KpPerson person)
    {
        _logger.LogInformation("Person '{PersonNameRu}' with KinopoiskId '{PersonPersonId}' found", person.NameRu, person.PersonId);

        var toReturn = new Person()
        {
            Name = person.NameRu,
            SortName = person.NameRu,
            OriginalTitle = person.NameEn,
            ProviderIds = new() { { Plugin.PluginKey, person.PersonId.ToString(CultureInfo.InvariantCulture) } }
        };
        if (DateTime.TryParse(person.Birthday, out DateTime birthDay))
        {
            toReturn.PremiereDate = birthDay;
        }

        if (DateTime.TryParse(person.Death, out DateTime deathDay))
        {
            toReturn.EndDate = deathDay;
        }

        if (!string.IsNullOrWhiteSpace(person.BirthPlace))
        {
            toReturn.ProductionLocations = new string[] { person.BirthPlace };
        }

        var facts = person.Facts?.ToArray();
        if (facts?.Length > 0)
        {
            toReturn.Overview = string.Join("\n", facts);
        }

        return toReturn;
    }

    #endregion

    #region Common
    private void UpdatePersonsList<T>(MetadataResult<T> result, List<KpFilmStaff> staffList, string? movieName)
        where T : BaseItem
    {
        foreach (KpFilmStaff staff in staffList)
        {
            var personType = KpHelper.GetPersonType(staff.ProfessionKey);
            var name = string.IsNullOrWhiteSpace(staff.NameRu) ? staff.NameEn : staff.NameRu;
            if (string.IsNullOrWhiteSpace(name))
            {
                var staffId = staff.StaffId.ToString(CultureInfo.InvariantCulture);
                _logger.LogWarning("Skip adding staff with id '{StaffId}' as nameless to '{MovieName}'", staffId, movieName);
            }
            else if (personType == null)
            {
                _logger.LogWarning("Skip adding '{Name}' as '{StaffProfessionKey}' to '{MovieName}'", name, staff.ProfessionKey, movieName);
            }
            else
            {
                _logger.LogDebug("Adding '{Name}' as '{PersonType}' to '{MovieName}'", name, personType, movieName);
                var person = new PersonInfo()
                {
                    Name = name,
                    ImageUrl = staff.PosterUrl,
                    Type = personType,
                    Role = staff.Description,
                };
                person.SetProviderId(Plugin.PluginKey, staff.StaffId.ToString(CultureInfo.InvariantCulture));

                result.AddPerson(person);
            }
        }

        var providerId = result.Item.GetProviderId(Plugin.PluginKey);
        _logger.LogInformation(
            "Added {ResultPeopleCount} persons to the movie with id '{ProviderId}'",
            result.People.Count,
            providerId);
    }

    private List<KpFilm> FilterRelevantItems(List<KpFilm> list, string name, int? year)
    {
        _logger.LogInformation("Filtering out irrelevant items");
        if (list.Count > 1)
        {
            var toReturn = list
                .Where(m => (!string.IsNullOrWhiteSpace(name)
                    && (KpHelper.CleanName(m.NameRu) == KpHelper.CleanName(name)))
                        || KpHelper.CleanName(m.NameOriginal) == KpHelper.CleanName(name))
                .Where(m => year == null || m.Year == year)
                .ToList();
            return toReturn.Any() ? toReturn : list;
        }
        else
        {
            return list;
        }
    }

    private List<KpStaff> FilterPersonsByName(string name, List<KpStaff> list)
    {
        _logger.LogInformation("Filtering out irrelevant persons");
        if (list.Count > 1)
        {
            var toReturn = list
                .Where(m => (!string.IsNullOrWhiteSpace(name)
                    && (KpHelper.CleanName(m.NameRu) == KpHelper.CleanName(name)))
                        || KpHelper.CleanName(m.NameEn) == KpHelper.CleanName(name))
                .ToList();
            return toReturn.Any() ? toReturn : list;
        }
        else
        {
            return list;
        }
    }

    private async Task<KpFilm?> GetKpFilmByProviderId(ItemLookupInfo info, CancellationToken cancellationToken)
    {
        if (info.HasProviderId(Plugin.PluginKey) && !string.IsNullOrWhiteSpace(info.GetProviderId(Plugin.PluginKey)))
        {
            var movieId = info.GetProviderId(Plugin.PluginKey);
            _logger.LogInformation("Searching movie by movie id '{MovieId}'", movieId);
            return await _api.GetFilmById(movieId, cancellationToken).ConfigureAwait(false);
        }

        if (info.HasProviderId(MetadataProvider.Imdb) && !string.IsNullOrWhiteSpace(info.GetProviderId(MetadataProvider.Imdb)))
        {
            var imdbMovieId = info.GetProviderId(MetadataProvider.Imdb);
            _logger.LogInformation("Searching Kp movie by IMDB '{ImdbMovieId}'", imdbMovieId);
            KpSearchResult<KpFilm> kpSearchResult = await _api.GetFilmByImdbId(imdbMovieId, cancellationToken).ConfigureAwait(false);
            if (kpSearchResult.Items.Count != 1)
            {
                _logger.LogInformation("Nothing was found by IMDB '{ImdbMovieId}'. Skip search by IMDB ID", imdbMovieId);
                return null;
            }

            return await _api.GetFilmById(kpSearchResult.Items[0].KinopoiskId.ToString(CultureInfo.InvariantCulture), cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    #endregion

    #region Scheduled Tasks
    public Task<List<BaseItem>> GetTop250MovieCollection(CancellationToken cancellationToken)
    {
        _logger.LogInformation("KinopoiskUnofficial doesn't have information about Top250");
        return Task.FromResult(new List<BaseItem>());
    }

    public Task<List<BaseItem>> GetTop250SeriesCollection(CancellationToken cancellationToken)
    {
        _logger.LogInformation("KinopoiskUnofficial doesn't have information about Top250");
        return Task.FromResult(new List<BaseItem>());
    }

    public Task<ApiResult<Dictionary<string, long>>> GetKpIdByAnotherId(string externalIdType, List<string> idList, CancellationToken cancellationToken)
    {
        _logger.LogInformation("KinopoiskUnofficial unable to search by IMDB nor by TMDB");
        return Task.FromResult(new ApiResult<Dictionary<string, long>>(new Dictionary<string, long>()));
    }

    #endregion
}
