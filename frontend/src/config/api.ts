/**
 * Configuration centralisée de l'accès à l'API ASP.NET Core.
 * Les variables EXPO_PUBLIC_* sont injectées par Expo au build (voir .env.example).
 * Ne jamais y mettre de secret : elles sont lisibles dans le bundle.
 */

export const API_BASE_URL: string = process.env.EXPO_PUBLIC_API_BASE_URL ?? 'http://localhost:5000';

// Mocks activés par défaut tant que l'API n'existe pas.
export const USE_MOCKS: boolean = process.env.EXPO_PUBLIC_USE_MOCKS !== 'false';

export const API_TIMEOUT_MS = 15000;

export const endpoints = {
  login: '/api/auth/login',
  optimizeRoute: '/api/routes/optimize',
  myRoute: '/api/routes/me',
  deliverStop: (stopId: string) => `/api/routes/me/stops/${encodeURIComponent(stopId)}/deliver`,
  assignBestDriver: '/api/deliveries/assign-best-driver',
  confirmAssignment: '/api/deliveries/confirm-assignment',
} as const;
