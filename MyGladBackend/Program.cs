using Npgsql;
using server;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Google.Apis.Auth;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var jwtKey = "D7skJ!38dF6g0Ql^19Xm28j@NsX#LpZ2";
builder.Services.AddSingleton(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();



// Registrera tjänster här, innan builder.Build()
var dataSourceBuilder = new NpgsqlDataSourceBuilder("Host=89.116.27.193;Database=my_glad_db;Username=grisviktor;Password=admin132;Port=5432;Search Path=public");
var db = dataSourceBuilder.Build();
builder.Services.AddSingleton<NpgsqlDataSource>(db); // <-- måste ligga här

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

// Mappar efter
app.MapPost("/api/characters", CharacterRoutes.AddCharacter);
app.MapGet("/api/characters/{characterId}", CharacterRoutes.GetCharacterByCharacterId);
app.MapGet("/api/characters/random", CharacterRoutes.GetRandomCharacterName);
app.MapGet("/api/users/me", UserServiceRoutes.GetUserInfo).RequireAuthorization();
app.MapGet("/api/user/characters", UserServiceRoutes.GetUserCharacters).RequireAuthorization();
app.MapPost("/api/user/characters", CharacterRoutes.LinkCharacterToUser).RequireAuthorization();
app.MapGet("/api/characters/energy", CharacterRoutes.UpdateCharacterEnergy).RequireAuthorization();
app.MapPost("/api/characters/useenergy", CharacterRoutes.UseEnergy).RequireAuthorization();
app.MapGet("/api/monsterhunt", MonsterHuntRoutes.GetMonsterHuntInfo).RequireAuthorization();
app.MapPut("/api/monsterhunt", MonsterHuntRoutes.UpdateMonsterHuntStage).RequireAuthorization();
app.MapPost("/api/monsterhunt", MonsterHuntRoutes.AddMonsterHuntInfo).RequireAuthorization();
app.MapPost("/api/replays", ReplayRoutes.SaveReplay).RequireAuthorization();
app.MapGet("/api/replays/character", ReplayRoutes.GetReplaysForCharacter);
app.MapPost("/api/auth/google", (
    HttpRequest request,
    NpgsqlDataSource db,
    [FromServices] string jwtKey) =>
    UserServiceRoutes.GoogleAuthHandler(request, db, jwtKey));
app.Run();
