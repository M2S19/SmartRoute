# SmartRoute — API ASP.NET Core

API REST de l'application mobile SmartRoute (.NET 10, MongoDB).
Documentation des endpoints : Swagger (voir plus bas). Décisions d'API : `../AGENTS.md`.

## Prérequis

- SDK .NET 10
- Docker Desktop (pour MongoDB)

## Lancer en local

```bash
# 1. MongoDB (depuis le dossier backend/)
docker compose up -d

# 2. Clé JWT (une seule fois, stockée hors du dépôt) : au moins 32 octets aléatoires
dotnet user-secrets set "Jwt:SigningKey" "<clé aléatoire>" --project SmartRoute.Api

# 3. API
dotnet run --project SmartRoute.Api
```

- API : `http://0.0.0.0:5000` (joignable depuis le téléphone via l'IP du PC ; autoriser le port dans le pare-feu Windows).
- Swagger : <http://127.0.0.1:5000/swagger>. Pour une route protégée : `POST /api/auth/login`, puis « Authorize » avec le token.
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
et une clé JWT de test (`SmartRoute.Tests/Api/SmartRouteApiFactory.cs`).
Runner : Microsoft.Testing.Platform (`global.json`).

## Structure

```
backend/
├── SmartRoute.Api/
│   ├── Controllers/     endpoints HTTP (minces : délèguent aux services)
│   ├── Services/        logique applicative (AuthService…)
│   ├── Algorithms/      algorithmes purs (à venir)
│   ├── Models/          documents MongoDB (attributs Bson)
│   ├── DTOs/            contrats JSON d'entrée / sortie
│   ├── Repositories/    accès MongoDB
│   ├── Auth/            JwtTokenService, rôles, politiques, noms de claims
│   ├── Errors/          ApiException, handler global, ProblemDetails
│   ├── Settings/        classes de configuration
│   └── Program.cs       enregistrement des services, JWT, politiques, Swagger
├── SmartRoute.Tests/
│   ├── Algorithms/      tests des algorithmes (priorité, à venir)
│   └── Api/             tests d'intégration HTTP
└── docker-compose.yml   MongoDB
```

## Erreurs

Toutes les erreurs sont en ProblemDetails (`application/problem+json`) avec :
- `detail` : message en français, affichable tel quel ;
- `code` : code stable pour le front (`VALIDATION_ERROR`, `INVALID_CREDENTIALS`, `UNAUTHORIZED`, `FORBIDDEN`…) ;
- `errors` : erreurs par champ (400 de validation uniquement).

Une erreur métier se lève avec `throw new ApiException(status, code, message)`.
