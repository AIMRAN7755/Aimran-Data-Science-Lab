namespace AIMRAN_Data_Science_Lab.Themes;

/// <summary>
/// Color constants and utilities for MudBlazor components
/// </summary>
public static class ThemeColor
{
    // Standard colors
    public const string Primary = "primary";
    public const string Secondary = "secondary";
    public const string Tertiary = "tertiary";
    public const string Success = "success";
    public const string Error = "error";
    public const string Warning = "warning";
    public const string Info = "info";

    // Semantic colors
    public const string Default = "default";
    public const string Inherit = "inherit";
    public const string Surface = "surface";
    public const string Background = "background";

    // Text colors
    public const string TextPrimary = "textprimary";
    public const string TextSecondary = "textsecondary";
    public const string TextDisabled = "textdisabled";

    // Additional utility
    public static string GetColorClass(string color) => $"mud-color-{color}";
}
