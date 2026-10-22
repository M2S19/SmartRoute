import { ReactNode } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { PARCEL_TYPE_LABELS } from '../../constants/labels';
import { colors, fonts, spacing, typography } from '../../constants/theme';
import type { RouteStop } from '../../types';
import { formatTime } from '../../utils/format';
import { Badge } from '../common';
import { StepIndex } from './StepIndex';

/**
 * - delivered : arrêt livré (barré, atténué)
 * - next      : prochain arrêt (bordure bleue, action)
 * - pending   : arrêt à venir
 * - plain     : simple affichage d'ordre, sans statut (résultat d'optimisation)
 */
export type StopCardVariant = 'delivered' | 'next' | 'pending' | 'plain';

interface StopCardProps {
  stop: RouteStop;
  variant: StopCardVariant;
  /** Zone d'action affichée en pied de carte (ex. « Marquer comme livré »). */
  footer?: ReactNode;
}

function describeStop(stop: RouteStop, variant: StopCardVariant): string {
  const { parcelRef, parcelType, instructions } = stop.delivery;
  // La référence colis n'existe pas toujours (l'API ne la gère pas encore) : rien n'est affiché à sa place.
  if (variant === 'delivered') {
    const parts = [parcelRef ? `Colis #${parcelRef}` : null, stop.deliveredAt ? `Validé à ${formatTime(stop.deliveredAt)}` : null];
    return parts.filter(Boolean).join(' • ');
  }
  const base = parcelRef ? `Colis ${PARCEL_TYPE_LABELS[parcelType]} #${parcelRef}` : null;
  return [base, instructions].filter(Boolean).join(' • ');
}

export function StopCard({ stop, variant, footer }: StopCardProps) {
  const isNext = variant === 'next';
  const isDelivered = variant === 'delivered';

  return (
    <View style={[styles.card, isNext && styles.cardNext, isDelivered && styles.cardDelivered]}>
      <View style={styles.row}>
        <View style={styles.main}>
          <StepIndex value={stop.order} tone={isNext ? 'active' : isDelivered ? 'muted' : 'default'} />
          <View style={styles.texts}>
            {isNext ? (
              <View style={styles.nextLabel}>
                <View style={styles.dot} />
                <Text style={styles.nextLabelText}>PROCHAIN ARRÊT</Text>
              </View>
            ) : null}
            <Text style={[styles.address, isNext && styles.addressNext, isDelivered && styles.addressDelivered]}>
              {stop.delivery.address.label}
            </Text>
            {variant !== 'plain' && describeStop(stop, variant) ? (
              <Text style={styles.meta}>{describeStop(stop, variant)}</Text>
            ) : null}
          </View>
        </View>
        {variant === 'delivered' ? <Badge label="Livré" tone="muted" uppercase={false} /> : null}
        {variant === 'next' ? <Badge label="À livrer" tone="active" /> : null}
        {variant === 'pending' ? <Badge label="À livrer" tone="neutral" /> : null}
      </View>
      {footer ? <View style={styles.footer}>{footer}</View> : null}
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
  cardNext: {
    borderWidth: 2,
    borderColor: colors.primaryContainer,
  },
  cardDelivered: {
    opacity: 0.75,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    justifyContent: 'space-between',
    gap: spacing.sm,
  },
  main: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: spacing.sm,
    flex: 1,
  },
  texts: {
    flex: 1,
  },
  nextLabel: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  dot: {
    width: 6,
    height: 6,
    borderRadius: 3,
    backgroundColor: colors.primaryContainer,
  },
  nextLabelText: {
    ...typography.labelSm,
    color: colors.primaryContainer,
  },
  address: {
    ...typography.bodyMd,
    color: colors.onSurface,
  },
  addressNext: {
    ...typography.bodyLg,
    fontFamily: fonts.plex600,
    marginTop: 2,
  },
  addressDelivered: {
    textDecorationLine: 'line-through',
    textDecorationColor: colors.secondary,
  },
  meta: {
    ...typography.bodySm,
    color: colors.secondary,
  },
  footer: {
    marginTop: spacing.md,
    paddingTop: spacing.sm,
    borderTopWidth: 1,
    borderTopColor: colors.surfaceContainer,
  },
});
