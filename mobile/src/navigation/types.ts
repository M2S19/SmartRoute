import type { NativeStackScreenProps } from '@react-navigation/native-stack';

export type AuthStackParamList = {
  Login: undefined;
};

export type ManagerStackParamList = {
  ManagerDashboard: undefined;
  OptimizeRoute: undefined;
};

export type DriverStackParamList = {
  MyRoute: undefined;
};

export type ManagerScreenProps<T extends keyof ManagerStackParamList> = NativeStackScreenProps<ManagerStackParamList, T>;
export type DriverScreenProps<T extends keyof DriverStackParamList> = NativeStackScreenProps<DriverStackParamList, T>;
