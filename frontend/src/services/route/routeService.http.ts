import { endpoints } from '../../config/api';
import { httpClient } from '../http/httpClient';
import { type DriverRouteApi, fromDriverRouteApi } from './driverRouteMapper';
import {
  fromOptimizeRouteApiResponse,
  type OptimizeRouteApiResponse,
  toOptimizeRouteApiRequest,
} from './routeOptimizationMapper';
import type { RouteService } from './routeService';

export const routeServiceHttp: RouteService = {
  optimize: async (request) => {
    const response = await httpClient.post<OptimizeRouteApiResponse>(
      endpoints.optimizeRoute,
      toOptimizeRouteApiRequest(request),
    );
    return fromOptimizeRouteApiResponse(response, request);
  },
  getMyRoutes: async () => {
    const routes = await httpClient.get<DriverRouteApi[]>(endpoints.myRoute);
    return routes.map(fromDriverRouteApi);
  },
  markStopDelivered: async (routeId, stopId) => {
    const route = await httpClient.patch<DriverRouteApi>(endpoints.deliverStop(routeId, stopId));
    return fromDriverRouteApi(route);
  },
};
