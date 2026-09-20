# AIMRAN Data Science Lab Theme Library - Quick Reference

## 🎨 Theme Namespaces

```csharp
using AIMRAN_Data_Science_Lab.Themes;
using MudBlazor;  // For Typo, Color enums
```

## 📏 Spacing

```csharp
Spacing.XSmall          // 4px
Spacing.Small           // 8px
Spacing.Default         // 16px
Spacing.Medium          // 24px
Spacing.Large           // 32px
Spacing.XLarge          // 48px
Spacing.XXLarge         // 64px

// Semantic
Spacing.FormFieldGap    // 12px (form fields)
Spacing.ComponentGap    // 16px (between components)
Spacing.SectionGap      // 24px (section boundaries)
Spacing.PageGutter      // 32px (page margins)
```

## 🎯 Typography

```razor
<MudText Typo="Typo.H1">Heading 1</MudText>
<MudText Typo="Typo.H2">Heading 2</MudText>
<MudText Typo="Typo.H3">Heading 3</MudText>
<MudText Typo="Typo.H4">Heading 4</MudText>
<MudText Typo="Typo.H5">Heading 5</MudText>
<MudText Typo="Typo.H6">Heading 6</MudText>

<MudText Typo="Typo.Subtitle1">Subtitle 1</MudText>
<MudText Typo="Typo.Subtitle2">Subtitle 2</MudText>

<MudText Typo="Typo.Body1">Regular body text</MudText>
<MudText Typo="Typo.Body2">Smaller body text</MudText>

<MudText Typo="Typo.Button">Button text</MudText>
<MudText Typo="Typo.Caption">Caption text</MudText>
<MudText Typo="Typo.Overline">Overline text</MudText>
```

## 🎨 Colors

```razor
<MudButton Color="MudBlazor.Color.Primary">Primary</MudButton>
<MudButton Color="MudBlazor.Color.Secondary">Secondary</MudButton>
<MudButton Color="MudBlazor.Color.Success">Success</MudButton>
<MudButton Color="MudBlazor.Color.Error">Error</MudButton>
<MudButton Color="MudBlazor.Color.Warning">Warning</MudButton>
<MudButton Color="MudBlazor.Color.Info">Info</MudButton>

<!-- Text Colors -->
<MudText Style="color: var(--mud-palette-text-primary);">Primary Text</MudText>
<MudText Style="color: var(--mud-palette-text-secondary);">Secondary Text</MudText>
<MudText Style="color: var(--mud-palette-text-disabled);">Disabled Text</MudText>
```

## 🌟 Shadows

```razor
<MudPaper Style="box-shadow: @Shadows.Subtle;">Subtle Shadow</MudPaper>
<MudPaper Style="box-shadow: @Shadows.Default;">Default Shadow</MudPaper>
<MudPaper Style="box-shadow: @Shadows.Raised;">Raised Shadow</MudPaper>
<MudPaper Style="box-shadow: @Shadows.Floating;">Floating Shadow</MudPaper>
<MudPaper Style="box-shadow: @Shadows.Modal;">Modal Shadow</MudPaper>

<!-- Elevation approach -->
<MudPaper Elevation="1">Subtle</MudPaper>
<MudPaper Elevation="3">Default</MudPaper>
<MudPaper Elevation="6">Raised</MudPaper>
<MudPaper Elevation="12">Floating</MudPaper>
```

## ⏱️ Transitions

```razor
<!-- Standard transitions -->
<div style="@Transitions.StandardTransition">Smooth on all properties</div>
<div style="@Transitions.ShortTransition">Fast transition</div>
<div style="@Transitions.LongTransition">Slow transition</div>

<!-- Property-specific transitions -->
<div style="@Transitions.ColorTransition">Only color changes animate</div>
<div style="@Transitions.OpacityTransition">Only opacity changes animate</div>
<div style="@Transitions.TransformTransition">Only transforms animate</div>

<!-- Duration values -->
@* 
Transitions.Fast       = 150ms
Transitions.Base       = 250ms
Transitions.Slow       = 350ms
Transitions.Slower     = 500ms
*@
```

## 📍 Z-Index

```csharp
int dropdown = ZIndex.Dropdown;        // 1000
int sticky = ZIndex.Sticky;            // 1020
int fixed = ZIndex.Fixed;              // 1030
int modal = ZIndex.Modal;              // 1060
int toast = ZIndex.Toast;              // 1070
int tooltip = ZIndex.Tooltip;          // 1080
int fullscreen = ZIndex.FullscreenDialog; // 3000
```

