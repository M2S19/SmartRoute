# SmartRoute — application mobile

Frontend React Native (Expo, TypeScript) de SmartRoute. Données mock pour l'instant ;
l'API ASP.NET Core sera branchée plus tard sans modifier les écrans.

## Lancer

```bash
npm install
npx expo start
```

Scanner le QR code avec Expo Go.

## Comptes de démonstration (mode mock)

| Email | Rôle |
|---|---|
| `responsable@smartroute.fr` | RESPONSABLE_LOGISTIQUE |
| `livreur@smartroute.fr` | LIVREUR |

N'importe quel mot de passe non vide est accepté. Tout autre email renvoie une erreur.

Erreurs simulées : une adresse contenant « erreur » fait échouer l'optimisation,
l'affectation ou la validation d'un arrêt.

## Architecture

```
UI (screens, components) → services (auth, route, delivery) → mock | HTTP
```

- `src/config/api.ts` : `API_BASE_URL` (`EXPO_PUBLIC_API_BASE_URL`), `USE_MOCKS` (`EXPO_PUBLIC_USE_MOCKS`), endpoints.
- `src/services/*/xxxService.ts` : interface + choix de l'implémentation selon `USE_MOCKS`.
- `src/services/http/httpClient.ts` : client `fetch`, ajoute le JWT (`Authorization: Bearer`).
- `src/storage/tokenStorage.ts` : session (JWT + utilisateur) dans `expo-secure-store` (mobile) ou `localStorage` (web),
  relue au démarrage pour rester connecté sans réseau.
- `src/storage/offline/` : organisation prévue du mode hors ligne (SQLite), non implémentée.
- `design/` : maquettes Stitch de référence.

Pour passer sur l'API : copier `.env.example` en `.env.local`, mettre `EXPO_PUBLIC_USE_MOCKS=false`
et l'URL de l'API, puis relancer `npx expo start -c`.

| Service | Endpoint |
|---|---|
| `authService.login` | `POST /api/auth/login` |
| `routeService.optimize` | `POST /api/routes/optimize` |
| `routeService.getMyRoute` | `GET /api/routes/me` |
| `routeService.markStopDelivered` | `PATCH /api/routes/me/stops/{stopId}/deliver` |
| `deliveryService.assignBestDriver` | `POST /api/deliveries/assign-best-driver` |
| `deliveryService.confirmAssignment` | `POST /api/deliveries/confirm-assignment` |

Les algorithmes (Nearest Neighbor, 2-opt, Cheapest Insertion) sont exécutés par l'API uniquement.

## Vérifications

```bash
npx tsc --noEmit
npx expo-doctor
```
