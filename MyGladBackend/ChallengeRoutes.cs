using Npgsql;
namespace server;

using Microsoft.IdentityModel.Tokens;
using System.Text;
using Google.Apis.Auth;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;

public static class ChallengeRoutes
{
    public record ChallengeRequest(int ChallengerId, int OpponentId);

    public static async Task<IResult> AddChallenge(ChallengeRequest req, NpgsqlDataSource db)
    {
        await using var cmd = db.CreateCommand();
        cmd.CommandText = @"
        INSERT INTO challenges (challenger, opponent, date)
        VALUES (@challenger, @opponent, NOW())";

        cmd.Parameters.AddWithValue("challenger", req.ChallengerId);
        cmd.Parameters.AddWithValue("opponent", req.OpponentId);

        await cmd.ExecuteNonQueryAsync();
        return Results.Ok("Challenge created");
    }

    public static async Task<IResult> GetChallengesForCharacter([FromQuery] int characterId, NpgsqlDataSource db)
    {
        var pending = new List<object>();
        var received = new List<object>();

        // Pending (jag har skickat)
        await using (var cmd = db.CreateCommand())
        {
            cmd.CommandText = @"
        SELECT ch.id, c.name
        FROM challenges ch
        JOIN characters c ON ch.opponent = c.id
        WHERE ch.challenger = @id";
            cmd.Parameters.AddWithValue("id", characterId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                pending.Add(new
                {
                    id = reader.GetInt32(0),
                    opponentName = reader.GetString(1)
                });
            }
        }

        // Received (någon har utmanat mig)
        await using (var cmd = db.CreateCommand())
        {
            cmd.CommandText = @"
    SELECT ch.id, c.id, c.name, c.level
    FROM challenges ch
    JOIN characters c ON ch.challenger = c.id
    WHERE ch.opponent = @id";
            cmd.Parameters.AddWithValue("id", characterId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                received.Add(new
                {
                    id = reader.GetInt32(0),            // challenge id
                    challengerId = reader.GetInt32(1),  // needed for match
                    challengerName = reader.GetString(2),
                    challengerLevel = reader.GetInt32(3)
                });
            }
        }

        return Results.Json(new { pending, received });
    }

    public static async Task<IResult> DeleteChallenge([FromQuery] int id, NpgsqlDataSource db)
    {
        await using var cmd = db.CreateCommand();
        cmd.CommandText = "DELETE FROM challenges WHERE id = @id";
        cmd.Parameters.AddWithValue("id", id);

        int rowsAffected = await cmd.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
            return Results.NotFound("Ingen utmaning med det ID:t hittades.");

        return Results.Ok("Utmaning raderad.");
    }

    public static async Task<IResult> GetCharacterVisualsById([FromQuery] int id, NpgsqlDataSource db)
    {
        await using var cmd = db.CreateCommand();
        cmd.CommandText = @"
        SELECT c.name, c.level, bp.hair, bp.eyes, bp.chest
        FROM characters c
        JOIN body_parts bp ON bp.character = c.id
        WHERE c.id = @id";

        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var dto = new CharacterVisualDTO(
                reader.GetString(0), // name
                reader.GetInt32(1),  // level
                reader.GetString(2), // hair
                reader.GetString(3), // eyes
                reader.GetString(4)  // chest
            );

            return Results.Json(dto);
        }

        return Results.NotFound("Character not found");
    }

    public record CharacterVisualDTO(string name, int level, string hair, string eyes, string chest);

}