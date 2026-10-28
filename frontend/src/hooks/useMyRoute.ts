
import { useCallback, useEffect, useRef, useState } from 'react';

import { useAuth } from './useAuth';

import {
  getLocalRoutes,
  refreshRoutesFromServer,
} from '../services/route/offlineRouteService';

import { deliverStopLocally } from '../database/repositories/localDeliveryRepository';
import { countPendingOperations } from '../database/repositories/syncQueueRepository';

import { toErrorMessage } from '../types';
import type { Route } from '../types';

import { todayIsoDate } from '../utils/format';
import { syncDeliveriesWithApi } from '../services/route/syncService';

type LoadStatus = 'loading' | 'ready' | 'error';

/**
 * Sélectionne la tournée à afficher par défaut.
 *
 * Priorité :
 * 1. Tournée active du jour
 * 2. Première tournée active
 * 3. Tournée la plus récente
 */
function defaultRouteId(routes: Route[]): string | null {
  const today = todayIsoDate();
  const active = routes.filter(
    (r) => r.status !== 'COMPLETED'
  );

  return (
    active.find((r) => r.date === today) ??
    active[0] ??
    routes[0]
  )?.id ?? null;
}

/**
 * Gestion des tournées du livreur en mode Offline-first.
 *
 * - Lecture locale SQLite
 * - Actualisation depuis l'API
 * - Validation locale des livraisons
 * - Suivi des opérations en attente de synchronisation
 *
 * La synchronisation vers MongoDB sera implémentée
 * dans une prochaine étape.
 */
