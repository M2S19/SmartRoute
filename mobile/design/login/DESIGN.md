---
name: SmartRoute Engineering Console
colors:
  surface: '#faf8ff'
  surface-dim: '#d2d9f4'
  surface-bright: '#faf8ff'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f2f3ff'
  surface-container: '#eaedff'
  surface-container-high: '#e2e7ff'
  surface-container-highest: '#dae2fd'
  on-surface: '#131b2e'
  on-surface-variant: '#434653'
  inverse-surface: '#283044'
  inverse-on-surface: '#eef0ff'
  outline: '#737784'
  outline-variant: '#c3c6d5'
  surface-tint: '#1d59c1'
  primary: '#003c90'
  on-primary: '#ffffff'
  primary-container: '#0f52ba'
  on-primary-container: '#bcceff'
  inverse-primary: '#b0c6ff'
  secondary: '#515f74'
  on-secondary: '#ffffff'
  secondary-container: '#d5e3fc'
  on-secondary-container: '#57657a'
  tertiary: '#732900'
  on-tertiary: '#ffffff'
  tertiary-container: '#993900'
  on-tertiary-container: '#ffc0a7'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#d9e2ff'
  primary-fixed-dim: '#b0c6ff'
  on-primary-fixed: '#001945'
  on-primary-fixed-variant: '#00419c'
  secondary-fixed: '#d5e3fc'
  secondary-fixed-dim: '#b9c7df'
  on-secondary-fixed: '#0d1c2e'
  on-secondary-fixed-variant: '#3a485b'
  tertiary-fixed: '#ffdbcd'
  tertiary-fixed-dim: '#ffb596'
  on-tertiary-fixed: '#360f00'
  on-tertiary-fixed-variant: '#7d2d00'
  background: '#faf8ff'
  on-background: '#131b2e'
  surface-variant: '#dae2fd'
typography:
  display:
    fontFamily: Space Grotesk
    fontSize: 48px
    fontWeight: '700'
    lineHeight: 52px
    letterSpacing: -0.03em
  headline-lg:
    fontFamily: Space Grotesk
    fontSize: 32px
    fontWeight: '600'
    lineHeight: 36px
    letterSpacing: -0.02em
  headline-lg-mobile:
    fontFamily: Space Grotesk
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 28px
    letterSpacing: -0.02em
  headline-md:
    fontFamily: Space Grotesk
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 24px
    letterSpacing: -0.01em
  headline-sm:
    fontFamily: Space Grotesk
    fontSize: 16px
    fontWeight: '600'
    lineHeight: 20px
    letterSpacing: 0em
  body-lg:
    fontFamily: IBM Plex Sans
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  body-md:
    fontFamily: IBM Plex Sans
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  body-sm:
    fontFamily: IBM Plex Sans
    fontSize: 12px
    fontWeight: '400'
    lineHeight: 16px
  label-lg:
    fontFamily: Space Grotesk
    fontSize: 13px
    fontWeight: '600'
    lineHeight: 16px
    letterSpacing: 0.04em
  label-md:
    fontFamily: Space Grotesk
    fontSize: 11px
    fontWeight: '500'
    lineHeight: 14px
    letterSpacing: 0.06em
  label-sm:
    fontFamily: Space Grotesk
    fontSize: 10px
    fontWeight: '600'
    lineHeight: 12px
    letterSpacing: 0.08em
spacing:
  gutter: 1rem
  margin: 1.5rem
  space-xs: 0.25rem
  space-sm: 0.5rem
  space-md: 0.75rem
  space-lg: 1.25rem
  space-xl: 2rem
---

## Brand & Style

This design system establishes a stark, utilitarian engineering terminal interface built for rigorous internal operations, algorithmic routing, and logistics management. Inspired by precision CAD interfaces, mainframe control boards, and technical blueprints, the aesthetic discards all ornamental softness in favor of structural clarity, density, and unyielding functionality.

The target users are systems engineers, dispatch operators, and logistics analysts who value high density of operational data over decorative chrome. The emotional resonance is one of analytical calm, extreme reliability, and structural discipline:
- **Tone:** Academic, clinical, transparent, and deterministic.
- **Visual Stance:** Neo-brutalist engineering minimalism. Absolute zero radii across all elements, visible architectural line work, rigid wireframe delineation, and high-legibility geometric typography.
- **Restraint:** No skeuomorphic bevels, no gradient fills, no soft atmospheric blurs. Boundaries and layers are communicated exclusively through mechanical perimeter rules, gridlines, and hard-edged background shifts.

## Colors

The palette operates with clinical precision. Structural clarity depends on a light substrate, reinforced by the authoritative seed `#0F52BA` (Sapphire Blue) as the operational focal point.

- **Primary (`#0F52BA`):** Used strictly for active execution states, authoritative interactive elements, key routing paths, and system verification flags.
- **Secondary (`#475569`):** A cold slate tone dedicated to structural metadata, secondary controls, coordinate tags, and technical sub-labels.
- **Neutral Base (`#0F172A` / `#020617`):** Deep carbon inks applied to core data displays, monospace numeric figures, and boundary rules to establish uncompromising readability.
- **Background Tiers:**
  - Canvas / Substrate: `#F8FAFC` (clinical engineering white-grey).
  - Surface Panel: `#FFFFFF` (pure contrast plane).
  - Nested Inset / Terminal Well: `#F1F5F9`.
- **System Borders (`#CBD5E1` & `#94A3B8`):** Sharp hairpins that define the CAD wireframe grid structure without overwhelming density. High-contrast outlines use `#0F172A` for focused inputs and selected states.

