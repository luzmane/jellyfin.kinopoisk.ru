using System.Globalization;
using System.Text;

using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskDev.Model;
using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskDev.Model.Movie;
using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskDev.Model.Person;
using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskDev.Model.Season;
using Jellyfin.Plugin.KinopoiskRu.Helper;

using MediaBrowser.Model.Activity;

namespace Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskDev;

internal sealed class KinopoiskDevApi
{
    private readonly ILogger<KinopoiskDevApi> _logger;
    private readonly IActivityManager _activityManager;
    private readonly Plugin _pluginInstance;

    internal KinopoiskDevApi(ILoggerFactory loggerFactory, IActivityManager activityManager)
    {
        _logger = loggerFactory.CreateLogger<KinopoiskDevApi>();
        _activityManager = activityManager;
        _pluginInstance = Plugin.Instance!;
    }

    internal async Task<KpMovie?> GetMovieById(string movieId, CancellationToken cancellationToken)
    {
        var json = await SendRequest($"https://api.kinopoisk.dev/v1.3/movie/{movieId}", cancellationToken).ConfigureAwait(false);
        return JsonHelper.Deserialize<KpMovie>(json);
    }

    internal async Task<KpSearchResult<KpMovie>?> GetMoviesByIds(List<string> movieIdList, CancellationToken cancellationToken)
    {
        if (!movieIdList.Any())
        {
            _logger.LogInformation("GetMoviesByIds - received empty id's list");
            return new KpSearchResult<KpMovie>();
        }

        var url = new StringBuilder("https://api.kinopoisk.dev/v1.3/movie?")
            .Append(CultureInfo.InvariantCulture, $"id={string.Join("&id=", movieIdList)}")
            .Append(CultureInfo.InvariantCulture, $"&limit={movieIdList.Count}")
            .Append("&selectFields=alternativeName backdrop countries description enName externalId genres id logo movieLength name persons poster premiere productionCompanies rating ratingMpaa slogan videos year sequelsAndPrequels top250 facts releaseYears seasonsInfo")
            .ToString();
        var json = await SendRequest(url, cancellationToken).ConfigureAwait(false);
        return JsonHelper.Deserialize<KpSearchResult<KpMovie>>(json);
    }

