export const MOCK_DELAY_MS = 800;

/** Simule la latence réseau d'un appel API. */
export function simulateDelay(ms: number = MOCK_DELAY_MS): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

/** Mot-clé déclenchant une erreur simulée dans les mocks (optimisation, affectation). */
export const MOCK_ERROR_KEYWORD = 'erreur';

export function containsErrorKeyword(value: string): boolean {
  return value.toLowerCase().includes(MOCK_ERROR_KEYWORD);
}
