import { createContext, ReactNode, useCallback, useEffect, useMemo, useState } from 'react';
import { authService } from '../services';
import { tokenStorage } from '../storage/tokenStorage';
import type { LoginRequest, User } from '../types';

export interface AuthContextValue {
  user: User | null;
  /** Vrai tant que la session enregistrée n'a pas été relue au démarrage. */
  isRestoring: boolean;
  /** Lève une ApiError si la connexion échoue : l'écran gère l'affichage. */
  login(request: LoginRequest): Promise<User>;
  logout(): Promise<void>;
}

export const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isRestoring, setIsRestoring] = useState(true);

  // Restauration hors ligne : on ne revalide pas le token auprès de l'API ici,
  // un livreur doit pouvoir rouvrir l'app sans réseau.
  useEffect(() => {
    let cancelled = false;
    tokenStorage
      .loadSession()
      .catch(() => null)
      .then((session) => {
        if (cancelled) return;
        setUser(session?.user ?? null);
        setIsRestoring(false);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const login = useCallback(async (request: LoginRequest) => {
    const response = await authService.login(request);
    await tokenStorage.saveSession({ token: response.token, user: response.user });
    setUser(response.user);
    return response.user;
  }, []);

  const logout = useCallback(async () => {
    try {
      await tokenStorage.clearSession();
    } finally {
      // On quitte l'espace protégé même si l'effacement du stockage échoue.
      setUser(null);
    }
  }, []);

  const value = useMemo(() => ({ user, isRestoring, login, logout }), [user, isRestoring, login, logout]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
