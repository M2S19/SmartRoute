import { API_BASE_URL, API_TIMEOUT_MS } from '../../config/api';
import { tokenStorage } from '../../storage/tokenStorage';
import { ApiError } from '../../types';
import type { ApiErrorCode, FieldErrors } from '../../types';

type HttpMethod = 'GET' | 'POST' | 'PATCH';

interface RequestOptions {
  body?: unknown;
  /** Ajoute l'en-tête Authorization: Bearer <JWT>. Vrai par défaut ; faux pour le login et le healthcheck. */
  auth?: boolean;
}

/** Champs utiles d'une réponse d'erreur ASP.NET Core ProblemDetails (RFC 7807 / 9457). */
interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  code?: string;
  errors?: FieldErrors;
}

// Appelé quand une requête authentifiée reçoit 401 (token expiré ou invalide). Enregistré par AuthProvider.
let unauthorizedHandler: (() => void) | null = null;

export function setUnauthorizedHandler(handler: (() => void) | null): void {
  unauthorizedHandler = handler;
}

const KNOWN_CODES: readonly ApiErrorCode[] = [
  'INVALID_CREDENTIALS',
  'VALIDATION_ERROR',
  'UNAUTHORIZED',
  'FORBIDDEN',
  'NOT_FOUND',
  'CONFLICT',
  'NO_ELIGIBLE_ROUTE',
  'ROUTE_NOT_ELIGIBLE',
  'PROPOSAL_OUTDATED',
  'ROUTE_NOT_MODIFIABLE',
  'STOP_ALREADY_DELIVERED',
  'STOP_NOT_NEXT',
];

const DEFAULT_MESSAGES: Record<number, string> = {
  400: 'Certains champs sont invalides.',
  401: 'Authentification requise.',
  403: 'Accès refusé.',
  404: 'Ressource introuvable.',
};

const SERVER_ERROR_MESSAGE = 'Le serveur a rencontré une erreur. Réessayez plus tard.';
const SESSION_EXPIRED_MESSAGE = 'Votre session a expiré. Veuillez vous reconnecter.';

function asString(value: unknown): string | undefined {
  return typeof value === 'string' && value.trim().length > 0 ? value : undefined;
}

/** Garde uniquement les champs attendus, avec le bon type (le corps peut être n'importe quoi). */
function parseProblemDetails(value: unknown): ProblemDetails {
  if (typeof value !== 'object' || value === null) return {};
  const raw = value as Record<string, unknown>;

  let errors: FieldErrors | undefined;
  if (typeof raw.errors === 'object' && raw.errors !== null) {
    errors = {};
    for (const [field, messages] of Object.entries(raw.errors as Record<string, unknown>)) {
      if (Array.isArray(messages)) {
        const texts = messages.filter((m): m is string => typeof m === 'string');
        if (texts.length > 0) errors[field] = texts;
      }
    }
    if (Object.keys(errors).length === 0) errors = undefined;
  }

  return {
    title: asString(raw.title),
    detail: asString(raw.detail),
    status: typeof raw.status === 'number' ? raw.status : undefined,
    code: asString(raw.code),
    errors,
  };
}

function defaultCodeFor(status: number): ApiErrorCode {
  switch (status) {
    case 400:
      return 'VALIDATION_ERROR';
    case 401:
      return 'UNAUTHORIZED';
    case 403:
      return 'FORBIDDEN';
    case 404:
      return 'NOT_FOUND';
    case 409:
      return 'CONFLICT';
    default:
      return 'UNKNOWN';
  }
}

/**
 * Convertit une réponse d'erreur en ApiError au message affichable.
 * - 5xx : message générique, jamais le détail du serveur (il peut contenir des informations techniques).
 * - 401 sur une requête authentifiée : session expirée. 401 sans token (ex. login) : identifiants refusés.
 * - 403 : accès refusé (rôle insuffisant).
 */
function toApiError(status: number, problem: ProblemDetails, sentToken: boolean): ApiError {
  if (status >= 500) return new ApiError('SERVER_ERROR', SERVER_ERROR_MESSAGE, status);
  if (status === 401 && sentToken) return new ApiError('UNAUTHORIZED', SESSION_EXPIRED_MESSAGE, status);
  if (status === 403) return new ApiError('FORBIDDEN', DEFAULT_MESSAGES[403], status);

  const code = KNOWN_CODES.find((known) => known === problem.code) ?? defaultCodeFor(status);
  // Pour une validation, le premier message par champ est plus précis que le détail générique.
  const firstFieldError = problem.errors ? Object.values(problem.errors)[0]?.[0] : undefined;
  const message =
    firstFieldError ?? problem.detail ?? problem.title ?? DEFAULT_MESSAGES[status] ?? `Erreur inattendue (${status}).`;

  return new ApiError(code, message, status, problem.errors);
}

async function request<T>(method: HttpMethod, path: string, { body, auth = true }: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (body !== undefined) headers['Content-Type'] = 'application/json';

  const token = auth ? await tokenStorage.getToken() : null;
  if (token) headers.Authorization = `Bearer ${token}`;

  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), API_TIMEOUT_MS);

  let response: Response;
  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
      signal: controller.signal,
    });
  } catch {
    if (controller.signal.aborted) {
      throw new ApiError('TIMEOUT', 'Le serveur ne répond pas (délai dépassé). Réessayez plus tard.');
    }
    throw new ApiError('NETWORK_ERROR', 'Impossible de joindre le serveur. Vérifiez votre connexion réseau.');
  } finally {
    clearTimeout(timeout);
  }

  if (!response.ok) {
    const problem = parseProblemDetails(await response.json().catch(() => null));
    const error = toApiError(response.status, problem, token !== null);
    if (error.code === 'UNAUTHORIZED' && token !== null) unauthorizedHandler?.();
    throw error;
  }

  if (response.status === 204) return undefined as T;
  try {
    return (await response.json()) as T;
  } catch {
    throw new ApiError('UNKNOWN', 'Réponse du serveur illisible.', response.status);
  }
}

export const httpClient = {
  get: <T>(path: string, options?: Omit<RequestOptions, 'body'>) => request<T>('GET', path, options),
  post: <T>(path: string, body?: unknown, options?: Omit<RequestOptions, 'body'>) =>
    request<T>('POST', path, { ...options, body }),
  patch: <T>(path: string, body?: unknown, options?: Omit<RequestOptions, 'body'>) =>
    request<T>('PATCH', path, { ...options, body }),
};
