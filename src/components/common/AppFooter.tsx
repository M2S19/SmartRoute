import { StyleSheet, Text, View } from 'react-native';
import { colors, fonts, spacing, typography } from '../../constants/theme';
import type { UserRole } from '../../types';

interface AppFooterProps {
  role: UserRole;
}

export function AppFooter({ role }: AppFooterProps) {
  return (
    <View style={styles.container}>
      <Text style={styles.text}>
        Application interne SmartRoute • Rôle : <Text style={styles.role}>{role}</Text>
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    marginTop: spacing.xl,
    paddingTop: spacing.md,
    borderTopWidth: 1,
    borderTopColor: colors.borderFaint,
  },
  text: {
    ...typography.bodySm,
    color: colors.secondary,
    textAlign: 'center',
  },
  role: {
    fontFamily: fonts.plex600,
  },
});
