# AIMRAN Data Science Lab Theme Library

## Overview

The `AIMRAN_Data_Science_Lab.Themes` namespace provides a unified, production-grade theming system designed to work seamlessly with MudBlazor. This library consolidates all design tokens, spacing scales, color definitions, and typography constants into a single source of truth.

## Architecture

### Core Files

1. **Colors.cs** - Color constants and utilities
   - Standard palette colors (Primary, Secondary, Success, etc.)
   - Semantic colors (Background, Surface, TextPrimary, etc.)
   - Helper methods for color manipulation

2. **Typography.cs** / **TypographyConstants.cs** - Typography constants
   - **Note**: Currently implemented as `TypoStyle` in Typo.cs
   - Provides string constants for MudBlazor typography variants
   - Usage: `Typo="TypoStyle.H1"`, `Typo="TypoStyle.Body1"`, etc.

3. **Spacing.cs** - Spacing and layout constants
   - Base unit (4px) and semantic scales (XSmall, Small, Default, etc.)
   - Form-specific gaps, component gaps, and section gaps
   - Helper methods for CSS class generation

4. **Shadows.cs** - Shadow and elevation definitions
   - Material Design elevation shadows (0-24)
   - Semantic shadows (Subtle, Default, Raised, Floating, Modal)
   - Inset and focus ring shadows
   - Helper to map shadow values to elevation levels

5. **Transitions.cs** - Animation and transition timing
   - Duration values (Fast: 150ms, Base: 250ms, Slow: 350ms, etc.)
   - Easing functions (EaseInOut, EaseOut, EaseIn, Linear, Sharp)
   - Pre-built transition combinations
   - Helper methods for custom transitions

6. **ZIndex.cs** - Z-index stacking scale
   - Standard stacking levels (Hidden, Base, Default, etc.)
   - Component-specific layers (Drawer, Modal, Toast, etc.)
   - MudBlazor-specific z-index values
   - Helper to generate sequential z-indices

7. **ThemeExtensions.cs** - Extension methods and utilities
   - Helper methods for CSS generation
   - Responsive utility functions
   - Color contrast calculation
   - Custom transition generation

8. **ThemeProvider.razor** - Cascading theme provider component
   - Provides theme constants to child components
   - Methods for accessing theme values
   - CascadingValue pattern for component tree

## Usage Patterns

### In Blazor Components

#### Using Typography Constants

```razor
@using AIMRAN_Data_Science_Lab.Themes

<MudStack>
	<MudText Typo="Typo.H1">Large Heading</MudText>
	<MudText Typo="Typo.Body1">Regular body text</MudText>
	<MudText Typo="Typo.Caption">Small caption text</MudText>
</MudStack>
```

#### Using Spacing Constants

```csharp
@using AIMRAN_Data_Science_Lab.Themes

// In component code
var paddingSize = Spacing.Default;      // 16px
var marginSize = Spacing.Large;         // 32px
var formGap = Spacing.FormFieldGap;     // 12px
```

#### Using Color Constants

```razor
@using AIMRAN_Data_Science_Lab.Themes

<MudButton Color="MudBlazor.Color.Primary">Primary Button</MudButton>
<MudText Style="color: var(--mud-palette-text-secondary);">Secondary Text</MudText>
```

#### Using Shadows

```razor
@using AIMRAN_Data_Science_Lab.Themes

<MudPaper Style="box-shadow: @Shadows.Raised;">
	Content with raised shadow
</MudPaper>

<div style="box-shadow: @Shadows.Modal;">Floating Modal Content</div>
```

#### Using Transitions

```razor
@using AIMRAN_Data_Science_Lab.Themes

<div style="@Transitions.StandardTransition">Smooth transitions on all properties</div>
<div style="@Transitions.ColorTransition">Only color changes are animated</div>
```

#### Using Z-Index

```csharp
@using AIMRAN_Data_Science_Lab.Themes

// In component code
int dropdownLayer = ZIndex.Dropdown;    // 1000
int modalLayer = ZIndex.Modal;          // 1060
int toastLayer = ZIndex.Toast;          // 1070
```

