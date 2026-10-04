using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace SmartRoute.Api.Controllers;

[ApiController]
[Route("api/mongo-health")]
public class MongoHealthController : ControllerBase
{
    private readonly IMongoDatabase _database;

    public MongoHealthController(IMongoDatabase database)
    {
        _database = database;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get()
    {
        try
        {
            var command = new BsonDocument("ping", 1);

            await _database.RunCommandAsync<BsonDocument>(command);

            return Ok(new
            {
                status = "MongoDB connection successful"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                status = "MongoDB connection failed",
                error = ex.Message
            });
        }
    }
}