    internal async Task<KpSearchResult<KpMovie>> GetMoviesByMovieDetails(string? name, int? year, CancellationToken cancellationToken)
    {
        return await GetMoviesByMovieDetails(name, name, year, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<KpSearchResult<KpMovie>> GetMoviesByMovieDetails(string? name, string? alternativeName, int? year, CancellationToken cancellationToken)
    {
        var hasName = !string.IsNullOrWhiteSpace(name);
        var hasYear = year != null && year > 1000;
        var hasAlternativeName = !string.IsNullOrWhiteSpace(alternativeName);
        var url = "https://api.kinopoisk.dev/v1.3/movie?limit=50";
        var selectFields = "&selectFields=alternativeName backdrop countries description enName externalId genres id logo movieLength name persons poster premiere productionCompanies rating ratingMpaa slogan videos year sequelsAndPrequels top250 facts releaseYears seasonsInfo";
        var namePart = $"&name={name}";
        var alternativeNamePart = $"&alternativeName={alternativeName}";
        var yearPart = $"&year={year}";

        if (hasName && hasYear)
        {
            var request = url + namePart + yearPart + selectFields;
            var json = await SendRequest(request, cancellationToken).ConfigureAwait(false);
            KpSearchResult<KpMovie>? toReturn = JsonHelper.Deserialize<KpSearchResult<KpMovie>>(json);
            if (toReturn?.Docs.Count > 0)
            {
                _logger.LogInformation("Found {ToReturnDocsCount} items", toReturn.Docs.Count);
                return toReturn;
            }
        }

        if (hasName)
        {
            var request = url + namePart + selectFields;
            var json = await SendRequest(request, cancellationToken).ConfigureAwait(false);
            KpSearchResult<KpMovie>? toReturn = JsonHelper.Deserialize<KpSearchResult<KpMovie>>(json);
            if (toReturn?.Docs.Count > 0)
            {
                _logger.LogInformation("Found {ToReturnDocsCount} items", toReturn.Docs.Count);
                return toReturn;
            }
        }

        if (hasAlternativeName && hasYear)
        {
            var request = url + alternativeNamePart + yearPart + selectFields;
            var json = await SendRequest(request, cancellationToken).ConfigureAwait(false);
            KpSearchResult<KpMovie>? toReturn = JsonHelper.Deserialize<KpSearchResult<KpMovie>>(json);
            if (toReturn?.Docs.Count > 0)
            {
                _logger.LogInformation("Found {ToReturnDocsCount} items", toReturn.Docs.Count);
                return toReturn;
            }
        }

        if (hasAlternativeName)
        {
            var request = url + alternativeNamePart + selectFields;
            var json = await SendRequest(request, cancellationToken).ConfigureAwait(false);
            KpSearchResult<KpMovie>? toReturn = JsonHelper.Deserialize<KpSearchResult<KpMovie>>(json);
            if (toReturn?.Docs.Count > 0)
            {
                _logger.LogInformation("Found {ToReturnDocsCount} items", toReturn.Docs.Count);
                return toReturn;
            }
        }

        return new KpSearchResult<KpMovie>();
    }

    internal async Task<KpSearchResult<KpMovie>?> GetTop250Collection(CancellationToken cancellationToken)
    {
        var request = $"https://api.kinopoisk.dev/v1.3/movie?";
        request += "limit=1000&top250=!null";
        request += "&selectFields=alternativeName externalId id name top250 typeNumber";
        var json = await SendRequest(request, cancellationToken).ConfigureAwait(false);
        return JsonHelper.Deserialize<KpSearchResult<KpMovie>>(json);
    }

    internal async Task<KpPerson?> GetPersonById(string personId, CancellationToken cancellationToken)
    {
        var json = await SendRequest($"https://api.kinopoisk.dev/v1/person/{personId}", cancellationToken).ConfigureAwait(false);
        return JsonHelper.Deserialize<KpPerson>(json);
    }

    internal async Task<KpSearchResult<KpPerson>> GetPersonsByName(string name, CancellationToken cancellationToken)
    {
        var url = $"https://api.kinopoisk.dev/v1/person?";
        url += "selectFields=id name enName photo birthday death birthPlace deathPlace facts";
        var namePart = $"&name={name}";
        var enNamePart = $"&enName={name}";

        var json = await SendRequest(url + namePart, cancellationToken).ConfigureAwait(false);
        KpSearchResult<KpPerson>? toReturn = JsonHelper.Deserialize<KpSearchResult<KpPerson>>(json);
        if (toReturn?.Docs.Count > 0)
        {
            _logger.LogInformation("Found {ToReturnDocsCount} persons by '{Name}'", toReturn.Docs.Count, name);
            return toReturn;
        }

        json = await SendRequest(url + enNamePart, cancellationToken).ConfigureAwait(false);
        toReturn = JsonHelper.Deserialize<KpSearchResult<KpPerson>>(json);
        if (toReturn?.Docs.Count > 0)
        {
            _logger.LogInformation("Found {ToReturnDocsCount} persons by '{EnNamePart}'", toReturn.Docs.Count, enNamePart);
            return toReturn;
        }

        return new KpSearchResult<KpPerson>();
    }

    internal async Task<KpSearchResult<KpPerson>> GetPersonsByMovieId(string movieId, CancellationToken cancellationToken)
    {
        var url = new StringBuilder("https://api.kinopoisk.dev/v1/person?")
             .Append(CultureInfo.InvariantCulture, $"movies.id={movieId}")
             .Append("&selectFields=id movies&limit=1000")
             .ToString();
        var json = await SendRequest(url, cancellationToken).ConfigureAwait(false);
        KpSearchResult<KpPerson>? toReturn = JsonHelper.Deserialize<KpSearchResult<KpPerson>>(json);
        if (toReturn?.Docs.Count > 0)
        {
            _logger.LogInformation("Found {ToReturnDocsCount} persons", toReturn.Docs.Count);
            return toReturn;
        }

        return new KpSearchResult<KpPerson>();
    }

    internal async Task<KpSearchResult<KpSeason>?> GetEpisodesBySeriesId(string seriesId, CancellationToken cancellationToken)
    {
        var url = $"https://api.kinopoisk.dev/v1/season?movieId={seriesId}&limit=50";
        var json = await SendRequest(url, cancellationToken).ConfigureAwait(false);
        return JsonHelper.Deserialize<KpSearchResult<KpSeason>>(json);
    }

    internal async Task<KpSearchResult<KpMovie>?> GetKpIdByAnotherId(string externalIdType, IEnumerable<string> idList, CancellationToken cancellationToken)
    {
        if (!idList.Any())
        {
            _logger.LogInformation("Received ids list is empty");
            return new KpSearchResult<KpMovie>();
        }

        var request = $"https://api.kinopoisk.dev/v1.3/movie?";
        request += $"selectFields=externalId.{externalIdType.ToLowerInvariant()} id&limit=1000";
        var delimeter = $"&externalId.{externalIdType.ToLowerInvariant()}=";
        request += $"{delimeter}{string.Join(delimeter, idList)}";
        var json = await SendRequest(request, cancellationToken).ConfigureAwait(false);
        var hasError = json.Length == 0;
        return hasError
            ? new KpSearchResult<KpMovie>() { HasError = true }
            : JsonHelper.Deserialize<KpSearchResult<KpMovie>>(json);
    }

    private async Task<string> SendRequest(string url, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending request to {Url}", url);
        var token = _pluginInstance.Configuration.GetCurrentToken();
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
                case 403:
                    _logger.LogWarning("Request limit exceeded (either daily or total) for current token");
                    await AddToActivityLog("Request limit exceeded (either daily or total) for current token", "Request limit exceeded").ConfigureAwait(false);
                    return string.Empty;
                default:
                    KpErrorResponse? error = JsonHelper.Deserialize<KpErrorResponse>(content);
                    _logger.LogError(
                        "Received '{ResponseMessageStatusCode}' from API: Error-'{ErrorError}', Message-'{ErrorMessage}'",
                        responseMessage.StatusCode,
                        error?.Error,
                        error?.Message);
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
