import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';
import type { User, UserRole } from '../types';

/**
 * Session persistée : JWT + utilisateur (JSON).
 * - Mobile (Expo Go) : expo-secure-store (Keychain iOS / Keystore Android).
 * - Web : localStorage (expo-secure-store n'existe pas sur le web).
 * Pas d'AsyncStorage ni de SQLite pour le token.
 */

const TOKEN_KEY = 'smartroute.jwt';
const USER_KEY = 'smartroute.user';

const isWeb = Platform.OS === 'web';

async function getItem(key: string): Promise<string | null> {
  if (isWeb) {
    try {
      return window.localStorage.getItem(key);
    } catch {
      return null;
    }
  }
  return SecureStore.getItemAsync(key);
}

async function setItem(key: string, value: string): Promise<void> {
  if (isWeb) {
    try {
      window.localStorage.setItem(key, value);
    } catch {
      // Stockage indisponible (navigation privée) : la session ne sera pas conservée.
    }
    return;
  }
  await SecureStore.setItemAsync(key, value);
}

async function removeItem(key: string): Promise<void> {
  if (isWeb) {
    try {
      window.localStorage.removeItem(key);
    } catch {
      // Rien à faire.
    }
    return;
  }
  await SecureStore.deleteItemAsync(key);
}

const ROLES: readonly UserRole[] = ['RESPONSABLE_LOGISTIQUE', 'LIVREUR'];

/** Relit l'utilisateur stocké en vérifiant sa forme (donnée potentiellement corrompue). */
function parseUser(raw: string): User | null {
  try {
    const value: unknown = JSON.parse(raw);
    if (typeof value !== 'object' || value === null) return null;
    const candidate = value as Partial<User>;
    if (
      typeof candidate.id === 'string' &&
      typeof candidate.email === 'string' &&
      typeof candidate.fullName === 'string' &&
      typeof candidate.role === 'string' &&
      ROLES.includes(candidate.role)
    ) {
      return {
        id: candidate.id,
        email: candidate.email,
        fullName: candidate.fullName,
        role: candidate.role,
        driverCode: typeof candidate.driverCode === 'string' ? candidate.driverCode : undefined,
      };
    }
    return null;
  } catch {
    return null;
  }
}

export interface StoredSession {
  token: string;
  user: User;
}

export const tokenStorage = {
  getToken(): Promise<string | null> {
    return getItem(TOKEN_KEY);
  },

  async saveSession({ token, user }: StoredSession): Promise<void> {
    await setItem(TOKEN_KEY, token);
    await setItem(USER_KEY, JSON.stringify(user));
  },

  /** Renvoie la session complète, ou null si le token ou l'utilisateur manque / est invalide. */
  async loadSession(): Promise<StoredSession | null> {
    const [token, rawUser] = await Promise.all([getItem(TOKEN_KEY), getItem(USER_KEY)]);
    if (!token || !rawUser) return null;
    const user = parseUser(rawUser);
    return user ? { token, user } : null;
  },

  async clearSession(): Promise<void> {
    await Promise.all([removeItem(TOKEN_KEY), removeItem(USER_KEY)]);
  },
};
