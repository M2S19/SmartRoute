import { MOCKS } from '../../config/api';
import type { OptimizeRouteRequest, OptimizeRouteResult, Route } from '../../types';
import { routeServiceHttp } from './routeService.http';
import { routeServiceMock } from './routeService.mock';

export interface RouteService {
  /** POST /api/routes/optimize : l'ordre est calculé exclusivement par le backend. */
  optimize(request: OptimizeRouteRequest): Promise<OptimizeRouteResult>;
  /** GET /api/routes/me : tournées du livreur connecté (identifié par son JWT), la plus récente d'abord. */
  getMyRoutes(): Promise<Route[]>;
  /** PATCH /api/routes/me/{routeId}/stops/{stopId}/deliver : renvoie la tournée à jour (état serveur). */
  markStopDelivered(routeId: string, stopId: string): Promise<Route>;
}

// L'optimisation (US1) et la tournée du livreur (US3) se branchent sur l'API indépendamment.
const optimizeImpl = MOCKS.optimize ? routeServiceMock : routeServiceHttp;
const myRouteImpl = MOCKS.myRoute ? routeServiceMock : routeServiceHttp;

export const routeService: RouteService = {
  optimize: optimizeImpl.optimize,
  getMyRoutes: myRouteImpl.getMyRoutes,
  markStopDelivered: myRouteImpl.markStopDelivered,
};
