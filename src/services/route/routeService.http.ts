import { endpoints } from '../../config/api';
import type { OptimizeRouteResult, Route, RouteStop } from '../../types';
import { httpClient } from '../http/httpClient';
import type { RouteService } from './routeService';

export const routeServiceHttp: RouteService = {
  optimize: (request) => httpClient.post<OptimizeRouteResult>(endpoints.optimizeRoute, request),
  getMyRoute: () => httpClient.get<Route>(endpoints.myRoute),
  markStopDelivered: (stopId) => httpClient.patch<RouteStop>(endpoints.deliverStop(stopId)),
};
