using System.Text.Json;

using FluentAssertions;

using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskApiUnofficial.Model.Film;

namespace Jellyfin.Plugin.KinopoiskRu.Tests.Api.KinopoiskUnofficial;

public class KinopoiskUnofficialApiTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string KINOPOISK_UNOFFICIAL_TOKEN = "0f162131-81c1-4979-b46c-3eea4263fb11";
    private static readonly NLog.ILogger Logger = NLog.LogManager.GetCurrentClassLogger();


    public KinopoiskUnofficialApiTests()
    {
        _httpClient = new();
        _httpClient.DefaultRequestHeaders.Add("X-API-KEY", GetKinopoiskUnofficialToken());

        _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    }


    [Fact]
    public async Task GetFilm()
    {
        var request = $"https://kinopoiskapiunofficial.tech/api/v2.2/films/326";
        using HttpResponseMessage responseMessage = await _httpClient.GetAsync(new Uri(request)).ConfigureAwait(false);
        _ = responseMessage.EnsureSuccessStatusCode();
        var response = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
        KpFilm? film = JsonSerializer.Deserialize<KpFilm>(response, _jsonOptions);
        film.Should().NotBeNull("");
        film!.Countries?.Count.Should().Be(0);
        film.CoverUrl.Should().Be("https://avatars.mds.yandex.net/get-ott/1652588/2a00000186aca5e13ea6cec11d584ac5455b/orig");
        film.Description.Should().Be("Бухгалтер Энди Дюфрейн обвинён в убийстве собственной жены и её любовника. Оказавшись в тюрьме под названием Шоушенк, он сталкивается с жестокостью и беззаконием, царящими по обе стороны решётки. Каждый, кто попадает в эти стены, становится их рабом до конца жизни. Но Энди, обладающий живым умом и доброй душой, находит подход как к заключённым, так и к охранникам, добиваясь их особого к себе расположения.");
        film.FilmLength.Should().Be(142);
        film.Genres?.Count.Should().Be(0);
        film.ImdbId.Should().Be("tt0111161");
        film.KinopoiskId.Should().Be(326);
        film.LogoUrl.Should().Be("https://avatars.mds.yandex.net/get-ott/1648503/2a000001705c8bf514c033f1019473a4caae/orig");
        film.NameOriginal.Should().Be("The Shawshank Redemption");
        film.NameRu.Should().Be("Побег из Шоушенка");
        film.PosterUrl.Should().Be("https://kinopoiskapiunofficial.tech/images/posters/kp/326.jpg");
        film.PosterUrlPreview.Should().Be("https://kinopoiskapiunofficial.tech/images/posters/kp_small/326.jpg");
        film.RatingMpaa.Should().Be("r");
        film.Slogan.Should().Be("Страх - это кандалы. Надежда - это свобода");
        film.Year.Should().Be(1994);
        film.RatingKinopoisk.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetFilmsStaff()
    {
        var request = $"https://kinopoiskapiunofficial.tech/api/v1/staff?filmId=326";
        using HttpResponseMessage responseMessage = await _httpClient.GetAsync(new Uri(request)).ConfigureAwait(false);
        _ = responseMessage.EnsureSuccessStatusCode();
        var response = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
        List<KpFilmStaff>? filmStaffList = JsonSerializer.Deserialize<List<KpFilmStaff>>(response, _jsonOptions);
        filmStaffList.Should().NotBeNull("");
        filmStaffList!.Count.Should().Be(90);
        KpFilmStaff filmStaff = filmStaffList[1];
        filmStaff.Description.Should().Be("Andy Dufresne");
        filmStaff.NameEn.Should().Be("Tim Robbins");
        filmStaff.NameRu.Should().Be("Тим Роббинс");
        filmStaff.PosterUrl.Should().Be("https://kinopoiskapiunofficial.tech/images/actor_posters/kp/7987.jpg");
        filmStaff.ProfessionKey.Should().Be("ACTOR");
        filmStaff.StaffId.Should().Be(7987);
    }


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposeAll)
    {
        _httpClient.Dispose();
    }

    private static string GetKinopoiskUnofficialToken()
    {
        var token = Environment.GetEnvironmentVariable("KINOPOISK_UNOFFICIAL_TOKEN");
        Logger.Info($"Env token length is: {(token != null ? token.Length : 0)}");
        return string.IsNullOrWhiteSpace(token) ? KINOPOISK_UNOFFICIAL_TOKEN : token;
    }
}