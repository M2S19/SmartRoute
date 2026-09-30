import { StyleSheet, Text, View } from 'react-native';
import { colors, sizes, typography } from '../../constants/theme';

interface StepIndexProps {
  value: number;
  tone?: 'default' | 'active' | 'muted';
}

/** Carré numéroté de l'ordre de passage (maquette driver-route). */
export function StepIndex({ value, tone = 'default' }: StepIndexProps) {
  return (
    <View style={[styles.box, tone === 'active' && styles.boxActive]}>
      <Text style={[styles.text, tone === 'active' && styles.textActive, tone === 'muted' && styles.textMuted]}>
        {value}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  box: {
    width: sizes.stepIndex,
    height: sizes.stepIndex,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.surfaceContainer,
  },
  boxActive: {
    backgroundColor: colors.primaryContainer,
  },
  text: {
    ...typography.headlineSm,
    color: colors.onSurface,
  },
  textActive: {
    color: colors.onPrimary,
  },
  textMuted: {
    color: colors.secondary,
  },
});
