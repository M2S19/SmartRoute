export type ApiErrorCode =
  | 'INVALID_CREDENTIALS'
  | 'VALIDATION_ERROR'
  | 'UNAUTHORIZED'
  | 'FORBIDDEN'
  | 'OPTIMIZATION_FAILED'
  | 'ASSIGNMENT_FAILED'
  | 'NOT_FOUND'
  | 'CONFLICT'
  // Affectation d'une livraison (US2)
  | 'NO_ELIGIBLE_ROUTE'
  | 'ROUTE_NOT_ELIGIBLE'
  | 'PROPOSAL_OUTDATED'
  // Tournée du livreur (US3)
  | 'ROUTE_NOT_MODIFIABLE'
  | 'STOP_ALREADY_DELIVERED'
  | 'STOP_NOT_NEXT'
  | 'SERVER_ERROR'
  | 'TIMEOUT'
  | 'NETWORK_ERROR'
  | 'UNKNOWN';

/** Erreurs de validation par champ (ProblemDetails « errors »), ex. { email: ["L'email n'est pas valide."] }. */
export type FieldErrors = Record<string, string[]>;

/** Erreur normalisée levée par tous les services (mock ou HTTP). Le message est toujours affichable. */
export class ApiError extends Error {
  readonly code: ApiErrorCode;
  readonly status?: number;
  readonly fieldErrors?: FieldErrors;

  constructor(code: ApiErrorCode, message: string, status?: number, fieldErrors?: FieldErrors) {
    super(message);
    this.name = 'ApiError';
    this.code = code;
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}

export function toErrorCode(error: unknown): ApiErrorCode {
  return error instanceof ApiError ? error.code : 'UNKNOWN';
}

export function toErrorMessage(error: unknown): string {
  if (error instanceof ApiError) return error.message;
  if (error instanceof Error) return error.message;
  return 'Une erreur inattendue est survenue.';
}
