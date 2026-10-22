
/**
 * Version Web de l'initialisation de la base.
 *
 * Sur le navigateur, SmartRoute utilise directement
 * l'API ASP.NET Core.
 *
 * SQLite reste réservé aux plateformes mobiles.
 */
export async function initDatabase(): Promise<void> {
  console.log('[SmartRoute Web] SQLite non utilisé.');
}
