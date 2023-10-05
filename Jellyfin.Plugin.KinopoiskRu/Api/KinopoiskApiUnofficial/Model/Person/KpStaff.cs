namespace Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskApiUnofficial.Model.Person;

internal sealed class KpStaff
{
    /// <summary>
    /// Gets or sets KinopoiskId.
    /// </summary>
    public long KinopoiskId { get; set; }

    public string? NameRu { get; set; }

    public string? NameEn { get; set; }

    public string? PosterUrl { get; set; }
}
