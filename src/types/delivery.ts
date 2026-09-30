import type { User } from './auth';
import type { Address } from './route';

export type ParcelType = 'STANDARD' | 'FRAGILE';

export interface Delivery {
  id: string;
  /** Référence colis affichée, ex. « 8802 ». */
  parcelRef: string;
  parcelType: ParcelType;
  address: Address;
  /** Complément d'accès, ex. « Bâtiment B, Interphone 14 ». */
  instructions?: string;
}

export interface AssignDeliveryRequest {
  address: Address;
}

/**
 * Proposition renvoyée par POST /api/deliveries/assign-best-driver.
 * Rien n'est enregistré tant que la proposition n'est pas confirmée.
 */
export interface AssignmentProposal {
  delivery: Delivery;
  routeId: string;
  routeName: string;
  driver: User;
  /** Position d'insertion dans la tournée (1 = premier arrêt). */
  insertionPosition: number;
  extraDistanceKm: number;
}

export interface AssignmentConfirmation {
  deliveryId: string;
  routeId: string;
  driverName: string;
}
