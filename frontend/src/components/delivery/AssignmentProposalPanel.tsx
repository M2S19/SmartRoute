import type { AssignmentProposal } from '../../types';
import { formatDistance } from '../../utils/format';
import { ResultPanel } from '../common';

interface AssignmentProposalPanelProps {
  proposal: AssignmentProposal;
}

/** Proposition d'affectation : uniquement des données renvoyées par l'API (plus l'adresse saisie). */
export function AssignmentProposalPanel({ proposal }: AssignmentProposalPanelProps) {
  const { driver } = proposal;
  return (
    <ResultPanel
      rows={[
        { label: 'Livraison :', value: proposal.address.label },
        { label: 'Tournée :', value: proposal.routeName },
        {
          label: 'Livreur sélectionné :',
          value: driver.driverCode ? `${driver.fullName} (#${driver.driverCode})` : driver.fullName,
        },
        { label: "Position d'insertion :", value: `Arrêt n° ${proposal.insertionPosition}` },
        { label: 'Distance supplémentaire :', value: formatDistance(proposal.extraDistanceKm, true), tone: 'success' },
      ]}
    />
  );
}
