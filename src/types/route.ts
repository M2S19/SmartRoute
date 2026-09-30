import type { User } from './auth';
import type { Delivery } from './delivery';

/** Coordonnées optionnelles : le backend les calcule (géocodage) si absentes. */
export interface Address {
  label: string;
  latitude?: number;
  longitude?: number;
}

export interface Depot {
  id: string;
  name: string;
  address: Address;
}

export type StopStatus = 'PENDING' | 'DELIVERED';

/** Un arrêt d'une tournée. L'adresse vit uniquement dans delivery.address. */
export interface RouteStop {
  id: string;
  /** Ordre de passage, à partir de 1. */
  order: number;
  status: StopStatus;
  delivery: Delivery;
  /** Date ISO de validation de la livraison. */
  deliveredAt?: string;
}

export type RouteStatus = 'DRAFT' | 'ASSIGNED' | 'IN_PROGRESS' | 'COMPLETED';

export interface Route {
  id: string;
  name: string;
  /** Date ISO (jour de la tournée). */
  date: string;
  status: RouteStatus;
  depot: Depot;
  stops: RouteStop[];
  driver?: User;
}

export interface OptimizeRouteRequest {
  name?: string;
  depot: Address;
  deliveries: Address[];
}

export interface OptimizeRouteResult {
  routeName: string;
  depot: Depot;
  /** Arrêts dans l'ordre optimisé calculé par le backend. */
  orderedStops: RouteStop[];
  distanceBeforeKm: number;
  distanceAfterKm: number;
  gainKm: number;
  gainPercent: number;
}
