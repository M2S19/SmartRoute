import type { ParcelType, RouteStatus } from '../types';

export const ROUTE_STATUS_LABELS: Record<RouteStatus, string> = {
  DRAFT: 'Brouillon',
  ASSIGNED: 'Affectée',
  IN_PROGRESS: 'En cours',
  COMPLETED: 'Terminée',
};

export const PARCEL_TYPE_LABELS: Record<ParcelType, string> = {
  STANDARD: 'standard',
  FRAGILE: 'fragile',
};
