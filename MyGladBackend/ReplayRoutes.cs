using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Newtonsoft.Json;

namespace server;

public class ReplayRoutes
{
    public record MatchEventDTO(int Turn, string Actor, string Action, string Target, int Value);
    public record ReplayPayload(
        CharacterRoutes.CharacterWrapperDTO player,
        CharacterRoutes.CharacterWrapperDTO enemy,
        List<MatchEventDTO> actions,
        string mapName,
        string winner,
        string timestamp
    );

    public static async Task<IResult> SaveReplay(HttpContext context, NpgsqlDataSource db)
    {
        try
        {
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();

            var replay = JsonConvert.DeserializeObject<ReplayPayload>(body);
            if (replay == null)
                return Results.BadRequest("Invalid replay data");

            int replayId = -1;

            // === Först: spara replay och hämta ID ===
            await using (var cmd = db.CreateCommand())
            {
                cmd.CommandText = @"
                INSERT INTO replays (player_snapshot, enemy_snapshot, actions, map_name, winner, timestamp)
                VALUES (@player, @enemy, @actions, @map, @winner, @timestamp)
                RETURNING id
            ";

                cmd.Parameters.Add("player", NpgsqlTypes.NpgsqlDbType.Jsonb).Value = JsonConvert.SerializeObject(replay.player);
                cmd.Parameters.Add("enemy", NpgsqlTypes.NpgsqlDbType.Jsonb).Value = JsonConvert.SerializeObject(replay.enemy);
                cmd.Parameters.Add("actions", NpgsqlTypes.NpgsqlDbType.Jsonb).Value = JsonConvert.SerializeObject(replay.actions);
                cmd.Parameters.AddWithValue("map", replay.mapName);
                cmd.Parameters.AddWithValue("winner", replay.winner);
                cmd.Parameters.AddWithValue("timestamp", DateTime.Parse(replay.timestamp));

                await using var dbReader = await cmd.ExecuteReaderAsync();
                if (await dbReader.ReadAsync())
                    replayId = dbReader.GetInt32(0);
                else
                    return Results.Problem("Failed to save replay.");
            }

            // === Därefter: koppla karaktärer till replay ===
            if (!string.IsNullOrEmpty(replay.player.character.charName))
            {
                int? playerCharId = await FindCharacterIdByName(replay.player.character.charName, db);
                if (playerCharId.HasValue)
                {
                    using var linkCmd = db.CreateCommand("INSERT INTO characterxreplay (character_id, replay_id) VALUES (@charId, @replayId)");
                    linkCmd.Parameters.AddWithValue("charId", playerCharId.Value);
                    linkCmd.Parameters.AddWithValue("replayId", replayId);
                    await linkCmd.ExecuteNonQueryAsync();
                }
            }

            if (!string.IsNullOrEmpty(replay.enemy.character.charName))
            {
                int? enemyCharId = await FindCharacterIdByName(replay.enemy.character.charName, db);
                if (enemyCharId.HasValue)
                {
                    using var linkCmd = db.CreateCommand("INSERT INTO characterxreplay (character_id, replay_id) VALUES (@charId, @replayId)");
                    linkCmd.Parameters.AddWithValue("charId", enemyCharId.Value);
                    linkCmd.Parameters.AddWithValue("replayId", replayId);
                    await linkCmd.ExecuteNonQueryAsync();
                }
            }

            return Results.Ok("Replay saved and linked.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error saving replay: {ex.Message}");
            return Results.Problem("Internal server error when saving replay.");
        }
    }

    private static async Task<int?> FindCharacterIdByName(string name, NpgsqlDataSource db)
    {
        await using var cmd = db.CreateCommand("SELECT id FROM characters WHERE name = @name LIMIT 1");
        cmd.Parameters.AddWithValue("name", name);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return reader.GetInt32(0);

        return null;
    }

    public static async Task<IResult> GetReplaysForCharacter(int characterId, NpgsqlDataSource db)
    {
        var replays = new List<ReplayPayload>();

        await using var cmd = db.CreateCommand(@"
        SELECT r.player_snapshot, r.enemy_snapshot, r.actions, r.map_name, r.winner, r.timestamp
        FROM characterxreplay cxr
        JOIN replays r ON cxr.replay_id = r.id
        WHERE cxr.character_id = @characterId
        ORDER BY r.timestamp DESC
    ");
        cmd.Parameters.AddWithValue("characterId", characterId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var playerSnapshot = JsonConvert.DeserializeObject<CharacterRoutes.CharacterWrapperDTO>(reader.GetString(0));
            var enemySnapshot = JsonConvert.DeserializeObject<CharacterRoutes.CharacterWrapperDTO>(reader.GetString(1));
            var actions = JsonConvert.DeserializeObject<List<MatchEventDTO>>(reader.GetString(2));
            var mapName = reader.GetString(3);
            var winner = reader.GetString(4);
            var timestamp = reader.GetDateTime(5).ToString("o");

            var replay = new ReplayPayload(
                player: playerSnapshot,
                enemy: enemySnapshot,
                actions: actions,
                mapName: mapName,
                winner: winner,
                timestamp: timestamp
            );

            replays.Add(replay);
        }

        return Results.Json(replays);
    }

}
