# SmartRoute — application mobile

Frontend React Native (Expo, TypeScript) de SmartRoute.
L'authentification peut passer par l'API ASP.NET Core ; les tournées et les livraisons sont encore simulées (mocks).

## Lancer

Depuis le dossier `frontend/` :

```bash
npm install
npx expo start
```

Scanner le QR code avec Expo Go (ou `npx expo start --web` pour le navigateur).

## Configuration (`.env.local`)

Copier `.env.example` en `.env.local` (fichier local, non commité), puis adapter :

| Variable | Rôle |
|---|---|
| `EXPO_PUBLIC_API_BASE_URL` | URL de l'API. Vide → `http://10.0.2.2:5000` sur Android (émulateur), `http://127.0.0.1:5000` ailleurs (web, simulateur iOS). **Obligatoire sur un téléphone physique** : `http://<IP du PC sur le Wi-Fi>:5000`. |
| `EXPO_PUBLIC_USE_MOCKS` | Valeur par défaut des bascules ci-dessous (`true` si absente). |
| `EXPO_PUBLIC_MOCK_AUTH` | `false` : login réel via `POST /api/auth/login`. |
| `EXPO_PUBLIC_MOCK_OPTIMIZE` | `false` : US1 réelle via `POST /api/routes/optimize` (rôle RESPONSABLE_LOGISTIQUE). |
| `EXPO_PUBLIC_MOCK_MY_ROUTE` | `false` : US3 réelle (tournées du livreur et validation des arrêts, enregistrées dans MongoDB, rôle LIVREUR). |
| `EXPO_PUBLIC_MOCK_DELIVERIES` | `false` : US2 réelle (proposition puis confirmation enregistrée dans MongoDB). |

Après modification : recharger l'application, ou relancer `npx expo start -c`.
Une variable définie dans le shell l'emporte sur `.env.local`.

Ne pas brancher un domaine sur l'API avec une authentification simulée : le token factice serait refusé (401).

En développement, l'écran de connexion indique si l'API est joignable (`GET /api/health`) et quelle URL est utilisée.

## Comptes de démonstration

| Email | Rôle |
|---|---|
| `responsable@smartroute.fr` | RESPONSABLE_LOGISTIQUE |
| `livreur@smartroute.fr` | LIVREUR |

- Authentification réelle : mot de passe du seed de l'API (`Seed:DefaultPassword` dans
  `backend/SmartRoute.Api/appsettings.Development.json`).
- Authentification simulée : n'importe quel mot de passe non vide.

Erreurs simulées : une adresse contenant « erreur » fait échouer l'optimisation,
l'affectation ou la validation d'un arrêt.

## Architecture

```
UI (screens, components) → services (auth, route, delivery, health) → mock | HTTP
```

- `src/config/api.ts` : `API_BASE_URL`, bascules `MOCKS.auth / optimize / myRoute / deliveries`, endpoints.
- `src/services/route/routeOptimizationMapper.ts` : conversion entre l'écran d'optimisation et le contrat de l'API
  (`deliveries` → `stops` ; `optimizedOrder` → arrêts ordonnés, dépôt exclu par position ; `gainKm` calculé).
- `src/services/delivery/deliveryAssignmentMapper.ts` : conversion du contrat d'affectation (US2) ; la proposition
  garde `routeVersion`, renvoyée à la confirmation (contrôle de concurrence côté API).
- `src/services/*/xxxService.ts` : interface + choix de l'implémentation selon `MOCKS`.
- `src/services/http/httpClient.ts` : client `fetch` (délai de 15 s), ajoute le JWT (`Authorization: Bearer`),
  lit les erreurs ProblemDetails (`title`, `detail`, `status`, `code`, `errors`) et renvoie un message affichable.
  - 401 sur une requête authentifiée → session invalidée, retour au Login (« session expirée ») ;
    401 au login → « Email ou mot de passe incorrect. » ;
  - 403 → « Accès refusé. » ; 5xx → message générique (jamais le détail du serveur) ;
  - erreur réseau / délai dépassé → message dédié.
- `src/context/AuthContext.tsx` : session ; au démarrage, un token déjà expiré (claim `exp`) n'est pas restauré.
- `src/storage/tokenStorage.ts` : session (JWT + utilisateur) dans `expo-secure-store` (mobile) ou `localStorage` (web),
  relue au démarrage pour rester connecté sans réseau.
- `src/storage/offline/` : organisation prévue du mode hors ligne (SQLite), non implémentée.
- `design/` : maquettes Stitch de référence.

| Service | Endpoint | État |
|---|---|---|
| `healthService.check` | `GET /api/health` | branché |
| `authService.login` | `POST /api/auth/login` | branché (`MOCK_AUTH=false`) |
| `routeService.optimize` | `POST /api/routes/optimize` | branché (`MOCK_OPTIMIZE=false`) |
| `routeService.getMyRoutes` | `GET /api/routes/me` | branché (`MOCK_MY_ROUTE=false`) |
| `routeService.markStopDelivered` | `PATCH /api/routes/me/{routeId}/stops/{stopId}/deliver` | branché (`MOCK_MY_ROUTE=false`) |
| `deliveryService.assignBestDriver` | `POST /api/deliveries/assign-best-driver` | branché (`MOCK_DELIVERIES=false`) |
| `deliveryService.confirmAssignment` | `POST /api/deliveries/confirm-assignment` | branché (`MOCK_DELIVERIES=false`) |

Les algorithmes (Nearest Neighbor, 2-opt, Cheapest Insertion) sont exécutés par l'API uniquement.

## Vérifications

```bash
npx tsc --noEmit
npx expo-doctor
```
