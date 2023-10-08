namespace Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskApiUnofficial.Model.Season;

internal sealed class KpSeason
{
    public int Number { get; set; }

    public List<KpEpisode> Episodes { get; set; } = new();
}
