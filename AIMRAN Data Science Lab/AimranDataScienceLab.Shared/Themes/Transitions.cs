namespace AIMRAN_Data_Science_Lab.Themes;

/// <summary>
/// Animation and transition timing constants for consistent motion
/// </summary>
public static class Transitions
{
    // Duration values (milliseconds)
    public const int Fast = 150;
    public const int Base = 250;
    public const int Slow = 350;
    public const int Slower = 500;

    // Semantic durations
    public const int Instant = 0;
    public const int Brief = 100;
    public const int Short = 200;
    public const int Medium = 300;
    public const int Long = 400;
    public const int VeryLong = 600;

    // Easing functions (CSS values)
    public const string EaseInOut = "cubic-bezier(0.4, 0, 0.2, 1)";
    public const string EaseOut = "cubic-bezier(0.0, 0, 0.2, 1)";
    public const string EaseIn = "cubic-bezier(0.4, 0, 1, 1)";
    public const string Linear = "linear";
    public const string Sharp = "cubic-bezier(0.4, 0, 0.6, 1)";

    // Standard transition combinations
    public const string StandardTransition = "all 250ms cubic-bezier(0.4, 0, 0.2, 1)";
    public const string ShortTransition = "all 150ms cubic-bezier(0.4, 0, 0.2, 1)";
    public const string LongTransition = "all 350ms cubic-bezier(0.4, 0, 0.2, 1)";

    // Property-specific transitions
    public const string ColorTransition = "background-color 250ms cubic-bezier(0.4, 0, 0.2, 1), color 250ms cubic-bezier(0.4, 0, 0.2, 1)";
    public const string BackgroundTransition = "background-color 250ms cubic-bezier(0.4, 0, 0.2, 1)";
    public const string OpacityTransition = "opacity 250ms cubic-bezier(0.4, 0, 0.2, 1)";
    public const string TransformTransition = "transform 250ms cubic-bezier(0.4, 0, 0.2, 1)";
    public const string ShadowTransition = "box-shadow 250ms cubic-bezier(0.4, 0, 0.2, 1)";

    // Helper method to generate custom transition CSS
    public static string CreateTransition(string property, int duration = 250, string? easing = null)
    {
        easing ??= EaseInOut;
        return $"{property} {duration}ms {easing}";
    }

    // Helper method to generate multiple property transition
    public static string CreateMultipleTransition(int duration = 250, string? easing = null, params string[] properties)
    {
        easing ??= EaseInOut;
        var transitions = properties.Select(p => $"{p} {duration}ms {easing}");
        return string.Join(", ", transitions);
    }
}
