namespace Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskApiUnofficial.Model;

internal sealed class KpSearchResult<TItem>
{
    public List<TItem> Items { get; set; } = new();
}
