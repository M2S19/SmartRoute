# Contrat de l'API SmartRoute

API REST ASP.NET Core consommée par l'application mobile. Ce document fixe le contrat
**avant** l'écriture du backend. Source côté mobile : `src/types`, `src/config/api.ts`,
`src/services/*/xxxService.http.ts`.

Statut : **brouillon à valider**. Les décisions encore ouvertes sont signalées par **[À TRANCHER]**.
Les écarts avec le front actuel sont listés en [§ 10](#10-écarts-avec-le-front-actuel).

---

## Sommaire

1. [Conventions générales](#1-conventions-générales)
2. [Authentification (JWT)](#2-authentification-jwt)
3. [Erreurs (ProblemDetails)](#3-erreurs-problemdetails)
4. [Géocodage](#4-géocodage)
5. [POST /api/auth/login](#5-post-apiauthlogin)
6. [POST /api/routes/optimize](#6-post-apiroutesoptimize)
7. [POST /api/deliveries/assign-best-driver et POST /api/deliveries/confirm-assignment](#7-affectation-dune-livraison)
8. [GET /api/routes/me](#8-get-apiroutesme)
9. [PATCH /api/routes/me/stops/{stopId}/deliver](#9-patch-apiroutesmestopsstopiddeliver)
10. [Écarts avec le front actuel](#10-écarts-avec-le-front-actuel)

Récapitulatif :

| Méthode | Route | Rôle requis | Enregistre en base |
|---|---|---|---|
| POST | `/api/auth/login` | aucun (anonyme) | non |
| POST | `/api/routes/optimize` | `RESPONSABLE_LOGISTIQUE` | non (calcul seul) |
| POST | `/api/deliveries/assign-best-driver` | `RESPONSABLE_LOGISTIQUE` | **non** (proposition) |
| POST | `/api/deliveries/confirm-assignment` | `RESPONSABLE_LOGISTIQUE` | oui |
| GET | `/api/routes/me` | `LIVREUR` | non |
| PATCH | `/api/routes/me/stops/{stopId}/deliver` | `LIVREUR` | oui (idempotent) |

---

## 1. Conventions générales

| Sujet | Règle |
|---|---|
| Base URL | `API_BASE_URL` (variable `EXPO_PUBLIC_API_BASE_URL`), toutes les routes commencent par `/api`. |
| Format | JSON UTF-8. `Content-Type: application/json` en requête, `application/json` en réponse, `application/problem+json` pour les erreurs. |
| Nommage | **camelCase** (sérialiseur par défaut d'ASP.NET Core, identique aux types TypeScript). |
| Énumérations | Sérialisées en **chaînes** en MAJUSCULES (`"LIVREUR"`, `"PENDING"`). Côté API : `JsonStringEnumConverter` et valeurs d'énum nommées en majuscules (ou nommage explicite). |
| Identifiants | `string`, **ObjectId MongoDB** en hexadécimal sur 24 caractères, ex. `"66f1c2a9e4b0a1d2c3f40a11"`. Le front ne fait aucune hypothèse sur leur forme. |
| Dates | **ISO 8601 UTC** avec suffixe `Z`, ex. `"2026-09-30T06:42:00Z"`. Le front convertit en heure locale pour l'affichage. Exception : `Route.date`, voir ci-dessous. |
| Jour de tournée | `Route.date` est une **date calendaire** ISO 8601 sans heure (`"2026-09-30"`), interprétée dans le fuseau **Europe/Paris**. Ce n'est pas un instant, donc pas de conversion UTC. **[À TRANCHER]** : garder cette exception (recommandé) ou stocker l'instant UTC de minuit Paris. |
| Distances | `number`, en **kilomètres**, arrondies à 0,1 km par l'API. Le front ne fait que formater (`2,4 km`). |
| Pourcentages | `number` entre 0 et 100, arrondi à l'unité (`22` = 22 %). |
| Champs optionnels | Absents du JSON **ou** `null` : le front doit accepter les deux. L'API les omet de préférence (`JsonIgnoreCondition.WhenWritingNull`). |
| Chaînes | L'API applique `trim` sur toutes les chaînes reçues avant validation. |
| Mode de calcul des distances | v1 : distance à vol d'oiseau (formule de haversine). Toute tournée est une **boucle fermée** : dépôt → arrêts → retour au dépôt. **[À TRANCHER]** plus tard : distance routière (OSRM, etc.), sans changement de contrat. |

Exemple de représentation d'une adresse (type `Address`) :

```json
{ "label": "12 rue de Bâle, 68100 Mulhouse", "latitude": 47.7447, "longitude": 7.3431 }
```

> Les coordonnées de ce document sont **illustratives** (ordre de grandeur réaliste),
> pas le résultat d'un vrai géocodage.

---

## 2. Authentification (JWT)

| Sujet | Règle |
|---|---|
| Transport | En-tête `Authorization: Bearer <jwt>` sur **toutes** les routes sauf `POST /api/auth/login`. Déjà ajouté par `src/services/http/httpClient.ts`. |
| Algorithme | HS256, clé symétrique ≥ 256 bits lue depuis la configuration (`dotnet user-secrets` en dev, variable d'environnement en prod). **Jamais dans le dépôt.** |
| Durée de vie | **12 h** (couvre une journée de tournée). Pas de refresh token en v1. **[À TRANCHER]** si le mode hors ligne l'exige (voir remarque). |
| Tolérance d'horloge | `ClockSkew` = 1 minute. |
| Claims | `sub` = id utilisateur (ObjectId), `email`, `role` = `RESPONSABLE_LOGISTIQUE` \| `LIVREUR`, plus les claims standard `iat`, `exp`, `iss` = `smartroute-api`, `aud` = `smartroute-mobile`. |
| Lecture des claims côté API | `JwtBearerOptions.MapInboundClaims = false` et `TokenValidationParameters.RoleClaimType = "role"`, pour que `[Authorize(Roles = "LIVREUR")]` fonctionne avec le claim `role` court. |
| Identité | L'API ne fait **jamais** confiance à un id utilisateur envoyé dans le corps : le livreur « me » est toujours le `sub` du token. |
| Stockage mobile | `expo-secure-store` (web : `localStorage`), avec l'utilisateur, via `src/storage/tokenStorage.ts`. |

Comportement attendu du front :

| Réponse | Où | Action du front |
|---|---|---|
| **401** | `POST /api/auth/login` | Identifiants refusés : message « Email ou mot de passe incorrect. », **pas** de déconnexion (on est déjà sur le Login). |
| **401** | toute autre route | Token absent, expiré ou invalide : **déconnexion** (effacement de la session) et retour au Login avec le message « Session expirée, veuillez vous reconnecter. » |
| **403** | toute route | Rôle insuffisant : message « Accès refusé. », pas de déconnexion. |

> **Remarque hors ligne.** Avec un token de 12 h sans refresh, un livreur resté hors ligne
> au-delà de l'expiration recevra un 401 au moment de la synchronisation. Les actions en
> attente (file `pending_actions` en SQLite, voir `src/storage/offline/README.md`) ne doivent
> **pas** être effacées par la déconnexion : elles seront rejouées après reconnexion du même
> utilisateur. Alternative : refresh token à durée longue. Plus complexe, à décider si le besoin est avéré.

---

## 3. Erreurs (ProblemDetails)

Toutes les erreurs suivent **ProblemDetails** (RFC 7807, remplacée par la RFC 9457 au contenu
compatible), le format standard d'ASP.NET Core (`AddProblemDetails()`, `ValidationProblemDetails`).
Réponse `Content-Type: application/problem+json`.

| Champ | Type | Rôle |
|---|---|---|
| `type` | string | URI du type d'erreur. Valeur par défaut d'ASP.NET Core acceptée. **Le front ne s'en sert pas.** |
| `title` | string | Résumé court, en français. |
| `status` | number | Code HTTP (redondant avec la réponse). |
| `detail` | string | **Message affichable à l'utilisateur**, en français. |
| `instance` | string | Chemin de la requête. |
| `code` | string | **Extension SmartRoute** : code machine stable (tableau ci-dessous). C'est ce que le front utilise pour décider. |
| `errors` | object | Uniquement pour les 400 de validation : `{ "chemin.du.champ": ["message", …] }`. |
| `traceId` | string | Ajouté par ASP.NET Core, utile au débogage. |

Exemple 400 de validation :

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Requête invalide",
  "status": 400,
  "detail": "Certains champs sont invalides.",
  "instance": "/api/routes/optimize",
  "code": "VALIDATION_ERROR",
  "errors": {
    "depot.label": ["L'adresse du dépôt est obligatoire."],
    "deliveries": ["Au moins 2 livraisons sont nécessaires."]
  },
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00"
}
```

Codes métier (`code`) :

| `code` | HTTP | Signification |
|---|---|---|
| `VALIDATION_ERROR` | 400 | Corps invalide (champs manquants, formats, bornes). `errors` renseigné. |
| `ADDRESS_NOT_FOUND` | 400 | Une adresse n'a pas pu être géocodée. `errors` indique laquelle (ex. `deliveries[2].label`). |
| `INVALID_CREDENTIALS` | 401 | Login refusé (même réponse si l'email est inconnu ou le mot de passe faux). |
| `UNAUTHORIZED` | 401 | Token absent, expiré ou invalide. |
| `FORBIDDEN` | 403 | Rôle insuffisant. |
| `NOT_FOUND` | 404 | Ressource inexistante **ou n'appartenant pas à l'appelant**. |
| `NO_ROUTE_TODAY` | 404 | Le livreur n'a pas de tournée ce jour. |
| `NO_ELIGIBLE_ROUTE` | 404 | Aucune tournée active ce jour ne peut recevoir la livraison. |
| `PROPOSAL_OUTDATED` | 409 | La tournée a changé depuis la proposition. Il faut relancer la recherche. |
| `ROUTE_COMPLETED` | 409 | La tournée ciblée est terminée. |

Les codes 500 et 503 restent possibles (erreur inattendue, service de géocodage indisponible).
Le front les traite comme `UNKNOWN` : « Le serveur a rencontré une erreur, réessayez. »

Les 401 et 403 générés par le middleware d'authentification n'ont pas de corps par défaut.
L'API doit les convertir en ProblemDetails (`JwtBearerEvents.OnChallenge` / `OnForbidden`,
ou `UseStatusCodePages` avec ProblemDetails) avec `code` = `UNAUTHORIZED` / `FORBIDDEN`.
Le front doit malgré tout **se fonder sur le statut HTTP** si le corps est vide.

---

## 4. Géocodage

Question : **qui transforme une adresse texte en latitude/longitude ?** Les distances
(optimisation, surcoût d'insertion) exigent des coordonnées.

| Option | Principe | Avantages | Inconvénients |
|---|---|---|---|
| **A. API seule** *(ta proposition)* | Le mobile envoie `label` seul. L'API appelle un service de géocodage et met les résultats en cache dans MongoDB. | Une seule source de vérité. Clé éventuelle côté serveur, jamais exposée. Coordonnées cohérentes et contrôlées. Front simple. | Latence ajoutée à chaque appel non caché. Dépendance à un service externe (quotas, pannes → 503). L'utilisateur ne découvre une adresse introuvable **qu'après** l'envoi. |
| **B. Mobile seul** | Le mobile géocode (`expo-location` `geocodeAsync`, disponible dans Expo Go) et envoie `label` + coordonnées. | Pas de dépendance côté API. | Résultats différents selon la plateforme (géocodeur iOS ≠ Android, **indisponible sur le web**). L'API doit faire confiance à des coordonnées fournies par le client. Clé exposée dans le bundle si service payant. Pas de cache partagé. |
| **C. Autocomplétion côté mobile + vérification API** | Le mobile propose des suggestions (ex. API Adresse) ; l'utilisateur choisit ; le mobile envoie `label` + coordonnées ; l'API les utilise si présentes, sinon géocode (A). | Adresses validées par l'utilisateur **avant** l'envoi : moins d'erreurs. Bonne expérience de saisie. | Plus de travail UI (liste de suggestions, debounce). Coordonnées fournies par le client : l'API doit au minimum vérifier les bornes, éventuellement re-géocoder. |
| **D. Adresses pré-géocodées en base** | Dépôts et clients connus stockés avec leurs coordonnées. | Aucun appel externe pour les adresses connues. | Ne couvre pas les adresses nouvelles, donc A ou C reste nécessaire. |

Fournisseur possible : **API Adresse / Base Adresse Nationale** (`api-adresse.data.gouv.fr`) :
gratuite, sans clé, très précise en France, et elle gère aussi l'autocomplétion (utile pour C).
Limite : France uniquement, ce qui convient aux tournées Mulhouse, Strasbourg et Colmar.
Nominatim (OpenStreetMap) est une alternative mondiale, mais sa politique d'usage est stricte
(1 requête par seconde, pas d'usage massif).

**Recommandation** : **A maintenant**, avec la BAN et un cache MongoDB (collection `geocodeCache`, clé =
libellé normalisé). Le contrat accepte **déjà** `latitude`/`longitude` optionnels en entrée,
ce qui permet de passer à **C** plus tard sans changer d'endpoint.
Règle d'entrée : si l'une des deux coordonnées est fournie, l'autre est obligatoire.
Si les deux sont fournies et valides, l'API les utilise telles quelles.

**[À TRANCHER]** : A, C ou autre.

---

## 5. POST /api/auth/login

**Rôle requis** : aucun (anonyme).

### Requête

```json
{ "email": "livreur@smartroute.fr", "password": "••••••••" }
```

| Champ | Validation |
|---|---|
| `email` | Obligatoire, format email, ≤ 254 caractères. Comparaison insensible à la casse (normalisé en minuscules). |
| `password` | Obligatoire, 1 à 128 caractères. Vérifié contre un hash (BCrypt ou `PasswordHasher<T>` d'ASP.NET Core Identity). |

### Réponse 200

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI2NmYxYzJhOWU0YjBhMWQyYzNmNDBhMTEiLCJlbWFpbCI6ImxpdnJldXJAc21hcnRyb3V0ZS5mciIsInJvbGUiOiJMSVZSRVVSIiwiaWF0IjoxNzkwNzQ4OTAwLCJleHAiOjE3OTA3OTIxMDAsImlzcyI6InNtYXJ0cm91dGUtYXBpIiwiYXVkIjoic21hcnRyb3V0ZS1tb2JpbGUifQ.<signature>",
  "expiresAt": "2026-09-30T18:15:00Z",
  "user": {
    "id": "66f1c2a9e4b0a1d2c3f40a11",
    "email": "livreur@smartroute.fr",
    "fullName": "Pierre Dubois",
    "role": "LIVREUR",
    "driverCode": "4092"
  }
}
```

`driverCode` n'est présent que pour un `LIVREUR`.

Payload décodé du token ci-dessus :

```json
{
  "sub": "66f1c2a9e4b0a1d2c3f40a11",
  "email": "livreur@smartroute.fr",
  "role": "LIVREUR",
  "iat": 1790748900,
  "exp": 1790792100,
  "iss": "smartroute-api",
  "aud": "smartroute-mobile"
}
```

`exp` − `iat` = 43 200 s = 12 h. `expiresAt` reprend `exp` au format ISO 8601.

### Codes

| Code | Cas |
|---|---|
| 200 | Connexion réussie. |
| 400 | `VALIDATION_ERROR` (email mal formé, champ vide). |
| 401 | `INVALID_CREDENTIALS` : `detail` = « Email ou mot de passe incorrect. » Même réponse pour un email inconnu (pas d'énumération des comptes). |
| 403 | *Optionnel* : compte désactivé (`code` = `FORBIDDEN`, `detail` = « Compte désactivé. »). |
| 404, 409 | Non applicables. |

Recommandé mais hors du périmètre des codes listés : limitation de débit sur cette route (réponse 429).

---

## 6. POST /api/routes/optimize

**Rôle requis** : `RESPONSABLE_LOGISTIQUE`.

**Calcul seul** : rien n'est enregistré. L'API géocode les adresses, calcule la distance dans
l'ordre saisi, puis applique Nearest Neighbor et 2-opt, et renvoie l'ordre optimisé.
**Aucun algorithme côté mobile.**

### Requête

```json
{
  "name": "Tournée Mulhouse matin",
  "depot": { "label": "10 rue de l'Industrie, 68100 Mulhouse" },
  "deliveries": [
    { "label": "12 rue de Bâle, 68100 Mulhouse" },
    { "label": "5 avenue Kennedy, 68100 Mulhouse" },
    { "label": "21 rue de Colmar, 68100 Mulhouse" },
    { "label": "8 rue de Strasbourg, 68100 Mulhouse" }
  ]
}
```

| Champ | Validation |
|---|---|
| `name` | Optionnel, ≤ 100 caractères. Si absent, l'API renvoie `"routeName": "Nouvelle tournée"`. |
| `depot` | Obligatoire. `depot.label` : 5 à 200 caractères. |
| `deliveries` | Obligatoire, **2 à 50** éléments. Chaque `label` : 5 à 200 caractères. Les doublons exacts (libellé normalisé) sont refusés (`VALIDATION_ERROR`). |
| `latitude` / `longitude` (toute adresse) | Optionnels. Si l'un est fourni, l'autre aussi. Bornes : latitude −90 à 90, longitude −180 à 180. |

### Réponse 200

`orderedStops[].inputIndex` renvoie à la position dans `deliveries` (base 0) : le front
peut ainsi relier le résultat à sa saisie sans comparer des libellés.

```json
{
  "routeName": "Tournée Mulhouse matin",
  "depot": { "label": "10 rue de l'Industrie, 68100 Mulhouse", "latitude": 47.7508, "longitude": 7.3359 },
  "orderedStops": [
    { "order": 1, "inputIndex": 0, "address": { "label": "12 rue de Bâle, 68100 Mulhouse", "latitude": 47.7447, "longitude": 7.3431 }, "legDistanceKm": 0.9 },
    { "order": 2, "inputIndex": 3, "address": { "label": "8 rue de Strasbourg, 68100 Mulhouse", "latitude": 47.7494, "longitude": 7.3399 }, "legDistanceKm": 0.6 },
    { "order": 3, "inputIndex": 2, "address": { "label": "21 rue de Colmar, 68100 Mulhouse", "latitude": 47.7538, "longitude": 7.3346 }, "legDistanceKm": 0.6 },
    { "order": 4, "inputIndex": 1, "address": { "label": "5 avenue Kennedy, 68100 Mulhouse", "latitude": 47.7466, "longitude": 7.3312 }, "legDistanceKm": 0.8 }
  ],
  "returnToDepotKm": 0.5,
  "distanceBeforeKm": 4.3,
  "distanceAfterKm": 3.4,
  "gainKm": 0.9,
  "gainPercent": 21
}
```

Invariants :
- `distanceAfterKm` = somme des `legDistanceKm` + `returnToDepotKm` ;
- `gainKm` = `distanceBeforeKm − distanceAfterKm` ≥ 0 ;
- `gainPercent` = `round(gainKm / distanceBeforeKm × 100)`, et 0 si `distanceBeforeKm` = 0.

### Codes

| Code | Cas |
|---|---|
| 200 | Optimisation calculée. |
| 400 | `VALIDATION_ERROR`, ou `ADDRESS_NOT_FOUND` avec `errors`, ex. `{ "deliveries[2].label": ["Adresse introuvable."] }`. |
| 401 | `UNAUTHORIZED`. |
| 403 | `FORBIDDEN` (appel par un `LIVREUR`). |
| 404, 409 | Non applicables (aucune ressource persistée n'est manipulée). |

**[À TRANCHER]** : l'optimisation devra-t-elle un jour **créer** une tournée (statut `DRAFT`)
à affecter à un livreur ? Aucun endpoint de ce contrat ne crée de tournée : celles de
`GET /api/routes/me` proviennent pour l'instant de données initiales (seed) MongoDB.

---

## 7. Affectation d'une livraison

L'affectation se fait **en deux temps**, comme dans la maquette :

1. `assign-best-driver` calcule une **proposition**. **Rien n'est enregistré**, pas même la proposition.
2. `confirm-assignment` enregistre la livraison dans la tournée choisie, **après revérification**.

Pour que la proposition reste « sans état » tout en empêchant une confirmation sur des données
périmées, chaque tournée porte un compteur **`version`**, incrémenté à chaque modification
(ajout d'arrêt, arrêt livré). La proposition renvoie la `version` observée ; la confirmation la
renvoie, et l'API refuse avec **409** si la tournée a changé entre-temps.

Ce mécanisme protège aussi contre la **double confirmation** : après une première
confirmation, la version a changé, et un second envoi reçoit 409 au lieu de créer un doublon.

### 7.1 POST /api/deliveries/assign-best-driver

**Rôle requis** : `RESPONSABLE_LOGISTIQUE`.

#### Requête

```json
{
  "address": { "label": "14 rue de l'Industrie, 67000 Strasbourg" },
  "parcelType": "STANDARD",
  "instructions": "Sonner à l'accueil"
}
```

| Champ | Validation |
|---|---|
| `address.label` | Obligatoire, 5 à 200 caractères. Même règle de coordonnées qu'au § 6. |
| `parcelType` | Optionnel, `STANDARD` \| `FRAGILE`. `STANDARD` par défaut. |
| `instructions` | Optionnel, ≤ 200 caractères. |

L'API considère toutes les tournées **du jour** (Europe/Paris) dont le statut est `ASSIGNED` ou
`IN_PROGRESS` et qui ont un livreur. Pour chacune, elle calcule le meilleur surcoût
d'insertion (Cheapest Insertion) **après le dernier arrêt livré**, puis retient le plus faible.

#### Réponse 200

```json
{
  "delivery": {
    "address": { "label": "14 rue de l'Industrie, 67000 Strasbourg", "latitude": 48.5780, "longitude": 7.7840 },
    "parcelType": "STANDARD",
    "instructions": "Sonner à l'accueil"
  },
  "routeId": "66f1c2a9e4b0a1d2c3f40b01",
  "routeName": "Tournée Strasbourg centre",
  "routeVersion": 7,
  "driver": { "id": "66f1c2a9e4b0a1d2c3f40a11", "fullName": "Pierre Dubois", "driverCode": "4092" },
  "insertionPosition": 3,
  "extraDistanceKm": 2.4
}
```

- `delivery` n'a **ni `id` ni `parcelRef`**, puisque rien n'est encore créé.
- `insertionPosition` : ordre qu'aura le nouvel arrêt, à partir de 1. Les arrêts suivants seront décalés de +1.

#### Codes

| Code | Cas |
|---|---|
| 200 | Proposition calculée. |
| 400 | `VALIDATION_ERROR`, ou `ADDRESS_NOT_FOUND`. |
| 401 | `UNAUTHORIZED`. |
| 403 | `FORBIDDEN`. |
| 404 | `NO_ELIGIBLE_ROUTE` : aucune tournée active aujourd'hui. `detail` = « Aucune tournée active ne peut recevoir cette livraison. » |
| 409 | Non applicable (lecture seule). |

### 7.2 POST /api/deliveries/confirm-assignment

**Rôle requis** : `RESPONSABLE_LOGISTIQUE`.

Le front renvoie les **éléments de la proposition**. L'API ne leur fait pas confiance
aveuglément : elle revérifie la tournée, sa version et la position, puis crée la livraison.

#### Requête

```json
{
  "routeId": "66f1c2a9e4b0a1d2c3f40b01",
  "routeVersion": 7,
  "insertionPosition": 3,
  "delivery": {
    "address": { "label": "14 rue de l'Industrie, 67000 Strasbourg", "latitude": 48.5780, "longitude": 7.7840 },
    "parcelType": "STANDARD",
    "instructions": "Sonner à l'accueil"
  }
}
```

| Champ | Validation |
|---|---|
| `routeId` | Obligatoire, ObjectId valide (sinon 400). |
| `routeVersion` | Obligatoire, entier ≥ 0. |
| `insertionPosition` | Obligatoire, entier, entre (nombre d'arrêts livrés + 1) et (nombre d'arrêts + 1). |
| `delivery` | Obligatoire. Mêmes règles qu'au § 7.1. `latitude`/`longitude` obligatoires ici (ce sont celles de la proposition). |

`extraDistanceKm` n'est **pas** renvoyé : l'API le recalcule elle-même si elle en a besoin.

#### Réponse 200

```json
{
  "deliveryId": "66f1c2a9e4b0a1d2c3f40c55",
  "parcelRef": "8805",
  "routeId": "66f1c2a9e4b0a1d2c3f40b01",
  "routeVersion": 8,
  "insertionPosition": 3,
  "driverName": "Pierre Dubois"
}
```

`parcelRef` est attribué par l'API (compteur séquentiel).

#### Codes

| Code | Cas |
|---|---|
| 200 | Livraison créée et insérée. |
| 400 | `VALIDATION_ERROR` (position hors bornes, champs manquants). |
| 401 | `UNAUTHORIZED`. |
| 403 | `FORBIDDEN`. |
| 404 | `NOT_FOUND` : tournée inexistante. |
| 409 | `PROPOSAL_OUTDATED` : `routeVersion` différente de la version en base (tournée modifiée ou déjà confirmée). `detail` = « La tournée a changé depuis la proposition. Relancez la recherche. » |
| 409 | `ROUTE_COMPLETED` : tournée terminée entre-temps. |

---

## 8. GET /api/routes/me

**Rôle requis** : `LIVREUR`.

Renvoie la tournée **du jour** (Europe/Paris) du livreur identifié par le `sub` du token.

### Réponse 200

```json
{
  "id": "66f1c2a9e4b0a1d2c3f40b01",
  "name": "Tournée Strasbourg centre",
  "date": "2026-09-30",
  "status": "IN_PROGRESS",
  "version": 7,
  "depot": {
    "id": "66f1c2a9e4b0a1d2c3f40d01",
    "name": "Dépôt Strasbourg",
    "address": { "label": "2 rue du Port du Rhin, 67100 Strasbourg", "latitude": 48.5734, "longitude": 7.7931 }
  },
  "driver": {
    "id": "66f1c2a9e4b0a1d2c3f40a11",
    "email": "livreur@smartroute.fr",
    "fullName": "Pierre Dubois",
    "role": "LIVREUR",
    "driverCode": "4092"
  },
  "stops": [
    {
      "id": "66f1c2a9e4b0a1d2c3f40e01",
      "order": 1,
      "status": "DELIVERED",
      "deliveredAt": "2026-09-30T06:42:00Z",
      "delivery": {
        "id": "66f1c2a9e4b0a1d2c3f40c01",
        "parcelRef": "8801",
        "parcelType": "STANDARD",
        "address": { "label": "12 rue Exemple, 67000 Strasbourg", "latitude": 48.5801, "longitude": 7.7502 }
      }
    },
    {
      "id": "66f1c2a9e4b0a1d2c3f40e02",
      "order": 2,
      "status": "PENDING",
      "delivery": {
        "id": "66f1c2a9e4b0a1d2c3f40c02",
        "parcelRef": "8802",
        "parcelType": "STANDARD",
        "address": { "label": "25 avenue des Vosges, 67000 Strasbourg", "latitude": 48.5905, "longitude": 7.7518 },
        "instructions": "Bâtiment B, Interphone 14"
      }
    }
  ]
}
```

Règles :
- `stops` est **trié par `order` croissant**, avec des ordres consécutifs à partir de 1 ;
- `deliveredAt` est présent si et seulement si `status` = `DELIVERED` ;
- transitions de `status` de la tournée : `ASSIGNED` → `IN_PROGRESS` (au premier arrêt livré) → `COMPLETED` (quand tous les arrêts sont livrés). `DRAFT` n'est jamais renvoyé ici.

### Codes

| Code | Cas |
|---|---|
| 200 | Tournée du jour. |
| 401 | `UNAUTHORIZED`. |
| 403 | `FORBIDDEN` (appel par un responsable). |
| 404 | `NO_ROUTE_TODAY` : aucune tournée affectée aujourd'hui. **Le front affiche l'état vide** (« Aucune tournée aujourd'hui »), pas une erreur. |
| 400, 409 | Non applicables. |

---

## 9. PATCH /api/routes/me/stops/{stopId}/deliver

**Rôle requis** : `LIVREUR`.

Marque un arrêt de **la tournée du livreur connecté** comme livré.

### Requête

`stopId` : ObjectId de l'arrêt, dans l'URL. Corps :

```json
{ "deliveredAt": "2026-09-30T07:05:12Z" }
```

| Champ | Validation |
|---|---|
| `deliveredAt` | **Optionnel.** Heure réelle de la livraison (UTC), fournie par le mobile, **indispensable pour le mode hors ligne** : une livraison faite à 9 h et synchronisée à 11 h doit garder 9 h. Si absent, l'API utilise l'heure du serveur. Refusé (400) si dans le futur de plus de 5 minutes, ou antérieur au jour de la tournée. |

### Idempotence

Rejouer la requête (après une synchronisation hors ligne, un double tap, une nouvelle tentative réseau) **ne doit jamais échouer** :

| État de l'arrêt en base | Effet | Réponse |
|---|---|---|
| `PENDING` | Passe à `DELIVERED`, `deliveredAt` enregistré, `version` de la tournée +1, statut de la tournée mis à jour. | 200 + arrêt à jour |
| `DELIVERED` | **Aucune modification**. Le **premier** `deliveredAt` est conservé, même si la requête en contient un autre. | 200 + arrêt inchangé |

L'API **n'impose pas l'ordre de passage** : un livreur peut devoir livrer l'arrêt 4 avant le 3,
et une file hors ligne peut être rejouée dans un ordre quelconque. **[À TRANCHER]** si l'ordre
strict est souhaité (ce serait alors un 409).

### Réponse 200

L'arrêt (type `RouteStop`), plus le statut et la version de la tournée pour que le front les mette à jour sans recharger :

```json
{
  "stop": {
    "id": "66f1c2a9e4b0a1d2c3f40e02",
    "order": 2,
    "status": "DELIVERED",
    "deliveredAt": "2026-09-30T07:05:12Z",
    "delivery": {
      "id": "66f1c2a9e4b0a1d2c3f40c02",
      "parcelRef": "8802",
      "parcelType": "STANDARD",
      "address": { "label": "25 avenue des Vosges, 67000 Strasbourg", "latitude": 48.5905, "longitude": 7.7518 },
      "instructions": "Bâtiment B, Interphone 14"
    }
  },
  "routeStatus": "IN_PROGRESS",
  "routeVersion": 8
}
```

### Codes

| Code | Cas |
|---|---|
| 200 | Arrêt livré (ou déjà livré : idempotent). |
| 400 | `VALIDATION_ERROR` : `deliveredAt` mal formé ou hors bornes. |
| 401 | `UNAUTHORIZED`. Hors ligne : l'action reste dans la file et sera rejouée après reconnexion. |
| 403 | `FORBIDDEN` (appel par un responsable). |
| 404 | `NOT_FOUND` : `stopId` inexistant, mal formé, **ou appartenant à la tournée d'un autre livreur** (404 plutôt que 403 pour ne pas révéler son existence). |
| 409 | Non applicable : un arrêt déjà livré renvoie 200. |

---

## 10. Écarts avec le front actuel

Rien n'a été modifié dans `src/`. Liste à valider avant adaptation.

### Erreurs et authentification

| # | Fichier | Actuel | Contrat | Changement proposé |
|---|---|---|---|---|
| E1 | `src/services/http/httpClient.ts` | Lit `message ?? title`. | Le message affichable est `detail`. | Lire `detail ?? title`, plus `code` et `errors`. |
| E2 | `src/services/http/httpClient.ts` | Tout 401 → `INVALID_CREDENTIALS`. | 401 sur le login = identifiants refusés ; 401 ailleurs = session expirée. | Utiliser `code` du ProblemDetails, et à défaut le statut : 401 hors login → `UNAUTHORIZED`, 403 → `FORBIDDEN`. |
| E3 | `httpClient.ts` + `AuthContext.tsx` | Aucune déconnexion automatique. | 401 → le front se déconnecte. | Callback `onUnauthorized` enregistré par `AuthProvider` dans le client HTTP, appelé sur un 401 hors login. |
| E4 | `src/types/api.ts` | `ApiErrorCode` : `OPTIMIZATION_FAILED`, `ASSIGNMENT_FAILED`… | Codes du § 3. | Ajouter `UNAUTHORIZED`, `FORBIDDEN`, `ADDRESS_NOT_FOUND`, `NO_ROUTE_TODAY`, `NO_ELIGIBLE_ROUTE`, `PROPOSAL_OUTDATED`, `ROUTE_COMPLETED`. Retirer `OPTIMIZATION_FAILED` et `ASSIGNMENT_FAILED`. Garder `NETWORK_ERROR` et `UNKNOWN` (côté client uniquement). |
| E5 | `src/types/api.ts` | `ApiError` sans détail par champ. | `errors` de validation. | Ajouter `fieldErrors?: Record<string, string[]>` à `ApiError`. |
| E6 | `src/types/auth.ts` | `LoginResponse = { token, user }`. | + `expiresAt`. | Ajouter `expiresAt: string` (utile pour anticiper l'expiration hors ligne). |
| E7 | mocks | Statuts 422 et 500. | 400 / 404 / 409. | Aligner les mocks sur les codes et statuts du contrat. |

### Optimisation

| # | Fichier | Actuel | Contrat | Changement proposé |
|---|---|---|---|---|
| O1 | `src/types/route.ts` | `OptimizeRouteResult.orderedStops: RouteStop[]`, qui exige `id`, `status`, `delivery.id`, `parcelRef`, `parcelType`. | Une optimisation ne manipule que des adresses : pas de colis ni d'identifiants. Le mock les **invente** aujourd'hui. | Nouveau type `OptimizedStop { order, inputIndex, address, legDistanceKm }`. `StopCard` variante `plain` → petit composant dédié ou adaptation. |
| O2 | `src/types/route.ts` | `OptimizeRouteResult.depot: Depot` (avec `id`, `name`). | `depot: Address` (dépôt non persisté). | Changer le type. |
| O3 | `src/types/route.ts` | Pas de `returnToDepotKm`. | Présent. | Ajouter. |
| O4 | `routeService.mock.ts` | Accepte 1 livraison. | 2 à 50. | Aligner le mock (l'écran impose déjà 2). |

### Affectation

| # | Fichier | Actuel | Contrat | Changement proposé |
|---|---|---|---|---|
| A1 | `src/types/delivery.ts` | `AssignDeliveryRequest = { address }`. | + `parcelType?`, `instructions?`. | Ajouter. **[À TRANCHER]** : afficher ces deux champs dans le formulaire du Dashboard (absents de la maquette) ou envoyer `STANDARD` par défaut. |
| A2 | `src/types/delivery.ts` | `AssignmentProposal.delivery: Delivery` (avec `id`, `parcelRef`). | Rien n'est créé : ni `id` ni `parcelRef`. | Type `DeliveryDraft { address, parcelType, instructions? }`. La ligne « Livraison : Colis #8805 » de l'encart devient l'adresse ou disparaît. |
| A3 | `src/types/delivery.ts` | Pas de `routeVersion`. | Présent. | Ajouter. |
| A4 | `src/types/delivery.ts` | `driver: User` (avec `email`, `role`). | `driver: { id, fullName, driverCode? }`. | Type `DriverSummary`. |
| A5 | `deliveryService.ts` | `confirmAssignment(proposal)` envoie **toute** la proposition. | Corps = `{ routeId, routeVersion, insertionPosition, delivery }`. | Type `ConfirmAssignmentRequest`, construit à partir de la proposition dans le service. |
| A6 | `src/types/delivery.ts` | `AssignmentConfirmation = { deliveryId, routeId, driverName }`. | + `parcelRef`, `routeVersion`, `insertionPosition`. | Ajouter (le message de succès peut afficher « Colis #8805 »). |
| A7 | `ManagerDashboardScreen.tsx` | 409 affiché comme une erreur générique. | `PROPOSAL_OUTDATED` → relancer la recherche. | Sur ce code : effacer la proposition et afficher le message avec l'action « Relancer la recherche ». |

### Tournée du livreur

| # | Fichier | Actuel | Contrat | Changement proposé |
|---|---|---|---|---|
| R1 | `src/types/route.ts` | `Route` sans `version`. | `version: number`. | Ajouter. |
| R2 | `src/types/route.ts` | `Route.date` commenté « Date ISO ». | Date calendaire `YYYY-MM-DD` (Europe/Paris). | Préciser le commentaire. Le mock le respecte déjà. |
| R3 | `routeService.ts` | `markStopDelivered(stopId)`, sans corps. | Corps `{ deliveredAt }` pour le hors ligne. | `markStopDelivered(stopId, deliveredAt?: string)`. `useMyRoute` passe l'heure de l'action optimiste. |
| R4 | `routeService.ts` | Renvoie `RouteStop`. | Renvoie `{ stop, routeStatus, routeVersion }`. | Type `DeliverStopResult`. `useMyRoute` met à jour statut et version. |
| R5 | `useMyRoute.ts` / `MyRouteScreen.tsx` | 404 affiché comme une erreur. | `NO_ROUTE_TODAY` = état vide. | Brancher ce code sur l'`EmptyState` existant. |
| R6 | `routeService.mock.ts` | Le statut reste `ASSIGNED` après une livraison (choix de fidélité à la maquette). | `ASSIGNED` → `IN_PROGRESS` au premier arrêt livré. | Aligner le mock. Conséquence : le badge de la maquette (« Affectée » avec un arrêt déjà livré) affichera « En cours ». |
| R7 | `src/types/route.ts` | `RouteStatus` inclut `DRAFT`. | Jamais renvoyé par ce contrat. | Garder pour l'avenir (voir [À TRANCHER] du § 6) ou retirer. |

### Sans impact

- **Identifiants des mocks** (`"u-liv-4092"`, `"stop-8801"`…) : ce ne sont pas des ObjectId, mais le front ne fait aucune hypothèse sur leur forme. Aucun changement nécessaire.
- **`Address`** (`label`, `latitude?`, `longitude?`) : déjà conforme.
- **`User`, `UserRole`, `StopStatus`, `ParcelType`, `Depot`** : déjà conformes.

---

## Décisions à prendre

1. **Géocodage** (§ 4) : option A recommandée (API + BAN + cache), évolutive vers C.
2. **`Route.date`** en date calendaire plutôt qu'instant UTC (§ 1) : recommandé.
3. **Durée du JWT** : 12 h sans refresh (§ 2), à revoir avec le mode hors ligne.
4. **Optimisation** : calcul seul, ou création d'une tournée `DRAFT` (§ 6).
5. **Ordre de passage imposé** ou non pour `deliver` (§ 9) : non imposé recommandé.
6. **`parcelType` / `instructions`** dans le formulaire d'affectation (A1).
