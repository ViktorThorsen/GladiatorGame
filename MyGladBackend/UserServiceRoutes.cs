using Npgsql;
namespace server;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Google.Apis.Auth;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

public static class UserServiceRoutes
{

    public static async Task<IResult> GoogleAuthHandler(HttpRequest request, NpgsqlDataSource db, string jwtKey)
    {
        Console.WriteLine($"in googleauth");
        var body = await request.ReadFromJsonAsync<GoogleLoginRequest>();

        string email;

#if DEBUG
        // MOCKAD användare i utvecklingsläge
        email = "galt@gmail.com";
#else
        var payload = await GoogleJsonWebSignature.ValidateAsync(body.IdToken);
        email = payload.Email;
#endif

        var userId = await GetOrCreateUserAsync(email, db);
        var key = Encoding.UTF8.GetBytes(jwtKey);
        // OBS: i produktion – hämta detta från configuration/env

        var tokenHandler = new JwtSecurityTokenHandler();

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] {
                new Claim("userId", userId.ToString())
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwt = tokenHandler.WriteToken(token);

        return Results.Ok(new { token = jwt, id = userId });
    }

    public static async Task<int> GetOrCreateUserAsync(string email, NpgsqlDataSource db)
    {
        Console.WriteLine($"✅ Användare med email {email}");

        await using var conn = await db.OpenConnectionAsync();

        // Först: kolla om användaren finns
        await using (var checkCmd = conn.CreateCommand())
        {
            checkCmd.CommandText = "SELECT id FROM users WHERE email = @email";
            checkCmd.Parameters.AddWithValue("email", email);
            var result = await checkCmd.ExecuteScalarAsync();

            if (result != null)
                return (int)result;
        }

        // Sedan: skapa användare
        await using (var insertCmd = conn.CreateCommand())
        {
            insertCmd.CommandText = "INSERT INTO users (email) VALUES (@email) RETURNING id";
            insertCmd.Parameters.AddWithValue("email", email);
            return (int)(await insertCmd.ExecuteScalarAsync())!;
        }
    }

    public static async Task<IResult> GetUserInfo(HttpContext context, NpgsqlDataSource db)
    {
        var userIdClaim = context.User.FindFirst("userId");
        if (userIdClaim == null)
            return Results.Unauthorized();

        int userId = int.Parse(userIdClaim.Value);

        await using var conn = await db.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT character FROM userxcharacter WHERE \"user\" = @userId LIMIT 1";
        cmd.Parameters.AddWithValue("userId", userId);

        var result = await cmd.ExecuteScalarAsync();

        bool hasGladiator = result != null && result != DBNull.Value;

        return Results.Ok(new { hasGladiator });
    }


    public static async Task<IResult> GetUserCharacters(HttpContext context, NpgsqlDataSource db)
    {
        var userIdClaim = context.User.FindFirst("userId");
        if (userIdClaim == null)
            return Results.Unauthorized();

        int userId = int.Parse(userIdClaim.Value);

        using var conn = await db.OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
        SELECT c.id, c.name, c.level
        FROM userxcharacter ux
        JOIN characters c ON ux.character = c.id
        WHERE ux.user = @userId";

        cmd.Parameters.AddWithValue("userId", userId);

        await using var reader = await cmd.ExecuteReaderAsync();

        var characters = new List<object>();
        while (await reader.ReadAsync())
        {
            characters.Add(new
            {
                id = reader.GetInt32(0),
                name = reader.GetString(1),
                level = reader.GetInt32(2)
            });
        }

        return Results.Ok(characters);
    }
}