/** Copie profonde d'une donnée sérialisable en JSON (données mock). */
export function deepClone<T>(value: T): T {
  return JSON.parse(JSON.stringify(value)) as T;
}
