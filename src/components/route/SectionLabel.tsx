import { StyleSheet, Text, View } from 'react-native';
import { colors, spacing, typography } from '../../constants/theme';

interface SectionLabelProps {
  title: string;
  /** Texte aligné à droite, ex. « Progression : 1/4 ». */
  aside?: string;
}

/** Intitulé de liste (« Itinéraire séquentiel (4 étapes) »). */
export function SectionLabel({ title, aside }: SectionLabelProps) {
  return (
    <View style={styles.container}>
      <Text style={styles.title}>{title.toUpperCase()}</Text>
      {aside ? <Text style={styles.aside}>{aside}</Text> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: spacing.xs,
    marginBottom: spacing.xs,
  },
  title: {
    ...typography.labelMd,
    color: colors.secondary,
  },
  aside: {
    ...typography.bodySm,
    color: colors.secondary,
  },
});
