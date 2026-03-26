using System.Text.RegularExpressions;

namespace ClashVillagePulse.Infrastructure.StaticData.Helpers;

public static class HallItemAliasResolver
{
    public static string Normalize(string value)
    {
        return Regex.Replace(value.Trim().ToLowerInvariant(), "[^a-z0-9]+", string.Empty);
    }
}