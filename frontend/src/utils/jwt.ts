/**
 * Lit la date d'expiration (claim « exp ») d'un JWT, sans vérifier sa signature :
 * c'est l'API qui fait foi, ceci sert seulement à ne pas restaurer une session déjà expirée.
 * Renvoie null si le token n'est pas un JWT lisible (ex. token factice des mocks).
 */
export function getJwtExpiry(token: string): Date | null {
  const payload = token.split('.')[1];
  if (!payload) return null;

  try {
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/').padEnd(Math.ceil(payload.length / 4) * 4, '=');
    const claims: unknown = JSON.parse(atob(base64));
    if (typeof claims !== 'object' || claims === null) return null;
    const exp = (claims as Record<string, unknown>).exp;
    return typeof exp === 'number' ? new Date(exp * 1000) : null;
  } catch {
    return null;
  }
}

export function isJwtExpired(token: string, now: Date = new Date()): boolean {
  const expiry = getJwtExpiry(token);
  return expiry !== null && expiry.getTime() <= now.getTime();
}
