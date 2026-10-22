import { endpoints } from '../../config/api';
import { httpClient } from '../http/httpClient';
import {
  type AssignmentProposalApi,
  type ConfirmAssignmentApiResponse,
  fromAssignmentProposalApi,
  fromConfirmAssignmentApi,
  toAssignBestDriverApiRequest,
  toConfirmAssignmentApiRequest,
} from './deliveryAssignmentMapper';
import type { DeliveryService } from './deliveryService';

export const deliveryServiceHttp: DeliveryService = {
  assignBestDriver: async (request) => {
    const response = await httpClient.post<AssignmentProposalApi>(
      endpoints.assignBestDriver,
      toAssignBestDriverApiRequest(request),
    );
    return fromAssignmentProposalApi(response, request);
  },
  confirmAssignment: async (proposal) => {
    const response = await httpClient.post<ConfirmAssignmentApiResponse>(
      endpoints.confirmAssignment,
      toConfirmAssignmentApiRequest(proposal),
    );
    return fromConfirmAssignmentApi(response);
  },
};
