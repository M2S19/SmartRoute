import type { Address, OptimizeRouteRequest, OptimizeRouteResult, RouteStop } from '../../types';

/**
 * Conversion entre les types de l'écran d'optimisation et le contrat de POST /api/routes/optimize.
 * Module pur (aucune dépendance d'exécution) : les DTO du backend ne sont pas modifiés pour le front.
 */

/** Corps attendu par l'API (OptimizeRouteRequest, C#). */
export interface OptimizeRouteApiRequest {
  depot: { label: string };
  stops: { label: string }[];
}

/** Point de l'ordre optimisé renvoyé par l'API (OptimizedPointResponse, C#). */
export interface OptimizedPointApi {
  position: number;
  label: string;
  latitude: number;
  longitude: number;
}

/** Réponse de l'API (OptimizeRouteResponse, C#). Distances en km, gain en pourcentage. */
export interface OptimizeRouteApiResponse {
  distanceBeforeKm: number;
  distanceAfterKm: number;
  gainPercent: number;
  /** Ordre de passage complet : le dépôt en premier (départ) et en dernier (retour). */
  optimizedOrder: OptimizedPointApi[];
}

export const DEFAULT_ROUTE_NAME = 'Nouvelle tournée';

export function toOptimizeRouteApiRequest(request: OptimizeRouteRequest): OptimizeRouteApiRequest {
  return {
    depot: { label: request.depot.label },
    stops: request.deliveries.map((delivery) => ({ label: delivery.label })),
  };
}

function toAddress(point: OptimizedPointApi): Address {
  return { label: point.label, latitude: point.latitude, longitude: point.longitude };
}

/**
 * Le dépôt est exclu de la liste des arrêts par sa position (premier et dernier point),
 * jamais par son libellé : un arrêt peut avoir le même libellé que le dépôt.
 * Les identifiants et champs de colis n'existent pas pour une optimisation (rien n'est enregistré) :
 * valeurs techniques locales, non affichées par l'écran.
 */
export function fromOptimizeRouteApiResponse(
  response: OptimizeRouteApiResponse,
  request: OptimizeRouteRequest,
): OptimizeRouteResult {
  const points = [...response.optimizedOrder].sort((a, b) => a.position - b.position);
  if (points.length < 2) {
    throw new Error("Réponse d'optimisation incomplète.");
  }

  const depotPoint = points[0];
  const stopPoints = points.slice(1, -1);

  const orderedStops: RouteStop[] = stopPoints.map((point, index) => ({
    id: `optimized-stop-${index + 1}`,
    order: index + 1,
    status: 'PENDING',
    delivery: {
      id: `optimized-delivery-${index + 1}`,
      parcelRef: '',
      parcelType: 'STANDARD',
      address: toAddress(point),
    },
  }));

  return {
    routeName: request.name?.trim() || DEFAULT_ROUTE_NAME,
    depot: { id: 'depot', name: 'Dépôt', address: toAddress(depotPoint) },
    orderedStops,
    distanceBeforeKm: response.distanceBeforeKm,
    distanceAfterKm: response.distanceAfterKm,
    gainKm: response.distanceBeforeKm - response.distanceAfterKm,
    gainPercent: response.gainPercent,
  };
}
