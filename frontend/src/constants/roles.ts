import type { UserRole } from '../types';

/** Libellé court affiché dans l'en-tête (« SmartRoute • Responsable »). */
export const ROLE_SHORT_LABELS: Record<UserRole, string> = {
  RESPONSABLE_LOGISTIQUE: 'Responsable',
  LIVREUR: 'Livreur',
};
