namespace AIMRAN_Data_Science_Lab.Themes;

/// <summary>
/// Shadow definitions for consistent elevation and depth
/// </summary>
public static class Shadows
{
    // Elevation shadows (following Material Design)
    public const string Elevation0 = "none";
    public const string Elevation1 = "0px 2px 1px -1px rgba(0,0,0,0.2), 0px 1px 1px 0px rgba(0,0,0,0.14), 0px 1px 3px 0px rgba(0,0,0,0.12)";
    public const string Elevation2 = "0px 3px 1px -2px rgba(0,0,0,0.2), 0px 2px 2px 0px rgba(0,0,0,0.14), 0px 1px 5px 0px rgba(0,0,0,0.12)";
    public const string Elevation3 = "0px 3px 3px -2px rgba(0,0,0,0.2), 0px 3px 4px 0px rgba(0,0,0,0.14), 0px 1px 8px 0px rgba(0,0,0,0.12)";
    public const string Elevation4 = "0px 2px 4px -1px rgba(0,0,0,0.2), 0px 4px 5px 0px rgba(0,0,0,0.14), 0px 1px 10px 0px rgba(0,0,0,0.12)";
    public const string Elevation6 = "0px 3px 5px -1px rgba(0,0,0,0.2), 0px 6px 10px 0px rgba(0,0,0,0.14), 0px 1px 18px 0px rgba(0,0,0,0.12)";
    public const string Elevation8 = "0px 5px 5px -3px rgba(0,0,0,0.2), 0px 8px 10px 1px rgba(0,0,0,0.14), 0px 3px 14px 2px rgba(0,0,0,0.12)";
    public const string Elevation12 = "0px 7px 8px -4px rgba(0,0,0,0.2), 0px 12px 17px 2px rgba(0,0,0,0.14), 0px 5px 22px 4px rgba(0,0,0,0.12)";
    public const string Elevation16 = "0px 8px 10px -5px rgba(0,0,0,0.2), 0px 16px 24px 2px rgba(0,0,0,0.14), 0px 6px 30px 5px rgba(0,0,0,0.12)";
    public const string Elevation24 = "0px 11px 15px -7px rgba(0,0,0,0.2), 0px 24px 38px 3px rgba(0,0,0,0.14), 0px 9px 46px 8px rgba(0,0,0,0.12)";

    // Semantic shadows
    public const string Subtle = Elevation1;
    public const string Default = Elevation3;
    public const string Raised = Elevation6;
    public const string Floating = Elevation12;
    public const string Modal = Elevation24;

    // Inset/border shadows
    public const string Inset = "inset 0 1px 3px 0 rgba(0, 0, 0, 0.1), inset 0 1px 2px 0 rgba(0, 0, 0, 0.06)";
    public const string InsetTop = "inset 0 1px 0 0 rgba(0, 0, 0, 0.1)";
    public const string InsetBottom = "inset 0 -1px 0 0 rgba(0, 0, 0, 0.1)";

    // Focus ring shadow
    public const string FocusRing = "0 0 0 3px rgba(66, 153, 225, 0.5)";

    // MudBlazor Elevation enum compatibility
    public static int GetElevationLevel(string shadowValue) => shadowValue switch
    {
        var s when s == Elevation0 => 0,
        var s when s == Elevation1 => 1,
        var s when s == Elevation2 => 2,
        var s when s == Elevation3 => 3,
        var s when s == Elevation4 => 4,
        var s when s == Elevation6 => 6,
        var s when s == Elevation8 => 8,
        var s when s == Elevation12 => 12,
        var s when s == Elevation16 => 16,
        var s when s == Elevation24 => 24,
        _ => 0
    };
}
