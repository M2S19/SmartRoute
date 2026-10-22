import { createContext, ReactNode, useCallback, useEffect, useMemo, useState } from 'react';
import { authService } from '../services';
import { setUnauthorizedHandler } from '../services/http/httpClient';
import { tokenStorage } from '../storage/tokenStorage';
import type { LoginRequest, User } from '../types';
import { isJwtExpired } from '../utils/jwt';

const SESSION_EXPIRED_MESSAGE = 'Votre session a expiré. Veuillez vous reconnecter.';

export interface AuthContextValue {
  user: User | null;
  /** Vrai tant que la session enregistrée n'a pas été relue au démarrage. */
  isRestoring: boolean;
  /** Message à afficher sur l'écran de connexion (ex. session expirée), sinon null. */
  sessionMessage: string | null;
  /** Lève une ApiError si la connexion échoue : l'écran gère l'affichage. */
  login(request: LoginRequest): Promise<User>;
  logout(): Promise<void>;
}

export const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isRestoring, setIsRestoring] = useState(true);
  const [sessionMessage, setSessionMessage] = useState<string | null>(null);

  // Restauration hors ligne : on ne revalide pas le token auprès de l'API ici,
  // un livreur doit pouvoir rouvrir l'app sans réseau. Seul un token déjà expiré est écarté.
  useEffect(() => {
    let cancelled = false;
    tokenStorage
      .loadSession()
      .catch(() => null)
      .then(async (session) => {
        if (cancelled) return;
        if (session && isJwtExpired(session.token)) {
          await tokenStorage.clearSession().catch(() => undefined);
          setSessionMessage(SESSION_EXPIRED_MESSAGE);
          setUser(null);
        } else {
          setUser(session?.user ?? null);
        }
        setIsRestoring(false);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const login = useCallback(async (request: LoginRequest) => {
    const response = await authService.login(request);
    await tokenStorage.saveSession({ token: response.token, user: response.user });
    setSessionMessage(null);
    setUser(response.user);
    return response.user;
  }, []);

  const logout = useCallback(async () => {
    setSessionMessage(null);
    try {
      await tokenStorage.clearSession();
    } finally {
      // On quitte l'espace protégé même si l'effacement du stockage échoue.
      setUser(null);
    }
  }, []);

  // 401 sur une requête authentifiée (token expiré ou refusé) : la session est invalidée
  // et l'utilisateur revient au Login. Un échec de connexion (requête sans token) ne passe pas ici.
  useEffect(() => {
    setUnauthorizedHandler(() => {
      void tokenStorage
        .clearSession()
        .catch(() => undefined)
        .finally(() => {
          setSessionMessage(SESSION_EXPIRED_MESSAGE);
          setUser(null);
        });
    });
    return () => setUnauthorizedHandler(null);
  }, []);

  const value = useMemo(
    () => ({ user, isRestoring, sessionMessage, login, logout }),
    [user, isRestoring, sessionMessage, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
