import { useEffect } from 'react';
import { useContext } from 'react';

import { AuthContext } from '../context/AuthContext';
import { testDatabase } from './testDatabase';

export function DatabaseTestRunner() {
  const auth = useContext(AuthContext);

  const user = auth?.user;
  const isRestoring = auth?.isRestoring;

  useEffect(() => {
    if (isRestoring || !user) return;

    // Test temporaire réservé aux livreurs.
    if (user.role !== 'LIVREUR') return;

    testDatabase(user.id);
  }, [user?.id, user?.role, isRestoring]);

  return null;
}