namespace ZSR.Underwriting.Web.Helpers;

public static class AuthQueryHelper
{
    public static string QuerySuffix(string? searchQuery, string prefix)
    {
        if (string.IsNullOrWhiteSpace(searchQuery)) return "";
        return prefix + Uri.EscapeDataString(searchQuery);
    }

    public static string SearchRedirectUrl(string? searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery)) return "/search";
        return $"/search?q={Uri.EscapeDataString(searchQuery)}";
    }
}
