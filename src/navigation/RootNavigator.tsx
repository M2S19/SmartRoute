import { DefaultTheme, NavigationContainer, Theme } from '@react-navigation/native';
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
 */
export function RootNavigator() {
  const { user } = useAuth();

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
