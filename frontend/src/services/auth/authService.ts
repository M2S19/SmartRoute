import { MOCKS } from '../../config/api';
import type { LoginRequest, LoginResponse } from '../../types';
import { authServiceHttp } from './authService.http';
import { authServiceMock } from './authService.mock';

export interface AuthService {
  login(request: LoginRequest): Promise<LoginResponse>;
}

export const authService: AuthService = MOCKS.auth ? authServiceMock : authServiceHttp;
