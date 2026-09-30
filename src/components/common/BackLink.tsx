import { Pressable, StyleSheet, Text } from 'react-native';
import { colors, sizes, spacing, typography } from '../../constants/theme';
import { Icon } from './Icon';

interface BackLinkProps {
  label: string;
  onPress: () => void;
}

export function BackLink({ label, onPress }: BackLinkProps) {
  return (
    <Pressable
      onPress={onPress}
      hitSlop={8}
      accessibilityRole="link"
      style={({ pressed }) => [styles.container, pressed && styles.pressed]}
    >
      <Icon name="arrow-back" size={sizes.iconSm} color={colors.secondary} />
      <Text style={styles.label}>{label.toUpperCase()}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    alignSelf: 'flex-start',
    gap: spacing.xs,
    marginBottom: spacing.md,
  },
  pressed: {
    opacity: 0.6,
  },
  label: {
    ...typography.labelMd,
    color: colors.secondary,
  },
});