### Using ThemeProvider Component

```razor
@* App.razor or layout wrapper *@
@using AIMRAN_Data_Science_Lab.Themes

<ThemeProvider>
	<YourAppComponents />
</ThemeProvider>

@* In a child component *@
@inject ThemeProvider ThemeProvider

<MudPaper Style="box-shadow: @ThemeProvider.GetShadow("floating");">
	Content
</MudPaper>
```

## Best Practices

### 1. Namespace Organization

Always structure imports in this order:

```razor
@* System and Microsoft namespaces *@
@using System.Collections.Generic

@* MudBlazor *@
@using MudBlazor

@* AIMRAN namespaces *@
@using AIMRAN_Data_Science_Lab
@using AIMRAN_Data_Science_Lab.Themes
@using AIMRAN_Data_Science_Lab.Services
```

### 2. Consistent Typography Usage

Use MudBlazor's `Typo` enum values directly:

```razor
<!-- Good ✓ -->
<MudText Typo="Typo.H1">Heading</MudText>
<MudText Typo="Typo.Body1">Body text</MudText>

<!-- Avoid ✗ -->
<MudText Typo="'h1'">Heading</MudText>
<MudText Typo="'body1'">Body text</MudText>
```

### 3. Spacing Scales

Use semantic spacing names rather than arbitrary values:

```csharp
// Good ✓
Spacing.FormFieldGap      // 12px
Spacing.ComponentGap      // 16px
Spacing.SectionGap        // 24px
Spacing.PageGutter        // 32px

// Avoid ✗
var padding = 15;         // Arbitrary value
var margin = "1.2rem";    // Hard to maintain
```

### 4. Shadow and Elevation

Use semantic shadow names:

```razor
<!-- Good ✓ -->
<MudPaper Elevation="1" Style="box-shadow: @Shadows.Subtle;">
<MudPaper Elevation="3" Style="box-shadow: @Shadows.Default;">
<MudPaper Elevation="8" Style="box-shadow: @Shadows.Floating;">

<!-- Avoid ✗ -->
<div style="box-shadow: 0 1px 3px rgba(0,0,0,0.1);">
```

### 5. Transitions

Use pre-built transitions for consistency:

```razor
<!-- Good ✓ -->
<div style="@Transitions.StandardTransition">
<div style="@Transitions.ColorTransition">

<!-- Avoid ✗ -->
<div style="transition: all 250ms ease-in-out;">
```

### 6. Z-Index Management

Always use the ZIndex constants:

```csharp
// Good ✓
var dropdownZIndex = ZIndex.Dropdown;   // 1000
var modalZIndex = ZIndex.Modal;         // 1060

// Avoid ✗
var dropdownZIndex = 1000;
var modalZIndex = 2000;  // Inconsistent
```

## Global Imports Setup

All `_Imports.razor` files should follow this standardized pattern:

```razor
@* System and Microsoft Namespaces *@
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Authorization
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop

@* MudBlazor Components and Utilities *@
@using MudBlazor

@* AIMRAN_Data_Science_Lab Core Namespaces *@
@using AIMRAN_Data_Science_Lab
@using AIMRAN_Data_Science_Lab.Components
@using AIMRAN_Data_Science_Lab.Components.Layout

@* AIMRAN_Data_Science_Lab Theme Library *@
@using AIMRAN_Data_Science_Lab.Themes

@* AIMRAN_Data_Science_Lab Services *@
@using AIMRAN_Data_Science_Lab.Services
@using AIMRAN_Data_Science_Lab.Services.Settings
```

## Color Constants Reference

### Primary Colors
- `Primary` - Primary brand color
- `Secondary` - Secondary brand color
- `Tertiary` - Tertiary brand color

### Semantic Colors
- `Success` - Successful operations (green)
- `Error` - Errors and warnings (red)
- `Warning` - Warning state (yellow/orange)
- `Info` - Information (blue)

### Text Colors
- `TextPrimary` - Primary text
- `TextSecondary` - Secondary text
- `TextDisabled` - Disabled text

## Spacing Scale

