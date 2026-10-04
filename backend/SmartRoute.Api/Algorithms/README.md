# Algorithms

Emplacement des algorithmes d'optimisation de tournées, écrits à la main :

- `NearestNeighbor` : construction d'une tournée initiale ;
- `TwoOpt` : amélioration locale de la tournée ;
- `CheapestInsertion` : insertion d'une nouvelle livraison au moindre surcoût.

Les signatures seront définies au moment de l'implémentation de chaque algorithme.

## Règles

- Code pur : aucune dépendance à ASP.NET Core, MongoDB, aux Controllers, aux Services ni aux requêtes HTTP.
- Entrée : des données simples (par exemple une matrice de distances) ; sortie : un résultat (par exemple un ordre d'indices).
- Testés dans `SmartRoute.Tests/Algorithms/`, sans base de données ni serveur.

La matrice de distances sera construite en amont par `Services/DistanceService` (Haversine), lui aussi écrit à la main.
