namespace Jellyfin.Plugin.KinopoiskRu.Api;

internal sealed class ApiResult<TItem>
{
    public ApiResult(TItem item)
    {
        Item = item;
    }

    public TItem Item { get; set; }

    public bool HasError { get; set; }
}
