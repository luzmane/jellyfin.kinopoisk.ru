namespace Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskDev.Model.Movie;

internal sealed class KpVideos
{
    public List<KpVideo> Trailers { get; } = new List<KpVideo>();

    public List<KpVideo> Teasers { get; } = new List<KpVideo>();
}
