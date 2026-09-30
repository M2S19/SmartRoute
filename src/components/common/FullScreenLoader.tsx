import { ActivityIndicator, StyleSheet, View } from 'react-native';
import { colors } from '../../constants/theme';

/** Écran d'attente neutre (chargement des polices, restauration de session). */
export function FullScreenLoader() {
  return (
    <View style={styles.container} accessibilityRole="progressbar" accessibilityLabel="Chargement">
      <ActivityIndicator color={colors.primary} />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.surface,
  },
});
