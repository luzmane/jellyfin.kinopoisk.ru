using System.Diagnostics;

namespace Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskDev.Model.Movie;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
internal sealed class KpRating
{
    /// <summary>
    /// Gets or sets Kinopoisk rating.
    /// </summary>
    public float? Kp { get; set; }

    private string? DebuggerDisplay => $"Kp: {Kp}";
}
