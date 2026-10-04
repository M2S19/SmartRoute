# SmartRoute — instructions pour les agents

Monorepo d'un projet académique de logistique : optimisation de tournées de livraison.

```
frontend/              application Expo / React Native (TypeScript)
backend/             API ASP.NET Core (.NET 10) + MongoDB
docker-compose.yml   configuration Docker commune (MongoDB + API)
docker-compose.{dev,test,prod}.yml   surcharges par environnement (+ .env.<env>, modèles .env.<env>.example)
.gitlab-ci.yml       CI : backend:test, frontend:check
```

Lancer chaque commande depuis le bon dossier : `frontend/` pour npm / Expo, `backend/` pour dotnet,
la racine pour docker compose.

## frontend/ — application Expo

This is an Expo/React Native mobile application. Prioritize mobile-first patterns, performance, and cross-platform compatibility.

### Expo has changed — do not trust your training data

Expo ships breaking changes every SDK release. APIs you remember are likely renamed, moved, or removed. Before writing any code that touches an Expo, EAS, or React Native API:

1. Read the major version of the `expo` package in `frontend/package.json`.
2. Fetch the matching versioned docs: `https://docs.expo.dev/versions/v<major>.0.0/`
3. For anything else, fetch https://docs.expo.dev/llms.txt — an index of all Expo docs with corrections to common LLM misconceptions. Follow its links to the specific page you need; never answer from memory.

### Commands (from `frontend/`)

```bash
npx expo install <package>  # ALWAYS use instead of npm/yarn/pnpm/bun add — resolves SDK-compatible versions
npx expo start              # start the dev server
npx expo lint               # lint
npx tsc --noEmit            # typecheck
npx expo-doctor             # diagnose dependency and config issues
npx expo install --fix      # fix incompatible package versions
```

Run lint and typecheck before declaring any task done.

### Navigation & Routing

- This project deliberately uses **React Navigation** (`@react-navigation/native-stack`), not Expo Router. Navigators live in `frontend/src/navigation/`, screens in `frontend/src/screens/`.
- The root navigator switches stacks based on the authenticated user's role (`frontend/src/context/AuthContext.tsx`).
- Must run in **Expo Go**: do not add dependencies that require a development build.

### Building with EAS

Use EAS to build, sign, and submit the app in the cloud (`eas build`, `eas submit`) and to ship over-the-air updates (`eas update`) — no local Xcode or Android Studio required. Run EAS CLI as `npx eas-cli@latest <command>`; substitute that for bare `eas` in docs examples.
Docs: https://docs.expo.dev/eas/index.md

### Rules

- If `ios/` and `android/` directories do not exist, they are generated (Continuous Native Generation). Never create or edit them by hand — configure native behavior in `app.json` and config plugins.
- Expo Go only includes its bundled native modules. After adding a library with native code, the app needs a development build: `npx expo run:ios|android` locally, or `eas build --profile development`.
- Prefer recommended Expo modules over third-party libraries, and check your available skills before adding dependencies. Docs: https://docs.expo.dev/versions/latest/index.md

## Décisions API SmartRoute

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
- PATCH /api/routes/me/stops/{stopId}/deliver : idempotent (modification partielle de l'état d'un arrêt),
  200 même si l'arrêt est déjà livré ; 404 si l'arrêt n'appartient pas au livreur
- Géocodage : fait par l'API via IGeocodingService (API Adresse / BAN),
  résultats en cache dans MongoDB, adresse introuvable → 422
- Distances : matrice construite par Services/DistanceService (classe concrète), Haversine d'abord,
  API de routage plus tard sans modifier les algorithmes
- Swagger/OpenAPI avec schéma Bearer JWT pour tester les routes protégées
- GET /api/health : anonyme, vérifie seulement que l'API répond → 200 (aucun appel à MongoDB)
- GET /api/mongo-health : anonyme, ping MongoDB (RunCommandAsync) → 200, sinon 500.
  Contrôleur volontairement simple, repris de l'ancien backend : pas d'ApiException, de ProblemDetails, de DTO,
  de timeout ni de logger spécifiques. Ne pas le « mettre aux normes » sans demande explicite.
- Base MongoDB par environnement ASP.NET Core : Development → `smartroute_dev`, Testing → `smartroute_test`,
  Production → `smartroute`
- Ports : port interne de l'API toujours 8080 dans le conteneur ; port hôte 5000 en DEV (et pour `dotnet run`),
  8080 en PROD ; MongoDB exposé seulement en DEV, sur 127.0.0.1

## Docker et environnements

- `docker-compose.yml` (commun, racine) + une surcharge `docker-compose.<env>.yml` + `--env-file .env.<env>` :
  `docker compose --env-file .env.dev -f docker-compose.yml -f docker-compose.dev.yml up -d --build`
