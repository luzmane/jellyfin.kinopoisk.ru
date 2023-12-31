using FluentAssertions;

using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskApiUnofficial.Model;
using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskApiUnofficial.Model.Film;
using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskApiUnofficial.Model.Person;
using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskApiUnofficial.Model.Season;
using Jellyfin.Plugin.KinopoiskRu.Helper;

namespace Jellyfin.Plugin.KinopoiskRu.Tests.KinopoiskApiUnofficial;

public class ApiTests : IDisposable
{
    private const string KINOPOISK_UNOFFICIAL_TOKEN = "0f162131-81c1-4979-b46c-3eea4263fb11";
    private readonly HttpClient _httpClient;
    private readonly NLog.ILogger _logger;

    public ApiTests()
    {
        _logger = NLog.LogManager.GetCurrentClassLogger();

        _httpClient = new();
        _httpClient.DefaultRequestHeaders.Add("X-API-KEY", GetKinopoiskUnofficialToken());
    }

    [Fact]
    public async Task GetFilm()
    {
        var request = $"https://kinopoiskapiunofficial.tech/api/v2.2/films/326";
        using HttpResponseMessage responseMessage = await _httpClient.GetAsync(new Uri(request)).ConfigureAwait(false);
        _ = responseMessage.EnsureSuccessStatusCode();
        var response = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
        KpFilm? film = JsonHelper.Deserialize<KpFilm>(response);
        film.Should().NotBeNull();
        film!.Countries?.Count.Should().Be(1);
        film.CoverUrl.Should().Be("https://avatars.mds.yandex.net/get-ott/1652588/2a00000186aca5e13ea6cec11d584ac5455b/orig");
        film.Description.Should().Be("Бухгалтер Энди Дюфрейн обвинён в убийстве собственной жены и её любовника. Оказавшись в тюрьме под названием Шоушенк, он сталкивается с жестокостью и беззаконием, царящими по обе стороны решётки. Каждый, кто попадает в эти стены, становится их рабом до конца жизни. Но Энди, обладающий живым умом и доброй душой, находит подход как к заключённым, так и к охранникам, добиваясь их особого к себе расположения.");
        film.FilmLength.Should().Be(142);
        film.Genres?.Count.Should().Be(1);
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
    public async Task GetSeasons()
    {
        var request = $"https://kinopoiskapiunofficial.tech/api/v2.2/films/77044/seasons";
        using HttpResponseMessage responseMessage = await _httpClient.GetAsync(new Uri(request)).ConfigureAwait(false);
        _ = responseMessage.EnsureSuccessStatusCode();
        var response = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
        KpSearchResult<KpSeason>? seasons = JsonHelper.Deserialize<KpSearchResult<KpSeason>>(response);
        seasons.Should().NotBeNull();
        seasons!.Items.Count.Should().Be(10);
        KpSeason kpSeason = seasons.Items[0];
        kpSeason.Episodes.Count.Should().Be(24);
        KpEpisode kpEpisode = kpSeason.Episodes[23];
        kpEpisode.EpisodeNumber.Should().Be(24);
        kpEpisode.SeasonNumber.Should().Be(1);
        kpEpisode.NameRu.Should().Be("Эпизод, где Рейчел понимает");
        kpEpisode.NameEn.Should().Be("The One Where Rachel Finds Out");
        kpEpisode.ReleaseDate.Should().Be("1995-05-18");
    }

    [Fact]
    public async Task GetVideos()
    {
        var request = $"https://kinopoiskapiunofficial.tech/api/v2.2/films/77044/videos";
        using HttpResponseMessage responseMessage = await _httpClient.GetAsync(new Uri(request)).ConfigureAwait(false);
        _ = responseMessage.EnsureSuccessStatusCode();
        var response = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
        KpSearchResult<KpVideo>? videos = JsonHelper.Deserialize<KpSearchResult<KpVideo>>(response);
        videos.Should().NotBeNull();
        videos!.Items.Count.Should().Be(8);
        KpVideo kpVideo = videos.Items[0];
        kpVideo.Url.Should().Be("http://trailers.s3.mds.yandex.net/video_original/160983-05ae2e39521817fce4e34a89968f3808.mp4");
    }

    [Fact]
    public async Task GetStaff()
    {
        var request = $"https://kinopoiskapiunofficial.tech/api/v1/staff/7987";
        using HttpResponseMessage responseMessage = await _httpClient.GetAsync(new Uri(request)).ConfigureAwait(false);
        _ = responseMessage.EnsureSuccessStatusCode();
        var response = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
        KpPerson? kpPerson = JsonHelper.Deserialize<KpPerson>(response);
        kpPerson.Should().NotBeNull();
        kpPerson!.Birthday.Should().Be("1958-10-16");
        kpPerson.BirthPlace.Should().Be("Уэст-Ковина, Калифорния, США");
        kpPerson.Death.Should().BeNull("person still alive");
        kpPerson.DeathPlace.Should().BeNull("person still alive");
        kpPerson.Facts?.Count.Should().Be(4);
        kpPerson.NameEn.Should().Be("Tim Robbins");
        kpPerson.NameRu.Should().Be("Тим Роббинс");
        kpPerson.PersonId.Should().Be(7987);
        kpPerson.PosterUrl.Should().Be("https://kinopoiskapiunofficial.tech/images/actor_posters/kp/7987.jpg");
    }

    [Fact]
    public async Task GetFilmsStaff()
    {
        var request = $"https://kinopoiskapiunofficial.tech/api/v1/staff?filmId=326";
        using HttpResponseMessage responseMessage = await _httpClient.GetAsync(new Uri(request)).ConfigureAwait(false);
        _ = responseMessage.EnsureSuccessStatusCode();
        var response = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
        List<KpFilmStaff>? filmStaffList = JsonHelper.Deserialize<List<KpFilmStaff>>(response);
        filmStaffList.Should().NotBeNull();
        filmStaffList!.Count.Should().Be(90);
        KpFilmStaff filmStaff = filmStaffList[1];
        filmStaff.Description.Should().Be("Andy Dufresne");
        filmStaff.NameEn.Should().Be("Tim Robbins");
        filmStaff.NameRu.Should().Be("Тим Роббинс");
        filmStaff.PosterUrl.Should().Be("https://kinopoiskapiunofficial.tech/images/actor_posters/kp/7987.jpg");
        filmStaff.ProfessionKey.Should().Be("ACTOR");
        filmStaff.StaffId.Should().Be(7987);
    }

    [Fact]
    public async Task SearchFilmByName()
    {
        var request = $"https://kinopoiskapiunofficial.tech/api/v2.2/films?keyword=100 шагов";
        using HttpResponseMessage responseMessage = await _httpClient.GetAsync(new Uri(request)).ConfigureAwait(false);
        _ = responseMessage.EnsureSuccessStatusCode();
        var response = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
        KpSearchResult<KpFilm>? filmSearchResult = JsonHelper.Deserialize<KpSearchResult<KpFilm>>(response);
        filmSearchResult.Should().NotBeNull();
        filmSearchResult!.Items.Count.Should().Be(3);
        KpFilm film = filmSearchResult.Items.First(f => f.KinopoiskId == 933277);
        film.Should().NotBeNull();
        film!.KinopoiskId.Should().Be(933277);
        film.ImdbId.Should().Be("tt3904078");
        film.NameOriginal.Should().Be("100 Things to Do Before High School");
        film.NameRu.Should().Be("100 шагов: Успеть до старших классов");
        film.Countries?.Count.Should().Be(1);
        film.Genres?.Count.Should().Be(2);
        film.RatingKinopoisk.Should().BeGreaterThan(0);
        film.Year.Should().Be(2014);
        film.PosterUrl.Should().Be("https://kinopoiskapiunofficial.tech/images/posters/kp/933277.jpg");
        film.PosterUrlPreview.Should().Be("https://kinopoiskapiunofficial.tech/images/posters/kp_small/933277.jpg");
    }

    [Fact]
    public async Task SearchFilmByNameAndYear()
    {
        var request = $"https://kinopoiskapiunofficial.tech/api/v2.2/films?keyword=100 шагов&yearFrom=2014&yearTo=2014";
        using HttpResponseMessage responseMessage = await _httpClient.GetAsync(new Uri(request)).ConfigureAwait(false);
        _ = responseMessage.EnsureSuccessStatusCode();
        var response = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
        KpSearchResult<KpFilm>? filmSearchResult = JsonHelper.Deserialize<KpSearchResult<KpFilm>>(response);
        filmSearchResult.Should().NotBeNull();
        filmSearchResult!.Items.Count.Should().Be(1);
        KpFilm film = filmSearchResult.Items.First(f => f.KinopoiskId == 933277);
        film.Should().NotBeNull();
        film!.KinopoiskId.Should().Be(933277);
        film.ImdbId.Should().Be("tt3904078");
        film.NameOriginal.Should().Be("100 Things to Do Before High School");
        film.NameRu.Should().Be("100 шагов: Успеть до старших классов");
        film.Countries?.Count.Should().Be(1);
        film.Genres?.Count.Should().Be(2);
        film.RatingKinopoisk.Should().BeGreaterThan(0);
        film.Year.Should().Be(2014);
        film.PosterUrl.Should().Be("https://kinopoiskapiunofficial.tech/images/posters/kp/933277.jpg");
        film.PosterUrlPreview.Should().Be("https://kinopoiskapiunofficial.tech/images/posters/kp_small/933277.jpg");
    }

    [Fact]
    public async Task SearchStaffByName()
    {
        var request = $"https://kinopoiskapiunofficial.tech/api/v1/persons?name=Тим Роббинс";
        using HttpResponseMessage responseMessage = await _httpClient.GetAsync(new Uri(request)).ConfigureAwait(false);
        _ = responseMessage.EnsureSuccessStatusCode();
        var response = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
        KpSearchResult<KpStaff>? staffSearchResult = JsonHelper.Deserialize<KpSearchResult<KpStaff>>(response);
        staffSearchResult.Should().NotBeNull();
        staffSearchResult!.Items.Count.Should().Be(4);
        KpStaff staff = staffSearchResult.Items.First(f => f.KinopoiskId == 7987);
        staff.Should().NotBeNull();
        staff!.KinopoiskId.Should().Be(7987);
        staff.NameEn.Should().Be("Tim Robbins");
        staff.NameRu.Should().Be("Тим Роббинс");
        staff.PosterUrl.Should().Be("https://kinopoiskapiunofficial.tech/images/actor_posters/kp/7987.jpg");
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

    protected string GetKinopoiskUnofficialToken()
    {
        var token = Environment.GetEnvironmentVariable("KINOPOISK_UNOFFICIAL_TOKEN");
        _logger.Info($"Env token length is: {(token != null ? token.Length : 0)}");
        return string.IsNullOrWhiteSpace(token) ? KINOPOISK_UNOFFICIAL_TOKEN : token;
    }

}
