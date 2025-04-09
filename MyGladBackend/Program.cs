using Npgsql;
using server;

var builder = WebApplication.CreateBuilder(args);

// Registrera tjänster här, innan builder.Build()
var dataSourceBuilder = new NpgsqlDataSourceBuilder("Host=localhost;Database=my_glad_db;Username=postgres;Password=admin132;Port=5432");
var db = dataSourceBuilder.Build();
builder.Services.AddSingleton<NpgsqlDataSource>(db); // <-- måste ligga här

var app = builder.Build();

// Mappar efter
app.MapPost("/api/characters", CharacterRoutes.AddCharacter);
app.MapGet("/api/characters/{name}", CharacterRoutes.GetCharacterByName);

app.Run();
