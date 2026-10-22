import { StyleSheet, Text, View } from 'react-native';
import { ROUTE_STATUS_LABELS } from '../../constants/labels';
import { colors, fonts, spacing, typography } from '../../constants/theme';
import type { Route, User } from '../../types';
import { formatDay, todayIsoDate } from '../../utils/format';
import { Badge, Icon } from '../common';

interface RouteSummaryCardProps {
  route: Route;
  driver: User;
}

/** Fiche « Tournée du jour » (maquette driver-route). */
export function RouteSummaryCard({ route, driver }: RouteSummaryCardProps) {
  const title = route.date === todayIsoDate() ? 'TOURNÉE DU JOUR' : `TOURNÉE DU ${formatDay(route.date)}`;

  return (
    <View style={styles.card}>
      <View style={styles.header}>
        <Text style={styles.title}>{title}</Text>
        <Badge label={ROUTE_STATUS_LABELS[route.status]} tone="primary" />
      </View>
      <View style={styles.body}>
        <View style={styles.driverRow}>
          <Icon name="local-shipping" size={15} color={colors.secondary} />
          <Text style={[styles.text, styles.strong]}>{route.name}</Text>
        </View>
        <View style={styles.driverRow}>
          <Icon name="person-outline" size={15} color={colors.secondary} />
          <Text style={styles.text}>
            Livreur connecté : <Text style={styles.strong}>{driver.fullName}</Text>
            {driver.driverCode ? <Text style={styles.muted}> (ID #{driver.driverCode})</Text> : null}
          </Text>
        </View>
        <Text style={styles.note}>Données restreintes à votre affectation personnelle.</Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: colors.surfaceContainerLowest,
    borderWidth: 1,
    borderColor: colors.borderSubtle,
    padding: spacing.md,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingBottom: spacing.xs,
    marginBottom: spacing.sm,
    borderBottomWidth: 1,
    borderBottomColor: colors.surfaceContainer,
  },
  title: {
    ...typography.labelLg,
    color: colors.onSurface,
  },
  body: {
    gap: spacing.xs,
  },
  driverRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  text: {
    ...typography.bodySm,
    color: colors.onSurfaceVariant,
    flex: 1,
  },
  strong: {
    fontFamily: fonts.plex500,
    color: colors.onSurface,
  },
  muted: {
    color: colors.secondary,
  },
  note: {
    ...typography.bodySm,
    fontFamily: fonts.plex400Italic,
    color: colors.secondary,
  },
});
