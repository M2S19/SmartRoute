import { API_BASE_URL, API_TIMEOUT_MS } from '../../config/api';
import { tokenStorage } from '../../storage/tokenStorage';
import { ApiError } from '../../types';

type HttpMethod = 'GET' | 'POST' | 'PATCH';

interface RequestOptions {
  body?: unknown;
  /** Ajoute l'en-tête Authorization: Bearer <JWT>. Vrai par défaut. */
  auth?: boolean;
}

interface ErrorBody {
  message?: string;
  title?: string; // ProblemDetails ASP.NET Core
}

async function request<T>(method: HttpMethod, path: string, { body, auth = true }: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (body !== undefined) headers['Content-Type'] = 'application/json';

  if (auth) {
    const token = await tokenStorage.getToken();
    if (token) headers.Authorization = `Bearer ${token}`;
  }

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
    throw new ApiError('NETWORK_ERROR', 'Impossible de joindre le serveur.');
  } finally {
    clearTimeout(timeout);
  }

  if (!response.ok) {
    const errorBody = (await response.json().catch(() => ({}))) as ErrorBody;
    const message = errorBody.message ?? errorBody.title ?? `Erreur serveur (${response.status}).`;
    throw new ApiError(response.status === 401 ? 'INVALID_CREDENTIALS' : 'UNKNOWN', message, response.status);
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

export const httpClient = {
  get: <T>(path: string, options?: Omit<RequestOptions, 'body'>) => request<T>('GET', path, options),
  post: <T>(path: string, body?: unknown, options?: Omit<RequestOptions, 'body'>) =>
    request<T>('POST', path, { ...options, body }),
  patch: <T>(path: string, body?: unknown, options?: Omit<RequestOptions, 'body'>) =>
    request<T>('PATCH', path, { ...options, body }),
};
