import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { ManagerDashboardScreen } from '../screens/manager/ManagerDashboardScreen';
import { OptimizeRouteScreen } from '../screens/manager/OptimizeRouteScreen';
import type { ManagerStackParamList } from './types';

const Stack = createNativeStackNavigator<ManagerStackParamList>();

export function ManagerNavigator() {
  return (
    <Stack.Navigator screenOptions={{ headerShown: false }}>
      <Stack.Screen name="ManagerDashboard" component={ManagerDashboardScreen} />
      <Stack.Screen name="OptimizeRoute" component={OptimizeRouteScreen} />
    </Stack.Navigator>
  );
}
