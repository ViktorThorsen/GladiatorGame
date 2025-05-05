using Npgsql;
namespace server;
using Microsoft.AspNetCore.Mvc;

public class MonsterHuntRoutes
{
    public static async Task<int?> GetMonsterHuntInfo(int characterId, string map, NpgsqlDataSource db)
    {
        int? stage = null;

        using var cmd = db.CreateCommand(@"
        SELECT stage FROM monster_hunt WHERE character = @charId AND map = @map");

        cmd.Parameters.AddWithValue("charId", characterId);
        cmd.Parameters.AddWithValue("map", map);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            stage = reader.GetInt32(0);
        }

        return stage;
    }

    public class CharacterIdDTO
    {
        public int characterId { get; set; }
    }

    public static async Task AddMonsterHuntInfo([FromBody] CharacterIdDTO dto, NpgsqlDataSource db)
    {
        string[] maps = new[] { "Forest", "Savannah", "Frostlands", "Jungle" };

        foreach (var map in maps)
        {
            // Kolla om raden redan finns
            using (var checkCmd = db.CreateCommand(@"
            SELECT 1 FROM monster_hunt WHERE character = @character AND map = @map"))
            {
                checkCmd.Parameters.AddWithValue("character", dto.characterId);
                checkCmd.Parameters.AddWithValue("map", map);

                await using var reader = await checkCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    continue; // Den finns redan – hoppa över
                }
            }

            // Lägg till om den inte fanns
            using var insertCmd = db.CreateCommand(@"
            INSERT INTO monster_hunt (character, map, stage)
            VALUES (@character, @map, @stage)");

            insertCmd.Parameters.AddWithValue("character", dto.characterId);
            insertCmd.Parameters.AddWithValue("map", map);
            insertCmd.Parameters.AddWithValue("stage", 1);

            await insertCmd.ExecuteNonQueryAsync();
        }
    }

    public class UpdateMonsterHuntStageDTO
    {
        public int characterId { get; set; }
        public string map { get; set; }
        public int newStage { get; set; }
    }
    public static async Task<IResult> UpdateMonsterHuntStage(
    [FromBody] UpdateMonsterHuntStageDTO dto,
    NpgsqlDataSource db)
    {
        using var cmd = db.CreateCommand(@"
        UPDATE monster_hunt
        SET stage = @stage
        WHERE character = @charId AND map = @map AND stage < @stage
    ");

        cmd.Parameters.AddWithValue("charId", dto.characterId);
        cmd.Parameters.AddWithValue("map", dto.map);
        cmd.Parameters.AddWithValue("stage", dto.newStage);

        int rowsAffected = await cmd.ExecuteNonQueryAsync();

        return rowsAffected > 0
            ? Results.Ok()
            : Results.Ok("Stage not updated. It may already be equal or higher.");
    }

}