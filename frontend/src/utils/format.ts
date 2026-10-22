export function roundTo(value: number, decimals: number): number {
  const factor = 10 ** decimals;
  return Math.round(value * factor) / factor;
}

/** 2.4 → « 2,4 km » ; avec signe : « +2,4 km ». */
export function formatDistance(km: number, withSign = false): string {
  const sign = withSign && km > 0 ? '+' : '';
  return `${sign}${km.toFixed(1).replace('.', ',')} km`;
}

export function formatPercent(value: number): string {
  return `${Math.round(value)} %`;
}

/** Date ISO → « 08:42 » (heure locale). */
export function formatTime(iso: string): string {
  const date = new Date(iso);
  const hh = String(date.getHours()).padStart(2, '0');
  const mm = String(date.getMinutes()).padStart(2, '0');
  return `${hh}:${mm}`;
}

/** Jour local au format yyyy-MM-dd (même format que le champ « date » d'une tournée). */
export function todayIsoDate(now: Date = new Date()): string {
  const mm = String(now.getMonth() + 1).padStart(2, '0');
  const dd = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${mm}-${dd}`;
}

/** « 2026-10-18 » → « 18/10/2026 », sans conversion de fuseau horaire. */
export function formatDay(isoDate: string): string {
  const [yyyy, mm, dd] = isoDate.slice(0, 10).split('-');
  return yyyy && mm && dd ? `${dd}/${mm}/${yyyy}` : isoDate;
}
