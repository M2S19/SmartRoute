export type UserRole = 'RESPONSABLE_LOGISTIQUE' | 'LIVREUR';

export interface User {
  id: string;
  email: string;
  fullName: string;
  role: UserRole;
  /** Identifiant affiché pour un livreur, ex. « 4092 ». */
  driverCode?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  /** JWT renvoyé par POST /api/auth/login. */
  token: string;
  user: User;
}
