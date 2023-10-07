using System.Globalization;
using System.Text.RegularExpressions;

using Jellyfin.Data.Entities;
using Jellyfin.Plugin.KinopoiskRu.Api.KinopoiskDev.Model.Movie;

using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.KinopoiskRu.Helper;

/// <summary>
/// Helper methods for API.
/// </summary>
internal sealed class KpHelper
{
    private static readonly Regex YearRegex = new("(?<year>[0-9]{4})", RegexOptions.Compiled);

    private static readonly Regex NonAlphaNumericRegex = new("[^а-яёА-ЯЁa-zA-Z0-9\\s]", RegexOptions.Compiled);

    private static readonly Regex MultiWhitespaceRegex = new("\\s\\s+", RegexOptions.Compiled);

    private static readonly Dictionary<string, string> PersonTypeMap = new()
    {
        // KinopoiskDev
        {"composer", PersonType.Composer},
        {"director", PersonType.Director},
        {"producer", PersonType.Producer},
        {"voice_actor", PersonType.Actor},
        {"writer", PersonType.Writer},
        {"actor", PersonType.Actor},

        {"композиторы", PersonType.Composer},
        {"режиссеры", PersonType.Director},
        {"продюсеры", PersonType.Producer},
        {"актеры дубляжа", PersonType.Actor},
        {"редакторы", PersonType.Writer},
        {"актеры", PersonType.Actor},

        // KinopoiskUnofficial
        {"COMPOSER", PersonType.Composer},
        {"DIRECTOR", PersonType.Director},
        {"PRODUCER", PersonType.Producer},
        {"WRITER", PersonType.Writer},
        {"ACTOR", PersonType.Actor},

        {"Композиторы", PersonType.Composer},
        {"Режиссеры", PersonType.Director},
        {"Продюсеры", PersonType.Producer},
        {"Сценаристы", PersonType.Writer},
        {"Актеры", PersonType.Actor},
    };

    internal static DateTime? GetPremierDate(KpPremiere? premiere)
    {
        if (premiere == null)
        {
            return null;
        }

        if (DateTime.TryParseExact(
            premiere.World,
            "yyyy-MM-dd'T'HH:mm:ss.fffZ",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var world))
        {
            return world;
        }

        if (DateTime.TryParseExact(
            premiere.Russia,
            "yyyy-MM-dd'T'HH:mm:ss.fffZ",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var russia))
        {
            return russia;
        }

        if (DateTime.TryParseExact(
            premiere.Cinema,
            "yyyy-MM-dd'T'HH:mm:ss.fffZ",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var cinema))
        {
            return cinema;
        }

        if (DateTime.TryParseExact(
            premiere.Digital,
            "yyyy-MM-dd'T'HH:mm:ss.fffZ",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var digital))
        {
            return digital;
        }

        if (DateTime.TryParseExact(
            premiere.Bluray,
            "yyyy-MM-dd'T'HH:mm:ss.fffZ",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var bluray))
        {
            return bluray;
        }

        if (DateTime.TryParseExact(
            premiere.Dvd,
            "yyyy-MM-dd'T'HH:mm:ss.fffZ",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var dvd))
        {
            return dvd;
        }

        return null;
    }

    internal static string? TranslatePersonType(string? enProfesson, string? profession)
    {
        string? toReturn = null;
        if (enProfesson != null)
        {
            toReturn = PersonTypeMap.GetValueOrDefault(enProfesson);
        }
        if (toReturn == null && profession != null)
        {
            toReturn = PersonTypeMap.GetValueOrDefault(profession);
        }
        return toReturn;
    }

    internal static async Task AddToActivityLog(IActivityManager activityManager, string overview, string shortOverview)
    {
        await activityManager.CreateAsync(new ActivityLog(Plugin.PluginKey, "PluginError", Guid.NewGuid())
        {
            Overview = overview,
            ShortOverview = shortOverview,
            LogSeverity = LogLevel.Error
        }).ConfigureAwait(false);
    }

    internal static int? DetectYearFromMoviePath(string filePath, string movieName)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        fileName = fileName.Replace(movieName, " ", StringComparison.Ordinal);
        var yearSt = string.Empty;
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var match = YearRegex.Match(fileName);
            yearSt = match.Success ? match.Groups["year"].Value : string.Empty;
        }

        _ = int.TryParse(yearSt, out var year);
        _ = int.TryParse(DateTime.Now.ToString("yyyy", CultureInfo.InvariantCulture), out var currentYear);
        return year > 1800 && year <= currentYear + 1 ? year : null;
    }

    internal static string? CleanName(string? name)
    {
        return string.IsNullOrEmpty(name)
        ? name
        : MultiWhitespaceRegex.Replace(NonAlphaNumericRegex.Replace(name, " ").Trim(), " ").ToLowerInvariant();
    }
}
