using System.Globalization;
using System.Text;

using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskDev.Model;
using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskDev.Model.Movie;
using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskDev.Model.Person;
using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskDev.Model.Season;
using Jellyfin.Plugin.KinopoiskRu.Helper;

using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskDev;

internal sealed class KinopoiskDevService : IKinopoiskRuService
{
    private readonly ILogger<KinopoiskDevService> _logger;
    private readonly KinopoiskDevApi _api;
    // private readonly ILibraryManager _libraryManager;
    private readonly List<KpMovieType?> _movieTypes = new() { KpMovieType.Anime, KpMovieType.Movie, KpMovieType.Cartoon };
    // private readonly ICollectionManager _collectionManager;
    private readonly Plugin _pluginInstance;

    internal KinopoiskDevService(ILoggerFactory loggerFactory, IActivityManager activityManager)
    {
        _logger = loggerFactory.CreateLogger<KinopoiskDevService>();
        _api = new KinopoiskDevApi(loggerFactory, activityManager);
        _pluginInstance = Plugin.Instance!;
    }

    #region MovieProvider
    public async Task<List<Movie>> GetMoviesByOriginalNameAndYear(string? name, int? year, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_pluginInstance.Configuration.GetCurrentToken()))
        {
            _logger.LogWarning($"The Token for {Plugin.PluginName} is empty");
            return new List<Movie>();
        }

        name = KpHelper.CleanName(name);
        KpSearchResult<KpMovie> movies = await _api.GetMoviesByMovieDetails(name, year, cancellationToken).ConfigureAwait(false);
        List<KpMovie> relevantMovies = FilterRelevantItems(movies.Docs, name, year, name);
        var toReturn = new List<Movie>();
        foreach (KpMovie movie in relevantMovies)
        {
            toReturn.Add(await CreateMovieFromKpMovie(movie, cancellationToken).ConfigureAwait(false));
        }

        _logger.LogInformation(
            "By name '{Name}' and year '{Year}' found {ToReturnCount} movies",
            name,
            year,
            toReturn.Count);
        return toReturn;
    }

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

        KpMovie? movie = await GetKpMovieByProviderId(info, cancellationToken).ConfigureAwait(false);
        if (movie != null)
        {
            _logger.LogInformation("Movie found by provider ID, Kinopoisk ID: '{MovieId}'", movie.Id);
            result.Item = await CreateMovieFromKpMovie(movie, cancellationToken).ConfigureAwait(false);
            await UpdatePersonsList(result, movie.Persons, cancellationToken).ConfigureAwait(false);
            result.HasMetadata = true;
            return result;
        }

        _logger.LogInformation("Movie was not found by provider ID");

        var name = KpHelper.CleanName(info.Name);
        _logger.LogInformation("Searching movie by name '{Name}' and year '{InfoYear}'", name, info.Year);
        KpSearchResult<KpMovie> movies = await _api.GetMoviesByMovieDetails(name, info.Year, cancellationToken).ConfigureAwait(false);
        List<KpMovie> relevantMovies = FilterRelevantItems(movies.Docs, name, info.Year, name);
        if (relevantMovies.Count != 1)
        {
            _logger.LogError("Found {RelevantMoviesCount} movies, skipping movie update", relevantMovies.Count);
            return result;
        }

        result.Item = await CreateMovieFromKpMovie(relevantMovies[0], cancellationToken).ConfigureAwait(false);
        await UpdatePersonsList(result, relevantMovies[0].Persons, cancellationToken).ConfigureAwait(false);
        result.HasMetadata = true;
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

        KpMovie? movie = await GetKpMovieByProviderId(searchInfo, cancellationToken).ConfigureAwait(false);
        if (movie != null)
        {
            _logger.LogInformation("Movie found by provider ID, Kinopoisk ID: '{MovieId}'", movie.Id);
            var item = new RemoteSearchResult()
            {
                Name = movie.Name,
                ImageUrl = (movie.Poster?.PreviewUrl ?? movie.Poster?.Url) ?? string.Empty,
                SearchProviderName = Plugin.PluginKey,
                ProductionYear = movie.Year,
                Overview = PrepareOverview(movie),
            };
            item.SetProviderId(Plugin.PluginKey, movie.Id.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(movie.ExternalId?.Imdb))
            {
                item.ProviderIds.Add(MetadataProvider.Imdb.ToString(), movie.ExternalId.Imdb);
            }

            if (movie.ExternalId?.Tmdb != null)
            {
                item.ProviderIds.Add(MetadataProvider.Tmdb.ToString(), movie.ExternalId.Tmdb.ToString());
            }

            result.Add(item);
            return result;
        }

        _logger.LogInformation("Movie was not found by provider ID");

        var name = KpHelper.CleanName(searchInfo.Name);
        _logger.LogInformation("Searching movie by name '{Name}' and year '{SearchInfoYear}'", name, searchInfo.Year);
        KpSearchResult<KpMovie> movies = await _api.GetMoviesByMovieDetails(name, searchInfo.Year, cancellationToken).ConfigureAwait(false);
        foreach (KpMovie m in movies.Docs)
        {
            var imageUrl = (m.Poster?.PreviewUrl ?? m.Poster?.Url) ?? string.Empty;
            var item = new RemoteSearchResult()
            {
                Name = m.Name,
                ImageUrl = imageUrl,
                SearchProviderName = Plugin.PluginKey,
                ProductionYear = m.Year,
                Overview = PrepareOverview(m),
            };
            item.SetProviderId(Plugin.PluginKey, m.Id.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(m.ExternalId?.Imdb))
            {
                item.ProviderIds.Add(MetadataProvider.Imdb.ToString(), m.ExternalId.Imdb);
            }

            if (m.ExternalId?.Tmdb != null)
            {
                item.ProviderIds.Add(MetadataProvider.Tmdb.ToString(), m.ExternalId.Tmdb.ToString());
            }

            result.Add(item);
        }

        _logger.LogInformation("By name '{Name}' found {ResultCount} movies", name, result.Count);
        return result;
    }

    private Task<Movie> CreateMovieFromKpMovie(KpMovie movie, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Movie '{MovieName}' with {PluginPluginName} id '{MovieId}' found",
            movie.Name,
            Plugin.PluginName,
            movie.Id);

        var movieId = movie.Id.ToString(CultureInfo.InvariantCulture);
        var toReturn = new Movie()
        {
            CommunityRating = movie.Rating?.Kp,
            ExternalId = movieId,
            Name = movie.Name,
            OfficialRating = movie.RatingMpaa,
            OriginalTitle = movie.AlternativeName,
            Overview = PrepareOverview(movie),
            PremiereDate = KpHelper.GetPremierDate(movie.Premiere),
            ProductionLocations = movie.Countries?.Select(i => i.Name).ToArray(),
            ProductionYear = movie.Year,
            Size = movie.MovieLength ?? 0,
            SortName =
                string.IsNullOrWhiteSpace(movie.Name) ?
                    string.IsNullOrWhiteSpace(movie.AlternativeName) ?
                        string.Empty
                        : movie.AlternativeName
                    : movie.Name,
            Tagline = movie.Slogan
        };

        toReturn.SetProviderId(Plugin.PluginKey, movieId);

        if (!string.IsNullOrWhiteSpace(movie.ExternalId?.Imdb))
        {
            toReturn.ProviderIds.Add(MetadataProvider.Imdb.ToString(), movie.ExternalId.Imdb);
        }

        if (movie.ExternalId?.Tmdb != null)
        {
            toReturn.ProviderIds.Add(MetadataProvider.Tmdb.ToString(), movie.ExternalId.Tmdb.ToString());
        }

        toReturn.Genres = movie
            .Genres?
            .Where(i => !string.IsNullOrWhiteSpace(i.Name))
            .Select(i => i.Name!)
            .ToArray();

        IEnumerable<string>? studios = movie
            .ProductionCompanies?
            .Where(i => !string.IsNullOrWhiteSpace(i.Name))
            .Select(i => i.Name!)
            .AsEnumerable();
        if (studios != null)
        {
            toReturn.SetStudios(studios);
        }

        movie.Videos?.Teasers?
            .Where(i => !string.IsNullOrWhiteSpace(i.Url) && i.Url.Contains("youtube", StringComparison.InvariantCultureIgnoreCase))
            .Select(i => i.Url!
                    .Replace("https://www.youtube.com/embed/", "https://www.youtube.com/watch?v=", StringComparison.InvariantCultureIgnoreCase)
                    .Replace("https://www.youtube.com/v/", "https://www.youtube.com/watch?v=", StringComparison.InvariantCultureIgnoreCase))
            .Reverse()
            .ToList()
            .ForEach(j => toReturn.AddTrailerUrl(j));
        movie.Videos?.Trailers?
            .Where(i => !string.IsNullOrWhiteSpace(i.Url) && i.Url.Contains("youtube", StringComparison.InvariantCultureIgnoreCase))
            .Select(i => i.Url!
                    .Replace("https://www.youtube.com/embed/", "https://www.youtube.com/watch?v=", StringComparison.InvariantCultureIgnoreCase)
                    .Replace("https://www.youtube.com/v/", "https://www.youtube.com/watch?v=", StringComparison.InvariantCultureIgnoreCase))
            .Reverse()
            .ToList()
            .ForEach(j => toReturn.AddTrailerUrl(j));

        if (_pluginInstance.Configuration.NeedToCreateSequenceCollection() && movie.SequelsAndPrequels.Any())
        {
            // await AddMovieToCollection(toReturn, movie, cancellationToken).ConfigureAwait(false);
        }

        return Task.FromResult(toReturn);
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

        KpMovie? item = await GetKpMovieByProviderId(info, cancellationToken).ConfigureAwait(false);
        if (item != null)
        {
            _logger.LogInformation("Series found by provider ID, Kinopoisk ID: '{ItemId}'", item.Id);
            result.Item = await CreateSeriesFromKpMovie(item, cancellationToken).ConfigureAwait(false);
            await UpdatePersonsList(result, item.Persons, cancellationToken).ConfigureAwait(false);
            result.HasMetadata = true;
            return result;
        }

        _logger.LogInformation("Series was not found by provider ID");

        var name = KpHelper.CleanName(info.Name);
        _logger.LogInformation("Searching series by name '{Name}' and year '{InfoYear}'", name, info.Year);
        KpSearchResult<KpMovie> series = await _api.GetMoviesByMovieDetails(name, info.Year, cancellationToken).ConfigureAwait(false);
        List<KpMovie> relevantSeries = FilterRelevantItems(series.Docs, name, info.Year, name);
        if (relevantSeries.Count != 1)
        {
            _logger.LogError("Found {RelevantSeriesCount} series, skipping series update", relevantSeries.Count);
            return result;
        }

        result.Item = await CreateSeriesFromKpMovie(relevantSeries[0], cancellationToken).ConfigureAwait(false);
        await UpdatePersonsList(result, relevantSeries[0].Persons, cancellationToken).ConfigureAwait(false);
        result.HasMetadata = true;
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

        KpMovie? series = await GetKpMovieByProviderId(searchInfo, cancellationToken).ConfigureAwait(false);
        if (series != null)
        {
            _logger.LogInformation("Series found by provider ID, Kinopoisk ID: '{SeriesId}'", series.Id);
            var item = new RemoteSearchResult()
            {
                Name = series.Name,
                ImageUrl = (series.Poster?.PreviewUrl ?? series.Poster?.Url) ?? string.Empty,
                SearchProviderName = Plugin.PluginKey,
                ProductionYear = series.Year,
                Overview = PrepareOverview(series),
            };
            item.SetProviderId(Plugin.PluginKey, series.Id.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(series.ExternalId?.Imdb))
            {
                item.ProviderIds.Add(MetadataProvider.Imdb.ToString(), series.ExternalId.Imdb);
            }

            if (series.ExternalId?.Tmdb != null)
            {
                item.ProviderIds.Add(MetadataProvider.Tmdb.ToString(), series.ExternalId.Tmdb.ToString());
            }

            result.Add(item);
            return result;
        }

        _logger.LogInformation("Series was not found by provider ID");

        var name = KpHelper.CleanName(searchInfo.Name);
        _logger.LogInformation("Searching series by name '{Name}' and year '{SearchInfoYear}'", name, searchInfo.Year);
        KpSearchResult<KpMovie> seriesResult = await _api.GetMoviesByMovieDetails(name, searchInfo.Year, cancellationToken).ConfigureAwait(false);
        foreach (KpMovie s in seriesResult.Docs)
        {
            var imageUrl = (s.Poster?.PreviewUrl ?? s.Poster?.Url) ?? string.Empty;
            var item = new RemoteSearchResult()
            {
                Name = s.Name,
                ImageUrl = imageUrl,
                SearchProviderName = Plugin.PluginKey,
                ProductionYear = s.Year,
                Overview = PrepareOverview(s),
            };
            item.SetProviderId(Plugin.PluginKey, s.Id.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(s.ExternalId?.Imdb))
            {
                item.ProviderIds.Add(MetadataProvider.Imdb.ToString(), s.ExternalId.Imdb);
            }

            if (s.ExternalId?.Tmdb != null)
            {
                item.ProviderIds.Add(MetadataProvider.Tmdb.ToString(), s.ExternalId.Tmdb.ToString());
            }

            result.Add(item);
        }

        _logger.LogInformation("By name '{Name}' found {ResultCount} series", name, result.Count);
        return result;
    }

    private Task<Series> CreateSeriesFromKpMovie(KpMovie series, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Series '{SeriesName}' with KinopoiskId '{SeriesId}' found", series.Name, series.Id);

        var seriesId = series.Id.ToString(CultureInfo.InvariantCulture);
        var toReturn = new Series()
        {
            CommunityRating = series.Rating?.Kp,
            EndDate = GetEndDate(series.ReleaseYears),
            ExternalId = seriesId,
            Name = series.Name,
            OfficialRating = series.RatingMpaa,
            OriginalTitle = series.AlternativeName,
            Overview = PrepareOverview(series),
            PremiereDate = KpHelper.GetPremierDate(series.Premiere),
            ProductionLocations = series.Countries?.Select(i => i.Name).ToArray(),
            ProductionYear = series.Year,
            Size = series.MovieLength ?? 0,
            SortName =
                string.IsNullOrWhiteSpace(series.Name) ?
                    string.IsNullOrWhiteSpace(series.AlternativeName) ?
                        string.Empty
                        : series.AlternativeName
                    : series.Name,
            Tagline = series.Slogan
        };

        toReturn.SetProviderId(Plugin.PluginKey, seriesId);

        if (!string.IsNullOrWhiteSpace(series.ExternalId?.Imdb))
        {
            toReturn.ProviderIds.Add(MetadataProvider.Imdb.ToString(), series.ExternalId.Imdb);
        }

        if (series.ExternalId?.Tmdb != null)
        {
            toReturn.ProviderIds.Add(MetadataProvider.Tmdb.ToString(), series.ExternalId.Tmdb.ToString());
        }

        toReturn.Genres = series
            .Genres?
            .Where(i => !string.IsNullOrWhiteSpace(i.Name))
            .Select(i => i.Name!)
            .ToArray();

        IEnumerable<string>? studios = series
            .ProductionCompanies?
            .Where(i => !string.IsNullOrWhiteSpace(i.Name))
            .Select(i => i.Name!)
            .AsEnumerable();
        if (studios != null)
        {
            toReturn.SetStudios(studios);
        }

        series.Videos?.Teasers?
            .Where(i => !string.IsNullOrWhiteSpace(i.Url) && i.Url.Contains("youtube", StringComparison.InvariantCultureIgnoreCase))
            .Select(i => i.Url!
                    .Replace("https://www.youtube.com/embed/", "https://www.youtube.com/watch?v=", StringComparison.InvariantCultureIgnoreCase)
                    .Replace("https://www.youtube.com/v/", "https://www.youtube.com/watch?v=", StringComparison.InvariantCultureIgnoreCase))
            .Reverse()
            .ToList()
            .ForEach(j => toReturn.AddTrailerUrl(j));
        series.Videos?.Trailers?
            .Where(i => !string.IsNullOrWhiteSpace(i.Url) && i.Url.Contains("youtube", StringComparison.InvariantCultureIgnoreCase))
            .Select(i => i.Url!
                    .Replace("https://www.youtube.com/embed/", "https://www.youtube.com/watch?v=", StringComparison.InvariantCultureIgnoreCase)
                    .Replace("https://www.youtube.com/v/", "https://www.youtube.com/watch?v=", StringComparison.InvariantCultureIgnoreCase))
            .Reverse()
            .ToList()
            .ForEach(j => toReturn.AddTrailerUrl(j));

        if (_pluginInstance.Configuration.NeedToCreateSequenceCollection() && series.SequelsAndPrequels.Any())
        {
            // await AddMovieToCollection(toReturn, series, cancellationToken).ConfigureAwait(false);
        }

        return Task.FromResult(toReturn);
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
            _logger.LogDebug($"SeriesProviderId not exists for {Plugin.PluginName}, checking ProviderId");
            seriesId = info.GetProviderId(Plugin.PluginKey);
        }

        if (string.IsNullOrWhiteSpace(seriesId))
        {
            _logger.LogInformation($"The episode doesn't have series id for {Plugin.PluginName}");
            return result;
        }

        if (info.IndexNumber == null || info.ParentIndexNumber == null)
        {
            _logger.LogWarning(
                "Not enough parameters. Season index '{InfoParentIndexNumber}', episode index '{InfoIndexNumber}'",
                info.ParentIndexNumber,
                info.IndexNumber);
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

        KpSeason? kpSeason = item.Docs.FirstOrDefault(s => s.Number == info.ParentIndexNumber);
        if (kpSeason == null)
        {
            _logger.LogInformation("Season with index '{InfoParentIndexNumber}' not found", info.ParentIndexNumber);
            return result;
        }

        KpEpisode? kpEpisode = kpSeason.Episodes?.FirstOrDefault(e => e.Number == info.IndexNumber);
        if (kpEpisode == null)
        {
            _logger.LogInformation("Episode with index '{InfoIndexNumber}' not found", info.IndexNumber);
            return result;
        }

        _ = DateTime.TryParseExact(
            kpEpisode.Date,
            "yyyy-MM-dd'T'HH:mm:ss.fffZ",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime premiereDate);
        result.Item = new Episode()
        {
            Name = kpEpisode.Name,
            OriginalTitle = kpEpisode.EnName,
            Overview = kpEpisode.Description,
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

        _logger.LogInformation("Searching person by name {InfoName}", info.Name);
        KpSearchResult<KpPerson> persons = await _api.GetPersonsByName(info.Name, cancellationToken).ConfigureAwait(false);
        persons.Docs = FilterIrrelevantPersons(persons.Docs, info.Name);
        if (persons.Docs.Count != 1)
        {
            _logger.LogError("Found {PersonsDocsCount} persons, skipping person update", persons.Docs.Count);
            return result;
        }

        result.Item = CreatePersonFromKpPerson(persons.Docs[0]);
        result.HasMetadata = true;
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
                        Name = person.Name,
                        ImageUrl = person.Photo,
                        SearchProviderName = Plugin.PluginKey
                    };
                    item.SetProviderId(Plugin.PluginKey, personId);
                    result.Add(item);
                    return result;
                }

                _logger.LogInformation("Person by id '{PersonId}' not found", personId);
            }
        }

        _logger.LogInformation("Searching person by name {SearchInfoName}", searchInfo.Name);
        KpSearchResult<KpPerson> persons = await _api.GetPersonsByName(searchInfo.Name, cancellationToken).ConfigureAwait(false);
        persons.Docs = FilterIrrelevantPersons(persons.Docs, searchInfo.Name);
        foreach (KpPerson person in persons.Docs)
        {
            var item = new RemoteSearchResult()
            {
                Name = person.Name,
                ImageUrl = person.Photo,
                SearchProviderName = Plugin.PluginKey
            };
            item.SetProviderId(Plugin.PluginKey, person.Id.ToString(CultureInfo.InvariantCulture));
            result.Add(item);
        }

        _logger.LogInformation("By name '{SearchInfoName}' found {ResultCount} persons", searchInfo.Name, result.Count);
        return result;
    }

    private Person CreatePersonFromKpPerson(KpPerson person)
    {
        _logger.LogInformation("Person '{PersonName}' with KinopoiskId '{PersonId}' found", person.Name, person.Id);

        var toReturn = new Person()
        {
            Name = person.Name,
            SortName = person.Name,
            OriginalTitle = person.EnName
        };
        toReturn.ProviderIds.Add(Plugin.PluginKey, person.Id.ToString(CultureInfo.InvariantCulture));
        if (DateTime.TryParse(person.Birthday, out DateTime birthDay))
        {
            toReturn.PremiereDate = birthDay;
        }

        if (DateTime.TryParse(person.Death, out DateTime deathDay))
        {
            toReturn.EndDate = deathDay;
        }

        var birthPlace = person.BirthPlace?.Select(i => i.Value).ToArray();
        if (birthPlace != null && birthPlace.Length > 0 && !string.IsNullOrWhiteSpace(birthPlace[0]))
        {
            toReturn.ProductionLocations = new string[] { string.Join(", ", birthPlace) };
        }

        var facts = person.Facts?.Select(i => i.Value).ToArray();
        if (facts != null && facts.Length > 0 && !string.IsNullOrWhiteSpace(facts[0]))
        {
            toReturn.Overview = string.Join("\n", facts);
        }

        return toReturn;
    }

    private List<KpPerson> FilterIrrelevantPersons(List<KpPerson> list, string name)
    {
        _logger.LogInformation("Filtering out irrelevant persons");
        if (list.Count > 1)
        {
            var toReturn = list
                .Where(m => KpHelper.CleanName(m.Name) == KpHelper.CleanName(name)
                    || KpHelper.CleanName(m.EnName) == KpHelper.CleanName(name))
                .ToList();
            if (toReturn.Count > 1)
            {
                toReturn = toReturn
                    .Where(m => !string.IsNullOrWhiteSpace(m.Photo))
                    .ToList();
            }

            return toReturn.Any() ? toReturn : list;
        }
        else
        {
            return list;
        }
    }

    #endregion

    #region MovieImageProvider
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
            var movieId = item.ProviderIds[Plugin.PluginKey];
            if (!string.IsNullOrWhiteSpace(movieId))
            {
                _logger.LogInformation("Searching images by movie id '{MovieId}'", movieId);
                KpMovie? movie = await _api.GetMovieById(movieId, cancellationToken).ConfigureAwait(false);
                if (movie != null)
                {
                    UpdateRemoteImageInfoList(movie, result);
                    return result;
                }

                _logger.LogInformation("Images by movie id '{MovieId}' not found", movieId);
            }
        }

        var name = KpHelper.CleanName(item.Name);
        var originalTitle = KpHelper.CleanName(item.OriginalTitle);
        _logger.LogInformation(
            "Searching images by name: '{Name}', originalTitle: '{OriginalTitle}', productionYear: '{ItemProductionYear}'",
            name,
            originalTitle,
            item.ProductionYear);
        KpSearchResult<KpMovie> movies = await _api.GetMoviesByMovieDetails(name, originalTitle, item.ProductionYear, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Filtering out irrelevant films");
        List<KpMovie> relevantSeries = FilterRelevantItems(movies.Docs, name, item.ProductionYear, originalTitle);
        if (relevantSeries.Count != 1)
        {
            _logger.LogError("Found {RelevantSeriesCount} movies, skipping image update", relevantSeries.Count);
            return result;
        }

        UpdateRemoteImageInfoList(relevantSeries[0], result);
        return result;
    }

    private void UpdateRemoteImageInfoList(KpMovie movie, List<RemoteImageInfo> toReturn)
    {
        if (!string.IsNullOrWhiteSpace(movie.Backdrop?.Url))
        {
            toReturn.Add(new RemoteImageInfo()
            {
                ProviderName = Plugin.PluginKey,
                Url = movie.Backdrop.Url,
                ThumbnailUrl = movie.Backdrop.PreviewUrl,
                Language = "ru",
                Type = ImageType.Backdrop
            });
        }

        if (!string.IsNullOrWhiteSpace(movie.Poster?.Url))
        {
            toReturn.Add(new RemoteImageInfo()
            {
                ProviderName = Plugin.PluginKey,
                Url = movie.Poster.Url,
                ThumbnailUrl = movie.Poster.PreviewUrl,
                Language = "ru",
                Type = ImageType.Primary
            });
        }

        if (!string.IsNullOrWhiteSpace(movie.Logo?.Url))
        {
            toReturn.Add(new RemoteImageInfo()
            {
                ProviderName = Plugin.PluginKey,
                Url = movie.Logo.Url,
                ThumbnailUrl = movie.Logo.PreviewUrl,
                Language = "ru",
                Type = ImageType.Logo
            });
        }

        var imageTypes = string.Join(", ", toReturn.Select(i => i.Type).ToList());
        _logger.LogInformation("By movie id '{MovieId}' found '{ImageTypes}' image types", movie.Id, imageTypes);
    }
    #endregion

    #region Common
    private static DateTime? GetEndDate(List<KpYearRange> releaseYears)
    {
        if (releaseYears == null || releaseYears.Count == 0)
        {
            return null;
        }

        var max = 0;
        releaseYears
            .Where(i => i.End != null)
            .ToList()
            .ForEach(i => max = Math.Max(max, (int)i.End!));

        return DateTime.TryParseExact(
                        max.ToString(CultureInfo.InvariantCulture),
                        "yyyy",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime result)
            ? result
            : null;
    }

    private async Task UpdatePersonsList<T>(MetadataResult<T> result, List<KpPersonMovie> persons, CancellationToken cancellationToken)
        where T : BaseItem
    {
        var seriesId = result.Item.GetProviderId(Plugin.PluginKey);
        if (seriesId == null)
        {
            _logger.LogWarning("ProviderId is null. Unable to update persons list");
            return;
        }

        if (persons == null)
        {
            _logger.LogWarning("Received persons list is null for video with id '{SeriesId}'", seriesId);
            return;
        }

        _logger.LogInformation("Updating persons list of the video with id '{SeriesId}'", seriesId);
        var movieName = result.Item.Name;
        KpSearchResult<KpPerson> personsByVideoId = await _api.GetPersonsByMovieId(seriesId, cancellationToken).ConfigureAwait(false);
        personsByVideoId.Docs
            .ForEach(a =>
                a.Movies?.RemoveAll(b =>
                    b.Id.ToString(CultureInfo.InvariantCulture) != seriesId
                        || string.IsNullOrWhiteSpace(b.Description)));

        var idRoleDictionary = personsByVideoId.Docs
            .ToDictionary(
                c => c.Id,
                c => c.Movies?.FirstOrDefault()?.Description);

        var seriesName = result.Item.Name;
        foreach (KpPersonMovie kpPerson in persons)
        {
            var personType = KpHelper.GetPersonType(kpPerson.EnProfession);
            var name = string.IsNullOrWhiteSpace(kpPerson.Name) ? kpPerson.EnName : kpPerson.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("Skip adding staff with id '{KpPersonId}' as nameless to '{MovieName}'", kpPerson.Id, movieName);
            }
            else if (personType == null)
            {
                _logger.LogWarning("Skip adding {Name} as '{KpPersonEnProfession}' to {SeriesName}", name, kpPerson.EnProfession, seriesName);
            }
            else
            {
                _logger.LogDebug("Adding {Name} as '{PersonType}' to {SeriesName}", name, personType, seriesName);
                _ = idRoleDictionary.TryGetValue(kpPerson.Id, out var role);
                var person = new PersonInfo()
                {
                    Name = name,
                    ImageUrl = kpPerson.Photo,
                    Type = personType,
                    Role = role,
                };
                person.SetProviderId(Plugin.PluginKey, kpPerson.Id.ToString(CultureInfo.InvariantCulture));

                result.AddPerson(person);
            }
        }

        _logger.LogInformation(
            "Added {ResultPeopleCount} persons to the video with id '{SeriesId}'",
            result.People.Count,
            seriesId);
    }

    // private async Task AddMovieToCollection(BaseItem toReturn, KpMovie movie, CancellationToken cancellationToken)
    // {
    //     _logger.LogInformation("Adding '{ToReturnName}' to collection", toReturn.Name);

    //     // todo: fix me
    //     CollectionFolder? rootCollectionFolder = await JellyfinHelper.InsureCollectionLibraryFolder(_libraryManager, _log).ConfigureAwait(false);
    //     if (rootCollectionFolder == null)
    //     {
    //         _logger.LogInformation("The virtual folder 'Collections' was not found nor created");
    //         return;
    //     }

    //     // Get publicIds of each object in sequence
    //     var itemsToAdd = movie.SequelsAndPrequels
    //         .Select(seq => seq.Id)
    //         .ToList();
    //     List<BaseItem> publicCollectionItems = await JellyfinHelper.GetSequenceInternalIds(itemsToAdd, _libraryManager, _log, _api, cancellationToken).ConfigureAwait(false);
    //     var publicIdArray = publicCollectionItems
    //         .Select(item => item.publicId)
    //         .ToList();

    //     BoxSet collection = JellyfinHelper.SearchExistingCollection(publicIdArray, _libraryManager, _log);
    //     if (collection == null && publicCollectionItems.Count > 0)
    //     {
    //         var newCollectionName = GetNewCollectionName(movie);
    //         if (string.IsNullOrWhiteSpace(newCollectionName))
    //         {
    //             _logger.LogWarning("New collection has no name, skip creation");
    //             return;
    //         }
    //         _logger.LogInformation("Creating '{newCollectionName}' collection with following items: '{string.Join("', '", publicCollectionItems.Select(m => m.Name))}'");
    //         collection = await _collectionManager.CreateCollection(new CollectionCreationOptions()
    //         {
    //             IsLocked = false,
    //             Name = newCollectionName,
    //             ParentId = rootCollectionFolder.publicId,
    //             ItemIdList = publicIdArray.ToArray()
    //         });
    //         _ = toReturn.AddCollection(collection);
    //     }
    //     else if (collection != null && publicCollectionItems.Count > 0)
    //     {
    //         _logger.LogInformation("Updating '{collection.Name}' collection with following items: '{string.Join("', '", publicCollectionItems.Select(m => m.Name))}'");
    //         foreach (BaseItem item in publicCollectionItems)
    //         {
    //             if (item.AddCollection(collection))
    //             {
    //                 _logger.LogInformation("Adding '{item.Name}' to collection '{collection.Name}'");
    //                 item.UpdateToRepository(ItemUpdateType.MetadataEdit);
    //             }
    //             else
    //             {
    //                 _logger.LogInformation("'{item.Name}' already in the collection '{collection.Name}'");
    //             }
    //         }
    //         _ = toReturn.AddCollection(collection);
    //     }
    //     else
    //     {
    //         _logger.LogInformation("No other films were found in Emby, collection was not created");
    //     }

    //     _logger.LogInformation("Finished adding to collection");
    // }

    private static string? GetNewCollectionName(KpMovie movie)
    {
        var itemsList = movie.SequelsAndPrequels
            .Where(s => !string.IsNullOrWhiteSpace(s.Name))
            .Select(s => (s.Id, s.Name!))
            .ToList();
        itemsList.Add((movie.Id, movie.Name!));
        return itemsList
            .OrderBy(m => m.Id)
            .Select(m => m.Item2)
            .FirstOrDefault();
    }

    private static string? PrepareOverview(KpMovie movie)
    {
        var subj = "<br/><br/><b>Интересное:</b><br/>";
        StringBuilder sb = new(subj);
        movie.Facts?
            .Where(f => !f.Spoiler && "FACT".Equals(f.Type, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .ForEach(f => sb.Append("&nbsp;&nbsp;&nbsp;&nbsp;* ").Append(f.Value).Append("<br/>"));

        return (sb.Length == subj.Length)
            ? movie.Description
            : sb.Insert(0, movie.Description).ToString();
    }

    private List<KpMovie> FilterRelevantItems(List<KpMovie> list, string? name, int? year, string? alternativeName)
    {
        _logger.LogInformation("Filtering out irrelevant items");
        if (list.Count > 1)
        {
            var toReturn = list
                .Where(m =>
                       KpHelper.CleanName(m.Name) == KpHelper.CleanName(name)
                    || KpHelper.CleanName(m.Name) == KpHelper.CleanName(alternativeName)
                    || KpHelper.CleanName(m.AlternativeName) == KpHelper.CleanName(name)
                    || KpHelper.CleanName(m.AlternativeName) == KpHelper.CleanName(alternativeName))
                .Where(m => year == null || m.Year == year)
                .ToList();
            return toReturn.Any() ? toReturn : list;
        }
        else
        {
            return list;
        }
    }

    private async Task<KpMovie?> GetKpMovieByProviderId(ItemLookupInfo info, CancellationToken cancellationToken)
    {
        if (info.HasProviderId(Plugin.PluginKey) && !string.IsNullOrWhiteSpace(info.GetProviderId(Plugin.PluginKey)))
        {
            var movieId = info.GetProviderId(Plugin.PluginKey);
            _logger.LogInformation("Searching Kp movie by id '{MovieId}'", movieId);
            return await _api.GetMovieById(movieId!, cancellationToken).ConfigureAwait(false);
        }

        if (info.HasProviderId(MetadataProvider.Imdb) && !string.IsNullOrWhiteSpace(info.GetProviderId(MetadataProvider.Imdb)))
        {
            var imdbMovieId = info.GetProviderId(MetadataProvider.Imdb)!;
            _logger.LogInformation("Searching Kp movie by {MetadataProviderImdb} id '{ImdbMovieId}'", MetadataProvider.Imdb, imdbMovieId);
            ApiResult<Dictionary<string, long>> apiResult = await GetKpIdByAnotherId(MetadataProvider.Imdb.ToString(), new List<string>() { imdbMovieId }, cancellationToken).ConfigureAwait(false);
            if (apiResult.HasError || apiResult.Item.Count != 1)
            {
                _logger.LogInformation("Failed to get Kinopoisk ID by {MetadataProviderImdb} ID '{ImdbMovieId}'", MetadataProvider.Imdb, imdbMovieId);
            }
            else
            {
                var movieId = apiResult.Item[imdbMovieId].ToString(CultureInfo.InvariantCulture);
                _logger.LogInformation(
                    "Kinopoisk ID is '{MovieId}' for {MetadataProviderImdb} ID '{ImdbMovieId}'",
                    movieId,
                    MetadataProvider.Imdb,
                    imdbMovieId);
                return await _api.GetMovieById(movieId, cancellationToken).ConfigureAwait(false);
            }
        }

        if (info.HasProviderId(MetadataProvider.Tmdb) && !string.IsNullOrWhiteSpace(info.GetProviderId(MetadataProvider.Tmdb)))
        {
            var tmdbMovieId = info.GetProviderId(MetadataProvider.Tmdb)!;
            _logger.LogInformation("Searching Kp movie by {MetadataProviderTmdb} id '{TmdbMovieId}'", MetadataProvider.Tmdb, tmdbMovieId);
            ApiResult<Dictionary<string, long>> apiResult = await GetKpIdByAnotherId(MetadataProvider.Tmdb.ToString(), new List<string>() { tmdbMovieId }, cancellationToken).ConfigureAwait(false);
            if (apiResult.HasError || apiResult.Item.Count != 1)
            {
                _logger.LogInformation("Failed to get Kinopoisk ID by {MetadataProviderTmdb} ID '{TmdbMovieId}'", MetadataProvider.Tmdb, tmdbMovieId);
            }
            else
            {
                var movieId = apiResult.Item[tmdbMovieId].ToString(CultureInfo.InvariantCulture);
                _logger.LogInformation(
                    "Kinopoisk ID is '{MovieId}' for {MetadataProviderTmdb} ID '{TmdbMovieId}'",
                    movieId,
                    MetadataProvider.Tmdb,
                    tmdbMovieId);
                return await _api.GetMovieById(movieId, cancellationToken).ConfigureAwait(false);
            }
        }

        return null;
    }

    #endregion

    #region Scheduled Tasks
    public async Task<List<BaseItem>> GetTop250MovieCollection(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Get Top250 Movies Collection");
        var toReturn = new List<BaseItem>();
        KpSearchResult<KpMovie>? movies = await _api.GetTop250Collection(cancellationToken).ConfigureAwait(false);
        movies?.Docs
            .Where(m => _movieTypes.Contains(m.TypeNumber))
            .ToList()
            .ForEach(m =>
            {
                var movie = new Movie()
                {
                    Name = m.Name,
                    OriginalTitle = m.AlternativeName
                };
                movie.SetProviderId(Plugin.PluginKey, m.Id.ToString(CultureInfo.InvariantCulture));
                if (!string.IsNullOrWhiteSpace(m.ExternalId?.Imdb))
                {
                    movie.SetProviderId(MetadataProvider.Imdb.ToString(), m.ExternalId.Imdb);
                }

                if (m.ExternalId?.Tmdb != null && m.ExternalId.Tmdb > 0)
                {
                    movie.SetProviderId(MetadataProvider.Tmdb.ToString(), m.ExternalId.Tmdb.ToString());
                }

                toReturn.Add(movie);
            });
        return toReturn;
    }

    public async Task<List<BaseItem>> GetTop250SeriesCollection(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Get Top250 Series Collection");
        var toReturn = new List<BaseItem>();
        KpSearchResult<KpMovie>? movies = await _api.GetTop250Collection(cancellationToken).ConfigureAwait(false);
        movies?.Docs
            .Where(m => !_movieTypes.Contains(m.TypeNumber))
            .ToList()
            .ForEach(m =>
            {
                var series = new Series()
                {
                    Name = m.Name,
                    OriginalTitle = m.AlternativeName
                };
                series.SetProviderId(Plugin.PluginKey, m.Id.ToString(CultureInfo.InvariantCulture));
                if (!string.IsNullOrWhiteSpace(m.ExternalId?.Imdb))
                {
                    series.SetProviderId(MetadataProvider.Imdb.ToString(), m.ExternalId.Imdb);
                }

                if (m.ExternalId?.Tmdb != null && m.ExternalId.Tmdb > 0)
                {
                    series.SetProviderId(MetadataProvider.Tmdb.ToString(), m.ExternalId.Tmdb.ToString());
                }

                toReturn.Add(series);
            });
        return toReturn;
    }

    public async Task<ApiResult<Dictionary<string, long>>> GetKpIdByAnotherId(string externalIdType, List<string> idList, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Search Kinopoisk ID for {IdListCount} items by {ExternalIdType} provider", idList.Count, externalIdType);
        KpSearchResult<KpMovie>? movies = await _api.GetKpIdByAnotherId(externalIdType, idList, cancellationToken).ConfigureAwait(false);
        if (movies == null || movies.HasError)
        {
            _logger.LogInformation("Failed to get Kinopoisk ID by {ExternalIdType} provider", externalIdType);
            return new ApiResult<Dictionary<string, long>>(new Dictionary<string, long>())
            {
                HasError = true
            };
        }

        _logger.LogInformation(
            "Found {MoviesDocsCount} Kinopoisk IDs for {IdListCount} items by {ExternalIdType} provider",
            movies.Docs.Count,
            idList.Count,
            externalIdType);

        if (MetadataProvider.Imdb.ToString() == externalIdType)
        {
            return new ApiResult<Dictionary<string, long>>(movies.Docs
                .Where(m => !string.IsNullOrWhiteSpace(m.ExternalId?.Imdb))
                .Select(m => new KeyValuePair<string, long>(m.ExternalId!.Imdb!, m.Id))
                .Distinct(new KeyValuePairComparer())
                .ToDictionary(
                    m => m.Key,
                    m => m.Value));
        }

        if (MetadataProvider.Tmdb.ToString() == externalIdType)
        {
            return new ApiResult<Dictionary<string, long>>(movies.Docs
                .Where(m => m.ExternalId?.Tmdb != null && m.ExternalId.Tmdb > 0)
                .Select(m => new KeyValuePair<string, long>(m.ExternalId!.Tmdb!.ToString()!, m.Id))
                .Distinct(new KeyValuePairComparer())
                .ToDictionary(
                    m => m.Key,
                    m => m.Value));
        }

        _logger.LogInformation("Not supported provider: '{ExternalIdType}'", externalIdType);
        return new ApiResult<Dictionary<string, long>>(new Dictionary<string, long>())
        {
            HasError = true
        };
    }

    #endregion
}
