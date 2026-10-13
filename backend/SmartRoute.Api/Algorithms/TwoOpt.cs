/*

using System.Collections.Generic;



using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace SmartRoute.Api.Algorithms;

public class TwoOpt
{
    private double CalculateRouteDistance(
        List<int> route,
        double[,] matrix)
    {
        double totalDistance = 0;

        for (int i = 0; i < route.Count - 1; i++)
        {
            int pointA = route[i];
            int pointB = route[i + 1];
            double distance = matrix[pointA, pointB];
            totalDistance += distance;

        }

        return totalDistance;
    }

    public List<int> Optimize(
    List<int> route,
    double[,] matrix)
    {
        List<int> bestRoute = new List<int>(route);
        double bestDistance = CalculateRouteDistance(bestRoute, matrix);
        for (int i = 1; i < route.Count - 2; i++)
        {
            for (int j = i + 1; j < route.Count - 1; j++)
            {
                double newRoute = matrix[route[i], route[j]];
                ......


                double newRouteDistance = CalculateRouteDistance(newRoute, matrix)
            }
        }

        if (newRouteDistance < bestRoute)
        {
            return newRouteDistance
        } else {
            return bestRoute
        }
        // copie de la route
        // calcul distance actuelle
        // boucles i / j
        // test d'inversion
        // comparaison
    }
} */