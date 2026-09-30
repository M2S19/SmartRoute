import { StyleSheet, View } from 'react-native';
import {
  AppFooter,
  AppHeader,
  Button,
  EmptyState,
  LoadingIndicator,
  Notice,
  ScreenContainer,
  ScreenTitle,
} from '../../components/common';
import { RouteSummaryCard, SectionLabel, StopCard, StopCardVariant } from '../../components/route';
import { spacing } from '../../constants/theme';
import { useAuth } from '../../hooks/useAuth';
import { useMyRoute } from '../../hooks/useMyRoute';
import type { RouteStop } from '../../types';

function variantFor(stop: RouteStop, nextStopId: string | undefined): StopCardVariant {
  if (stop.status === 'DELIVERED') return 'delivered';
  return stop.id === nextStopId ? 'next' : 'pending';
}

export function MyRouteScreen() {
  const { user, logout } = useAuth();
  const { route, status, loadError, actionError, isRefreshing, pendingStopId, reload, refresh, markDelivered } =
    useMyRoute();

  const stops = route ? [...route.stops].sort((a, b) => a.order - b.order) : [];
  const nextStop = stops.find((s) => s.status === 'PENDING');
  const deliveredCount = stops.filter((s) => s.status === 'DELIVERED').length;
  const isCompleted = stops.length > 0 && !nextStop;

  return (
    <ScreenContainer header={<AppHeader role="LIVREUR" onLogout={logout} />}>
      <ScreenTitle title="Ma tournée" subtitle="Consultation des points de livraison affectés." />

      {status === 'loading' ? <LoadingIndicator label="Chargement de votre tournée…" /> : null}

      {status === 'error' ? (
        <Notice tone="error" message={loadError ?? 'Impossible de charger la tournée.'} actionLabel="Réessayer" onAction={reload} />
      ) : null}

      {status === 'ready' && route && user ? (
        <>
          <View style={styles.block}>
            <RouteSummaryCard route={route} driver={user} />
          </View>

          {loadError ? (
            <View style={styles.notice}>
              <Notice tone="error" message={`Actualisation impossible : ${loadError}`} />
            </View>
          ) : null}
          {actionError ? (
            <View style={styles.notice}>
              <Notice tone="error" message={actionError} />
            </View>
          ) : null}

          {stops.length === 0 ? (
            <View style={styles.block}>
              <EmptyState
                icon="local-shipping"
                title="Aucun arrêt affecté"
                message="Votre tournée ne contient pas encore de livraison. Actualisez plus tard."
              />
            </View>
          ) : (
            <View style={styles.list}>
              <SectionLabel
                title={`Itinéraire séquentiel (${stops.length} étape${stops.length > 1 ? 's' : ''})`}
                aside={`Progression : ${deliveredCount}/${stops.length}`}
              />
              {stops.map((stop) => {
                const variant = variantFor(stop, nextStop?.id);
                return (
                  <StopCard
                    key={stop.id}
                    stop={stop}
                    variant={variant}
                    footer={
                      variant === 'next' ? (
                        <Button
                          label="Marquer comme livré"
                          icon="check-circle-outline"
                          variant="accent"
                          size="sm"
                          onPress={() => markDelivered(stop.id)}
                          disabled={pendingStopId !== null || isRefreshing}
                        />
                      ) : undefined
                    }
                  />
                );
              })}
              {isCompleted ? <Notice tone="success" message="Tournée terminée : tous les arrêts sont livrés." /> : null}
            </View>
          )}

          <Button
            label="Actualiser la tournée"
            icon="sync"
            variant="secondary"
            size="sm"
            onPress={refresh}
            loading={isRefreshing}
            disabled={pendingStopId !== null}
          />
        </>
      ) : null}

      <AppFooter role="LIVREUR" />
    </ScreenContainer>
  );
}

const styles = StyleSheet.create({
  block: {
    marginBottom: spacing.lg,
  },
  notice: {
    marginBottom: spacing.md,
  },
  list: {
    gap: spacing.sm,
    marginBottom: spacing.xl,
  },
});
