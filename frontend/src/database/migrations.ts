
import * as SQLite from 'expo-sqlite';

/**
 * Vérifie si une colonne existe avant de l'ajouter.
 * Évite les erreurs "duplicate column name".
 */
async function addColumnIfMissing(
  db: SQLite.SQLiteDatabase,
  table: 'routes' | 'route_stops',
  column: string,
  declaration: string
): Promise<void> {

  const columns = await db.getAllAsync<{ name: string }>(
    `PRAGMA table_info("${table}");`
  );

  const columnExists = columns.some(
    (existing) => existing.name === column
  );

  if (columnExists) {
    console.log(`Colonne déjà existante : ${table}.${column}`);
    return;
  }

  await db.execAsync(
    `ALTER TABLE "${table}" ADD COLUMN ${declaration};`
  );

  console.log(`Colonne ajoutée : ${table}.${column}`);
}

/**
 * Initialise et met à jour la base SQLite de SmartRoute.
 * Les données existantes sont conservées.
 */
export async function migrateDatabase(
  db: SQLite.SQLiteDatabase
): Promise<void> {

  // 1. Création des tables si elles n'existent pas
  await db.execAsync(`
    CREATE TABLE IF NOT EXISTS routes (
      id TEXT PRIMARY KEY,
      status TEXT NOT NULL,
      updated_at TEXT
    );

    CREATE TABLE IF NOT EXISTS route_stops (
      id TEXT PRIMARY KEY,
      route_id TEXT NOT NULL,
      address TEXT NOT NULL,
      stop_order INTEGER NOT NULL,
      status TEXT NOT NULL,
      delivered_at TEXT,
      sync_status TEXT NOT NULL DEFAULT 'SYNCED',

      FOREIGN KEY (route_id)
        REFERENCES routes(id)
    );

    CREATE TABLE IF NOT EXISTS sync_queue (
      id TEXT PRIMARY KEY,
      route_id TEXT NOT NULL,
      stop_id TEXT NOT NULL,
      operation_type TEXT NOT NULL,
      payload TEXT NOT NULL,
      status TEXT NOT NULL DEFAULT 'PENDING',
      created_at TEXT NOT NULL,
      retry_count INTEGER NOT NULL DEFAULT 0,
      last_error TEXT
    );
  `);

  // 2. Migration de la table routes
  await addColumnIfMissing(
    db, 'routes', 'name', 'name TEXT'
  );

  await addColumnIfMissing(
    db, 'routes', 'date', 'date TEXT'
  );

  await addColumnIfMissing(
    db, 'routes', 'owner_user_id', 'owner_user_id TEXT'
  );

  await addColumnIfMissing(
    db, 'routes', 'depot_json', 'depot_json TEXT'
  );

  await addColumnIfMissing(
    db, 'routes', 'driver_json', 'driver_json TEXT'
  );

  // 3. Migration de la table route_stops
  await addColumnIfMissing(
    db, 'route_stops', 'delivery_json', 'delivery_json TEXT'
  );

  await addColumnIfMissing(
    db,
    'routes',
    'is_available',
    'is_available INTEGER NOT NULL DEFAULT 1'
  );

  console.log('Migration SQLite terminée avec succès');
}
