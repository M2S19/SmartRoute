import { StyleSheet, Text, View } from 'react-native';
import { colors, fonts, spacing, typography } from '../../constants/theme';

export interface ResultRow {
  label: string;
  value: string;
  /** "success" : valeur verte (ex. « +2,4 km »). */
  tone?: 'default' | 'success';
}

interface ResultPanelProps {
  rows: ResultRow[];
}

/** Encart de résultat calculé par le backend (maquette Dashboard). */
export function ResultPanel({ rows }: ResultPanelProps) {
  return (
    <View style={styles.panel}>
      {rows.map((row) => (
        <View key={row.label} style={styles.row}>
          <Text style={styles.label}>{row.label}</Text>
          <Text style={[styles.value, row.tone === 'success' && styles.valueSuccess]}>{row.value}</Text>
        </View>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  panel: {
    backgroundColor: colors.surfaceContainerLow,
    borderWidth: 1,
    borderColor: colors.surfaceContainerHigh,
    padding: 14,
    gap: 6,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'baseline',
    justifyContent: 'space-between',
    gap: spacing.md,
  },
  label: {
    ...typography.bodySm,
    fontFamily: fonts.plex500,
    color: colors.onSurfaceVariant,
  },
  value: {
    ...typography.bodySm,
    fontFamily: fonts.plex600,
    color: colors.onSurface,
    flexShrink: 1,
    textAlign: 'right',
  },
  valueSuccess: {
    color: colors.success,
    fontVariant: ['tabular-nums'],
  },
});
