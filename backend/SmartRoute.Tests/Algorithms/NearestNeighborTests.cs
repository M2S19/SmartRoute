using SmartRoute.Api.Algorithms;
using Xunit;

namespace SmartRoute.Tests.Algorithms;

public class NearestNeighborTests
{
    [Fact]
    public void Optimize_ShouldReturnNearestNeighborRoute()
    {
        double[,] matrix =
        {
            { 0,      0.8659, 0.3371 },
            { 0.8659, 0,      0.5748 },
            { 0.3371, 0.5748, 0      }
        };

        NearestNeighbor algorithm = new NearestNeighbor();

        List<int> route = algorithm.Optimize(matrix);

        Assert.Equal(new[] { 0, 2, 1, 0 }, route);
    }

    [Fact]
    public void Optimize_ShouldHandleSeveralSteps()
    {
        double[,] matrix =
        {
        { 0, 5, 2, 8 },
        { 5, 0, 4, 1 },
        { 2, 4, 0, 3 },
        { 8, 1, 3, 0 }
    };

        NearestNeighbor algorithm = new NearestNeighbor();

        List<int> route = algorithm.Optimize(matrix);

        Assert.Equal(new[] { 0, 2, 3, 1, 0 }, route);
    }
}