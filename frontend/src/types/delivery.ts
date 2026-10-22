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

/** Livreur tel que renvoyé par l'API d'affectation (DriverSummaryResponse). */
export interface DriverSummary {
  id: string;
  fullName: string;
  driverCode?: string;
}

/**
 * Proposition renvoyée par POST /api/deliveries/assign-best-driver.
 * Rien n'est enregistré tant que la proposition n'est pas confirmée.
 */
export interface AssignmentProposal {
  /** Adresse de la livraison, telle que saisie et envoyée à l'API. */
  address: Address;
  routeId: string;
  routeName: string;
  /** Version de la tournée au moment de la proposition (renvoyée à la confirmation). */
  routeVersion: number;
  driver: DriverSummary;
  /** Ordre qu'aura la livraison dans la tournée (1 = premier arrêt). */
  insertionPosition: number;
  extraDistanceKm: number;
}

/** Affectation enregistrée par POST /api/deliveries/confirm-assignment. */
export interface AssignmentConfirmation {
  routeId: string;
  routeName: string;
  /** Nouvelle version de la tournée. */
  routeVersion: number;
  /** Identifiant de l'arrêt créé. */
  stopId: string;
  insertionPosition: number;
  extraDistanceKm: number;
  driver: DriverSummary;
}
