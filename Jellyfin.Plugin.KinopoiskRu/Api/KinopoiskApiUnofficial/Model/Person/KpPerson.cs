namespace Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskApiUnofficial.Model.Person;

internal sealed class KpPerson
{
    /// <summary>
    /// Gets or sets KinopoiskId.
    /// </summary>
    public long PersonId { get; set; }

    public string? NameRu { get; set; }

    public string? NameEn { get; set; }

    public string? PosterUrl { get; set; }

    public string? Birthday { get; set; }

    public string? BirthPlace { get; set; }

    public string? Death { get; set; }

    public string? DeathPlace { get; set; }

    public List<string> Facts { get; } = new();
}
