using System.Collections.Generic;

namespace SmartRoute.Api.Algorithms;

public class NearestNeighbor
{
    public List<int> Optimize(double[,] matrix)
    {
        int size = matrix.GetLength(0);

        bool[] visited = new bool[size];
        List<int> route = new List<int>();

        int currentPoint = 0;

        // On commence au dépôt
        visited[0] = true;
        route.Add(0);

        // On choisit les autres points un par un
        for (int step = 0; step < size - 1; step++)
        {
            int nearestPoint = -1;
            double nearestDistance = double.MaxValue;

            // Recherche du point non visité le plus proche
            for (int j = 0; j < size; j++)
            {
                if (!visited[j])
                {
                    double distance = matrix[currentPoint, j];

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestPoint = j;
                    }
                }
            }

            // On sélectionne le meilleur candidat trouvé
            if (nearestPoint != -1)
            {
                visited[nearestPoint] = true;
                route.Add(nearestPoint);
                currentPoint = nearestPoint;
            }
        }

        // Retour au dépôt
        route.Add(0);

        return route;
    }
}