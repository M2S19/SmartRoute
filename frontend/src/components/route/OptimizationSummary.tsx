import type { OptimizeRouteResult } from '../../types';
import { formatDistance, formatPercent } from '../../utils/format';
import { ResultPanel } from '../common';

interface OptimizationSummaryProps {
  result: OptimizeRouteResult;
}

/** « −1,2 km (21 %) » quand la tournée raccourcit ; « +0,3 km (−5 %) » si elle s'allonge. */
function formatGain(gainKm: number, gainPercent: number): string {
  const sign = gainKm >= 0 ? '−' : '+';
  return `${sign}${formatDistance(Math.abs(gainKm))} (${formatPercent(gainPercent)})`;
}

export function OptimizationSummary({ result }: OptimizationSummaryProps) {
  return (
    <ResultPanel
      rows={[
        { label: 'Distance avant optimisation :', value: formatDistance(result.distanceBeforeKm) },
        { label: 'Distance après optimisation :', value: formatDistance(result.distanceAfterKm) },
        {
          label: "Gain d'optimisation :",
          value: formatGain(result.gainKm, result.gainPercent),
          tone: result.gainKm >= 0 ? 'success' : 'default',
        },
      ]}
    />
  );
}
