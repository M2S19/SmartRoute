
import * as Crypto from 'expo-crypto';
import { db } from '../database';

type LocalRoute = {
  id: string;
  status: string;
  is_available: number;
};

type LocalStop = {
  id: string;
  status: string;
  stop_order: number;
};

export async function deliverStopLocally(
  userId: string,
  routeId: string,
  stopId: string
): Promise<void> {

  // Identifiant unique de l'opération
  const operationId = Crypto.randomUUID();
  const deliveredAt = new Date().toISOString();

  await db.withExclusiveTransactionAsync(async (tx) => {

    // 1. Vérifier que la tournée appartient au livreur
    const route = await tx.getFirstAsync<LocalRoute>(
      `SELECT id, status, is_available
      FROM routes
      WHERE id = ? AND owner_user_id = ?`,
      [routeId, userId]
    );

    // 2. Vérifier que la tournée existe
    if (!route) {
      throw new Error(
        'Tournée introuvable ou non attribuée à ce livreur.'
      );
    }

    // 3. Vérifier que la tournée est toujours disponible
    if (route.is_available !== 1) {
      throw new Error(
        'Cette tournée a été retirée ou réattribuée. Impossible de valider une nouvelle livraison.'
      );
    }

    // 2. Vérifier que la tournée est modifiable
    if (
      route.status !== 'ASSIGNED' &&
      route.status !== 'IN_PROGRESS'
    ) {
      throw new Error('Cette tournée n’est plus modifiable.');
    }

    // 3. Récupérer les arrêts dans l'ordre
    const stops = await tx.getAllAsync<LocalStop>(
      `SELECT id, status, stop_order
       FROM route_stops
       WHERE route_id = ?
       ORDER BY stop_order ASC`,
      [routeId]
    );

    const stop = stops.find((s) => s.id === stopId);

    if (!stop) {
      throw new Error('Arrêt introuvable.');
    }

    if (stop.status === 'DELIVERED') {
      throw new Error('Cet arrêt est déjà livré.');
    }

    // 4. Respecter l'ordre séquentiel
    const nextStop = stops.find(
      (s) => s.status !== 'DELIVERED'
    );

    if (nextStop?.id !== stopId) {
      throw new Error(
        'Les arrêts doivent être livrés dans l’ordre.'
      );
    }

    // 5. Modifier l'arrêt dans SQLite
    await tx.runAsync(
      `UPDATE route_stops
       SET status = 'DELIVERED',
           delivered_at = ?,
           sync_status = 'PENDING'
       WHERE id = ? AND route_id = ?`,
      [deliveredAt, stopId, routeId]
    );

    // 6. Déterminer le statut de la tournée
    const allDelivered = stops.every(
      (s) => s.id === stopId || s.status === 'DELIVERED'
    );

    const newRouteStatus = allDelivered
      ? 'COMPLETED'
      : 'IN_PROGRESS';

    await tx.runAsync(
      `UPDATE routes
       SET status = ?, updated_at = ?
       WHERE id = ? AND owner_user_id = ?`,
      [newRouteStatus, deliveredAt, routeId, userId]
    );

    // 7. Enregistrer l'action à synchroniser
    await tx.runAsync(
      `INSERT INTO sync_queue
       (id, route_id, stop_id, operation_type,
        payload, status, created_at, retry_count)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?)`,
      [
        operationId,
        routeId,
        stopId,
        'DELIVER_STOP',
        JSON.stringify({
          operationId,
          routeId,
          stopId,
          deliveredAt,
        }),
        'PENDING',
        deliveredAt,
        0
      ]
    );
  });

  console.log(
    `Livraison ${stopId} enregistrée localement`
  );
}
