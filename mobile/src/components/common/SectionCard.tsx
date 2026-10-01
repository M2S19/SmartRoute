import { ReactNode } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { colors, spacing, typography } from '../../constants/theme';
import { Badge } from './Badge';
import { Card } from './Card';

interface SectionCardProps {
  title: string;
  /** Étiquette à droite du titre, ex. « Fonction 1 ». */
  tag?: string;
  description?: string;
  children?: ReactNode;
}

/** Carte titrée du Dashboard (« Optimiser une tournée », « Affecter une nouvelle livraison »). */
export function SectionCard({ title, tag, description, children }: SectionCardProps) {
  return (
    <Card style={styles.card}>
      <View style={styles.header}>
        <Text style={styles.title}>{title}</Text>
        {tag ? <Badge label={tag} tone="neutral" /> : null}
      </View>
      {description ? <Text style={styles.description}>{description}</Text> : null}
      {children}
    </Card>
  );
}

const styles = StyleSheet.create({
  card: {
    gap: spacing.gutter,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    justifyContent: 'space-between',
    gap: spacing.md,
  },
  title: {
    ...typography.headlineSm,
    color: colors.onSurface,
    flexShrink: 1,
  },
  description: {
    ...typography.bodySm,
    lineHeight: 20,
    color: colors.onSurfaceVariant,
    marginTop: -spacing.sm,
  },
});
