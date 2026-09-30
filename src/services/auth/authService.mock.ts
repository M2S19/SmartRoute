import { mockAccounts, simulateDelay } from '../../mocks';
import { ApiError } from '../../types';
import type { AuthService } from './authService';

export const authServiceMock: AuthService = {
  async login({ email, password }) {
    await simulateDelay();

    const user = mockAccounts[email.trim().toLowerCase()];
    if (!user || password.length === 0) {
      throw new ApiError('INVALID_CREDENTIALS', 'Email ou mot de passe incorrect.', 401);
    }

    // Faux jeton : le vrai JWT sera signé par l'API.
    return { token: `mock-jwt.${user.id}`, user };
  },
};
