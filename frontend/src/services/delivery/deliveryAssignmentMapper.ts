import type { AssignDeliveryRequest, AssignmentConfirmation, AssignmentProposal, DriverSummary } from '../../types';

/**
 * Conversion entre les types de l'écran et le contrat de l'API d'affectation (US2).
 * Module pur (aucune dépendance d'exécution). Aucune donnée n'est inventée :
 * seule l'adresse saisie (envoyée à l'API) est ajoutée à la proposition pour l'affichage.
 */

/** Réponse de POST /api/deliveries/assign-best-driver (AssignmentProposalResponse, C#). */
export interface AssignmentProposalApi {
  routeId: string;
  routeName: string;
  routeVersion: number;
  driver: DriverSummary;
  insertionPosition: number;
  extraDistanceKm: number;
}

/** Corps de POST /api/deliveries/confirm-assignment (ConfirmAssignmentRequest, C#). */
export interface ConfirmAssignmentApiRequest {
  address: { label: string };
  routeId: string;
  insertionPosition: number;
  routeVersion: number;
}

/** Réponse 201 de POST /api/deliveries/confirm-assignment (ConfirmAssignmentResponse, C#). */
export interface ConfirmAssignmentApiResponse {
  routeId: string;
  routeName: string;
  routeVersion: number;
  stopId: string;
  insertionPosition: number;
  extraDistanceKm: number;
  driver: DriverSummary;
}

export function toAssignBestDriverApiRequest(request: AssignDeliveryRequest): { address: { label: string } } {
  return { address: { label: request.address.label } };
}

export function fromAssignmentProposalApi(
  response: AssignmentProposalApi,
  request: AssignDeliveryRequest,
): AssignmentProposal {
  return {
    address: request.address,
    routeId: response.routeId,
    routeName: response.routeName,
    routeVersion: response.routeVersion,
    driver: response.driver,
    insertionPosition: response.insertionPosition,
    extraDistanceKm: response.extraDistanceKm,
  };
}

export function toConfirmAssignmentApiRequest(proposal: AssignmentProposal): ConfirmAssignmentApiRequest {
  return {
    address: { label: proposal.address.label },
    routeId: proposal.routeId,
    insertionPosition: proposal.insertionPosition,
    routeVersion: proposal.routeVersion,
  };
}

export function fromConfirmAssignmentApi(response: ConfirmAssignmentApiResponse): AssignmentConfirmation {
  return {
    routeId: response.routeId,
    routeName: response.routeName,
    routeVersion: response.routeVersion,
    stopId: response.stopId,
    insertionPosition: response.insertionPosition,
    extraDistanceKm: response.extraDistanceKm,
    driver: response.driver,
  };
}
