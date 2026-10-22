import type { Address, Route, RouteStatus, RouteStop, StopStatus } from '../../types';

/**
 * Conversion entre le contrat de GET /api/routes/me (et PATCH …/deliver) et les types de l'écran du livreur.
 * Module pur (aucune dépendance d'exécution). Aucune donnée n'est inventée : le backend ne gère pas
 * de référence ni de type de colis, ces champs restent vides et ne sont pas affichés.
 */

/** Point renvoyé par l'API (RoutePointResponse, C#). */
export interface RoutePointApi {
  label: string;
  latitude: number;
  longitude: number;
}

/** Arrêt renvoyé par l'API (DriverStopResponse, C#). */
export interface DriverStopApi {
  id: string;
  order: number;
  status: StopStatus;
  address: RoutePointApi;
  /** Date ISO UTC, absente tant que l'arrêt n'est pas livré. */
  deliveredAt?: string;
}

/** Tournée renvoyée par l'API (DriverRouteResponse, C#). */
export interface DriverRouteApi {
  id: string;
  name: string;
  /** Jour de la tournée (yyyy-MM-dd). */
  date: string;
  status: RouteStatus;
  depot: RoutePointApi;
  stops: DriverStopApi[];
}

function toAddress(point: RoutePointApi): Address {
  return { label: point.label, latitude: point.latitude, longitude: point.longitude };
}

function fromDriverStopApi(stop: DriverStopApi): RouteStop {
  return {
    id: stop.id,
    order: stop.order,
    status: stop.status,
    deliveredAt: stop.deliveredAt,
    delivery: {
      id: stop.id,
      parcelRef: '',
      parcelType: 'STANDARD',
      address: toAddress(stop.address),
    },
  };
}

export function fromDriverRouteApi(route: DriverRouteApi): Route {
  return {
    id: route.id,
    name: route.name,
    date: route.date,
    status: route.status,
    depot: { id: `${route.id}-depot`, name: 'Dépôt', address: toAddress(route.depot) },
    stops: [...route.stops].sort((a, b) => a.order - b.order).map(fromDriverStopApi),
  };
}
