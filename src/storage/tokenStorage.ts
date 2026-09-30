import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';

/**
 * Emplacement du JWT : expo-secure-store (Keychain iOS / Keystore Android).
 * Pas d'AsyncStorage ni de SQLite pour le token.
 */

const TOKEN_KEY = 'smartroute.jwt';

// expo-secure-store n'existe pas sur le web : repli en mémoire pour ne pas planter.
let webFallback: string | null = null;
const isWeb = Platform.OS === 'web';

export const tokenStorage = {
  async getToken(): Promise<string | null> {
    if (isWeb) return webFallback;
    return SecureStore.getItemAsync(TOKEN_KEY);
  },

  async setToken(token: string): Promise<void> {
    if (isWeb) {
      webFallback = token;
      return;
    }
    await SecureStore.setItemAsync(TOKEN_KEY, token);
  },

  async clearToken(): Promise<void> {
    if (isWeb) {
      webFallback = null;
      return;
    }
    await SecureStore.deleteItemAsync(TOKEN_KEY);
  },
};