## 🛠️ Common Patterns

### Form Layout
```razor
@using AIMRAN_Data_Science_Lab.Themes

<MudStack Spacing="@(Spacing.FormFieldGap / 4)">
	<MudTextField Label="Email" Variant="Variant.Outlined" />
	<MudTextField Label="Password" Variant="Variant.Outlined" InputType="InputType.Password" />
	<MudButton Variant="Variant.Filled" Color="MudBlazor.Color.Primary">
		Submit
	</MudButton>
</MudStack>
```

### Card Component
```razor
<MudPaper Elevation="1" Class="pa-4" Style="box-shadow: @Shadows.Raised;">
	<MudStack Spacing="@(Spacing.ComponentGap / 4)">
		<MudText Typo="Typo.H6" Class="font-weight-bold">Card Title</MudText>
		<MudText Typo="Typo.Body2">Card content goes here</MudText>
		<MudButton Variant="Variant.Text" Color="MudBlazor.Color.Primary">
			Action
		</MudButton>
	</MudStack>
</MudPaper>
```

### Hover Interactive
```razor
<div style="cursor: pointer; @ThemeExtensions.GetHoverTransform(1.05)">
	Hover me for scale effect
</div>
```

### Section Divider
```razor
<MudStack Spacing="@(Spacing.SectionGap / 4)">
	<MudText Typo="Typo.H5">Section 1</MudText>
	<MudText Typo="Typo.Body2">Content</MudText>

	<MudDivider />

	<MudText Typo="Typo.H5">Section 2</MudText>
	<MudText Typo="Typo.Body2">More content</MudText>
</MudStack>
```

## 📊 Responsive Spacing Example

```razor
@* Mobile: 16px, Tablet: 24px, Desktop: 32px *@
<MudStack Spacing="2">
	@foreach (var item in items)
	{
		<MudCard>@item.Name</MudCard>
	}
</MudStack>
```

## 🎭 Using ThemeProvider

```razor
@* App.razor or Layout *@
<ThemeProvider>
	<RouterView />
</ThemeProvider>

@* In child component *@
@inject ThemeProvider ThemeProvider

<MudPaper Style="box-shadow: @ThemeProvider.GetShadow("floating");">
	<MudStack Spacing="@(ThemeProvider.GetSpacing("md") / 4)">
		<MudText>Themed content</MudText>
	</MudStack>
</MudPaper>
```

## ✅ Best Practices

1. **Always use Typo enum** (not strings):
   ```razor
   <!-- ✅ Correct -->
   <MudText Typo="Typo.H1">Heading</MudText>

   <!-- ❌ Wrong -->
   <MudText Typo="'h1'">Heading</MudText>
   ```

2. **Use semantic spacing names**:
   ```csharp
   // ✅ Correct
   Spacing.ComponentGap

   // ❌ Wrong
   Spacing.Default  // Too vague
   ```

3. **Import themes globally**:
   ```razor
   @* In _Imports.razor *@
   @using AIMRAN_Data_Science_Lab.Themes
   ```

4. **Use color variables for dynamic styling**:
   ```razor
   <!-- ✅ Correct -->
   <div style="color: var(--mud-palette-text-primary);">

   <!-- ❌ Wrong (hard-coded) -->
   <div style="color: #424242;">
   ```

5. **Leverage transitions for polish**:
   ```razor
   <!-- ✅ Correct -->
   <button style="@Transitions.StandardTransition">

   <!-- ❌ Wrong (no transition) -->
   <button>
   ```

## 🔗 File References

- **Theme Library**: `Themes/` directory
- **README**: `Themes/README.md`
- **Examples**: See `Components/Settings/` components
- **Documentation**: `THEME_IMPLEMENTATION_REPORT.md`

## 📱 Common Spacing Combinations

```csharp
// Tight form
Spacing.FormFieldGap          // 12px

// Normal component gap
Spacing.ComponentGap          // 16px

// Section separation
Spacing.SectionGap            // 24px

// Page margins
Spacing.PageGutter            // 32px

// Custom: half spacing
Spacing.Default / 2           // 8px

// Custom: 1.5x spacing
Spacing.Default * 1.5         // 24px
```

## ⚡ Performance Tips

1. Use pre-built transitions instead of custom CSS
2. Reference constants instead of hard-coded values
3. Leverage MudBlazor's Elevation instead of custom shadows when possible
4. Use CSS variables for theme colors (automatic dark mode support)

---

**For complete documentation**: See `Themes/README.md`  
**Last Updated**: 2026-05-07  
**Version**: 1.0
