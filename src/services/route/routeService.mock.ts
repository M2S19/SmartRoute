import { containsErrorKeyword, mockDriverRoute, simulateDelay } from '../../mocks';
import { ApiError } from '../../types';
import type { Address, OptimizeRouteResult, Route, RouteStop } from '../../types';
import { deepClone } from '../../utils/clone';
import { roundTo } from '../../utils/format';
import type { RouteService } from './routeService';

// Tournée du livreur conservée en mémoire pour la durée de vie de l'app :
// un arrêt marqué livré le reste après « Actualiser ».
let myRouteState: Route = deepClone(mockDriverRoute);

/**
 * Résultat FICTIF. Aucun algorithme d'optimisation ici : l'ordre et les distances
 * sont simplement dérivés du nombre d'adresses pour produire un affichage plausible.
 * Le vrai calcul (Nearest Neighbor + 2-opt) est fait par l'API.
 */
function buildFakeResult(name: string, depot: Address, deliveries: Address[]): OptimizeRouteResult {
  const reordered = deliveries.length > 2 ? [deliveries[0], ...deliveries.slice(1).reverse()] : deliveries;

  const distanceBeforeKm = roundTo(3.2 + deliveries.length * 2.7, 1);
  const distanceAfterKm = deliveries.length > 2 ? roundTo(distanceBeforeKm * 0.78, 1) : distanceBeforeKm;
  const gainKm = roundTo(distanceBeforeKm - distanceAfterKm, 1);

  return {
    routeName: name,
    depot: { id: 'dep-new', name: 'Dépôt', address: depot },
    orderedStops: reordered.map((address, index) => ({
      id: `opt-stop-${index + 1}`,
      order: index + 1,
      status: 'PENDING',
      delivery: { id: `opt-dlv-${index + 1}`, parcelRef: String(9500 + index + 1), parcelType: 'STANDARD', address },
    })),
    distanceBeforeKm,
    distanceAfterKm,
    gainKm,
    gainPercent: roundTo((gainKm / distanceBeforeKm) * 100, 0),
  };
}

export const routeServiceMock: RouteService = {
  async optimize({ name, depot, deliveries }) {
    await simulateDelay();

    if (deliveries.length === 0) {
      throw new ApiError('VALIDATION_ERROR', 'Ajoutez au moins une adresse de livraison.', 400);
    }
    if ([depot, ...deliveries].some((a) => containsErrorKeyword(a.label))) {
      throw new ApiError('OPTIMIZATION_FAILED', "L'optimisation a échoué : une adresse n'a pas pu être localisée.", 422);
    }

    return buildFakeResult(name?.trim() || 'Nouvelle tournée', depot, deliveries);
  },

  async getMyRoute() {
    await simulateDelay();
    return deepClone(myRouteState);
  },

  async markStopDelivered(stopId) {
    await simulateDelay();

    const stop = myRouteState.stops.find((s) => s.id === stopId);
    if (!stop) {
      throw new ApiError('NOT_FOUND', 'Arrêt introuvable dans votre tournée.', 404);
    }
    if (containsErrorKeyword(stop.delivery.address.label)) {
      throw new ApiError('UNKNOWN', 'le serveur a refusé la mise à jour.', 500);
    }

    const updated: RouteStop =
      stop.status === 'DELIVERED' ? stop : { ...stop, status: 'DELIVERED', deliveredAt: new Date().toISOString() };
    const stops = myRouteState.stops.map((s) => (s.id === stopId ? updated : s));
    const allDelivered = stops.every((s) => s.status === 'DELIVERED');

    myRouteState = { ...myRouteState, stops, status: allDelivered ? 'COMPLETED' : myRouteState.status };
    return deepClone(updated);
  },
};
