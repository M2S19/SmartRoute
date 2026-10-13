using SmartRoute.Api.Algorithms;
using Xunit;

namespace SmartRoute.Tests.Algorithms;

public class CheapestInsertionTests
{
    [Fact]
    public void FindBestInsertion_ShouldReturnBestPosition()
    {
        double[,] matrix =
        {
            { 0, 5, 8, 4 },
            { 5, 0, 3, 1 },
            { 8, 3, 0, 2 },
            { 4, 1, 2, 0 }
        };

        List<int> route = new List<int> { 0, 1, 2, 0 };

        CheapestInsertion algorithm = new CheapestInsertion();

        var result = algorithm.FindBestInsertion(
            3,
            matrix,
            route
        );

        Assert.Equal(3, result.Position);
        Assert.Equal(-2.0, result.ExtraDistance);
    }
}