import { useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { ROLE_SHORT_LABELS } from '../../constants/roles';
import { colors, sizes, spacing, typography } from '../../constants/theme';
import type { UserRole } from '../../types';
import { Icon, IconName } from './Icon';

interface AppHeaderProps {
  role: UserRole;
  onLogout: () => Promise<void>;
}

const ROLE_ICONS: Record<UserRole, IconName> = {
  RESPONSABLE_LOGISTIQUE: 'shuffle',
  LIVREUR: 'local-shipping',
};

/** Barre supérieure : « SmartRoute • {rôle} » et bouton Déconnexion. */
export function AppHeader({ role, onLogout }: AppHeaderProps) {
  const [isLoggingOut, setIsLoggingOut] = useState(false);

  const handleLogout = async () => {
    if (isLoggingOut) return;
    setIsLoggingOut(true);
    try {
      await onLogout();
      // Succès : l'écran est démonté par RootNavigator, rien à réinitialiser.
    } catch {
      setIsLoggingOut(false);
    }
  };

  return (
    <View style={styles.container}>
      <View style={styles.brand}>
        <Icon name={ROLE_ICONS[role]} size={sizes.iconMd} color={colors.primary} />
        <Text style={styles.title}>
          SmartRoute <Text style={styles.role}>• {ROLE_SHORT_LABELS[role]}</Text>
        </Text>
      </View>
      <Pressable
        onPress={handleLogout}
        disabled={isLoggingOut}
        hitSlop={8}
        accessibilityRole="button"
        accessibilityState={{ disabled: isLoggingOut, busy: isLoggingOut }}
        style={({ pressed }) => [styles.logout, (pressed || isLoggingOut) && styles.logoutPressed]}
      >
        {({ pressed }) => (
          <>
            <Icon name="logout" size={sizes.iconSm} color={pressed ? colors.error : colors.secondary} />
            <Text style={[styles.logoutLabel, pressed && { color: colors.error }]}>Déconnexion</Text>
          </>
        )}
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: spacing.gutter,
    paddingVertical: spacing.md,
    backgroundColor: colors.surface,
    borderBottomWidth: 1,
    borderBottomColor: colors.borderFaint,
  },
  brand: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
  },
  title: {
    ...typography.headlineSm,
    color: colors.onSurface,
  },
  role: {
    ...typography.bodySm,
    fontSize: typography.headlineSm.fontSize,
    color: colors.secondary,
  },
  logout: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    paddingVertical: spacing.xs,
  },
  logoutPressed: {
    opacity: 0.8,
  },
  logoutLabel: {
    ...typography.labelMd,
    color: colors.secondary,
  },
});
