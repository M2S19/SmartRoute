
import { db } from '../database';
import type { Route, RouteStop } from '../../types';

type RouteRow = {
  id: string;
  name: string;
  date: string;
  status: Route['status'];
  depot_json: string;
  driver_json: string | null;
};

type StopRow = {
  id: string;
  stop_order: number;
  status: RouteStop['status'];
  delivery_json: string;
  delivered_at: string | null;
};

type IdRow = {
  id: string;
};

type CountRow = {
  total: number;
};

/**
 * Vérifie si une tournée possède des opérations
 * qui ne sont pas définitivement synchronisées.
 */
async function hasUnresolvedOperations(
  tx: typeof db,
  routeId: string
): Promise<boolean> {
  const result = await tx.getFirstAsync<CountRow>(
    `SELECT COUNT(*) AS total
     FROM sync_queue
     WHERE route_id = ?
       AND status != 'SYNCED'`,
    [routeId]
  );

  return (result?.total ?? 0) > 0;
}

/**
 * Réconcilie SQLite avec la liste complète des
 * tournées renvoyées par GET /api/routes/me.
 *
 * Important :
 * - Appeler uniquement après une réponse HTTP réussie.
 * - Ne jamais écraser une tournée ayant des
 *   opérations locales non résolues.
 * - Préserver les actions dans sync_queue.
 */
