import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { MyRouteScreen } from '../screens/driver/MyRouteScreen';
import type { DriverStackParamList } from './types';

const Stack = createNativeStackNavigator<DriverStackParamList>();

export function DriverNavigator() {
  return (
    <Stack.Navigator screenOptions={{ headerShown: false }}>
      <Stack.Screen name="MyRoute" component={MyRouteScreen} />
    </Stack.Navigator>
  );
}
