namespace ZSR.Underwriting.Web.Helpers;

public static class AuthQueryHelper
{
    /// <summary>
    /// Returns the URL if it is a safe local path; otherwise returns the fallback.
    /// Prevents open-redirect attacks from user-controlled returnUrl parameters.
    /// </summary>
    public static string SafeLocalRedirect(string? url, string fallback = "/search")
    {
        if (string.IsNullOrWhiteSpace(url))
            return fallback;

        // Must start with "/" and must NOT start with "//" or "/\" (protocol-relative)
        if (url.StartsWith('/') && !url.StartsWith("//") && !url.StartsWith("/\\"))
            return url;

        return fallback;
    }

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
