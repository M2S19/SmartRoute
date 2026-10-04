# SmartRoute

Application mobile de logistique (projet académique) : un responsable logistique optimise des
tournées de livraison et affecte les nouvelles livraisons ; un livreur consulte sa tournée.

| Dossier | Contenu | Documentation |
|---|---|---|
| [`frontend/`](frontend/) | Application Expo / React Native (TypeScript) | [frontend/README.md](frontend/README.md) |
| [`backend/`](backend/) | API ASP.NET Core (.NET 10) + MongoDB, algorithmes d'optimisation | [backend/README.md](backend/README.md) |

Autres fichiers à la racine : `docker-compose.yml` (MongoDB, API conteneurisée en option),
`.gitlab-ci.yml` (CI), `AGENTS.md` (décisions d'API et règles d'architecture).

## Démarrage rapide

Prérequis : Node.js 22, SDK .NET 10, Docker Desktop, Expo Go sur le téléphone.

```bash
# 1. MongoDB (depuis la racine)
docker compose up -d

# 2. API (depuis backend/) — la première fois, définir la clé JWT (voir backend/README.md)
cd backend
dotnet run --project SmartRoute.Api

# 3. Application mobile (depuis frontend/)
cd frontend
npm install
npx expo start
```

- API : `http://<IP du PC>:5000`, Swagger sur <http://127.0.0.1:5000/swagger>, santé sur `/api/health`.
- L'application mobile fonctionne aussi sans API, avec des données mock (`EXPO_PUBLIC_USE_MOCKS=true`, par défaut).

API conteneurisée (optionnel) : copier `.env.example` en `.env`, renseigner `JWT_SIGNING_KEY`, puis :

```bash
docker compose --profile api up -d --build
```

## Vérifications

```bash
cd backend && dotnet build && dotnet test
cd frontend && npx tsc --noEmit && npx expo-doctor
```

La CI GitLab exécute les mêmes vérifications (`backend:test`, `frontend:check`).
