using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartRoute.Api.Models;
using SmartRoute.Api.Services;

namespace SmartRoute.Api.Controllers;

[ApiController]
[Route("api/test-geocoding")]
[AllowAnonymous]
public class TestGeocodingController : ControllerBase
{
    private readonly GeocodingService _geocodingService;
    private readonly DistanceService _distanceService;

    public TestGeocodingController(
        GeocodingService geocodingService,
        DistanceService distanceService)
    {
        _geocodingService = geocodingService;
        _distanceService = distanceService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string address)
    {
        var coordinates =
            await _geocodingService.GeocodeAsync(address);

        return Ok(new
        {
            latitude = coordinates.Latitude,
            longitude = coordinates.Longitude
        });
    }

    [HttpGet("test-matrix")]
    public IActionResult TestMatrix()
    {
        List<GeoPoint> points = new List<GeoPoint>
        {
            new GeoPoint
            {
                Label = "Depot",
                Latitude = 47.7508,
                Longitude = 7.3359
            },
            new GeoPoint
            {
                Label = "Point A",
                Latitude = 47.7447,
                Longitude = 7.3431
            },
            new GeoPoint
            {
                Label = "Point B",
                Latitude = 47.7494,
                Longitude = 7.3399
            }
        };

        double[,] matrix =
            _distanceService.BuildDistanceMatrix(points);

        double[][] result = new double[matrix.GetLength(0)][];

        for (int i = 0; i < matrix.GetLength(0); i++)
        {
            result[i] = new double[matrix.GetLength(1)];

            for (int j = 0; j < matrix.GetLength(1); j++)
            {
                result[i][j] = matrix[i, j];
            }
        }

        return Ok(result);
    }
}