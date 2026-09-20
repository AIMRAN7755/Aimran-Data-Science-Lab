namespace AIMRAN_Data_Science_Lab.Themes;

/// <summary>
/// Theme extension methods and helper utilities
/// </summary>
public static class ThemeExtensions
{
    /// <summary>
    /// Apply responsive spacing based on screen size
    /// </summary>
    public static string GetResponsiveSpacing(string mobileSpacing, string tabletSpacing, string desktopSpacing)
    {
        return $"py-{mobileSpacing} md:py-{tabletSpacing} lg:py-{desktopSpacing}";
    }

    /// <summary>
    /// Create a CSS variable reference
    /// </summary>
    public static string CssVar(string variableName) => $"var(--mud-palette-{variableName})";

    /// <summary>
    /// Get text color for optimal contrast against a background color
    /// </summary>
    public static string GetContrastTextColor(string backgroundColor) => backgroundColor switch
    {
        "white" => "rgba(0, 0, 0, 0.87)",
        "black" => "rgba(255, 255, 255, 0.87)",
        _ => "rgba(0, 0, 0, 0.87)"
    };

    /// <summary>
    /// Build a shadow stack for complex depth effects
    /// </summary>
    public static string CreateShadowStack(params string[] shadows)
    {
        return string.Join(", ", shadows);
    }

    /// <summary>
    /// Apply consistent hover state transformation
    /// </summary>
    public static string GetHoverTransform(double scalePercent = 1.05)
    {
        return $"transform: scale({scalePercent}); {Transitions.StandardTransition}";
    }

    /// <summary>
    /// Apply consistent focus ring styling
    /// </summary>
    public static string GetFocusStyle(string color = "primary")
    {
        return $"outline: 2px solid {CssVar(color)}; outline-offset: 2px;";
    }

    /// <summary>
    /// Get responsive font size CSS
    /// </summary>
    public static string GetResponsiveFontSize(int mobileSize, int tabletSize, int desktopSize)
    {
        return $"font-size: {mobileSize}px; @media (min-width: 600px) {{ font-size: {tabletSize}px; }} @media (min-width: 1024px) {{ font-size: {desktopSize}px; }}";
    }

    /// <summary>
    /// Generate a linear gradient background
    /// </summary>
    public static string CreateGradient(string startColor, string endColor, string direction = "to right")
    {
        return $"linear-gradient({direction}, {startColor}, {endColor})";
    }

    /// <summary>
    /// Apply opacity to a color
    /// </summary>
    public static string ApplyOpacity(string color, double opacity)
    {
        if (color.StartsWith("rgba"))
            return color; // Already has opacity
        if (color.StartsWith("rgb"))
            return color.Replace("rgb", "rgba").Replace(")", ", " + opacity + ")");
        return color; // Fallback
    }

    /// <summary>
    /// Get disabled state styling
    /// </summary>
    public static string GetDisabledStyle()
    {
        return $"opacity: 0.6; cursor: not-allowed; {Transitions.StandardTransition}";
    }

    /// <summary>
    /// Create a common breakpoint media query
    /// </summary>
    public static string GetMediaQuery(string breakpoint) => breakpoint switch
    {
        "sm" => "@media (min-width: 600px)",
        "md" => "@media (min-width: 960px)",
        "lg" => "@media (min-width: 1264px)",
        "xl" => "@media (min-width: 1904px)",
        _ => "@media (min-width: 600px)"
    };
}
