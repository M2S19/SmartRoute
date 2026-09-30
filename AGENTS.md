This is an Expo/React Native mobile application. Prioritize mobile-first patterns, performance, and cross-platform compatibility.

## Expo has changed — do not trust your training data

Expo ships breaking changes every SDK release. APIs you remember are likely renamed, moved, or removed. Before writing any code that touches an Expo, EAS, or React Native API:

1. Read the major version of the `expo` package in `package.json`.
2. Fetch the matching versioned docs: `https://docs.expo.dev/versions/v<major>.0.0/`
3. For anything else, fetch https://docs.expo.dev/llms.txt — an index of all Expo docs with corrections to common LLM misconceptions. Follow its links to the specific page you need; never answer from memory.

## Commands

Use `bunx` instead of `npx` if the project uses bun (`bun.lock` present).

```bash
npx expo install <package>  # ALWAYS use instead of npm/yarn/pnpm/bun add — resolves SDK-compatible versions
npx expo start              # start the dev server
npx expo lint               # lint
npx tsc --noEmit            # typecheck
npx expo-doctor             # diagnose dependency and config issues
npx expo install --fix      # fix incompatible package versions
```

Run lint and typecheck before declaring any task done.

## Navigation & Routing

- This project deliberately uses **React Navigation** (`@react-navigation/native-stack`), not Expo Router. Navigators live in `src/navigation/`, screens in `src/screens/`.
- The root navigator switches stacks based on the authenticated user's role (`src/context/AuthContext.tsx`).
- Must run in **Expo Go**: do not add dependencies that require a development build.

## Building with EAS

Use EAS to build, sign, and submit the app in the cloud (`eas build`, `eas submit`) and to ship over-the-air updates (`eas update`) — no local Xcode or Android Studio required. Run EAS CLI as `bunx eas-cli <command>` in Bun projects, or `npx eas-cli@latest <command>` otherwise; substitute that for bare `eas` in docs examples.
Docs: https://docs.expo.dev/eas/index.md

## Rules

- If `ios/` and `android/` directories do not exist, they are generated (Continuous Native Generation). Never create or edit them by hand — configure native behavior in `app.json` and config plugins.
- Expo Go only includes its bundled native modules. After adding a library with native code, the app needs a development build: `npx expo run:ios|android` locally, or `eas build --profile development`.
- Prefer recommended Expo modules over third-party libraries, and check your available skills before adding dependencies. Docs: https://docs.expo.dev/versions/latest/index.md

Décisions API SmartRoute :
- Erreurs : ProblemDetails ASP.NET Core
- Identifiants : string (ObjectId MongoDB)
- Dates : ISO 8601 UTC ; distances : km
- JWT : claims sub, email, role ; header Authorization: Bearer <token>
- 401 : token absent, invalide ou expiré (le front déconnecte, sauf sur /api/auth/login)
- 403 : authentifié mais rôle insuffisant
- optimize : calcul uniquement, rien n'est enregistré → 200
- assign-best-driver : proposition uniquement, rien n'est enregistré → 200
  (422 si adresse non géocodable ou aucun livreur/tournée disponible) ;
  la proposition inclut routeVersion
- confirm-assignment : reçoit address, routeId, insertionPosition, routeVersion ;
  recalcule et vérifie côté serveur ; 201 si enregistré ;
  409 si la tournée a changé depuis la proposition
- Concurrence optimiste des tournées : chaque tournée a un champ version (int),
  incrémenté à chaque modification ; toute mise à jour MongoDB est filtrée
  sur { _id, version } → 0 document modifié = 409
- GET /api/routes/me : 404 si aucune tournée (le front affiche l'état vide)
- POST /api/routes/me/stops/{stopId}/deliver : idempotent,
  200 même si l'arrêt est déjà livré ; 404 si l'arrêt n'appartient pas au livreur
- Géocodage : fait par l'API via IGeocodingService (API Adresse / BAN),
  résultats en cache dans MongoDB, adresse introuvable → 422
- Distances : via IDistanceMatrixProvider ; Haversine d'abord,
  API de routage plus tard sans modifier les algorithmes
- Swagger/OpenAPI avec schéma Bearer JWT pour tester les routes protégées