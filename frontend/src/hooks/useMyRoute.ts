import { useCallback, useEffect, useState } from 'react';
import { routeService } from '../services';
import { ApiError, toErrorMessage } from '../types';
import type { Route } from '../types';
import { todayIsoDate } from '../utils/format';

type LoadStatus = 'loading' | 'ready' | 'error';

/**
 * Tournée affichée par défaut : celle du jour encore active, sinon la première active,
 * sinon la plus récente (l'API renvoie les tournées de la plus récente à la plus ancienne).
 */
function defaultRouteId(routes: Route[]): string | null {
  const today = todayIsoDate();
  const active = routes.filter((r) => r.status !== 'COMPLETED');
  return (active.find((r) => r.date === today) ?? active[0] ?? routes[0])?.id ?? null;
}

/**
 * Tournées du livreur connecté : chargement, actualisation, choix de la tournée et validation d'un arrêt.
 * Pas de mise à jour optimiste : un arrêt n'apparaît « Livré » qu'une fois la réponse du serveur reçue,
 * et la tournée affichée est alors celle renvoyée par le serveur.
 */
export function useMyRoute() {
  const [routes, setRoutes] = useState<Route[]>([]);
  const [selectedRouteId, setSelectedRouteId] = useState<string | null>(null);
  const [status, setStatus] = useState<LoadStatus>('loading');
  const [loadError, setLoadError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [pendingStopId, setPendingStopId] = useState<string | null>(null);

  const fetchRoutes = useCallback(async (mode: 'initial' | 'refresh') => {
    if (mode === 'initial') setStatus('loading');
    else setIsRefreshing(true);
    setLoadError(null);

    try {
      const data = await routeService.getMyRoutes();
      setRoutes(data);
      // La tournée choisie reste affichée si elle existe toujours.
      setSelectedRouteId((current) => (current && data.some((r) => r.id === current) ? current : defaultRouteId(data)));
      setStatus('ready');
    } catch (error) {
      setLoadError(toErrorMessage(error));
      // En actualisation, on garde les tournées déjà affichées.
      if (mode === 'initial') setStatus('error');
    } finally {
      setIsRefreshing(false);
    }
  }, []);

  useEffect(() => {
    void fetchRoutes('initial');
  }, [fetchRoutes]);

  const route = routes.find((r) => r.id === selectedRouteId) ?? null;

  const markDelivered = useCallback(
    async (stopId: string) => {
      if (!route || pendingStopId || isRefreshing) return;

      setActionError(null);
      setPendingStopId(stopId);

      try {
        const updated = await routeService.markStopDelivered(route.id, stopId);
        setRoutes((current) => current.map((r) => (r.id === updated.id ? updated : r)));
      } catch (error) {
        setActionError(`La livraison n'a pas pu être validée : ${toErrorMessage(error)}`);
        // 404 / 409 : l'état serveur diffère de l'affichage (arrêt déjà livré, tournée modifiée…) : on le recharge.
        if (error instanceof ApiError && (error.status === 404 || error.status === 409)) {
          void fetchRoutes('refresh');
        }
      } finally {
        setPendingStopId(null);
      }
    },
    [route, pendingStopId, isRefreshing, fetchRoutes],
  );

  const selectRoute = useCallback((routeId: string) => {
    setActionError(null);
    setSelectedRouteId(routeId);
  }, []);

  return {
    routes,
    route,
    status,
    loadError,
    actionError,
    isRefreshing,
    pendingStopId,
    selectRoute,
    reload: () => fetchRoutes('initial'),
    refresh: () => fetchRoutes('refresh'),
    markDelivered,
  };
}
