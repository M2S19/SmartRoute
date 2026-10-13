using System.Collections.Generic;

namespace SmartRoute.Api.Algorithms;

public class CheapestInsertion
{
    public (int Position, double ExtraDistance) FindBestInsertion(
        int newPoint,
        double[,] matrix,
        List<int> route)
    {
        int bestInsertionPosition = -1;
        double bestExtraDistance = double.MaxValue;

        // On teste chaque segment de la tournée :
        // route[i] -> route[i + 1]
        for (int i = 0; i < route.Count - 1; i++)
        {
            int pointA = route[i];
            int pointB = route[i + 1];

            // Distance A -> nouvelle livraison
            double distanceA = matrix[pointA, newPoint];

            // Distance nouvelle livraison -> B
            double distanceB = matrix[newPoint, pointB];

            // Distance actuelle A -> B
            double originalDistance = matrix[pointA, pointB];

            // Surcoût provoqué par l'insertion
            double extraDistance =
                distanceA + distanceB - originalDistance;

            // Si cette insertion est meilleure que les précédentes,
            // on mémorise son coût et sa position.
            if (extraDistance < bestExtraDistance)
            {
                bestExtraDistance = extraDistance;

                // Le nouveau point sera inséré après route[i]
                bestInsertionPosition = i + 1;
            }
        }

        return (bestInsertionPosition, bestExtraDistance);
    }
}