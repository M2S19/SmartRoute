
import type { Route } from '../../types';
import { sendDeliveryForSync } from './routeService.http';
import { ApiError } from '../../types';
import {
  getPendingOperations,
  confirmSyncOperation,
  recordSyncFailure,
  markSyncConflict,
} from '../../database/repositories/syncQueueRepository';

import { refreshRoutesFromServer } from './offlineRouteService';

export type SendDeliveryOperation = (
  routeId: string,
  stopId: string,
  operationId: string
) => Promise<Route>;

export interface SyncResult {
  syncedCount: number;
  pendingCount: number;
  error: string | null;
}

/**
 * Lecture du statut HTTP d'une erreur.
 *
 * À adapter si httpClient utilise une autre
 * structure pour ses erreurs.
 */
function getHttpStatus(error: unknown): number | null {
  if (typeof error !== 'object' || error === null) {
    return null;
  }

  if ('status' in error && typeof error.status === 'number') {
    return error.status;
  }

  if (
    'statusCode' in error &&
    typeof error.statusCode === 'number'
  ) {
    return error.statusCode;
  }

  return null;
}

export async function syncPendingDeliveries(
  userId: string,
  sendDelivery: SendDeliveryOperation
): Promise<SyncResult> {

  let syncedCount = 0;
  let errorMessage: string | null = null;

  const operations = await getPendingOperations(userId);

  for (const operation of operations) {
    // Une opération en conflit sur cette tournée
    // bloque les validations suivantes.
    // Ce contrôle doit être complété côté repository
    // pour s'appliquer également aux synchronisations futures.

    if (operation.operation_type !== 'DELIVER_STOP') {
      errorMessage =
        `Opération non prise en charge : ${operation.operation_type}`;

      await recordSyncFailure(
        userId,
        operation.id,
        errorMessage
      );

      break;
    }

    try {
      await sendDelivery(
        operation.route_id,
        operation.stop_id,
        operation.id
      );
} catch (error) {

  errorMessage =
    error instanceof Error
      ? error.message
      : String(error);

  // Identifier les conflits métier renvoyés par l'API
  const isBusinessConflict =
    error instanceof ApiError &&
    error.status === 409 &&
    [
      'STOP_ALREADY_DELIVERED',
      'STOP_NOT_NEXT',
      'ROUTE_NOT_MODIFIABLE'
    ].includes(error.code);

  if (isBusinessConflict) {

    // Conserver l'opération en état CONFLICT
    await markSyncConflict(
      userId,
      operation.id,
      errorMessage
    );

    console.warn(
      `[SYNC] Conflit métier : ${operation.id}`
    );

  } else {

    // Conserver l'opération en état PENDING
    await recordSyncFailure(
      userId,
      operation.id,
      errorMessage
    );

    console.warn(
      `[SYNC] Échec temporaire : ${operation.id}`
    );
  }

  // Ne pas envoyer les opérations suivantes
  // tant que celle-ci n'est pas résolue.
  break;
  }

    // Ne pas classer une erreur SQLite en échec HTTP :
    // si le serveur a accepté l'opération, son UUID
    // permettra de reconnaître une nouvelle tentative.
    await confirmSyncOperation(
      userId,
      operation.id
    );

    syncedCount++;
  }

  if (syncedCount > 0) {
    try {
      await refreshRoutesFromServer(userId);
    } catch (error) {
      console.warn(
        '[SYNC] Rafraîchissement indisponible :',
        error
      );
    }
  }

  const pendingCount = await (
    await import('../../database/repositories/syncQueueRepository')
  ).countPendingOperations(userId);

  return {
    syncedCount,
    pendingCount,
    error: errorMessage
  };
}

export async function syncDeliveriesWithApi(
  userId: string
): Promise<SyncResult> {
  return syncPendingDeliveries(
    userId,
    sendDeliveryForSync
  );
}
