import { API_BASE_URL, endpoints } from '../../config/api';
import { httpClient } from '../http/httpClient';

export interface HealthStatus {
  status: string;
}

/** GET /api/health : vérifie que l'API répond (anonyme, sans appel à MongoDB). Pas de mock. */
export const healthService = {
  baseUrl: API_BASE_URL,
  check: () => httpClient.get<HealthStatus>(endpoints.health, { auth: false }),
};
