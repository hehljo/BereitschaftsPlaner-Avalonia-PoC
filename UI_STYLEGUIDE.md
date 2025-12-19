# UI Styleguide - Bereitschaftsplaner Avalonia

**Brand:** Yunex Traffic
**Design System:** Corporate Identity 2024/2025
**Framework:** Avalonia 11.3.10 + Fluent Theme

---

## 🎨 Color Palette (Yunex Brand)

### Primary Colors
```
Orange (Primary Action)
#F47738 | RGB(244, 119, 56)
Verwendung: Primary Buttons, Links, Highlights

Yellow (Warning/Attention)
#FFE564 | RGB(255, 229, 100)
Verwendung: Warnings, Tooltips, Hints

Purple (Secondary Action)
#A483FF | RGB(164, 131, 255)
Verwendung: Secondary Buttons, Badges, Tags
```

### Accent Colors
```
Royal Blue (Information)
#1E2ED9 | RGB(30, 46, 217)
Verwendung: Info Dialogs, Links, Headers

Green (Success)
#00E38C | RGB(0, 227, 143)
Verwendung: Success Messages, Checkmarks

Gray (Neutral)
#E4EDED | RGB(228, 237, 237)
Verwendung: Backgrounds, Borders, Disabled States

Steel (Muted)
#688ABA | RGB(104, 138, 186)
Verwendung: Secondary Text, Icons

Sky (Light Accent)
#DEECFF | RGB(222, 236, 255)
Verwendung: Hover States, Light Backgrounds

Lavender (Soft Accent)
#9DBBFF | RGB(157, 187, 255)
Verwendung: Selected States, Focus Indicators

Pistachio (Fresh Accent)
#AFFAD7 | RGB(175, 250, 215)
Verwendung: Positive Feedback, Fresh States
```

### Neutrals
```
Black
#000000 | RGB(0, 0, 0)
Verwendung: Text (Dark Mode Background)

White
#FFFFFF | RGB(255, 255, 255)
Verwendung: Text (Light Mode), Backgrounds
```

---

## 🌓 Dark/Light Mode Variants

### Light Mode (Standard)
```
Background: White (#FFFFFF)
Surface: Gray (#E4EDED)
Text Primary: Black (#000000)
Text Secondary: Steel (#688ABA)
Border: Steel (#688ABA) at 30% opacity
Accent Surface: Sky (#DEECFF)
```

### Dark Mode
```
Background: #1A1A1A (Near Black)
Surface: #2D2D2D (Dark Gray)
Text Primary: White (#FFFFFF)
Text Secondary: Sky (#DEECFF)
Border: Steel (#688ABA) at 50% opacity
Accent Surface: Steel (#688ABA) at 20% opacity
```

**Wichtig:** Alle Brand Colors (Orange, Yellow, Purple, etc.) bleiben **identisch** in beiden Modi!

---

## 📐 Typography (Yunex Corporate Fonts)

### Font Families
```
Primary (Headlines):
  - Jeko SemiBold (Headlines, Titles)
  - Jeko Regular (Callouts, Quotes)

Secondary (Body):
  - Inter Regular (Body Text, Labels)
  - Inter Medium (Emphasized Text, Subheadings)

Fallback (wenn Yunex-Fonts nicht verfügbar):
  - Segoe UI (Windows)
  - San Francisco (macOS)
  - Ubuntu (Linux)
```

### Font Sizes & Hierarchy
```
Display (Hero):     32pt | Jeko SemiBold
H1 (Page Title):    24pt | Jeko SemiBold
H2 (Section):       20pt | Jeko SemiBold
H3 (Subsection):    18pt | Jeko Regular
Body Large:         16pt | Inter Regular
Body:               14pt | Inter Regular
Body Small:         12pt | Inter Regular
Caption:            11pt | Inter Regular
Label (Buttons):    14pt | Inter Medium
```

### Line Heights
```
Headlines:  1.2 (tight)
Body:       1.5 (comfortable)
Captions:   1.3 (compact)
```

---

## 🎭 Component Styles

### Buttons

**Primary Button (Orange)**
```xaml
<Button Classes="primary">
  Background: #F47738 (Orange)
  Foreground: White
  BorderRadius: 6
  Padding: 12,8
  FontFamily: Inter Medium
  FontSize: 14

  Hover: Lighten 10%
  Pressed: Darken 10%
  Disabled: Gray (#E4EDED), Text: Steel (#688ABA)
</Button>
```

