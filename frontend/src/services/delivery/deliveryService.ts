import { MOCKS } from '../../config/api';
import type { AssignDeliveryRequest, AssignmentConfirmation, AssignmentProposal } from '../../types';
import { deliveryServiceHttp } from './deliveryService.http';
import { deliveryServiceMock } from './deliveryService.mock';

export interface DeliveryService {
  /** POST /api/deliveries/assign-best-driver : proposition, rien n'est enregistré. */
  assignBestDriver(request: AssignDeliveryRequest): Promise<AssignmentProposal>;
  /** POST /api/deliveries/confirm-assignment : enregistre la proposition. */
  confirmAssignment(proposal: AssignmentProposal): Promise<AssignmentConfirmation>;
}

export const deliveryService: DeliveryService = MOCKS.deliveries ? deliveryServiceMock : deliveryServiceHttp;
