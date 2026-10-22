import { Platform } from 'react-native';

/**
 * Configuration centralisée de l'accès à l'API ASP.NET Core.
 * Les variables EXPO_PUBLIC_* sont injectées par Expo au build (voir .env.example).
 * Ne jamais y mettre de secret : elles sont lisibles dans le bundle.
 * Elles doivent être lues en notation pointée (process.env.EXPO_PUBLIC_X), sinon Expo ne les injecte pas.
 */

/**
 * URL utilisée quand EXPO_PUBLIC_API_BASE_URL n'est pas définie :
 * - émulateur Android : 10.0.2.2 désigne la machine hôte ;
 * - web et simulateur iOS : 127.0.0.1 (l'API écoute en IPv4 ; « localhost » peut être résolu en IPv6).
 * Sur un téléphone physique, définir EXPO_PUBLIC_API_BASE_URL avec l'IP du PC sur le réseau local.
 */
function defaultApiBaseUrl(): string {
  return Platform.OS === 'android' ? 'http://10.0.2.2:5000' : 'http://127.0.0.1:5000';
}

export const API_BASE_URL: string = (process.env.EXPO_PUBLIC_API_BASE_URL || defaultApiBaseUrl()).replace(/\/+$/, '');

export const API_TIMEOUT_MS = 15000;

function readFlag(value: string | undefined, fallback: boolean): boolean {
  if (value === 'true') return true;
  if (value === 'false') return false;
  return fallback;
}

// Valeur par défaut de chaque domaine : mocks activés tant que rien n'est configuré.
const MOCKS_BY_DEFAULT = readFlag(process.env.EXPO_PUBLIC_USE_MOCKS, true);

/**
 * Données simulées ou API réelle, par fonctionnalité.
 * Si l'authentification est simulée, le token est factice : les fonctionnalités branchées sur l'API répondront 401.
 */
export const MOCKS = {
  /** Connexion (POST /api/auth/login). */
  auth: readFlag(process.env.EXPO_PUBLIC_MOCK_AUTH, MOCKS_BY_DEFAULT),
  /** US1 — optimisation d'une tournée (POST /api/routes/optimize). */
  optimize: readFlag(process.env.EXPO_PUBLIC_MOCK_OPTIMIZE, MOCKS_BY_DEFAULT),
  /** US3 — tournées du livreur (GET /api/routes/me, PATCH /api/routes/me/{routeId}/stops/{stopId}/deliver). */
  myRoute: readFlag(process.env.EXPO_PUBLIC_MOCK_MY_ROUTE, MOCKS_BY_DEFAULT),
  /** US2 — affectation d'une livraison (assign-best-driver, confirm-assignment). */
  deliveries: readFlag(process.env.EXPO_PUBLIC_MOCK_DELIVERIES, MOCKS_BY_DEFAULT),
} as const;

export const endpoints = {
  health: '/api/health',
  login: '/api/auth/login',
  optimizeRoute: '/api/routes/optimize',
  myRoute: '/api/routes/me',
  deliverStop: (routeId: string, stopId: string) =>
    `/api/routes/me/${encodeURIComponent(routeId)}/stops/${encodeURIComponent(stopId)}/deliver`,
  assignBestDriver: '/api/deliveries/assign-best-driver',
  confirmAssignment: '/api/deliveries/confirm-assignment',
} as const;
