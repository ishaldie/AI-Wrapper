using MudBlazor;

namespace ZSR.Underwriting.Web.Theme;

public static class ZsrTheme
{
    public static readonly MudTheme Instance = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#1E3A5F",
            PrimaryDarken = "#152D4A",
            PrimaryLighten = "#2A5080",
            Secondary = "#4A90D9",
            SecondaryDarken = "#3A7BC0",
            SecondaryLighten = "#6BA8E6",
            Tertiary = "#F97316",
            Background = "#F0F2F5",
            Surface = "#FFFFFF",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#1A1D23",
            DrawerBackground = "#FAFBFC",
            DrawerText = "#374151",
            DrawerIcon = "#6B7280",
            TextPrimary = "#1A1D23",
            TextSecondary = "#6B7280",
            ActionDefault = "#6B7280",
            ActionDisabled = "#D1D5DB",
            ActionDisabledBackground = "#F3F4F6",
            Divider = "#E5E7EB",
            DividerLight = "#F3F4F6",
            TableLines = "#E5E7EB",
            TableStriped = "#F9FAFB",
            TableHover = "#F3F4F6",
            HoverOpacity = 0.04,
            LinesDefault = "#E5E7EB",
            Info = "#3B82F6",
            Success = "#16A34A",
            Warning = "#EA580C",
            Error = "#DC2626",
            Dark = "#374151"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = new[] { "Plus Jakarta Sans", "Segoe UI", "sans-serif" },
                FontSize = "0.875rem",
                LineHeight = "1.5"
            },
            H1 = new H1Typography { FontSize = "2rem", FontWeight = "800", LineHeight = "1.15", LetterSpacing = "-0.03em" },
            H2 = new H2Typography { FontSize = "1.625rem", FontWeight = "800", LineHeight = "1.2", LetterSpacing = "-0.02em" },
            H3 = new H3Typography { FontSize = "1.375rem", FontWeight = "700", LineHeight = "1.25", LetterSpacing = "-0.02em" },
            H4 = new H4Typography { FontSize = "1.1875rem", FontWeight = "700", LineHeight = "1.3", LetterSpacing = "-0.01em" },
            H5 = new H5Typography { FontSize = "1.0625rem", FontWeight = "700", LineHeight = "1.35" },
            H6 = new H6Typography { FontSize = "0.9375rem", FontWeight = "700", LineHeight = "1.4" },
            Subtitle1 = new Subtitle1Typography { FontSize = "0.875rem", FontWeight = "600" },
            Body1 = new Body1Typography { FontSize = "0.875rem", FontWeight = "400", LineHeight = "1.6" },
            Body2 = new Body2Typography { FontSize = "0.8125rem", FontWeight = "400", LineHeight = "1.5" },
            Caption = new CaptionTypography { FontSize = "0.75rem", FontWeight = "500", LineHeight = "1.4" },
            Overline = new OverlineTypography { FontSize = "0.6875rem", FontWeight = "700", LetterSpacing = "0.1em" }
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px",
            DrawerWidthLeft = "240px"
        }
    };
}