**Secondary Button (Purple)**
```xaml
<Button Classes="secondary">
  Background: #A483FF (Purple)
  Foreground: White
  BorderRadius: 6
  Padding: 12,8

  Hover: Lighten 10%
  Pressed: Darken 10%
</Button>
```

**Outline Button**
```xaml
<Button Classes="outline">
  Background: Transparent
  Foreground: Orange (#F47738)
  Border: 2px solid Orange
  BorderRadius: 6
  Padding: 12,8

  Hover: Background Orange 10% opacity
</Button>
```

**Danger Button (Red - not in brand palette, use sparingly)**
```xaml
<Button Classes="danger">
  Background: #DC2626
  Foreground: White
  BorderRadius: 6
  Padding: 12,8

  Use only for destructive actions (Delete, Remove)
</Button>
```

### Cards & Panels

**Card (Elevated)**
```xaml
<Border Classes="card">
  Background: White (Light) / #2D2D2D (Dark)
  CornerRadius: 8
  Padding: 16
  BorderThickness: 1
  BorderBrush: Steel (#688ABA) at 20% opacity

  Box Shadow:
    - Light: 0 2px 8px rgba(0,0,0,0.08)
    - Dark: 0 2px 8px rgba(0,0,0,0.4)
</Border>
```

**Panel (Surface)**
```xaml
<Border Classes="panel">
  Background: Gray (#E4EDED) (Light) / #2D2D2D (Dark)
  CornerRadius: 6
  Padding: 12
  BorderThickness: 0
</Border>
```

**Accent Panel (Info)**
```xaml
<Border Classes="panel-accent">
  Background: Sky (#DEECFF) (Light) / Steel (#688ABA) 20% (Dark)
  CornerRadius: 6
  Padding: 12
  BorderThickness: 1
  BorderBrush: Royal Blue (#1E2ED9) at 30%
</Border>
```

### Inputs

**TextBox**
```xaml
<TextBox>
  Background: White (Light) / #1A1A1A (Dark)
  Foreground: Black (Light) / White (Dark)
  BorderThickness: 1
  BorderBrush: Steel (#688ABA) at 40%
  CornerRadius: 6
  Padding: 8
  FontFamily: Inter Regular
  FontSize: 14

  Focus: BorderBrush Orange (#F47738)
  Error: BorderBrush #DC2626
</TextBox>
```

**ComboBox/Dropdown**
```xaml
<ComboBox>
  Background: White (Light) / #2D2D2D (Dark)
  BorderBrush: Steel (#688ABA) at 40%
  CornerRadius: 6
  Padding: 8

  Hover: BorderBrush Orange (#F47738)
  Open: BorderBrush Orange (#F47738)
</ComboBox>
```

### DataGrid

**Header**
```xaml
Background: Sky (#DEECFF) (Light) / Steel (#688ABA) 30% (Dark)
Foreground: Black (Light) / White (Dark)
FontFamily: Inter Medium
FontSize: 14
Padding: 12,8
BorderBottom: 2px solid Royal Blue (#1E2ED9)
```

**Row**
```xaml
Background: White (Light) / #2D2D2D (Dark)
Foreground: Black (Light) / White (Dark)
FontFamily: Inter Regular
FontSize: 14
Padding: 8
BorderBottom: 1px solid Gray (#E4EDED) (Light) / #3D3D3D (Dark)

Hover: Background Sky (#DEECFF) 30% (Light) / Steel (#688ABA) 10% (Dark)
Selected: Background Lavender (#9DBBFF) 40% (Light) / Purple (#A483FF) 20% (Dark)
Alternate: Background Gray (#E4EDED) 50% (Light) / #2A2A2A (Dark)
```

### Status Indicators

**Success**
```xaml
Background: Green (#00E38C) at 20%
Border: Green (#00E38C)
Icon: ✓ in Green
```

**Warning**
```xaml
Background: Yellow (#FFE564) at 30%
Border: Orange (#F47738)
Icon: ⚠ in Orange
```

**Error**
```xaml
Background: #DC2626 at 20%
Border: #DC2626
Icon: ✕ in Red
```

**Info**
```xaml
Background: Royal Blue (#1E2ED9) at 20%
Border: Royal Blue (#1E2ED9)
Icon: ℹ in Royal Blue
```

### Progress Indicators

**Determinate Progress Bar**
```xaml
Background: Gray (#E4EDED)
Foreground: Orange (#F47738)
Height: 6
CornerRadius: 3
```

**Indeterminate Progress (Loading)**
```xaml
Use Yunex Silver Gradient:
  Steel (#688ABA) → White (#FFFFFF) → Sky (#DEECFF) → Pistachio (#AFFAD7)
Animation: Sweep left-to-right, 2s duration
```

