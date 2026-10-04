import { Pressable, StyleSheet, Text, View } from 'react-native';
import { colors, sizes, spacing, typography } from '../../constants/theme';
import { Icon, IconName } from './Icon';

export type NoticeTone = 'info' | 'success' | 'error';

interface NoticeProps {
  message: string;
  tone?: NoticeTone;
  /** Action optionnelle, ex. « Réessayer ». */
  actionLabel?: string;
  onAction?: () => void;
}

const TONES: Record<NoticeTone, { backgroundColor: string; color: string; icon: IconName }> = {
  // Style du message de redirection de la maquette Login.
  info: { backgroundColor: colors.surfaceContainer, color: colors.onSurfaceVariant, icon: 'info-outline' },
  success: { backgroundColor: colors.surfaceContainer, color: colors.success, icon: 'check-circle-outline' },
  error: { backgroundColor: colors.errorContainer, color: colors.onErrorContainer, icon: 'error-outline' },
};

/** Message d'information, de succès ou d'erreur (ErrorMessage = tone "error"). */
export function Notice({ message, tone = 'info', actionLabel, onAction }: NoticeProps) {
  const t = TONES[tone];
  return (
    <View
      style={[styles.container, { backgroundColor: t.backgroundColor }]}
      accessibilityRole={tone === 'error' ? 'alert' : undefined}
    >
      <Icon name={t.icon} size={sizes.iconSm} color={t.color} style={styles.icon} />
      <View style={styles.body}>
        <Text style={[styles.message, { color: t.color }]}>{message}</Text>
        {actionLabel && onAction ? (
          <Pressable onPress={onAction} hitSlop={8} accessibilityRole="button">
            <Text style={[styles.action, { color: t.color }]}>{actionLabel.toUpperCase()}</Text>
          </Pressable>
        ) : null}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    gap: spacing.sm,
    padding: spacing.sm,
  },
  icon: {
    marginTop: 1,
  },
  body: {
    flex: 1,
    gap: spacing.xs,
  },
  message: {
    ...typography.bodySm,
  },
  action: {
    ...typography.labelMd,
    textDecorationLine: 'underline',
  },
});
