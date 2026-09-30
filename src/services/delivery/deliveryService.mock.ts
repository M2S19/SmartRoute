import {
  containsErrorKeyword,
  mockColmarRoute,
  mockDriverRoute,
  mockMulhouseRoute,
  simulateDelay,
} from '../../mocks';
import { ApiError } from '../../types';
import type { AssignmentProposal, Route } from '../../types';
import type { DeliveryService } from './deliveryService';

let nextParcelRef = 8805;

/**
 * Choix FICTIF de la tournée : simple correspondance sur le nom de ville.
 * Le vrai calcul (Cheapest Insertion sur toutes les tournées) est fait par l'API.
 */
function pickFakeRoute(label: string): { route: Route; extraDistanceKm: number } {
  const lower = label.toLowerCase();
  if (lower.includes('mulhouse')) return { route: mockMulhouseRoute, extraDistanceKm: 3.1 };
  if (lower.includes('colmar')) return { route: mockColmarRoute, extraDistanceKm: 1.8 };
  return { route: mockDriverRoute, extraDistanceKm: 2.4 };
}

export const deliveryServiceMock: DeliveryService = {
  async assignBestDriver({ address }) {
    await simulateDelay();

    if (address.label.trim().length === 0) {
      throw new ApiError('VALIDATION_ERROR', 'Saisissez une adresse de livraison.', 400);
    }
    if (containsErrorKeyword(address.label)) {
      throw new ApiError('ASSIGNMENT_FAILED', "Aucun livreur disponible n'a pu être trouvé pour cette adresse.", 422);
    }

    const { route, extraDistanceKm } = pickFakeRoute(address.label);
    if (!route.driver) {
      throw new ApiError('ASSIGNMENT_FAILED', 'La tournée sélectionnée n’a pas de livreur.', 422);
    }

    const parcelRef = String(nextParcelRef++);
    return {
      delivery: { id: `dlv-${parcelRef}`, parcelRef, parcelType: 'STANDARD', address },
      routeId: route.id,
      routeName: route.name,
      driver: route.driver,
      insertionPosition: Math.min(route.stops.length + 1, 3),
      extraDistanceKm,
    };
  },

  async confirmAssignment(proposal: AssignmentProposal) {
    await simulateDelay();
    return {
      deliveryId: proposal.delivery.id,
      routeId: proposal.routeId,
      driverName: proposal.driver.fullName,
    };
  },
};
