import { Pressable, StyleSheet, View } from 'react-native';
import { colors, sizes, spacing } from '../../constants/theme';
import { Button, Icon, TextField } from '../common';
import { StepIndex } from './StepIndex';

export interface AddressRow {
  id: string;
  label: string;
}

interface AddressListEditorProps {
  rows: AddressRow[];
  onChange: (id: string, label: string) => void;
  onAdd: () => void;
  onRemove: (id: string) => void;
  /** Nombre minimal de lignes conservées (bouton supprimer désactivé en dessous). */
  minRows?: number;
  disabled?: boolean;
  placeholder?: string;
}

export function AddressListEditor({
  rows,
  onChange,
  onAdd,
  onRemove,
  minRows = 1,
  disabled = false,
  placeholder,
}: AddressListEditorProps) {
  const canRemove = rows.length > minRows && !disabled;

  return (
    <View style={styles.container}>
      {rows.map((row, index) => (
        <View key={row.id} style={styles.row}>
          <StepIndex value={index + 1} />
          <View style={styles.field}>
            <TextField
              value={row.label}
              onChangeText={(text) => onChange(row.id, text)}
              placeholder={placeholder}
              editable={!disabled}
              accessibilityLabel={`Adresse de livraison ${index + 1}`}
            />
          </View>
          <Pressable
            onPress={() => onRemove(row.id)}
            disabled={!canRemove}
            hitSlop={6}
            accessibilityRole="button"
            accessibilityLabel={`Supprimer la livraison ${index + 1}`}
            style={({ pressed }) => [styles.remove, pressed && styles.removePressed, !canRemove && styles.removeDisabled]}
          >
            <Icon name="delete-outline" size={sizes.iconMd} color={colors.secondary} />
          </Pressable>
        </View>
      ))}
      <Button label="Ajouter une livraison" icon="add" variant="secondary" onPress={onAdd} disabled={disabled} />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: spacing.sm,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
  },
  field: {
    flex: 1,
  },
  remove: {
    width: sizes.controlHeight,
    height: sizes.controlHeight,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: colors.borderSubtle,
    backgroundColor: colors.surfaceContainerLowest,
  },
  removePressed: {
    backgroundColor: colors.surfaceContainer,
  },
  removeDisabled: {
    opacity: 0.4,
  },
});