export function useMyRoute() {
  const { user } = useAuth();
  const userId = user?.id;

  const [isSyncing, setIsSyncing] = useState(false);
  const [syncMessage, setSyncMessage] = useState<string | null>(null);

  const syncLock = useRef(false);

  // État des tournées
  const [routes, setRoutes] = useState<Route[]>([]);
  const [selectedRouteId, setSelectedRouteId] =
    useState<string | null>(null);

  // État du chargement
  const [status, setStatus] =
    useState<LoadStatus>('loading');

  const [loadError, setLoadError] =
    useState<string | null>(null);

  const [actionError, setActionError] =
    useState<string | null>(null);

  const [isRefreshing, setIsRefreshing] =
    useState(false);

  const [pendingStopId, setPendingStopId] =
    useState<string | null>(null);

  // Nombre d'opérations en attente de synchronisation
  const [pendingSyncCount, setPendingSyncCount] =
    useState(0);

  // Numéro de requête pour ignorer les réponses obsolètes
  const requestId = useRef(0);

  /**
   * Met à jour les tournées affichées tout en
   * conservant la sélection actuelle si possible.
   */
  const updateRoutes = useCallback((data: Route[]) => {
    setRoutes(data);

    setSelectedRouteId((current) =>
      current && data.some((r) => r.id === current)
        ? current
        : defaultRouteId(data)
    );
  }, []);

  /**
   * Charge les tournées.
   *
   * 1. Lecture SQLite
   * 2. Actualisation depuis l'API
   * 3. Conservation des données locales si l'API échoue
   */
  const fetchRoutes = useCallback(
    async (mode: 'initial' | 'refresh') => {
      if (!userId) return;

      const currentRequest = ++requestId.current;

      const isCurrent = () =>
        currentRequest === requestId.current;

      if (mode === 'initial') {
        setStatus('loading');
      } else {
        setIsRefreshing(true);
      }

      setLoadError(null);

      let hasLocalData = mode === 'refresh' && routes.length > 0;

      try {
        // ÉTAPE 1 : lecture SQLite
        const localRoutes = await getLocalRoutes(userId);

        if (!isCurrent()) return;

        if (localRoutes.length > 0) {
          hasLocalData = true;
          updateRoutes(localRoutes);
          setStatus('ready');
        }

        // Compter les opérations non synchronisées
        const count = await countPendingOperations(userId);

        if (!isCurrent()) return;

        setPendingSyncCount(count);

        // ÉTAPE 2 : récupération depuis l'API
        const remoteRoutes =
          await refreshRoutesFromServer(userId);

        if (!isCurrent()) return;

        // ÉTAPE 3 : actualiser l'interface
        updateRoutes(remoteRoutes);
        setStatus('ready');
        setLoadError(null);

        console.log(
          `Tournées chargées : ${remoteRoutes.length}`
        );

        console.log(
          `Opérations en attente : ${count}`
        );

      } catch (error) {
        if (!isCurrent()) return;

        const message = toErrorMessage(error);
        setLoadError(message);

        // Conserver les tournées présentes dans SQLite
        if (hasLocalData) {
          setStatus('ready');
        } else {
          setStatus('error');
        }

      } finally {
        if (isCurrent()) {
          setIsRefreshing(false);
        }
      }
    },
    [userId, updateRoutes]
  );

  /**
   * Chargement initial et changement d'utilisateur.
   */
  useEffect(() => {
    setRoutes([]);
    setSelectedRouteId(null);
    setActionError(null);
    setLoadError(null);
    setPendingSyncCount(0);

    void fetchRoutes('initial');

    return () => {
      // Ignore les réponses arrivées après un changement
      // de compte ou le démontage du composant.
      requestId.current++;
    };
  }, [fetchRoutes]);

  const route =
    routes.find((r) => r.id === selectedRouteId) ?? null;

  /**
   * Valide un arrêt localement.
   *
   * Transaction SQLite :
   * - route_stops : DELIVERED
   * - routes : IN_PROGRESS ou COMPLETED
   * - sync_queue : PENDING
   *
   * Aucun appel réseau.
   */
  const markDelivered = useCallback(
    async (stopId: string) => {
      if (
        !route ||
        !userId ||
        pendingStopId ||
        isRefreshing
      ) {
        return;
      }

      setActionError(null);
      setPendingStopId(stopId);

      // Invalider les résultats des chargements précédents
      requestId.current++;

      try {
        // 1. Enregistrement local atomique
        await deliverStopLocally(
          userId,
          route.id,
          stopId
        );

        // 2. Relecture des tournées SQLite
        const localRoutes = await getLocalRoutes(userId);

        // 3. Actualisation de l'écran
        updateRoutes(localRoutes);
        setStatus('ready');

        // 4. Compter les actions à synchroniser
        const count =
          await countPendingOperations(userId);

        setPendingSyncCount(count);

        console.log(
          `Livraison ${stopId} validée localement`
        );

        console.log(
          `Opérations en attente : ${count}`
        );

      } catch (error) {
        setActionError(
          `La livraison n'a pas pu être enregistrée : ${toErrorMessage(error)}`
        );

        // Restaurer l'affichage depuis SQLite
        try {
          const localRoutes = await getLocalRoutes(userId);
          updateRoutes(localRoutes);

          const count =
            await countPendingOperations(userId);

          setPendingSyncCount(count);
        } catch (readError) {
          console.error(
            'Erreur de relecture SQLite :',
            readError
          );
        }

      } finally {
        setPendingStopId(null);
      }
    },
    [
      route,
      userId,
      pendingStopId,
      isRefreshing,
      updateRoutes,
    ]
  );



  const synchronize = useCallback(async () => {
    // Éviter les exécutions concurrentes.
    if (
      !userId ||
      syncLock.current ||
      pendingStopId !== null ||
      isRefreshing
    ) {
      return;
    }

    syncLock.current = true;
    setIsSyncing(true);
    setSyncMessage(null);

    // Ignorer un ancien rafraîchissement HTTP.
    requestId.current++;

    try {
      // 1. Envoyer les opérations SQLite vers l'API.
      const result = await syncDeliveriesWithApi(userId);

      // 2. Mettre à jour le compteur.
      setPendingSyncCount(result.pendingCount);

      // 3. Relire SQLite après synchronisation.
      const localRoutes = await getLocalRoutes(userId);
      updateRoutes(localRoutes);

      // 4. Afficher le résultat.
      if (result.error) {
        setSyncMessage(
          `Synchronisation incomplète : ${result.error}`
        );
      } else {
        setSyncMessage(
          `${result.syncedCount} livraison(s) synchronisée(s).`
        );
      }

      console.log('[SYNC] Résultat :', result);

    } catch (error) {
      setSyncMessage(
        `Erreur de synchronisation : ${toErrorMessage(error)}`
      );
    } finally {
      syncLock.current = false;
      setIsSyncing(false);
    }
  }, [
    userId,
    pendingStopId,
    isRefreshing,
    updateRoutes,
  ]);

  /**
   * Sélection d'une tournée.
   */
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
    pendingSyncCount,
    isSyncing,
    syncMessage,
    selectRoute,
    reload: () => fetchRoutes('initial'),
    refresh: () => fetchRoutes('refresh'),
    markDelivered,
    synchronize,
  };
}
