import { USE_MOCKS } from '../../config/api';
import type { LoginRequest, LoginResponse } from '../../types';
import { authServiceHttp } from './authService.http';
import { authServiceMock } from './authService.mock';

export interface AuthService {
  login(request: LoginRequest): Promise<LoginResponse>;
}

export const authService: AuthService = USE_MOCKS ? authServiceMock : authServiceHttp;
