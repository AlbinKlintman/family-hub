using System.ComponentModel.DataAnnotations;

namespace WebApp.Helpers;

public static class UrlValidator
{
    private static readonly UrlAttribute Attribute = new();

    public static bool IsValid(string url) => Attribute.IsValid(url);
}
