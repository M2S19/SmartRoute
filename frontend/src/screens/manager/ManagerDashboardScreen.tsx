import { useEffect, useState } from 'react';
import { StyleSheet, View } from 'react-native';
import {
  AppFooter,
  AppHeader,
  Button,
  Notice,
  ScreenContainer,
  ScreenTitle,
  SectionCard,
  TextField,
} from '../../components/common';
import { AssignmentProposalPanel } from '../../components/delivery/AssignmentProposalPanel';
import { spacing } from '../../constants/theme';
import { useAsync } from '../../hooks/useAsync';
import { useAuth } from '../../hooks/useAuth';
import type { ManagerScreenProps } from '../../navigation/types';
import { deliveryService } from '../../services';
import { isNonEmpty } from '../../utils/validation';

export function ManagerDashboardScreen({ navigation }: ManagerScreenProps<'ManagerDashboard'>) {
  const { logout } = useAuth();
  const [address, setAddress] = useState('');
  const search = useAsync(deliveryService.assignBestDriver);
  const confirm = useAsync(deliveryService.confirmAssignment);

  const proposal = search.state.status === 'success' ? search.state.data : null;
  const isBusy = search.isLoading || confirm.isLoading;
  const resetSearch = search.reset;

  // Tournée modifiée ou plus éligible depuis la proposition : celle-ci n'est plus confirmable,
  // on la retire ; le message d'erreur reste affiché et invite à relancer la recherche.
  const confirmErrorCode = confirm.state.status === 'error' ? confirm.state.code : null;
  useEffect(() => {
    if (confirmErrorCode === 'PROPOSAL_OUTDATED' || confirmErrorCode === 'ROUTE_NOT_ELIGIBLE') resetSearch();
  }, [confirmErrorCode, resetSearch]);

  const handleAddressChange = (text: string) => {
    setAddress(text);
    // Une proposition ne vaut que pour l'adresse qui l'a produite.
    if (search.state.status !== 'idle') search.reset();
    if (confirm.state.status !== 'idle') confirm.reset();
  };

  const handleSearch = () => {
    if (isBusy) return;
    confirm.reset();
    void search.run({ address: { label: address.trim() } });
  };

  const handleConfirm = async () => {
    // Une seule confirmation à la fois (le bouton est aussi désactivé pendant la requête).
    if (!proposal || confirm.isLoading) return;
    const confirmation = await confirm.run(proposal);
    if (confirmation) {
      setAddress('');
      search.reset();
    }
  };

  return (
    <ScreenContainer header={<AppHeader role="RESPONSABLE_LOGISTIQUE" onLogout={logout} />}>
      <ScreenTitle title="Dashboard Responsable" subtitle="Gestion opérationnelle des tournées et dispatching." />

      <View style={styles.cards}>
        <SectionCard
          title="Optimiser une tournée"
          tag="Fonction 1"
          description="Calculer automatiquement l'ordre optimal des livraisons afin de réduire la distance totale parcourue."
        >
          <Button label="Optimiser une tournée" onPress={() => navigation.navigate('OptimizeRoute')} />
        </SectionCard>

        <SectionCard
          title="Affecter une nouvelle livraison"
          tag="Fonction 2"
          description="Affectation automatique au livreur ayant le plus faible surcoût kilométrique."
        >
          <TextField
            label="Adresse de livraison"
            value={address}
            onChangeText={handleAddressChange}
            placeholder="ex. 14 rue de l'Industrie, Strasbourg"
            editable={!isBusy}
            returnKeyType="search"
            onSubmitEditing={() => isNonEmpty(address) && handleSearch()}
          />

          <Button
            label="Chercher le meilleur livreur"
            variant="outline"
            onPress={handleSearch}
            loading={search.isLoading}
            disabled={!isNonEmpty(address) || confirm.isLoading}
          />

          {search.state.status === 'error' ? (
            <Notice tone="error" message={search.state.error} actionLabel="Réessayer" onAction={handleSearch} />
          ) : null}

          {proposal ? (
            <>
              <AssignmentProposalPanel proposal={proposal} />
              <Button label="Confirmer l'affectation" onPress={handleConfirm} loading={confirm.isLoading} />
            </>
          ) : null}

          {confirm.state.status === 'error' ? <Notice tone="error" message={confirm.state.error} /> : null}

          {/* Affiché seulement après la réponse de succès de l'API (201). */}
          {confirm.state.status === 'success' ? (
            <Notice
              tone="success"
              message={`Affectation enregistrée : livraison ajoutée à « ${confirm.state.data.routeName} » (arrêt n° ${confirm.state.data.insertionPosition}), livreur ${confirm.state.data.driver.fullName}.`}
            />
          ) : null}
        </SectionCard>
      </View>

      <AppFooter role="RESPONSABLE_LOGISTIQUE" />
    </ScreenContainer>
  );
}

const styles = StyleSheet.create({
  cards: {
    gap: spacing.margin,
  },
});
