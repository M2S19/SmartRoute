import { StyleSheet, Text, View } from 'react-native';
import { colors, spacing, typography } from '../../constants/theme';

export type BadgeTone = 'neutral' | 'primary' | 'active' | 'muted';

interface BadgeProps {
  label: string;
  tone?: BadgeTone;
  uppercase?: boolean;
}

const TONES: Record<BadgeTone, { backgroundColor: string; color: string }> = {
  neutral: { backgroundColor: colors.surfaceContainerLow, color: colors.secondary }, // « Fonction 1 », « À livrer »
  primary: { backgroundColor: colors.primaryTint, color: colors.primary }, // « Affectée »
  active: { backgroundColor: colors.secondaryContainer, color: colors.onSecondaryFixed }, // prochain arrêt
  muted: { backgroundColor: colors.surfaceContainer, color: colors.secondary }, // « Livré »
};

export function Badge({ label, tone = 'neutral', uppercase = true }: BadgeProps) {
  const { backgroundColor, color } = TONES[tone];
  return (
    <View style={[styles.badge, { backgroundColor }]}>
      <Text style={[styles.label, { color }]}>{uppercase ? label.toUpperCase() : label}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  badge: {
    paddingHorizontal: spacing.sm,
    paddingVertical: 2,
    alignSelf: 'flex-start',
  },
  label: {
    ...typography.labelSm,
  },
});
