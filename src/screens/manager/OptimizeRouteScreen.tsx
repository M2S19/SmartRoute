import { useRef, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import {
  AppFooter,
  AppHeader,
  BackLink,
  Badge,
  Button,
  Card,
  Icon,
  Notice,
  ScreenContainer,
  ScreenTitle,
  TextField,
} from '../../components/common';
import {
  AddressListEditor,
  AddressRow,
  OptimizationSummary,
  SectionLabel,
  StopCard,
} from '../../components/route';
import { colors, sizes, spacing, typography } from '../../constants/theme';
import { useAsync } from '../../hooks/useAsync';
import { useAuth } from '../../hooks/useAuth';
import type { ManagerScreenProps } from '../../navigation/types';
import { routeService } from '../../services';
import type { OptimizeRouteResult } from '../../types';
import { isNonEmpty } from '../../utils/validation';

const MIN_DELIVERIES = 2;

interface FormErrors {
  depot?: string;
  deliveries?: string;
}

export function OptimizeRouteScreen({ navigation }: ManagerScreenProps<'OptimizeRoute'>) {
  const { logout } = useAuth();
  const nextRowId = useRef(0);
  const createRow = (): AddressRow => ({ id: `row-${nextRowId.current++}`, label: '' });

  const [name, setName] = useState('');
  const [depot, setDepot] = useState('');
  const [rows, setRows] = useState<AddressRow[]>(() => [createRow(), createRow()]);
  const [errors, setErrors] = useState<FormErrors>({});
  const optimization = useAsync(routeService.optimize);

  const handleOptimize = () => {
    const nextErrors: FormErrors = {};
    if (!isNonEmpty(depot)) nextErrors.depot = "Saisissez l'adresse du dépôt.";
    if (rows.some((r) => !isNonEmpty(r.label))) {
      nextErrors.deliveries = 'Complétez ou supprimez les adresses vides.';
    } else if (rows.length < MIN_DELIVERIES) {
      nextErrors.deliveries = `Ajoutez au moins ${MIN_DELIVERIES} livraisons.`;
    }
    setErrors(nextErrors);
    if (nextErrors.depot || nextErrors.deliveries) return;

    void optimization.run({
      name: name.trim() || undefined,
      depot: { label: depot.trim() },
      deliveries: rows.map((r) => ({ label: r.label.trim() })),
    });
  };

  // « Modifier » : retour au formulaire, saisies conservées.
  const handleEdit = () => optimization.reset();

  // « Nouvelle tournée » : formulaire vierge.
  const handleNew = () => {
    setName('');
    setDepot('');
    setRows([createRow(), createRow()]);
    setErrors({});
    optimization.reset();
  };

  const header = <AppHeader role="RESPONSABLE_LOGISTIQUE" onLogout={logout} />;

  return (
    <ScreenContainer header={header}>
      <BackLink label="Dashboard" onPress={() => navigation.goBack()} />
      <ScreenTitle
        title="Optimiser une tournée"
        subtitle="Calculer automatiquement l'ordre optimal des livraisons afin de réduire la distance totale parcourue."
      />

      {optimization.state.status === 'success' ? (
        <OptimizationResultView result={optimization.state.data} onEdit={handleEdit} onNew={handleNew} />
      ) : (
        <View style={styles.sections}>
          <Card style={styles.section}>
            <Text style={styles.sectionTitle}>Tournée</Text>
            <TextField
              label="Nom de la tournée"
              value={name}
              onChangeText={setName}
              placeholder="ex. Tournée Mulhouse matin"
              editable={!optimization.isLoading}
            />
            <TextField
              label="Dépôt"
              value={depot}
              onChangeText={setDepot}
              placeholder="ex. 10 rue de l'Industrie, Mulhouse"
              editable={!optimization.isLoading}
              error={errors.depot}
            />
          </Card>

          <Card style={styles.section}>
            <View style={styles.sectionHeader}>
              <Text style={styles.sectionTitle}>Livraisons</Text>
              <Badge label={`${rows.length} adresse${rows.length > 1 ? 's' : ''}`} tone="neutral" />
            </View>
            <AddressListEditor
              rows={rows}
              onChange={(id, label) => setRows((prev) => prev.map((r) => (r.id === id ? { ...r, label } : r)))}
              onAdd={() => setRows((prev) => [...prev, createRow()])}
              onRemove={(id) => setRows((prev) => prev.filter((r) => r.id !== id))}
              placeholder="ex. 12 rue de Bâle, Mulhouse"
              disabled={optimization.isLoading}
            />
            {errors.deliveries ? <Text style={styles.fieldError}>{errors.deliveries}</Text> : null}
          </Card>

          {optimization.state.status === 'error' ? (
            <Notice tone="error" message={optimization.state.error} actionLabel="Réessayer" onAction={handleOptimize} />
          ) : null}

          <Button
            label={optimization.isLoading ? 'Optimisation en cours…' : 'Optimiser la tournée'}
            onPress={handleOptimize}
            loading={optimization.isLoading}
          />
        </View>
      )}

      <AppFooter role="RESPONSABLE_LOGISTIQUE" />
    </ScreenContainer>
  );
}

interface OptimizationResultViewProps {
  result: OptimizeRouteResult;
  onEdit: () => void;
  onNew: () => void;
}

function OptimizationResultView({ result, onEdit, onNew }: OptimizationResultViewProps) {
  return (
    <View style={styles.sections}>
      <Card style={styles.section}>
        <View style={styles.sectionHeader}>
          <Text style={styles.sectionTitle}>{result.routeName}</Text>
          <Badge label="Optimisée" tone="primary" />
        </View>
        <OptimizationSummary result={result} />
      </Card>

      <View style={styles.stops}>
        <SectionLabel title={`Ordre de passage (${result.orderedStops.length} arrêts)`} />
        <View style={styles.depot}>
          <Icon name="warehouse" size={sizes.iconSm} color={colors.secondary} />
          <Text style={styles.depotText}>
            Départ : <Text style={styles.depotAddress}>{result.depot.address.label}</Text>
          </Text>
        </View>
        {result.orderedStops.map((stop) => (
          <StopCard key={stop.id} stop={stop} variant="plain" />
        ))}
      </View>

      <View style={styles.actions}>
        <Button label="Modifier" icon="edit" variant="secondary" onPress={onEdit} style={styles.action} />
        <Button label="Nouvelle tournée" icon="add" variant="outline" onPress={onNew} style={styles.action} />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  sections: {
    gap: spacing.lg,
  },
  section: {
    gap: spacing.gutter,
  },
  sectionHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.md,
  },
  sectionTitle: {
    ...typography.headlineSm,
    color: colors.onSurface,
    flexShrink: 1,
  },
  fieldError: {
    ...typography.bodySm,
    color: colors.error,
  },
  stops: {
    gap: spacing.sm,
  },
  depot: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    paddingHorizontal: spacing.xs,
  },
  depotText: {
    ...typography.bodySm,
    color: colors.secondary,
    flex: 1,
  },
  depotAddress: {
    color: colors.onSurface,
  },
  actions: {
    flexDirection: 'row',
    gap: spacing.sm,
  },
  action: {
    flex: 1,
    paddingHorizontal: spacing.sm,
  },
});
