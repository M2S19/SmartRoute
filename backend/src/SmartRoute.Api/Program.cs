var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.Run();

// Exposé pour WebApplicationFactory<Program> dans les tests.
public partial class Program;