---

## 🌈 Gradients

### Yunex Silver (Primary Brand Gradient)
```
Steel (#688ABA) → White (#FFFFFF) → Sky (#DEECFF) → Pistachio (#AFFAD7)
Verwendung: Hero Sections, Large Backgrounds, Brand Headers
```

### Frosted (Soft Accent)
```
Lavender (#9DBBFF) → Sky (#DEECFF) → White (#FFFFFF)
Verwendung: Overlays, Modal Backgrounds
```

### Spring (Fresh)
```
Sky (#DEECFF) → Gray (#E4EDED) → Pistachio (#AFFAD7)
Verwendung: Success States, Positive Feedback Areas
```

### Deep Blue (Bold)
```
Royal Blue (#1E2ED9) → Lavender (#9DBBFF)
Verwendung: Headers, CTAs, Emphasis Areas
```

### Lagoon (Icons - Special)
```
Royal Blue (#1E2ED9) → Lavender (#9DBBFF) → Pistachio (#AFFAD7)
Verwendung: Icon Fills, Small Accents
```

---

## 📏 Spacing & Layout

### Spacing Scale (8pt Grid)
```
xs:   4pt
sm:   8pt
md:   16pt
lg:   24pt
xl:   32pt
2xl:  48pt
3xl:  64pt
```

### Border Radius
```
Small (Inputs):     6pt
Medium (Cards):     8pt
Large (Panels):     12pt
Circle (Avatars):   50%
```

### Shadows
```
Light Mode:
  - Card: 0 2px 8px rgba(0,0,0,0.08)
  - Elevated: 0 4px 16px rgba(0,0,0,0.12)
  - Modal: 0 8px 32px rgba(0,0,0,0.16)

Dark Mode:
  - Card: 0 2px 8px rgba(0,0,0,0.4)
  - Elevated: 0 4px 16px rgba(0,0,0,0.5)
  - Modal: 0 8px 32px rgba(0,0,0,0.6)
```

---

## 🎯 Icon System

**Icon Library:** FluentAvalonia Icons (Fluent Design System)

**Sizes:**
```
Small:  16x16pt (Inline Icons)
Medium: 24x24pt (Buttons, Tabs)
Large:  32x32pt (Headers)
Hero:   48x48pt (Empty States, Splash)
```

