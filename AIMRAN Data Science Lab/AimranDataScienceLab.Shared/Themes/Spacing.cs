namespace AIMRAN_Data_Science_Lab.Themes;

/// <summary>
/// Spacing and margin/padding constants for consistent layout
/// </summary>
public static class Spacing
{
    /// <summary>
    /// Base spacing unit (4px)
    /// </summary>
    public const int Unit = 4;

    // Margin and padding scales
    public const int XSmall = 4;      // 0.25rem / 4px
    public const int Small = 8;       // 0.5rem / 8px
    public const int Default = 16;    // 1rem / 16px
    public const int Medium = 24;     // 1.5rem / 24px
    public const int Large = 32;      // 2rem / 32px
    public const int XLarge = 48;     // 3rem / 48px
    public const int XXLarge = 64;    // 4rem / 64px

    // Semantic spacing
    public const int Compact = 8;
    public const int Normal = 16;
    public const int Relaxed = 24;
    public const int Spacious = 32;

    // Specific use cases
    public const int FormFieldGap = 12;
    public const int ComponentGap = 16;
    public const int SectionGap = 24;
    public const int PageGutter = 32;

    // CSS class names for Tailwind/Bootstrap compatibility
    public static string GetMarginClass(int pixels) => $"m-{pixels}";
    public static string GetPaddingClass(int pixels) => $"p-{pixels}";
    public static string GetGapClass(int pixels) => $"gap-{pixels}";
}
