import type { AssignmentProposal } from '../../types';
import { formatDistance } from '../../utils/format';
import { ResultPanel } from '../common';

interface AssignmentProposalPanelProps {
  proposal: AssignmentProposal;
}

export function AssignmentProposalPanel({ proposal }: AssignmentProposalPanelProps) {
  return (
    <ResultPanel
      rows={[
        { label: 'Livraison :', value: `Colis #${proposal.delivery.parcelRef}` },
        { label: 'Tournée :', value: proposal.routeName },
        { label: 'Livreur sélectionné :', value: proposal.driver.fullName },
        { label: "Position d'insertion :", value: `Arrêt n° ${proposal.insertionPosition}` },
        { label: 'Distance supplémentaire :', value: formatDistance(proposal.extraDistanceKm, true), tone: 'success' },
      ]}
    />
  );
}
