import { useContext } from 'react';
import { AuthContext, AuthContextValue } from '../context/AuthContext';

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth doit être utilisé dans un AuthProvider.');
  }
  return context;
}
