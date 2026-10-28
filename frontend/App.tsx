
import { useEffect, useState } from 'react';

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

  // 1. État de l'initialisation SQLite
  const [databaseReady, setDatabaseReady] = useState(false);

  // 2. Initialiser SQLite une seule fois au démarrage
  useEffect(() => {
    initDatabase()
      .then(() => {
        console.log('Base SQLite prête');
        setDatabaseReady(true);
      })
      .catch((error) => {
        console.error('Erreur initialisation SQLite :', error);
      });
  }, []);

  // 3. Chargement des polices
  const [fontsLoaded] = useFonts({
    SpaceGrotesk_500Medium,
    SpaceGrotesk_600SemiBold,
    SpaceGrotesk_700Bold,
    IBMPlexSans_400Regular,
    IBMPlexSans_400Regular_Italic,
    IBMPlexSans_500Medium,
    IBMPlexSans_600SemiBold,
  });

  // 4. Attendre SQLite et les polices
  if (!fontsLoaded || !databaseReady) {
    return <FullScreenLoader />;
  }

  // 5. Afficher l'application
  return (
    <SafeAreaProvider>
      <AuthProvider>
        <StatusBar style="dark" />
        <RootNavigator />
      </AuthProvider>
    </SafeAreaProvider>
  );
}
