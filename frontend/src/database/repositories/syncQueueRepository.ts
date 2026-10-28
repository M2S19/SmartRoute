
import { db } from '../database';

export interface SyncOperation {
  id: string;
  route_id: string;
  stop_id: string;
  operation_type: string;
  payload: string;
  status: string;
  created_at: string;
  retry_count: number;
  last_error: string | null;
}

/**
 * Récupère les opérations à synchroniser.
 * Les opérations les plus anciennes passent en premier.
 */
export async function getPendingOperations(
  userId: string
): Promise<SyncOperation[]> {

  return db.getAllAsync<SyncOperation>(
    `SELECT q.*
    FROM sync_queue q
    INNER JOIN routes r ON r.id = q.route_id
    WHERE q.status = 'PENDING'
      AND r.owner_user_id = ?
      AND r.is_available = 1
      AND NOT EXISTS (
        SELECT 1
        FROM sync_queue conflict
        WHERE conflict.route_id = q.route_id
          AND conflict.status = 'CONFLICT'
      )
    ORDER BY q.created_at ASC, q.rowid ASC`,
    [userId]
  );
}

/**
 * Compte les opérations en attente.
 */
export async function countPendingOperations(
  userId: string
): Promise<number> {

  const result = await db.getFirstAsync<{
    total: number;
  }>(
    `SELECT COUNT(*) AS total
     FROM sync_queue q
     INNER JOIN routes r ON r.id = q.route_id
     WHERE q.status = 'PENDING'
       AND r.owner_user_id = ?`,
    [userId]
  );

  return result?.total ?? 0;
}


/**
 * Affiche les opérations en attente de synchronisation.
 * Fonction temporaire de diagnostic.
 *
 * Aucune opération n'est envoyée au serveur.
 */
export async function debugPendingOperations(
  userId: string
): Promise<void> {
  const operations = await getPendingOperations(userId);

  console.log('=== SMARTROUTE SYNC QUEUE ===');
  console.log('Nombre d’opérations :', operations.length);

  for (const operation of operations) {
    console.log({
      operationId: operation.id,
      routeId: operation.route_id,
      stopId: operation.stop_id,
      operationType: operation.operation_type,
      status: operation.status,
      retryCount: operation.retry_count,
      createdAt: operation.created_at,
    });
  }
}


/**
 * Confirme une opération après une réponse positive
 * de l'API ASP.NET Core.
 */
export async function confirmSyncOperation(
  userId: string,
  operationId: string
): Promise<void> {
  await db.withExclusiveTransactionAsync(async (tx) => {
    const operation = await tx.getFirstAsync<{
      route_id: string;
      stop_id: string;
    }>(
      `SELECT q.route_id, q.stop_id
       FROM sync_queue q
       INNER JOIN routes r ON r.id = q.route_id
       WHERE q.id = ?
         AND r.owner_user_id = ?
         AND q.status = 'PENDING'`,
      [operationId, userId]
    );

    if (!operation) {
      throw new Error(
        'Opération en attente introuvable.'
      );
    }

    await tx.runAsync(
      `UPDATE sync_queue
       SET status = 'SYNCED',
           last_error = NULL
       WHERE id = ? AND status = 'PENDING'`,
      [operationId]
    );

    // Ne confirmer le statut local de l'arrêt
    // que s'il n'existe plus d'opération non synchronisée.
    const remaining = await tx.getFirstAsync<{
      total: number;
    }>(
      `SELECT COUNT(*) AS total
       FROM sync_queue
       WHERE route_id = ?
         AND stop_id = ?
         AND status != 'SYNCED'`,
      [operation.route_id, operation.stop_id]
    );

    if ((remaining?.total ?? 0) === 0) {
      await tx.runAsync(
        `UPDATE route_stops
         SET sync_status = 'SYNCED'
         WHERE route_id = ? AND id = ?`,
        [operation.route_id, operation.stop_id]
      );
    }
  });
}

/**
 * Mémorise un échec temporaire.
 * L'opération reste PENDING.
 */
export async function recordSyncFailure(
  userId: string,
  operationId: string,
  message: string
): Promise<void> {
  await db.runAsync(
    `UPDATE sync_queue
     SET retry_count = retry_count + 1,
         last_error = ?
     WHERE id = ?
       AND status = 'PENDING'
       AND route_id IN (
         SELECT id FROM routes WHERE owner_user_id = ?
       )`,
    [message, operationId, userId]
  );
}

/**
 * Conserve une opération refusée pour conflit métier.
 * Elle ne sera plus renvoyée automatiquement.
 */
export async function markSyncConflict(
  userId: string,
  operationId: string,
  errorMessage: string
): Promise<void> {
  await db.runAsync(
    `UPDATE sync_queue
     SET status = 'CONFLICT',
         last_error = ?,
         retry_count = retry_count + 1
     WHERE id = ?
       AND status = 'PENDING'
       AND route_id IN (
         SELECT id FROM routes WHERE owner_user_id = ?
       )`,
    [errorMessage, operationId, userId]
  );
}


