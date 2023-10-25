using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskApiUnofficial.Model;
using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskApiUnofficial.Model.Film;
using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskApiUnofficial.Model.Person;
using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskApiUnofficial.Model.Season;
using Jellyfin.Plugin.KinopoiskRu.Helper;

using MediaBrowser.Model.Activity;

namespace Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskApiUnofficial;

internal sealed class KinopoiskUnofficialApi
{
    private readonly ILogger<KinopoiskUnofficialApi> _logger;
    private readonly IActivityManager _activityManager;
    private readonly Plugin _pluginInstance;

    internal KinopoiskUnofficialApi(ILoggerFactory loggerFactory, IActivityManager activityManager)
    {
        _logger = loggerFactory.CreateLogger<KinopoiskUnofficialApi>();
        _activityManager = activityManager;
        _pluginInstance = Plugin.Instance!;
    }

    internal async Task<KpFilm?> GetFilmById(string? movieId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(movieId))
        {
            _logger.LogWarning("MovieId for GetFilmById was empty");
            return null;
        }

        var url = $"https://kinopoiskapiunofficial.tech/api/v2.2/films/{movieId}";
        var response = await SendRequest(url, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrEmpty(response) ? null : JsonHelper.Deserialize<KpFilm>(response);
    }

    internal async Task<KpSearchResult<KpFilm>> GetFilmsByNameAndYear(string name, int? year, CancellationToken cancellationToken)
    {
        var hasName = !string.IsNullOrWhiteSpace(name);
        var hasYear = year != null && year > 1000;
        var url = "https://kinopoiskapiunofficial.tech/api/v2.2/films";
        var namePart = $"?keyword={name}";
        var yearPart = $"&yearFrom={year}&yearTo={year}";

        if (hasYear && hasName)
        {
            var request = url + namePart + yearPart;
            var response = await SendRequest(request, cancellationToken).ConfigureAwait(false);
            KpSearchResult<KpFilm>? toReturn = JsonHelper.Deserialize<KpSearchResult<KpFilm>>(response);
            if (toReturn != null && toReturn.Items.Count > 0)
            {
                _logger.LogInformation("Found {ToReturnItemsCount} movies", toReturn.Items.Count);
                return toReturn;
            }
        }

        if (hasName)
        {
            var request = url + namePart;
            var response = await SendRequest(request, cancellationToken).ConfigureAwait(false);
            KpSearchResult<KpFilm>? toReturn = JsonHelper.Deserialize<KpSearchResult<KpFilm>>(response);
            if (toReturn != null && toReturn.Items.Count > 0)
            {
                _logger.LogInformation("Found {ToReturnItemsCount} movies", toReturn.Items.Count);
                return toReturn;
            }
        }

        return new KpSearchResult<KpFilm>();
    }

    internal async Task<List<KpFilmStaff>?> GetStaffByFilmId(string movieId, CancellationToken cancellationToken)
    {
        var url = $"https://kinopoiskapiunofficial.tech/api/v1/staff?filmId={movieId}";
        var response = await SendRequest(url, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrEmpty(response) ? null : JsonHelper.Deserialize<List<KpFilmStaff>>(response);
    }

    internal async Task<KpSearchResult<KpVideo>?> GetVideosByFilmId(string movieId, CancellationToken cancellationToken)
    {
        var url = $"https://kinopoiskapiunofficial.tech/api/v2.2/films/{movieId}/videos";
        var response = await SendRequest(url, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrEmpty(response) ? null : JsonHelper.Deserialize<KpSearchResult<KpVideo>>(response);
    }

    internal async Task<KpSearchResult<KpSeason>?> GetEpisodesBySeriesId(string seriesId, CancellationToken cancellationToken)
    {
        var url = $"https://kinopoiskapiunofficial.tech/api/v2.2/films/{seriesId}/seasons";
        var response = await SendRequest(url, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrEmpty(response) ? null : JsonHelper.Deserialize<KpSearchResult<KpSeason>>(response);
    }

    internal async Task<KpPerson?> GetPersonById(string personId, CancellationToken cancellationToken)
    {
        var url = $"https://kinopoiskapiunofficial.tech/api/v1/staff/{personId}";
        var response = await SendRequest(url, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrEmpty(response) ? null : JsonHelper.Deserialize<KpPerson>(response);
    }

    internal async Task<KpSearchResult<KpStaff>?> GetPersonsByName(string name, CancellationToken cancellationToken)
    {
        var url = $"https://kinopoiskapiunofficial.tech/api/v1/persons?name={name}";
        var response = await SendRequest(url, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrEmpty(response) ? null : JsonHelper.Deserialize<KpSearchResult<KpStaff>>(response);
    }

    internal async Task<KpSearchResult<KpFilm>> GetFilmByImdbId(string? imdbMovieId, CancellationToken cancellationToken)
    {
        var hasImdb = !string.IsNullOrWhiteSpace(imdbMovieId);
        var request = $"https://kinopoiskapiunofficial.tech/api/v2.2/films?imdbId={imdbMovieId}";

        if (hasImdb)
        {
            var response = await SendRequest(request, cancellationToken).ConfigureAwait(false);
            KpSearchResult<KpFilm>? toReturn = JsonHelper.Deserialize<KpSearchResult<KpFilm>>(response);
            if (toReturn != null && toReturn.Items.Count > 0)
            {
                _logger.LogInformation("Found {ToReturnItemsCount} items", toReturn.Items.Count);
                return toReturn;
            }
        }

        return new KpSearchResult<KpFilm>();
    }

    private async Task<string> SendRequest(string url, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending request to {Url}", url);
        var token = Plugin.Instance?.Configuration.GetCurrentToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogError("The token is empty. Skip request");
            return string.Empty;
        }

        using HttpRequestMessage httpRequest = new(HttpMethod.Get, url);
        httpRequest.Headers.Add("X-API-KEY", token);
        try
        {
            using HttpResponseMessage responseMessage = await _pluginInstance.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            var content = await responseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            switch ((int)responseMessage.StatusCode)
            {
                case int n when n >= 200 && n < 300:
                    _logger.LogInformation("Received response: '{Content}'", content);
                    return content;
                case 401:
                    _logger.LogError("Token is invalid: '{Token}'", token);
                    await AddToActivityLog($"Token is invalid: '{token}'", "Token is invalid").ConfigureAwait(false);
                    return string.Empty;
                case 402:
                    _logger.LogWarning("Request limit exceeded (either daily or total) for current token");
                    await AddToActivityLog("Request limit exceeded (either daily or total) for current token", "Request limit exceeded").ConfigureAwait(false);
                    return string.Empty;
                case 404:
                    _logger.LogInformation("Data not found for URL: {Url}", url);
                    return string.Empty;
                case 429:
                    _logger.LogInformation("Too many requests per second. Waiting 2 sec");
                    await Task.Delay(2000, cancellationToken).ConfigureAwait(false);
                    return await SendRequest(url, cancellationToken).ConfigureAwait(false);
                default:
                    _logger.LogError("Received '{ResponseMessageStatusCode}' from API: {Content}", responseMessage.StatusCode, content);
                    return string.Empty;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                "Received '{ExStatusCode}' from API: '{ExMessage}'",
                ex.StatusCode,
                ex.Message);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to fetch data from URL '{Url}' due to {ExMessage}", url, ex.Message);
            return string.Empty;
        }
    }

    private async Task AddToActivityLog(string overview, string shortOverview)
    {
        await KpHelper.AddToActivityLog(_activityManager, overview, shortOverview).ConfigureAwait(false);
    }
}
