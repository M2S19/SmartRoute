
import type { Route } from '../../types';
import { routeServiceHttp } from './routeService.http';
import {
  getRoutesByUser,
  saveRoutes,
} from '../../database/repositories/routeRepository';

/**
 * Récupère les tournées stockées sur le téléphone.
 * Aucun appel réseau.
 */
export async function getLocalRoutes(
  userId: string
): Promise<Route[]> {
  return getRoutesByUser(userId);
}

/**
 * Récupère les tournées depuis MongoDB
 * puis met à jour la base SQLite.
 *
 * À utiliser uniquement tant que les modifications
 * hors ligne ne sont pas encore activées.
 */
export async function refreshRoutesFromServer(
  userId: string
): Promise<Route[]> {
  const remoteRoutes = await routeServiceHttp.getMyRoutes();

  await saveRoutes(userId, remoteRoutes);

  return getRoutesByUser(userId);
}
