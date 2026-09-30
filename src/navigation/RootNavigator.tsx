import { DefaultTheme, NavigationContainer, Theme } from '@react-navigation/native';
import { FullScreenLoader } from '../components/common';
import { colors } from '../constants/theme';
import { useAuth } from '../hooks/useAuth';
import { AuthNavigator } from './AuthNavigator';
import { DriverNavigator } from './DriverNavigator';
import { ManagerNavigator } from './ManagerNavigator';

const navigationTheme: Theme = {
  ...DefaultTheme,
  colors: { ...DefaultTheme.colors, background: colors.surface, primary: colors.primary },
};

/**
 * Choisit la pile d'écrans selon l'utilisateur connecté.
 * Pas de navigate() après login/logout : le changement de `user` suffit.
 * Pendant la relecture de la session, on n'affiche pas le Login (pas de flash).
 */
export function RootNavigator() {
  const { user, isRestoring } = useAuth();

  if (isRestoring) return <FullScreenLoader />;

  return (
    <NavigationContainer theme={navigationTheme}>
      {!user ? (
        <AuthNavigator />
      ) : user.role === 'RESPONSABLE_LOGISTIQUE' ? (
        <ManagerNavigator />
      ) : (
        <DriverNavigator />
      )}
    </NavigationContainer>
  );
}
