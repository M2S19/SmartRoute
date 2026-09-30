import { USE_MOCKS } from '../../config/api';
import type { OptimizeRouteRequest, OptimizeRouteResult, Route, RouteStop } from '../../types';
import { routeServiceHttp } from './routeService.http';
import { routeServiceMock } from './routeService.mock';

export interface RouteService {
  /** POST /api/routes/optimize : l'ordre est calculé exclusivement par le backend. */
  optimize(request: OptimizeRouteRequest): Promise<OptimizeRouteResult>;
  /** GET /api/routes/me : tournée du livreur connecté. */
  getMyRoute(): Promise<Route>;
  /** PATCH /api/routes/me/stops/{stopId}/deliver */
  markStopDelivered(stopId: string): Promise<RouteStop>;
}

export const routeService: RouteService = USE_MOCKS ? routeServiceMock : routeServiceHttp;
