import type { OptimizeRouteResult } from '../../types';
import { formatDistance, formatPercent } from '../../utils/format';
import { ResultPanel } from '../common';

interface OptimizationSummaryProps {
  result: OptimizeRouteResult;
}

export function OptimizationSummary({ result }: OptimizationSummaryProps) {
  return (
    <ResultPanel
      rows={[
        { label: 'Distance avant optimisation :', value: formatDistance(result.distanceBeforeKm) },
        { label: 'Distance après optimisation :', value: formatDistance(result.distanceAfterKm) },
        {
          label: "Gain d'optimisation :",
          value: `−${formatDistance(result.gainKm)} (${formatPercent(result.gainPercent)})`,
          tone: 'success',
        },
      ]}
    />
  );
}
