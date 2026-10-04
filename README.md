# SmartRoute

Application mobile de logistique (projet académique) : un responsable logistique optimise des
tournées de livraison et affecte les nouvelles livraisons ; un livreur consulte sa tournée.

| Dossier | Contenu | Documentation |
|---|---|---|
| [`frontend/`](frontend/) | Application Expo / React Native (TypeScript) | [frontend/README.md](frontend/README.md) |
| [`backend/`](backend/) | API ASP.NET Core (.NET 10) + MongoDB, algorithmes d'optimisation | [backend/README.md](backend/README.md) |

Autres fichiers à la racine :
- `docker-compose.yml` (configuration commune) et `docker-compose.dev.yml` / `.test.yml` / `.prod.yml` (surcharges) ;
- `.env.dev.example` / `.env.test.example` / `.env.prod.example` : modèles à copier en `.env.<env>` (non commités) ;
- `.gitlab-ci.yml` (CI), `AGENTS.md` (décisions d'API et règles d'architecture).

## Démarrage rapide

Prérequis : Node.js 22, SDK .NET 10, Docker Desktop, Expo Go sur le téléphone.
Première fois : copier `.env.dev.example` en `.env.dev` et y renseigner `JWT_SIGNING_KEY`.

```bash
# 1. MongoDB DEV seul (depuis la racine), base smartroute_dev sur 127.0.0.1:27017
docker compose --env-file .env.dev -f docker-compose.yml -f docker-compose.dev.yml up -d mongo

# 2. API (depuis backend/) — la première fois, définir la clé JWT (voir backend/README.md)
cd backend
dotnet run --project SmartRoute.Api

# 3. Application mobile (depuis frontend/)
cd frontend
npm install
npx expo start
```

- API : `http://<IP du PC>:5000`, Swagger sur <http://127.0.0.1:5000/swagger>.
- Santé : `/api/health` (API seule) et `/api/mongo-health` (connexion MongoDB).
- L'application mobile fonctionne aussi sans API, avec des données mock (`EXPO_PUBLIC_USE_MOCKS=true`, par défaut).

## Docker par environnement

| Environnement | Commande (depuis la racine) | Base | Ports hôte |
|---|---|---|---|
| DEV | `docker compose --env-file .env.dev -f docker-compose.yml -f docker-compose.dev.yml up -d --build` | `smartroute_dev` | API 5000, MongoDB 127.0.0.1:27017 |
| TEST | `docker compose --env-file .env.test -f docker-compose.yml -f docker-compose.test.yml up -d --build` | `smartroute_test` (éphémère) | aucun |
| PROD | `docker compose --env-file .env.prod -f docker-compose.yml -f docker-compose.prod.yml up -d --build` | `smartroute` | API 8080 |

En PROD, `JWT_SIGNING_KEY` doit être renseignée dans `.env.prod`, sinon l'API refuse de démarrer.

## Vérifications

```bash
cd backend && dotnet build && dotnet test
cd frontend && npx tsc --noEmit && npx expo-doctor
```

La CI GitLab exécute les mêmes vérifications (`backend:test`, `frontend:check`).
