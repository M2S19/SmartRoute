import { ActivityIndicator, Pressable, StyleProp, StyleSheet, Text, TextStyle, ViewStyle } from 'react-native';
import { colors, sizes, spacing, typography } from '../../constants/theme';
import { Icon, IconName } from './Icon';

export type ButtonVariant = 'primary' | 'accent' | 'outline' | 'secondary';

interface ButtonProps {
  label: string;
  onPress: () => void;
  variant?: ButtonVariant;
  size?: 'md' | 'sm';
  icon?: IconName;
  loading?: boolean;
  disabled?: boolean;
  style?: StyleProp<ViewStyle>;
}

interface VariantStyle {
  container: ViewStyle;
  pressed: ViewStyle;
  text: TextStyle;
  contentColor: string;
}

const VARIANTS: Record<ButtonVariant, VariantStyle> = {
  // Bouton plein principal (« Se connecter », « Optimiser une tournée »).
  primary: {
    container: { backgroundColor: colors.primary },
    pressed: { backgroundColor: colors.primaryContainer },
    text: typography.labelLg,
    contentColor: colors.onPrimary,
  },
  // Action de l'arrêt actif (« Marquer comme livré »).
  accent: {
    container: { backgroundColor: colors.primaryContainer },
    pressed: { backgroundColor: colors.primary },
    text: typography.labelMd,
    contentColor: colors.onPrimary,
  },
  // Contour bleu (« Chercher le meilleur livreur »).
  outline: {
    container: { borderWidth: 1, borderColor: colors.primary, backgroundColor: 'transparent' },
    pressed: { backgroundColor: colors.surfaceContainerLow },
    text: typography.labelLg,
    contentColor: colors.primary,
  },
  // Utilitaire neutre (« Actualiser la tournée »).
  secondary: {
    container: { borderWidth: 1, borderColor: colors.borderStrong, backgroundColor: colors.surfaceContainerLowest },
    pressed: { backgroundColor: colors.surfaceContainer },
    text: typography.labelMd,
    contentColor: colors.onSurface,
  },
};

export function Button({
  label,
  onPress,
  variant = 'primary',
  size = 'md',
  icon,
  loading = false,
  disabled = false,
  style,
}: ButtonProps) {
  const v = VARIANTS[variant];
  const isDisabled = disabled || loading;

  return (
    <Pressable
      onPress={onPress}
      disabled={isDisabled}
      accessibilityRole="button"
      accessibilityState={{ disabled: isDisabled, busy: loading }}
      style={({ pressed }) => [
        styles.base,
        size === 'sm' ? styles.sm : styles.md,
        v.container,
        pressed && v.pressed,
        isDisabled && styles.disabled,
        style,
      ]}
    >
      {loading ? (
        <ActivityIndicator size="small" color={v.contentColor} />
      ) : icon ? (
        <Icon name={icon} size={size === 'sm' ? sizes.iconSm : 18} color={v.contentColor} />
      ) : null}
      <Text style={[v.text, styles.label, { color: v.contentColor }]}>{label.toUpperCase()}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  base: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.sm,
    paddingHorizontal: spacing.lg,
  },
  md: {
    minHeight: sizes.controlHeight,
    paddingVertical: spacing.md,
  },
  sm: {
    minHeight: 32,
    paddingVertical: 6,
    paddingHorizontal: spacing.md,
  },
  label: {
    textAlign: 'center',
  },
  disabled: {
    opacity: 0.6,
  },
});
