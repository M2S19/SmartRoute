import type { User } from '../types';

export const mockManager: User = {
  id: 'u-resp-1',
  email: 'responsable@smartroute.fr',
  fullName: 'Claire Martin',
  role: 'RESPONSABLE_LOGISTIQUE',
};

export const mockDriver: User = {
  id: 'u-liv-4092',
  email: 'livreur@smartroute.fr',
  fullName: 'Pierre Dubois',
  role: 'LIVREUR',
  driverCode: '4092',
};

export const mockOtherDrivers: User[] = [
  {
    id: 'u-liv-4107',
    email: 'sophie.laurent@smartroute.fr',
    fullName: 'Sophie Laurent',
    role: 'LIVREUR',
    driverCode: '4107',
  },
  {
    id: 'u-liv-4121',
    email: 'karim.benali@smartroute.fr',
    fullName: 'Karim Benali',
    role: 'LIVREUR',
    driverCode: '4121',
  },
];

/** Comptes acceptés par l'authentification mock (email en minuscules). */
export const mockAccounts: Record<string, User> = {
  [mockManager.email]: mockManager,
  [mockDriver.email]: mockDriver,
};
