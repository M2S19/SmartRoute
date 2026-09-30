import { createContext, ReactNode, useCallback, useMemo, useState } from 'react';
import { authService } from '../services';
import { tokenStorage } from '../storage/tokenStorage';
import type { LoginRequest, User } from '../types';

export interface AuthContextValue {
  user: User | null;
  /** Lève une ApiError si la connexion échoue : l'écran gère l'affichage. */
  login(request: LoginRequest): Promise<User>;
  logout(): Promise<void>;
}

export const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);

  const login = useCallback(async (request: LoginRequest) => {
    const response = await authService.login(request);
    await tokenStorage.setToken(response.token);
    setUser(response.user);
    return response.user;
  }, []);

  const logout = useCallback(async () => {
    await tokenStorage.clearToken();
    setUser(null);
  }, []);

  const value = useMemo(() => ({ user, login, logout }), [user, login, logout]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
