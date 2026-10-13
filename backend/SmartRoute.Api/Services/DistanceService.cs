using System;
using SmartRoute.Api.Models;


namespace SmartRoute.Api.Services;

public sealed class DistanceService
{
    // Calcule la distance entre deux points GPS en kilomètres
    // avec la formule de Haversine, en tenant compte de la courbure de la Terre.
    public double CalculateDistance(
     double latitude1,
     double longitude1,
     double latitude2,
     double longitude2)
    {
        double lat1Rad = latitude1 * Math.PI / 180;
        double lon1Rad = longitude1 * Math.PI / 180;

        double lat2Rad = latitude2 * Math.PI / 180;
        double lon2Rad = longitude2 * Math.PI / 180;

        double deltaLat = lat2Rad - lat1Rad;
        double deltaLon = lon2Rad - lon1Rad;

        double sinLat = Math.Sin(deltaLat / 2);
        double sinLon = Math.Sin(deltaLon / 2);

        double a =
            Math.Pow(sinLat, 2)
            + Math.Cos(lat1Rad)
            * Math.Cos(lat2Rad)
            * Math.Pow(sinLon, 2);

        double atan = Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        double c = 2 * atan;

        double R = 6371;
        double distance = R * c;

        return distance;
    }

    public double[,] BuildDistanceMatrix(List<GeoPoint> points)
    {
        int size = points.Count;
        double[,] matrix = new double[size, size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                GeoPoint pointA = points[i];
                GeoPoint pointB = points[j];

                double distance = CalculateDistance(
                    pointA.Latitude,
                    pointA.Longitude,
                    pointB.Latitude,
                    pointB.Longitude
                );

                matrix[i, j] = distance;
            }
        }

        return matrix;
    }


}