export async function saveRoutes(
  ownerUserId: string,
  routes: Route[]
): Promise<void> {

  await db.withExclusiveTransactionAsync(async (tx) => {

    // 1. Charger les tournées actuellement en cache.
    const localRoutes = await tx.getAllAsync<IdRow>(
      `SELECT id
       FROM routes
       WHERE owner_user_id = ?`,
      [ownerUserId]
    );

    // Identifiants reçus du serveur.
    const remoteRouteIds = new Set(
      routes.map((route) => route.id)
    );

    // 2. Insérer ou actualiser chaque tournée serveur.
    for (const route of routes) {

      // Vérifier qu'une éventuelle tournée existante
      // appartient bien au même utilisateur local.
      const existingRoute = await tx.getFirstAsync<{
        owner_user_id: string | null;
      }>(
        `SELECT owner_user_id
         FROM routes
         WHERE id = ?`,
        [route.id]
      );

      if (
        existingRoute &&
        existingRoute.owner_user_id !== ownerUserId
      ) {
        throw new Error(
          `Conflit de propriétaire local pour ${route.id}`
        );
      }

      // Protéger toute la tournée si une opération
      // est encore en attente ou en conflit.
      if (await hasUnresolvedOperations(tx, route.id)) {
        await tx.runAsync(
          `UPDATE routes
          SET is_available = 1
          WHERE id = ?
            AND owner_user_id = ?`,
          [route.id, ownerUserId]
        )
        console.log(
      ` [SYNC DOWN] Tournée ${route.id} protégée`
        );

        continue;
      }

      // 3. UPSERT de la tournée.
      await tx.runAsync(
        `INSERT INTO routes (
          id,
          owner_user_id,
          name,
          date,
          status,
          depot_json,
          driver_json,
          updated_at,
          is_available
        )
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, 1)

        ON CONFLICT(id) DO UPDATE SET
          name = excluded.name,
          date = excluded.date,
          status = excluded.status,
          depot_json = excluded.depot_json,
          driver_json = excluded.driver_json,
          updated_at = excluded.updated_at,
          is_available = 1

        WHERE routes.owner_user_id = excluded.owner_user_id`,
        [
          route.id,
          ownerUserId,
          route.name,
          route.date,
          route.status,
          JSON.stringify(route.depot),
          route.driver ? JSON.stringify(route.driver) : null,
          new Date().toISOString()
        ]
      );

      // 4. UPSERT des arrêts reçus.
      for (const stop of route.stops) {
        await tx.runAsync(
          `INSERT INTO route_stops (
            id,
            route_id,
            address,
            stop_order,
            status,
            delivery_json,
            delivered_at,
            sync_status
          )
          VALUES (?, ?, ?, ?, ?, ?, ?, ?)

          ON CONFLICT(id) DO UPDATE SET
            address = excluded.address,
            stop_order = excluded.stop_order,
            status = excluded.status,
            delivery_json = excluded.delivery_json,
            delivered_at = excluded.delivered_at,
            sync_status = excluded.sync_status

          WHERE route_stops.route_id =
                excluded.route_id
            AND route_stops.sync_status = 'SYNCED'`,
          [
            stop.id,
            route.id,
            stop.delivery.address.label,
            stop.order,
            stop.status,
            JSON.stringify(stop.delivery),
            stop.deliveredAt ?? null,
            'SYNCED'
          ]
        );
      }

      // 5. Identifier les arrêts disparus du serveur.
      const localStops = await tx.getAllAsync<IdRow>(
        `SELECT id
         FROM route_stops
         WHERE route_id = ?`,
        [route.id]
      );

      const remoteStopIds = new Set(
        route.stops.map((stop) => stop.id)
      );

      const removedStops = localStops.filter(
        (stop) => !remoteStopIds.has(stop.id)
      );

      // 6. Supprimer les arrêts disparus uniquement
      // s'ils ne possèdent pas d'opération non résolue.
      for (const stop of removedStops) {

        const unresolved = await tx.getFirstAsync<CountRow>(
          `SELECT COUNT(*) AS total
           FROM sync_queue
           WHERE route_id = ?
             AND stop_id = ?
             AND status != 'SYNCED'`,
          [route.id, stop.id]
        );

        if ((unresolved?.total ?? 0) > 0) {
          console.warn(
            `[SYNC DOWN] Arrêt ${stop.id} protégé`
          );
          continue;
        }

        // Nettoyer uniquement les anciennes opérations
        // déjà confirmées avant de retirer l'arrêt.
        await tx.runAsync(
          `DELETE FROM sync_queue
           WHERE route_id = ?
             AND stop_id = ?
             AND status = 'SYNCED'`,
          [route.id, stop.id]
        );

        await tx.runAsync(
          `DELETE FROM route_stops
           WHERE route_id = ?
             AND id = ?`,
          [route.id, stop.id]
        );

        console.log(
          `[SYNC DOWN] Arrêt ${stop.id} retiré du cache`
        );
      }
    }

    // 7. Identifier les tournées absentes
    // de la liste complète du serveur.
    const removedRoutes = localRoutes.filter(
      (route) => !remoteRouteIds.has(route.id)
    );

    for (const route of removedRoutes) {

    if (await hasUnresolvedOperations(tx, route.id)) {
      await tx.runAsync(
        `UPDATE routes
        SET is_available = 0
        WHERE id = ?
          AND owner_user_id = ?`,
        [route.id, ownerUserId]
      );

      console.warn(
        `[SYNC DOWN] Tournée ${route.id} retirée du serveur, opérations conservées`
      );

      continue;
    }

      // Supprimer dans l'ordre pour respecter les
      // dépendances entre les tables.
      await tx.runAsync(
        `DELETE FROM sync_queue
         WHERE route_id = ?
           AND status = 'SYNCED'`,
        [route.id]
      );

      await tx.runAsync(
        `DELETE FROM route_stops
         WHERE route_id = ?`,
        [route.id]
      );

      await tx.runAsync(
        `DELETE FROM routes
         WHERE id = ?
           AND owner_user_id = ?`,
        [route.id, ownerUserId]
      );

      console.log(
        `[SYNC DOWN] Tournée ${route.id} retirée du cache`
      );
    }
  });
}

/**
 * Reconstruit les tournées depuis SQLite.
 * Aucun appel réseau.
 */
export async function getRoutesByUser(
  ownerUserId: string
): Promise<Route[]> {

  const rows = await db.getAllAsync<RouteRow>(
    `SELECT
       id,
       name,
       date,
       status,
       depot_json,
       driver_json
     FROM routes
     WHERE owner_user_id = ?
     ORDER BY date DESC`,
    [ownerUserId]
  );

  const routes: Route[] = [];

  for (const row of rows) {

    const stops = await db.getAllAsync<StopRow>(
      `SELECT
         id,
         stop_order,
         status,
         delivery_json,
         delivered_at
       FROM route_stops
       WHERE route_id = ?
       ORDER BY stop_order ASC`,
      [row.id]
    );

    routes.push({
      id: row.id,
      name: row.name,
      date: row.date,
      status: row.status,
      depot: JSON.parse(row.depot_json),
      driver: row.driver_json
        ? JSON.parse(row.driver_json)
        : undefined,

      stops: stops.map((stop) => ({
        id: stop.id,
        order: stop.stop_order,
        status: stop.status,
        delivery: JSON.parse(stop.delivery_json),
        deliveredAt: stop.delivered_at ?? undefined
      }))
    });
  }

  return routes;
}
