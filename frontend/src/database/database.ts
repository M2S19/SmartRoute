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