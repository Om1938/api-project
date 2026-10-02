using System.Text;
using System.Text.RegularExpressions;

namespace ApiGateway.Application.Apis;

public static partial class Slugs
{
    public const int MaxLength = 64;

    public static bool IsValid(string slug) => SlugPattern().IsMatch(slug);

    public static string From(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var character in text.Trim().ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(character);
            }
            else if (builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        var slug = builder.ToString().Trim('-');
        return slug.Length <= MaxLength ? slug : slug[..MaxLength].Trim('-');
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
