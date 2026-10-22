import { useEffect, useState } from 'react';
import { DatabaseTestRunner } from './src/database/DatabaseTestRunner';
import {
  IBMPlexSans_400Regular,
  IBMPlexSans_400Regular_Italic,
  IBMPlexSans_500Medium,
  IBMPlexSans_600SemiBold,
} from '@expo-google-fonts/ibm-plex-sans';

import {
  SpaceGrotesk_500Medium,
  SpaceGrotesk_600SemiBold,
  SpaceGrotesk_700Bold,
} from '@expo-google-fonts/space-grotesk';

import { useFonts } from 'expo-font';
import { StatusBar } from 'expo-status-bar';
import { SafeAreaProvider } from 'react-native-safe-area-context';

import { FullScreenLoader } from './src/components/common';
import { AuthProvider } from './src/context/AuthContext';
import { RootNavigator } from './src/navigation/RootNavigator';

import { initDatabase } from './src/database/database';

export default function App() {

  const [databaseReady, setDatabaseReady] = useState(false);

  useEffect(() => {
    initDatabase()
      .then(() => setDatabaseReady(true))
      .catch((error) => {
        console.error('Erreur initialisation SQLite :', error);
      });
  }, []);
  // Initialisation SQLite au démarrage
  useEffect(() => {
    initDatabase()
      .then(() => {
        console.log('SQLite SmartRoute initialisée');
      })
      .catch((error) => {
        console.error('Erreur initialisation SQLite :', error);
      });
  }, []);

  
  // Chargement des polices
  const [fontsLoaded] = useFonts({
    SpaceGrotesk_500Medium,
    SpaceGrotesk_600SemiBold,
    SpaceGrotesk_700Bold,
    IBMPlexSans_400Regular,
    IBMPlexSans_400Regular_Italic,
    IBMPlexSans_500Medium,
    IBMPlexSans_600SemiBold,
  });

  if (!fontsLoaded || !databaseReady) {
    return <FullScreenLoader />;
  }

  return (
    <SafeAreaProvider>
      <AuthProvider>
        <DatabaseTestRunner />
        <StatusBar style="dark" />
        <RootNavigator />
      </AuthProvider>
    </SafeAreaProvider>
  );
}