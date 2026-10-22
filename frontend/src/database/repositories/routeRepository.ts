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

export async function saveRoutes(
  ownerUserId: string,
  routes: Route[]
): Promise<void> {
  await db.withExclusiveTransactionAsync(async (tx) => {
    for (const route of routes) {
      await tx.runAsync(
        `INSERT OR REPLACE INTO routes
         (id, owner_user_id, name, date, status,
          depot_json, driver_json, updated_at)
         VALUES (?, ?, ?, ?, ?, ?, ?, ?)`,
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

      for (const stop of route.stops) {
        await tx.runAsync(
          `INSERT OR REPLACE INTO route_stops
           (id, route_id, address, stop_order, status,
            delivery_json, delivered_at, sync_status)
           VALUES (?, ?, ?, ?, ?, ?, ?, ?)`,
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
    }
  });
}

export async function getRoutesByUser(
  ownerUserId: string
): Promise<Route[]> {
  const rows = await db.getAllAsync<RouteRow>(
    `SELECT id, name, date, status, depot_json, driver_json
     FROM routes
     WHERE owner_user_id = ?
     ORDER BY date DESC`,
    [ownerUserId]
  );

  const routes: Route[] = [];

  for (const row of rows) {
    const stops = await db.getAllAsync<StopRow>(
      `SELECT id, stop_order, status,
              delivery_json, delivered_at
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