using System.Diagnostics;

namespace Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskApiUnofficial.Model.Film;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
internal sealed class KpFilm
{
    public long KinopoiskId { get; set; }

    public string? ImdbId { get; set; }

    public string? NameRu { get; set; }

    public string? NameOriginal { get; set; }

    public string? PosterUrl { get; set; }

    public string? PosterUrlPreview { get; set; }

    /// <summary>
    /// Gets or sets backdrop.
    /// </summary>
    public string? CoverUrl { get; set; }

    public string? LogoUrl { get; set; }

    public float? RatingKinopoisk { get; set; }

    public int? Year { get; set; }

    public int? FilmLength { get; set; }

    public string? Slogan { get; set; }

    public string? Description { get; set; }

    public string? RatingMpaa { get; set; }

    public List<KpCountry> Countries { get; set; } = new();

    public List<KpGenre> Genres { get; set; } = new();

    private string DebuggerDisplay => $"#{KinopoiskId}, {NameRu}";
}