```
XSmall   = 4px   (0.25rem)
Small    = 8px   (0.5rem)
Default  = 16px  (1rem)
Medium   = 24px  (1.5rem)
Large    = 32px  (2rem)
XLarge   = 48px  (3rem)
XXLarge  = 64px  (4rem)
```

## Typography Variants

```
H1, H2, H3, H4, H5, H6    - Headings
Subtitle1, Subtitle2       - Subtitles
Body1, Body2               - Body text
Button                     - Button text
Caption                    - Small captions
Overline                   - Overline text
```

## Shadow Elevation Scale

```
Elevation 0   = No shadow
Elevation 1   = Subtle
Elevation 2-3 = Default
Elevation 4-6 = Raised
Elevation 8   = Floating
Elevation 12+ = Modal/Prominent
```

## Transition Durations

```
Fast       = 150ms
Base       = 250ms
Slow       = 350ms
Slower     = 500ms
```

## Z-Index Scale

```
Hidden           = -1
Base             = 0
Default          = 1
Dropdown         = 1000
Modal            = 1060
Toast            = 1070
Tooltip          = 1080
Popover          = 1090
FullscreenDialog = 3000
```

## Migration Guide

### Renaming (If applicable)
- `Typo.cs` → Implemented as `TypoStyle` in current file
- `Color.cs` → Implemented as `ThemeColor` in current file

### Namespace Updates
- Old: `AimranDataScienceLab.Shared.Services.Settings`
- New: `AIMRAN_Data_Science_Lab.Services.Settings`

- Old: `AimranDataScienceLab.Engine`
- New: `AIMRAN_Data_Science_Lab.Engine` (via RootNamespace)

## Troubleshooting

### Issue: Type Mismatch for Typo
**Solution**: Use MudBlazor's `Typo` enum directly. `TypoStyle` provides string constants for reference.

```razor
<!-- Correct -->
<MudText Typo="Typo.H1">Heading</MudText>

<!-- Incorrect -->
<MudText Typo="TypoStyle.H1">Heading</MudText>
```

### Issue: Color Enum Conflict
**Solution**: Use `MudBlazor.Color` explicitly when needed.

```csharp
Color.Primary           // MudBlazor enum
ThemeColor.Primary      // String constant "primary"
```

### Issue: Missing Imports
**Solution**: Ensure `_Imports.razor` includes `@using AIMRAN_Data_Science_Lab.Themes`

## Contributing

When adding new theme constants:

1. Identify the category (Spacing, Shadows, Transitions, etc.)
2. Add to the appropriate file in `Themes/`
3. Use semantic naming (not numeric/arbitrary values)
4. Add XML documentation comments
5. Update this README with examples

## Examples by Component Type

### Buttons
```razor
<MudButton Variant="Variant.Filled" 
		   Color="MudBlazor.Color.Primary"
		   Style="@Transitions.StandardTransition">
	Click Me
</MudButton>
```

### Cards
```razor
<MudPaper Elevation="1" 
		  Class="pa-4"
		  Style="box-shadow: @Shadows.Raised;">
	<MudText Typo="Typo.H6">Card Title</MudText>
	<MudText Typo="Typo.Body2">Card content</MudText>
</MudPaper>
```

### Dialogs/Modals
```razor
<MudDialog DefaultFocus="DefaultFocus.FirstChild">
	<DialogContent Style="box-shadow: @Shadows.Modal; z-index: @ZIndex.Modal;">
		<MudText Typo="Typo.H5">Dialog Title</MudText>
	</DialogContent>
</MudDialog>
```

### Forms
```razor
<MudStack Spacing="@(Spacing.FormFieldGap / 4)">
	<MudTextField Label="Email" Variant="Variant.Outlined" />
	<MudTextField Label="Password" Variant="Variant.Outlined" InputType="InputType.Password" />
	<MudButton Variant="Variant.Filled" Color="MudBlazor.Color.Primary">Submit</MudButton>
</MudStack>
```

---

**Last Updated**: 2026-05-07  
**Version**: 1.0  
**Namespace**: `AIMRAN_Data_Science_Lab.Themes`
