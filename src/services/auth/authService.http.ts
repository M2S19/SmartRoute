import { endpoints } from '../../config/api';
import type { LoginResponse } from '../../types';
import { httpClient } from '../http/httpClient';
import type { AuthService } from './authService';

export const authServiceHttp: AuthService = {
  login: (request) => httpClient.post<LoginResponse>(endpoints.login, request, { auth: false }),
};
