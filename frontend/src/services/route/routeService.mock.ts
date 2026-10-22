import { containsErrorKeyword, mockDriverRoute, simulateDelay } from '../../mocks';
import { ApiError } from '../../types';
import type { Address, OptimizeRouteResult, Route, RouteStop } from '../../types';
import { deepClone } from '../../utils/clone';
import { roundTo } from '../../utils/format';
import type { RouteService } from './routeService';

// Tournées du livreur conservées en mémoire pour la durée de vie de l'app :
// un arrêt marqué livré le reste après « Actualiser ».
let myRoutesState: Route[] = [deepClone(mockDriverRoute)];

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

  async getMyRoutes() {
    await simulateDelay();
    return deepClone(myRoutesState);
  },

  // Mêmes règles que l'API (RouteService.DeliverStopAsync).
  async markStopDelivered(routeId, stopId) {
    await simulateDelay();

    const route = myRoutesState.find((r) => r.id === routeId);
    const stop = route?.stops.find((s) => s.id === stopId);
    if (!route || !stop) {
      throw new ApiError('NOT_FOUND', 'Arrêt introuvable dans cette tournée.', 404);
    }
    if (route.status !== 'ASSIGNED' && route.status !== 'IN_PROGRESS') {
      throw new ApiError('ROUTE_NOT_MODIFIABLE', "Cette tournée n'est plus modifiable.", 409);
    }
    if (stop.status === 'DELIVERED') {
      throw new ApiError('STOP_ALREADY_DELIVERED', 'Cet arrêt est déjà livré.', 409);
    }
    const nextStop = [...route.stops].sort((a, b) => a.order - b.order).find((s) => s.status !== 'DELIVERED');
    if (nextStop?.id !== stopId) {
      throw new ApiError('STOP_NOT_NEXT', "Les arrêts doivent être livrés dans l'ordre de la tournée.", 409);
    }
    if (containsErrorKeyword(stop.delivery.address.label)) {
      throw new ApiError('SERVER_ERROR', 'Le serveur a rencontré une erreur. Réessayez plus tard.', 500);
    }

    const updated: RouteStop = { ...stop, status: 'DELIVERED', deliveredAt: new Date().toISOString() };
    const stops = route.stops.map((s) => (s.id === stopId ? updated : s));
    const allDelivered = stops.every((s) => s.status === 'DELIVERED');
    const updatedRoute: Route = { ...route, stops, status: allDelivered ? 'COMPLETED' : 'IN_PROGRESS' };

    myRoutesState = myRoutesState.map((r) => (r.id === routeId ? updatedRoute : r));
    return deepClone(updatedRoute);
  },
};
