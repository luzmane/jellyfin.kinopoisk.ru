namespace Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskApiUnofficial.Model.Film;

internal sealed class KpFilmStaff
{
    /// <summary>
    /// Gets or sets KinopoiskId.
    /// </summary>
    public long StaffId { get; set; }

    public string? NameRu { get; set; }

    public string? NameEn { get; set; }

    public string? Description { get; set; }

    public string? PosterUrl { get; set; }

    public string? ProfessionKey { get; set; }

    public string? ProfessionText { get; set; }
}
