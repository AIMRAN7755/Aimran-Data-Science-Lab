namespace AIMRAN_Data_Science_Lab.Themes;

/// <summary>
/// Z-index scale for consistent layering and stacking context
/// </summary>
public static class ZIndex
{
    // Base layers
    public const int Hidden = -1;
    public const int Base = 0;

    // Content layers
    public const int Default = 1;
    public const int Docked = 10;
    public const int Floating = 100;

    // Dropdown and menus
    public const int Dropdown = 1000;
    public const int Sticky = 1020;
    public const int Fixed = 1030;
    public const int ModalBackdrop = 1040;
    public const int OffCanvas = 1050;
    public const int Modal = 1060;

    // Toasts and notifications
    public const int Toast = 1070;
    public const int Tooltip = 1080;
    public const int Popover = 1090;

    // MudBlazor compatibility layers
    public const int DrawerBackdrop = 1040;
    public const int Drawer = 1050;
    public const int AppBar = 1100;
    public const int FAB = 1050;
    public const int Snackbar = 1000;

    // High-priority overlays
    public const int Alert = 2000;
    public const int Confirmation = 2010;
    public const int FullscreenDialog = 3000;

    // Special cases
    public const int ContextMenu = 1100;
    public const int CommandPalette = 2000;
    public const int Spotlight = 2000;

    // Helper to ensure z-index is always sufficient for layering
    public static int GetNextZIndex(int currentZIndex = 0) => currentZIndex + 10;
}
