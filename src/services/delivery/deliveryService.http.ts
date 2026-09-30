import { endpoints } from '../../config/api';
import type { AssignmentConfirmation, AssignmentProposal } from '../../types';
import { httpClient } from '../http/httpClient';
import type { DeliveryService } from './deliveryService';

export const deliveryServiceHttp: DeliveryService = {
  assignBestDriver: (request) => httpClient.post<AssignmentProposal>(endpoints.assignBestDriver, request),
  confirmAssignment: (proposal) => httpClient.post<AssignmentConfirmation>(endpoints.confirmAssignment, proposal),
};
