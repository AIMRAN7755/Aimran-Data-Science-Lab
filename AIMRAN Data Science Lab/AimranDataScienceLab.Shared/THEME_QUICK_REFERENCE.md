# Theme System - Quick Reference Guide

## Overview
The AIMRAN Data Science Lab theme system provides consistent, production-grade styling through centralized theme constants and utilities.

## Theme Namespaces

```csharp
using AIMRAN_Data_Science_Lab.Themes;

// Access theme constants
Spacing.Default          // 16px
Shadows.Elevation3       // Material Design elevation 3
Transitions.StandardTransition  // 250ms cubic-bezier ease
ZIndex.Modal             // 1060
ThemeColor.Primary       // "primary" string
TypoStyle.H1             // "h1" string
```

## Common Patterns

### Spacing
```razor
<!-- Using constants -->
<MudStack Spacing="@(Spacing.Default / 4)">
	<MudText>Compact spacing</MudText>
</MudStack>

<!-- CSS classes -->
<div class="p-16 m-24">Padding 16px, Margin 24px</div>
```

### Shadows
```razor
<!-- Material Design elevation -->
<MudPaper Elevation="3">Standard card</MudPaper>

<!-- Using shadow constants -->
<MudPaper Elevation="0" Style="box-shadow: @Shadows.Elevation6;">
	Raised content
</MudPaper>
```

### Transitions
```razor
<!-- Standard animation -->
<div Style="transition: @Transitions.StandardTransition;">
	Animates on change
</div>

<!-- Custom timing -->
<div Style="transition: @Transitions.CreateTransition("opacity", 200);">
	200ms opacity change
</div>
```

### Z-Index
```razor
<!-- Dialog z-index -->
<MudDialog Style="z-index: @ZIndex.Modal;">
	Modal dialog
</MudDialog>

<!-- Tooltip z-index -->
<MudTooltip Style="z-index: @ZIndex.Tooltip;">
	Hover me
</MudTooltip>
```

### Typography
```razor
<!-- Using MudBlazor Typo enum (preferred) -->
<MudText Typo="Typo.H1">Large heading</MudText>
<MudText Typo="Typo.Body1">Body text</MudText>

<!-- Using TypoStyle constants -->
<MudText Typo="TypoStyle.Subtitle2">Subtitle</MudText>
```

### Colors
```razor
<!-- Using MudBlazor Color enum (preferred) -->
<MudButton Color="Color.Primary">Click me</MudButton>

<!-- Using ThemeColor constants for text -->
<MudText Style="color: @ThemeColor.Primary;">
	Colored text
</MudText>
```

## Theme Extensions

### CSS Variables
```csharp
string primary = ThemeExtensions.CssVar("primary");
// Result: "var(--mud-palette-primary)"
```

### Hover Effects
```csharp
string hover = ThemeExtensions.GetHoverTransform(1.05);
// Result: "transform: scale(1.05); transition: all 250ms cubic-bezier(0.4, 0, 0.2, 1);"
```

### Focus Styling
```csharp
string focus = ThemeExtensions.GetFocusStyle("primary");
// Result: "outline: 2px solid var(--mud-palette-primary); outline-offset: 2px;"
```

### Custom Transitions
```csharp
// Single property
string transition = Transitions.CreateTransition("opacity", 300, Transitions.EaseOut);

// Multiple properties
string multi = Transitions.CreateMultipleTransition(250, Transitions.EaseInOut, 
	"background-color", "color", "transform");
```

## Constants Reference

### Spacing Values
- `XSmall`: 4px
- `Small`: 8px
- `Default`: 16px
- `Medium`: 24px
- `Large`: 32px
- `XLarge`: 48px
- `XXLarge`: 64px

### Shadow Elevations
- `Elevation0`: No shadow
- `Elevation1`: 2px subtle
- `Elevation3`: 6px standard
- `Elevation6`: 12px raised
- `Elevation12`: 24px floating
- `Elevation24`: 48px modal

### Transition Durations
- `Fast`: 150ms
- `Base`: 250ms
- `Slow`: 350ms
- `Slower`: 500ms

### Z-Index Layers
- `Base`: 0
- `Floating`: 100
- `Dropdown`: 1000
- `Modal`: 1060
- `Toast`: 1070
- `Tooltip`: 1080
- `Alert`: 2000

## Best Practices

✅ **DO:**
- Use `Typo` enum for component properties (MudBlazor)
- Use `Spacing.Default` for consistency
- Use `Shadows.Elevation3` for standard cards
- Use `Transitions.StandardTransition` for animations
- Import `AIMRAN_Data_Science_Lab.Themes` in _Imports.razor

❌ **DON'T:**
- Hard-code color values like `#FF0000`
- Use arbitrary spacing values like `18px`
- Create custom shadow definitions
- Hardcode transition timings
- Use conflicting type names

## _Imports.razor Template

```razor
@* Add to your component scope's _Imports.razor *@
@using AIMRAN_Data_Science_Lab.Themes
```

Global imports already include themes:
```razor
@* Components/_Imports.razor includes: *@
@using AIMRAN_Data_Science_Lab.Themes
```

## Examples

### Settings Panel
```razor
<MudPaper Elevation="0" Class="p-24" Style="background-color: var(--mud-palette-surface);">
	<MudStack Spacing="@(Spacing.Medium / 4)">
		<MudText Typo="Typo.H5" Class="font-weight-bold">Appearance</MudText>
		<MudText Typo="Typo.Body2">Customize the look and feel</MudText>
		<MudDivider />
		<MudButton Variant="Variant.Filled" Color="Color.Primary" 
				   Style="transition: @Transitions.StandardTransition;">
			Save Changes
		</MudButton>
	</MudStack>
</MudPaper>
```

### Animated Card
```razor
<MudPaper Elevation="3" 
		  Class="pa-4" 
		  Style="box-shadow: @Shadows.Elevation6; 
				  transition: @Transitions.StandardTransition;"
		  @onmouseenter="@((e) => cardHovered = true)"
		  @onmouseleave="@((e) => cardHovered = false)">
	<MudText Typo="Typo.Body1">Interactive content</MudText>
</MudPaper>
```

### Dialog
```razor
<MudDialog Style="z-index: @ZIndex.Modal;">
	<DialogContent>
		<MudStack Spacing="@(Spacing.Medium)">
			<MudText Typo="Typo.H6">Confirm Action</MudText>
			<MudText Typo="Typo.Body2">Are you sure?</MudText>
		</MudStack>
	</DialogContent>
</MudDialog>
```

## Troubleshooting

### "TypoStyle not found"
→ Add `@using AIMRAN_Data_Science_Lab.Themes` to _Imports.razor

### "Typo.h4 is invalid"
→ Use `Typo.H4` (capital H) - this is MudBlazor enum

### "Theme constants undefined"
→ Ensure global _Imports.razor includes `@using AIMRAN_Data_Science_Lab.Themes`

### Build fails on namespace
→ Check RootNamespace in .csproj files match AIMRAN_Data_Science_Lab pattern

---

**Last Updated**: January 8, 2025
**Version**: 1.0 - Production Ready
