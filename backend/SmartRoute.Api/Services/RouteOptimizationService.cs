using SmartRoute.Api.Algorithms;
using SmartRoute.Api.DTOs.Routes;
using SmartRoute.Api.Models;

namespace SmartRoute.Api.Services;

public class RouteOptimizationService
{
    private readonly GeocodingService _geocodingService;
    private readonly DistanceService _distanceService;
    private readonly NearestNeighbor _nearestNeighbor;

    public RouteOptimizationService(
        GeocodingService geocodingService,
        DistanceService distanceService,
        NearestNeighbor nearestNeighbor)
    {
        _geocodingService = geocodingService;
        _distanceService = distanceService;
        _nearestNeighbor = nearestNeighbor;
    }

    public async Task<OptimizeRouteResponse> OptimizeAsync(
        OptimizeRouteRequest request)
    {
        if (request.Stops.Count == 0)
        {
            throw new ArgumentException(
                "La tournée doit contenir au moins un arrêt.");
        }

        List<GeoPoint> points = new List<GeoPoint>();

        // 1. Géocoder le dépôt
        var depotCoordinates =
            await _geocodingService.GeocodeAsync(
                request.Depot.Label
            );

        points.Add(new GeoPoint
        {
            Label = request.Depot.Label,
            Latitude = depotCoordinates.Latitude,
            Longitude = depotCoordinates.Longitude
        });

        // 2. Géocoder les arrêts
        foreach (RouteAddressRequest stop in request.Stops)
        {
            var coordinates =
                await _geocodingService.GeocodeAsync(
                    stop.Label
                );

            points.Add(new GeoPoint
            {
                Label = stop.Label,
                Latitude = coordinates.Latitude,
                Longitude = coordinates.Longitude
            });
        }

        // 3. Construire la matrice des distances
        double[,] matrix =
            _distanceService.BuildDistanceMatrix(points);

        // 4. Tournée initiale :
        // dépôt → stop 1 → stop 2 → ... → dépôt
        List<int> originalRoute = new List<int>();

        for (int i = 0; i < points.Count; i++)
        {
            originalRoute.Add(i);
        }

        originalRoute.Add(0);

        double distanceBefore =
            CalculateRouteDistance(
                originalRoute,
                matrix
            );

        // 5. Optimisation Nearest Neighbor
        List<int> optimizedRoute =
            _nearestNeighbor.Optimize(matrix);

        double distanceAfter =
            CalculateRouteDistance(
                optimizedRoute,
                matrix
            );

        // 6. Calcul du gain
        double gainPercent = 0;

        if (distanceBefore > 0)
        {
            gainPercent =
                ((distanceBefore - distanceAfter)
                / distanceBefore) * 100;
        }

        // 7. Transformer les indices en résultat lisible
        List<OptimizedPointResponse> optimizedOrder =
            new List<OptimizedPointResponse>();

        for (int i = 0; i < optimizedRoute.Count; i++)
        {
            int pointIndex = optimizedRoute[i];

            GeoPoint point = points[pointIndex];

            optimizedOrder.Add(
                new OptimizedPointResponse
                {
                    Position = i,
                    Label = point.Label,
                    Latitude = point.Latitude,
                    Longitude = point.Longitude
                }
            );
        }

        return new OptimizeRouteResponse
        {
            DistanceBeforeKm = distanceBefore,
            DistanceAfterKm = distanceAfter,
            GainPercent = gainPercent,
            OptimizedOrder = optimizedOrder
        };
    }

    private double CalculateRouteDistance(
        List<int> route,
        double[,] matrix)
    {
        double totalDistance = 0;

        for (int i = 0; i < route.Count - 1; i++)
        {
            int pointA = route[i];
            int pointB = route[i + 1];

            totalDistance += matrix[pointA, pointB];
        }

        return totalDistance;
    }
}