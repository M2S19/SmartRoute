import { TextStyle } from 'react-native';

/**
 * Design tokens extraits des maquettes Stitch (design/login et design/driver-route,
 * config Tailwind de code.html). Le Dashboard (design/manager-dashboard) est harmonisé
 * sur ces mêmes tokens.
 */

export const colors = {
  primary: '#003c90',
  onPrimary: '#ffffff',
  primaryContainer: '#0f52ba',
  primaryTint: 'rgba(0, 60, 144, 0.10)', // bg-primary/10 (badge « Affectée »)

  secondary: '#515f74',
  secondaryContainer: '#d5e3fc',
  onSecondaryFixed: '#0d1c2e',

  surface: '#faf8ff',
  surfaceContainerLowest: '#ffffff',
  surfaceContainerLow: '#f2f3ff',
  surfaceContainer: '#eaedff',
  surfaceContainerHigh: '#e2e7ff',
  onSurface: '#131b2e',
  onSurfaceVariant: '#434653',

  outline: '#737784',
  outlineVariant: '#c3c6d5',
  borderSubtle: 'rgba(195, 198, 213, 0.4)', // border-outline-variant/40
  borderFaint: 'rgba(195, 198, 213, 0.3)', // border-outline-variant/30
  borderStrong: 'rgba(19, 27, 46, 0.2)', // border-on-surface/20

  error: '#ba1a1a',
  errorContainer: '#ffdad6',
  onErrorContainer: '#93000a',

  // Seule couleur hors design system Stitch : « +2,4 km » du Dashboard (emerald-700).
  success: '#047857',
} as const;

export const spacing = {
  xs: 4, // space-xs
  sm: 8, // space-sm
  md: 12, // space-md
  gutter: 16, // gutter
  lg: 20, // space-lg
  margin: 24, // margin
  xl: 32, // space-xl
} as const;

// Maquettes en angles vifs : aucun arrondi.
export const radius = {
  none: 0,
  full: 9999,
} as const;

export const fonts = {
  grotesk500: 'SpaceGrotesk_500Medium',
  grotesk600: 'SpaceGrotesk_600SemiBold',
  grotesk700: 'SpaceGrotesk_700Bold',
  plex400: 'IBMPlexSans_400Regular',
  plex400Italic: 'IBMPlexSans_400Regular_Italic',
  plex500: 'IBMPlexSans_500Medium',
  plex600: 'IBMPlexSans_600SemiBold',
} as const;

// En React Native, letterSpacing est en points : em × fontSize.
export const typography = {
  headlineLgMobile: { fontFamily: fonts.grotesk600, fontSize: 24, lineHeight: 28, letterSpacing: -0.48 },
  headlineMd: { fontFamily: fonts.grotesk600, fontSize: 20, lineHeight: 24, letterSpacing: -0.2 },
  headlineSm: { fontFamily: fonts.grotesk600, fontSize: 16, lineHeight: 20 },
  bodyLg: { fontFamily: fonts.plex400, fontSize: 16, lineHeight: 24 },
  bodyMd: { fontFamily: fonts.plex400, fontSize: 14, lineHeight: 20 },
  bodySm: { fontFamily: fonts.plex400, fontSize: 12, lineHeight: 16 },
  labelLg: { fontFamily: fonts.grotesk600, fontSize: 13, lineHeight: 16, letterSpacing: 0.52 },
  labelMd: { fontFamily: fonts.grotesk500, fontSize: 11, lineHeight: 14, letterSpacing: 0.66 },
  labelSm: { fontFamily: fonts.grotesk600, fontSize: 10, lineHeight: 12, letterSpacing: 0.8 },
} satisfies Record<string, TextStyle>;

export const sizes = {
  controlHeight: 44, // h-11 (boutons et champs)
  iconSm: 16,
  iconMd: 20,
  iconLg: 24,
  stepIndex: 24, // w-6 h-6 (numéro d'arrêt)
  logo: 48, // w-12 h-12
} as const;
