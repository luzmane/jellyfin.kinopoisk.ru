using System.Text.Json;

using FluentAssertions;

using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskDev.Model.Movie;

namespace Jellyfin.Plugin.KinopoiskRu.Tests.Api.KinopoiskDev;

public class KinopoiskDevApiTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string KINOPOISK_DEV_TOKEN = "8DA0EV2-KTP4A5Q-G67QP3K-S2VFBX7";
    private static readonly NLog.ILogger Logger = NLog.LogManager.GetCurrentClassLogger();


    public KinopoiskDevApiTests()
    {
        _httpClient = new();
        _httpClient.DefaultRequestHeaders.Add("X-API-KEY", GetKinopoiskDevToken());

        _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    }


    [Fact]
    public async Task GetMovieById()
    {
        var request = new Uri("https://api.kinopoisk.dev/v1.3/movie/689");
        using HttpResponseMessage responseMessage = await _httpClient.GetAsync(request).ConfigureAwait(false);
        _ = responseMessage.EnsureSuccessStatusCode();
        var response = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
        KpMovie? kpMovie = JsonSerializer.Deserialize<KpMovie>(response, _jsonOptions);
        kpMovie.Should().NotBeNull();
        kpMovie!.AlternativeName.Should().Be("The Green Mile");
        kpMovie.Backdrop?.Url.Should().Be("https://avatars.mds.yandex.net/get-ott/224348/2a00000169e39ef77f588ccdfe574dae8227/orig");
        kpMovie.Backdrop?.PreviewUrl.Should().Be("https://avatars.mds.yandex.net/get-ott/224348/2a00000169e39ef77f588ccdfe574dae8227/x1000");
        kpMovie.Countries?.Count.Should().Be(1);
        kpMovie.Description.Should().Be("Пол Эджкомб — начальник блока смертников в тюрьме «Холодная гора», каждый из узников которого однажды проходит «зеленую милю» по пути к месту казни. Пол повидал много заключённых и надзирателей за время работы. Однако гигант Джон Коффи, обвинённый в страшном преступлении, стал одним из самых необычных обитателей блока.");
        kpMovie.ExternalId?.Imdb.Should().Be("tt0120689");
        kpMovie.ExternalId?.Tmdb.Should().Be(497);
        kpMovie.Genres?.Count.Should().Be(3);
        kpMovie.Id.Should().Be(435);
        kpMovie.Logo?.Url.Should().Be("https://avatars.mds.yandex.net/get-ott/239697/2a0000016f12f1eb8870b609ee94313774b2/orig");
        kpMovie.MovieLength.Should().Be(189);
        kpMovie.Name.Should().Be("Зеленая миля");
        kpMovie.Persons?.Count.Should().Be(87);
        kpMovie.Poster?.Url.Should().Be("https://st.kp.yandex.net/images/film_big/435.jpg");
        kpMovie.Poster?.PreviewUrl.Should().Be("https://st.kp.yandex.net/images/film_iphone/iphone360_435.jpg");
        kpMovie.Premiere?.World.Should().Be("1999-12-06T00:00:00.000Z");
        kpMovie.ProductionCompanies?.Count.Should().Be(4);
        kpMovie.Rating?.Kp.Should().NotBeNull("");
        kpMovie.RatingMpaa.Should().Be("r");
        kpMovie.Slogan.Should().Be("Пол Эджкомб не верил в чудеса. Пока не столкнулся с одним из них");
        kpMovie.Videos?.Teasers.Count.Should().Be(0);
        kpMovie.Videos?.Trailers.Count.Should().Be(2);
        kpMovie.Year.Should().Be(1999);
        kpMovie.Facts?.Count.Should().Be(21);
        kpMovie.SequelsAndPrequels?.Count.Should().Be(0);
        kpMovie.Top250.Should().Be(1);
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

    private static string GetKinopoiskDevToken()
    {
        var token = Environment.GetEnvironmentVariable("KINOPOISK_DEV_TOKEN");
        Logger.Info($"Env token length is: {(token != null ? token.Length : 0)}");
        return string.IsNullOrWhiteSpace(token) ? KINOPOISK_DEV_TOKEN : token;
    }
}