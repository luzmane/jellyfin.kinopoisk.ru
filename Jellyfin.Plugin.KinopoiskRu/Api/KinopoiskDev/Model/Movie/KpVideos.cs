namespace Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskDev.Model.Movie;

internal sealed class KpVideos
{
    public List<KpVideo> Trailers { get; set; } = new List<KpVideo>();

    public List<KpVideo> Teasers { get; set; } = new List<KpVideo>();
}
