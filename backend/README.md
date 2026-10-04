# SmartRoute — API ASP.NET Core

API REST de l'application mobile SmartRoute (.NET 10, MongoDB).
Documentation des endpoints : Swagger (voir plus bas). Décisions d'API : `../AGENTS.md`.

## Prérequis

- SDK .NET 10
- Docker Desktop (pour MongoDB)

## Lancer en local

```bash
# 1. MongoDB (depuis la racine du dépôt : docker-compose.yml y est)
docker compose up -d

# 2. Clé JWT (une seule fois, stockée hors du dépôt), depuis backend/ : au moins 32 octets aléatoires
dotnet user-secrets set "Jwt:SigningKey" "<clé aléatoire>" --project SmartRoute.Api

# 3. API, depuis backend/
dotnet run --project SmartRoute.Api
```

- API : `http://0.0.0.0:5000` (joignable depuis le téléphone via l'IP du PC ; autoriser le port dans le pare-feu Windows).
- Swagger : <http://127.0.0.1:5000/swagger>. Pour une route protégée : `POST /api/auth/login`, puis « Authorize » avec le token.
- Santé : `GET /api/health` (anonyme) : 200 si MongoDB répond, sinon 503.
- Sans clé JWT valide, l'API refuse de démarrer avec un message explicite.
- Kestrel écoute en IPv4 : si `localhost` ne répond pas, utiliser `127.0.0.1`.

Comptes créés au démarrage **en Development uniquement** (mot de passe de démo dans `appsettings.Development.json`) :

| Email | Rôle |
|---|---|
| `responsable@smartroute.fr` | `RESPONSABLE_LOGISTIQUE` |
| `livreur@smartroute.fr` | `LIVREUR` |

## Tests

```bash
dotnet test
```

Ni MongoDB ni user-secrets nécessaires : les tests d'API utilisent un repository en mémoire
et une clé JWT de test (`SmartRoute.Tests/Common/SmartRouteApiFactory.cs`).
Runner : Microsoft.Testing.Platform (`global.json`).

## Structure

Ouvrir `SmartRoute.sln` dans Visual Studio. Dépendances : Controller → Service → Algorithm / Repository → MongoDB.

```
backend/
├── SmartRoute.sln
├── SmartRoute.Api/
│   ├── Controllers/     HTTP uniquement : DTO en entrée, appel d'un Service, code HTTP en sortie
│   ├── Services/        logique métier (AuthService, DatabaseSeeder…)
│   ├── Algorithms/      algorithmes purs, sans dépendance (voir Algorithms/README.md)
│   ├── Models/          documents MongoDB (attributs Bson)
│   ├── DTOs/            entrées / sorties de l'API, par fonctionnalité (Auth/, Health/…)
│   ├── Repositories/    seul accès à MongoDB
│   ├── Auth/            JwtTokenService, PasswordHasher, AuthSetup (JWT + politiques), Roles
│   ├── Settings/        MongoDbSettings, JwtSettings, SeedSettings
│   ├── Errors/          ApiException, ErrorCodes, handler global, ProblemDetails
│   └── Program.cs       enregistrement des services, Swagger, pipeline HTTP
├── SmartRoute.Tests/
│   ├── Auth/            tests du login
│   ├── Routes/          tests des endpoints de tournées (à venir)
│   ├── Algorithms/      tests des algorithmes (priorité, à venir)
│   ├── Integration/     tests sur un vrai MongoDB (si nécessaire)
│   └── Common/          SmartRouteApiFactory, dépôt en mémoire
├── Dockerfile           image de l'API (utilisée par le profil « api » du docker-compose racine)
└── global.json          runner de tests Microsoft.Testing.Platform
```

## Image Docker

```bash
# Depuis la racine du dépôt (JWT_SIGNING_KEY dans .env, voir .env.example)
docker compose --profile api up -d --build
```

L'API écoute sur 8080 dans le conteneur (utilisateur non root), exposée sur le port 5000.
Configuration par variables d'environnement : `Jwt__SigningKey`, `MongoDb__ConnectionString`, `MongoDb__DatabaseName`.

## Erreurs

Toutes les erreurs sont en ProblemDetails (`application/problem+json`) avec :
- `detail` : message en français, affichable tel quel ;
- `code` : code stable pour le front (`VALIDATION_ERROR`, `INVALID_CREDENTIALS`, `UNAUTHORIZED`, `FORBIDDEN`…) ;
- `errors` : erreurs par champ (400 de validation uniquement).

Une erreur métier se lève avec `throw new ApiException(status, code, message)`.