**Colors:**
- Primary Icons: Steel (#688ABA)
- Active Icons: Orange (#F47738)
- Disabled Icons: Gray (#E4EDED)
- Special Icons: Use Lagoon Gradient

---

## 🖼️ Layout Patterns

### Header (App Bar)
```
Height: 64pt
Background: Yunex Silver Gradient (Light) / #1A1A1A (Dark)
Title: H2 (20pt Jeko SemiBold), White
Logo: 40pt height, left aligned
Padding: 16pt horizontal
```

### Content Area
```
Max Width: 1280pt (centered)
Padding: 24pt horizontal, 32pt vertical
Background: White (Light) / #1A1A1A (Dark)
```

### Sidebar (Navigation)
```
Width: 240pt (collapsed: 64pt)
Background: Gray (#E4EDED) (Light) / #2D2D2D (Dark)
Item Height: 48pt
Item Hover: Sky (#DEECFF) 50% (Light) / Steel (#688ABA) 20% (Dark)
Item Active: Orange (#F47738) left border 4pt
```

### Footer (Status Bar)
```
Height: 32pt
Background: Gray (#E4EDED) (Light) / #2D2D2D (Dark)
Text: Caption (11pt Inter Regular), Steel (#688ABA)
Padding: 8pt horizontal
```

---

## ♿ Accessibility

### Contrast Ratios (WCAG AAA)
```
Text on White Background:
  - Large Text (18pt+): 3:1 minimum
  - Normal Text: 7:1 minimum

Text on Dark Background:
  - Large Text (18pt+): 3:1 minimum
  - Normal Text: 7:1 minimum

✅ All brand colors meet WCAG AA standards
⚠ Yellow (#FFE564) on White: Use only with dark text or borders
```

### Focus Indicators
```
Outline: 2pt solid Orange (#F47738)
Offset: 2pt
Border Radius: Inherit from element
```

### Keyboard Navigation
```
Tab Order: Logical top-to-bottom, left-to-right
Shortcuts: Standard Avalonia shortcuts (Ctrl+C, Ctrl+V, etc.)
Escape: Close dialogs/modals
Enter: Confirm actions
```

---

## 🎨 Avalonia Resource Dictionary

```xaml
<Application.Resources>
  <!-- Brand Colors -->
  <SolidColorBrush x:Key="YunexOrange">#F47738</SolidColorBrush>
  <SolidColorBrush x:Key="YunexYellow">#FFE564</SolidColorBrush>
  <SolidColorBrush x:Key="YunexPurple">#A483FF</SolidColorBrush>
  <SolidColorBrush x:Key="YunexRoyalBlue">#1E2ED9</SolidColorBrush>
  <SolidColorBrush x:Key="YunexGreen">#00E38C</SolidColorBrush>
  <SolidColorBrush x:Key="YunexGray">#E4EDED</SolidColorBrush>
  <SolidColorBrush x:Key="YunexSteel">#688ABA</SolidColorBrush>
  <SolidColorBrush x:Key="YunexSky">#DEECFF</SolidColorBrush>
  <SolidColorBrush x:Key="YunexLavender">#9DBBFF</SolidColorBrush>
  <SolidColorBrush x:Key="YunexPistachio">#AFFAD7</SolidColorBrush>

  <!-- Typography -->
  <FontFamily x:Key="HeadlineFont">Jeko SemiBold, Segoe UI Semibold</FontFamily>
  <FontFamily x:Key="BodyFont">Inter Regular, Segoe UI</FontFamily>
  <FontFamily x:Key="BodyFontMedium">Inter Medium, Segoe UI Semibold</FontFamily>

  <!-- Spacing -->
  <Thickness x:Key="SpacingXs">4</Thickness>
  <Thickness x:Key="SpacingSm">8</Thickness>
  <Thickness x:Key="SpacingMd">16</Thickness>
  <Thickness x:Key="SpacingLg">24</Thickness>
  <Thickness x:Key="SpacingXl">32</Thickness>

  <!-- Corner Radius -->
  <CornerRadius x:Key="RadiusSmall">6</CornerRadius>
  <CornerRadius x:Key="RadiusMedium">8</CornerRadius>
  <CornerRadius x:Key="RadiusLarge">12</CornerRadius>
</Application.Resources>
```

---

## 📋 Component Usage Examples

### Primary Action Button
```xaml
<Button Classes="primary"
        Content="Importieren"
        HorizontalAlignment="Center"
        Padding="24,12"
        Margin="0,16,0,0"/>
```

### Info Card with Brand Colors
```xaml
<Border Classes="card"
        Background="{DynamicResource YunexSky}"
        BorderBrush="{DynamicResource YunexRoyalBlue}"
        BorderThickness="1"
        CornerRadius="{StaticResource RadiusMedium}"
        Padding="{StaticResource SpacingMd}">
  <StackPanel>
    <TextBlock Text="Information"
               FontFamily="{StaticResource HeadlineFont}"
               FontSize="18"
               Foreground="{DynamicResource YunexRoyalBlue}"/>
    <TextBlock Text="Ihre Daten wurden erfolgreich importiert."
               FontFamily="{StaticResource BodyFont}"
               FontSize="14"
               Margin="0,8,0,0"/>
  </StackPanel>
</Border>
```

### DataGrid with Yunex Colors
```xaml
<DataGrid HeaderBackground="{DynamicResource YunexSky}"
          GridLinesVisibility="Horizontal"
          BorderBrush="{DynamicResource YunexSteel}"
          BorderThickness="1"
          CornerRadius="{StaticResource RadiusMedium}">
  <!-- Columns -->
</DataGrid>
```

---

## 🚫 Don'ts

❌ **Niemals** Brand Colors für Dark Mode anpassen (außer Opazität)
❌ **Niemals** Schriftarten außerhalb der Corporate Fonts verwenden
❌ **Niemals** Random Colors verwenden - immer aus Palette wählen
❌ **Niemals** Yellow (#FFE564) als Text auf White Background
❌ **Niemals** mehr als 2 Primary Colors in einem Screen kombinieren
❌ **Niemals** Gradients als Button Backgrounds (nur Backgrounds/Headers)

---

## ✅ Do's

✅ **Immer** Orange (#F47738) für Primary Actions
✅ **Immer** Spacing aus 8pt Grid verwenden
✅ **Immer** Kontrast-Ratios prüfen (WCAG AA minimum)
✅ **Immer** Theme-aware Brushes verwenden (`{DynamicResource}`)
✅ **Immer** Focus Indicators sichtbar machen
✅ **Immer** Consistent Corner Radius verwenden

---

**Version:** 1.0
**Letzte Aktualisierung:** 2025-12-19
**Basierend auf:** Yunex Traffic Corporate Identity 2024/2025
