# Mode hors ligne (à venir)

Rien n'est implémenté ici pour l'instant. Organisation prévue :

- `database.ts` : ouverture de la base locale avec `expo-sqlite` (disponible dans Expo Go).
- `schema.ts` : tables
  - `routes` (id, name, date, status, depot_json, synced_at)
  - `route_stops` (id, route_id, order, status, delivered_at, delivery_json)
  - `pending_actions` (id, type, payload_json, created_at) : actions faites hors ligne,
    par exemple `DELIVER_STOP` avec le stopId, à rejouer vers l'API.
- `routeRepository.ts` : `saveMyRoute(route)`, `getMyRoute()`, `markStopDelivered(stopId)`.
- `syncService.ts` : rejoue `pending_actions` via `routeService` au retour du réseau.

Principe : `routeService.getMyRoute()` renverra la tournée de l'API et la mettra en cache ;
en cas d'échec réseau, il lira la copie SQLite. Les écrans n'auront pas à changer.

Le JWT reste dans `expo-secure-store` (voir `../tokenStorage.ts`), jamais dans SQLite.