- Projets Compose séparés (`smartroute-dev`, `smartroute-test`, `smartroute-prod`), pas de `container_name` fixe.
- DEV : volume persistant, MongoDB sur 127.0.0.1. TEST : MongoDB éphémère (tmpfs), aucun port.
  PROD : volume persistant, MongoDB non exposé, `restart: always`.
- Variables (une seule définition par paramètre) : `API_PORT`, `MONGO_PORT` (DEV), `MONGO_DATABASE`, `API_ENVIRONMENT`,
  `JWT_SIGNING_KEY` dans `.env.<env>` ; images (`mongo:8`, `smartroute-api:latest`) et chaîne de connexion
  (`mongodb://mongo:27017`) dans `docker-compose.yml`.
- Les vrais `.env.<env>` ne sont jamais commités ; seuls les `.env.<env>.example` (sans secret) le sont.
  `JWT_SIGNING_KEY` vide en PROD → l'API refuse de démarrer.
- Dockerfile : `backend/SmartRoute.Api/Dockerfile` (contexte de build = `backend/SmartRoute.Api`).

## backend/ — architecture (.NET 10)

Architecture académique simple, l'objectif est l'algorithmique et la documentation :
Controller → Service → Algorithm / Repository → MongoDB.

- Deux projets seulement, ouverts dans Visual Studio via `backend/SmartRoute.sln` :
  SmartRoute.Api et SmartRoute.Tests (xUnit v3, runner Microsoft.Testing.Platform via `backend/global.json`).
- Dossiers de SmartRoute.Api :
  - Controllers/ : HTTP uniquement (DTO en entrée, appel d'un Service, code HTTP en sortie). Jamais d'accès aux Repositories.
  - Services/ : logique métier ; reçoivent et renvoient des DTOs, manipulent les Models. Jamais d'IMongoCollection.
  - Algorithms/ : code pur, aucune dépendance à ASP.NET Core, MongoDB, Controllers, Services ou HTTP.
    Écrits à la main par l'auteur du projet : ne pas les implémenter ni figer leurs signatures sans demande explicite.
  - Models/ : documents MongoDB (attributs Bson ; sous-documents comme classes C# imbriquées).
  - DTOs/ : entrées / sorties de l'API, rangées par fonctionnalité (Auth/, Routes/, Deliveries/, Health/).
    Les Models ne sont jamais renvoyés directement ; conversion par méthodes statiques From(...).
  - Repositories/ : seul accès à MongoDB (Find, Insert, Update, filtres).
  - Auth/ : JwtTokenService, PasswordHasher, AuthSetup (JWT Bearer + politiques de rôles), Roles.
  - Settings/ : MongoDbSettings, JwtSettings, SeedSettings.
  - Errors/ : ApiException, ErrorCodes, GlobalExceptionHandler, ProblemDetailsConfig.
- DistanceService (Haversine) est écrit à la main par l'auteur du projet, comme les algorithmes.
- Une seule interface : IGeocodingService (appel externe, simulé dans les tests). Le reste en classes concrètes.
- Pas de CQRS, MediatR, repository générique, DependencyInjection.cs séparé ni BsonClassMap.
- Une seule exception métier : ApiException(status, code, message), convertie en ProblemDetails
  par le handler global (Errors/).
- Clé JWT uniquement en user-secrets en local (vérifiée au démarrage), variable `Jwt__SigningKey` en conteneur ;
  seed en Development uniquement.
- Tests (SmartRoute.Tests) : Auth/, Routes/, Algorithms/ (priorité), Integration/ (vrai MongoDB, base smartroute_test),
  Common/ (SmartRouteApiFactory, dépôts en mémoire, TestMongo, données de test).
  Integration/ : ignoré localement si MongoDB est absent ; obligatoire en CI (SMARTROUTE_REQUIRE_MONGO=true,
  SMARTROUTE_TEST_MONGO=mongodb://mongo:27017, service mongo:8).
  Tests d'API avec repository en mémoire (sous-classe aux méthodes virtuelles) et clé JWT de test ;
  `dotnet test` ne doit pas échouer faute de MongoDB en local, ni dépendre des user-secrets.

Commandes (depuis `backend/`) : `dotnet build`, `dotnet test`, `dotnet run --project SmartRoute.Api`.

## CI (.gitlab-ci.yml)

- `backend:test` : `dotnet test` dans `mcr.microsoft.com/dotnet/sdk:10.0` avec un service `mongo:8`, rapport JUnit.
- `frontend:check` : `npm ci`, `npx tsc --noEmit`, `npx expo-doctor` dans `node:22`.
- Chaque job ne tourne que si son dossier change. Pas de job Docker (runners UHA inconnus).
