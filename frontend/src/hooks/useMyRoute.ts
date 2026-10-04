import { useCallback, useEffect, useState } from 'react';
import { routeService } from '../services';
import { toErrorMessage } from '../types';
import type { Route, RouteStop } from '../types';

type LoadStatus = 'loading' | 'ready' | 'error';

function replaceStop(route: Route, stopId: string, update: (stop: RouteStop) => RouteStop): Route {
  return { ...route, stops: route.stops.map((s) => (s.id === stopId ? update(s) : s)) };
}

/**
 * Tournée du livreur connecté : chargement, actualisation et validation d'un arrêt.
 * La validation est optimiste : l'arrêt passe « Livré » immédiatement,
 * puis revient à son état précédent si le service échoue.
 */
export function useMyRoute() {
  const [route, setRoute] = useState<Route | null>(null);
  const [status, setStatus] = useState<LoadStatus>('loading');
  const [loadError, setLoadError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [pendingStopId, setPendingStopId] = useState<string | null>(null);

  const fetchRoute = useCallback(async (mode: 'initial' | 'refresh') => {
    if (mode === 'initial') setStatus('loading');
    else setIsRefreshing(true);
    setLoadError(null);

    try {
      const data = await routeService.getMyRoute();
      setRoute(data);
      setStatus('ready');
    } catch (error) {
      setLoadError(toErrorMessage(error));
      // En actualisation, on garde la tournée déjà affichée.
      if (mode === 'initial') setStatus('error');
    } finally {
      setIsRefreshing(false);
    }
  }, []);

  useEffect(() => {
    void fetchRoute('initial');
  }, [fetchRoute]);

  const markDelivered = useCallback(
    async (stopId: string) => {
      // Pas de validation pendant une actualisation : sa réponse écraserait l'état optimiste.
      if (!route || pendingStopId || isRefreshing) return;

      const previous = route;
      setActionError(null);
      setPendingStopId(stopId);
      setRoute(replaceStop(route, stopId, (s) => ({ ...s, status: 'DELIVERED', deliveredAt: new Date().toISOString() })));

      try {
        const updated = await routeService.markStopDelivered(stopId);
        setRoute((current) => (current ? replaceStop(current, stopId, () => updated) : current));
      } catch (error) {
        setRoute(previous);
        setActionError(`La livraison n'a pas pu être validée : ${toErrorMessage(error)}`);
      } finally {
        setPendingStopId(null);
      }
    },
    [route, pendingStopId, isRefreshing],
  );

  return {
    route,
    status,
    loadError,
    actionError,
    isRefreshing,
    pendingStopId,
    reload: () => fetchRoute('initial'),
    refresh: () => fetchRoute('refresh'),
    markDelivered,
  };
}