## Typography

Typography functions as a precision instrument. The typographic hierarchy pairs the raw geometric character of **Space Grotesk** for structural headers, machine markers, and operational controls with the balanced, industrial legibility of **IBM Plex Sans** for long-form telemetry, tabular reads, and parameter fields.

- **Uppercase Treatment:** All terminal tags, table column descriptors, step indices, and section caps utilize `Space Grotesk` set to uppercase with distinct tracking (`0.04em` to `0.08em`) for rapid scanning across dense layouts.
- **Numbers and Quantities:** Tabular and lining numerals are enforced across all telemetry panels, vehicle IDs, metrics, and timestamps to eliminate column jitter during live updates.
- **Contrast & Hierarchy:** Critical identifiers prioritize weight (`600`/`700`) over color diversity, anchoring the user's focus through typographical scale and layout coordinates.

## Layout & Spacing

The layout is grounded in an engineering workbench paradigm. The screen area is treated as a continuous matrix where modules, data tables, node trees, and interactive map consoles dock together edge-to-edge.

- **Grid Structure:** A rigid 12-column coordinate framework with `1rem` gutters on desktop displays. Outer gutters contract or snap flush against viewport margins (`1.5rem` on desktop, `0.75rem` on mobile consoles).
- **Spatial Rhythm:** Tight, disciplined micro-increments (`space-xs` = 4px, `space-sm` = 8px, `space-md` = 12px) guarantee high operational density without visual noise. Spacing separates functional components by exact offsets rather than ambient gaps.
- **Docking & Partitioning:** Containers use shared 1px solid boundaries instead of detached floating cards. Elements tile seamlessly into split panels, telemetry strips, and toolbars, ensuring the entire viewport serves data manipulation and decision execution.
- **Reflow Discipline:** On narrow displays (mobile diagnostics), multi-column monitoring boards stack vertically into linear step registers, maintaining flush rectangular edges.

## Elevation & Depth

This design system explicitly rejects simulated z-axis lighting, atmospheric drop shadows, and soft blurs. Depth is expressed purely through structural drafting techniques:

- **Surface Layering:** Hierarchy is generated by stacking hard tonal planes. An inset telemetry box sits directly on `#F1F5F9`, bordered by `#CBD5E1`, inset within a `#FFFFFF` panel.
- **Bold Line Borders:** Elevation and separation are defined by crisp, 1px or 2px rectilinear borders (`#CBD5E1` for passive bounds, `#0F172A` for active focus, and `#0F52BA` for primary operations).
- **Hard Technical Casts (Discrete States Only):** When a floating modal, context menu, or dropdown requires immediate spatial detachment from the canvas, it utilizes an unblurred, hard-offset directional shadow: `box-shadow: 3px 3px 0px 0px #0F172A`. This ensures the UI resembles an architectural draft or hardware terminal.

## Shapes

The shape system enforces an absolute zero-radius policy (`0px` / `0`). 

- Every button, input field, modal sheet, badge, notification tile, tab, and segment border terminates in razor-sharp 90-degree corners.
- Curves and organic radii are forbidden. This strict geometric austerity reinforces the CAD tool identity, maximizes physical screen space for data fields, and aligns all pixel boundaries cleanly to the hardware grid.

## Components

### Buttons
- **Primary:** Solid `#0F52BA` background, sharp `0px` corners, white bold `Space Grotesk` text, high-density padding (`0.5rem 1rem`). Hover: `#0A3F91`. Active: solid `#0F172A` fill.
- **Secondary / Outline:** Background transparent, 1px solid `#0F172A` border, `#0F172A` typography. Hover: `#0F172A` fill with `#FFFFFF` text.
- **Utility / Inactive:** Background `#F1F5F9`, 1px solid `#CBD5E1`, text `#475569`. Hover: `#E2E8F0`.

### Form Fields & Inputs
- Rectangular blocks with a 1px solid `#CBD5E1` border and `#FFFFFF` background.
- Focus state: Replaces border with a crisp 2px solid `#0F52BA` or `#0F172A` stroke with zero outline glow.
- Labels are positioned strictly above inputs using `label-md` uppercase Space Grotesk. Monospace notation (`IBM Plex Sans`) is used for input data.

### Data Cards & Modular Panels
- Flat white containers framed with 1px `#CBD5E1` rules.
- Card headers feature an integrated technical bar: a shaded `#F8FAFC` strip separated by a 1px bottom border, housing title text, step index, and right-aligned system status tags.

### Status Chips & Tags
- Zero border-radius rectangular tags.
- Constructed with a 1px solid outline matching the status type:
  - Nominal: `#0F52BA` border on `#EFF6FF` tint.
  - Caution: `#B45309` border on `#FEF3C7` tint.
  - Critical: `#B91C1C` border on `#FEE2E2` tint.
- All copy is set in `label-sm` uppercase.

### Checkboxes & Radios
- Hard-edged `16px x 16px` squares (`0px` radius).
- Unchecked: `#FFFFFF` fill with a 1px `#475569` border.
- Checked: `#0F52BA` solid fill containing an unsoftened, sharp geometric check indicator or solid inner square.

### Tables & Telemetry Lists
- Border-collapsed tabular registers. Header rows feature a solid `#F1F5F9` background with bottom-line separators (`1px solid #0F172A`).
- Alternating rows employ subtle zebra shading (`#FFFFFF` to `#F8FAFC`). Hover states trigger a clean `#EFF6FF` highlight across the entire horizontal scanline.