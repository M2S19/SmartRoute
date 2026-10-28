import * as SQLite from 'expo-sqlite';
import { migrateDatabase } from './migrations';

export const db = SQLite.openDatabaseSync('smartroute.db');

export async function initDatabase(): Promise<void> {

  // 1. Activer le mode WAL
  await db.execAsync('PRAGMA journal_mode = WAL;');

  // 2. Créer les tables si elles n'existent pas
  await migrateDatabase(db);

  // 3. Vérifier les tables créées
  const tables = await db.getAllAsync<{ name: string }>(
    `SELECT name FROM sqlite_master
     WHERE type = 'table'
     AND name NOT LIKE 'sqlite_%';`
  );

  

  console.log('Tables SQLite :', tables);
  console.log('SQLite SmartRoute initialisée');
}

export async function debugDatabase(): Promise<void> {
  // Afficher les tables
  const tables = await db.getAllAsync(
    `SELECT name
     FROM sqlite_master
     WHERE type = 'table'
     ORDER BY name`
  );

  console.log('=== TABLES SQLITE ===');
  console.log(tables);

  // Afficher les tournées
  const routes = await db.getAllAsync(
    'SELECT * FROM routes'
  );

  console.log('=== ROUTES ===');
  console.log(JSON.stringify(routes, null, 2));

  // Afficher les arrêts
  const stops = await db.getAllAsync(
    'SELECT * FROM route_stops'
  );

  console.log('=== ROUTE_STOPS ===');
  console.log(JSON.stringify(stops, null, 2));

  // Afficher la file de synchronisation
  const queue = await db.getAllAsync(
    'SELECT * FROM sync_queue'
  );

  console.log('=== SYNC_QUEUE ===');
  console.log(JSON.stringify(queue, null, 2));
}