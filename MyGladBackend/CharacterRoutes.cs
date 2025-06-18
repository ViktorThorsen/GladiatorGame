using Npgsql;
namespace server;

using Microsoft.AspNetCore.Mvc;

public class CharacterRoutes
{

    public static async Task GetCharacter()
    {

    }

    public record CharacterWrapperDTO(
        CharacterDTO character, CharacterBodyPartsDTO bodyPartLabels, SkillsDTO skills,
        PetsDTO pets, WeaponsDTO weapons, ConsumablesDTO consumables, ShortcutDTO shortcuts
    );

    public record CharacterDTO(string charName, int level, int xp, int health, int defense,
                    int lifeSteal, int dodgeRate, int critRate, int stunRate, int hitRate,
                    int strength, int agility, int intellect, int fortune, int coins, int valor, int precision, int initiative, int combo);
    public record CharacterBodyPartsDTO(
        string hair, string eyes, string chest, string legs
    );

    public record SkillsDTO(List<SkillEntryDTO> skills);

    public record SkillEntryDTO(string skillName, int level);
    public record PetsDTO(
        List<string> petNames
    );
    public record WeaponsDTO(
        List<string> weaponNames
    );
    public record ConsumablesDTO(
        List<string> consumableNames
    );
    public record ShortcutDTO(
    List<ShortcutEntry> shortcuts
);
    public record SimpleCharacterSearchDTO(
    int id,
    string name,
    int level,
    string hair,
    string eyes,
    string chest
);

    public record ShortcutEntry(
        int slotIndex,
        string weaponName
    );

    public static async Task<int> AddCharacter(CharacterWrapperDTO wrapper, NpgsqlDataSource db)
    {
        int characterId = -1;
        try
        {

            using var checkCmd = db.CreateCommand("SELECT id FROM characters WHERE name = @name");
            checkCmd.Parameters.AddWithValue("name", wrapper.character.charName);

            await using (var reader = await checkCmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                    characterId = reader.GetInt32(0);
                Console.WriteLine("Character ID: " + characterId);
            }

            if (characterId != -1)
            {
                using var updateCmd = db.CreateCommand(@"
        UPDATE characters SET
            level = @level, xp = @xp, health = @health,
            defense = @def, life_steal = @ls,
            dodge_rate = @dr, crit_rate = @cr,
            stun_rate = @sr, hit_rate = @hit,
            strength = @str, agility = @agi, 
            intellect = @int, fortune = @for, 
            coins = @coin, valor = @valor, 
            precision = @precision, initiative = @initiative, 
            combo = @combo
        WHERE id = @id");

                updateCmd.Parameters.AddWithValue("level", wrapper.character.level);
                updateCmd.Parameters.AddWithValue("xp", wrapper.character.xp);
                updateCmd.Parameters.AddWithValue("health", wrapper.character.health);
                updateCmd.Parameters.AddWithValue("def", wrapper.character.defense);
                updateCmd.Parameters.AddWithValue("ls", wrapper.character.lifeSteal);
                updateCmd.Parameters.AddWithValue("dr", wrapper.character.dodgeRate);
                updateCmd.Parameters.AddWithValue("cr", wrapper.character.critRate);
                updateCmd.Parameters.AddWithValue("sr", wrapper.character.stunRate);
                updateCmd.Parameters.AddWithValue("hit", wrapper.character.hitRate);
                updateCmd.Parameters.AddWithValue("str", wrapper.character.strength);
                updateCmd.Parameters.AddWithValue("agi", wrapper.character.agility);
                updateCmd.Parameters.AddWithValue("int", wrapper.character.intellect);
                updateCmd.Parameters.AddWithValue("for", wrapper.character.fortune);
                updateCmd.Parameters.AddWithValue("id", characterId);
                updateCmd.Parameters.AddWithValue("coin", wrapper.character.coins);
                updateCmd.Parameters.AddWithValue("valor", wrapper.character.valor);
                updateCmd.Parameters.AddWithValue("precision", wrapper.character.precision);
                updateCmd.Parameters.AddWithValue("initiative", wrapper.character.initiative);
                updateCmd.Parameters.AddWithValue("combo", wrapper.character.combo);

                await updateCmd.ExecuteNonQueryAsync();
            }
            else
            {
                // 1. Lägg till karaktären
                using var cmd = db.CreateCommand(@"
        INSERT INTO characters (
            name, level, xp, health, defense, life_steal,
            dodge_rate, crit_rate, stun_rate, hit_rate,
            strength, agility, intellect, fortune, coins, valor, precision, initiative, combo
        ) VALUES (
            @name, @level, @xp, @health, @def, @ls,
            @dr, @cr, @sr, @hit,
            @str, @agi, @int, @for, @coin, @valor, @precision, @initiative, @combo
        ) RETURNING id");
                cmd.Parameters.AddWithValue("name", wrapper.character.charName);
                cmd.Parameters.AddWithValue("level", wrapper.character.level);
                cmd.Parameters.AddWithValue("xp", wrapper.character.xp);
                cmd.Parameters.AddWithValue("health", wrapper.character.health);
                cmd.Parameters.AddWithValue("def", wrapper.character.defense);
                cmd.Parameters.AddWithValue("ls", wrapper.character.lifeSteal);
                cmd.Parameters.AddWithValue("dr", wrapper.character.dodgeRate);
                cmd.Parameters.AddWithValue("cr", wrapper.character.critRate);
                cmd.Parameters.AddWithValue("sr", wrapper.character.stunRate);
                cmd.Parameters.AddWithValue("hit", wrapper.character.hitRate);
                cmd.Parameters.AddWithValue("str", wrapper.character.strength);
                cmd.Parameters.AddWithValue("agi", wrapper.character.agility);
                cmd.Parameters.AddWithValue("int", wrapper.character.intellect);
                cmd.Parameters.AddWithValue("for", wrapper.character.fortune);
                cmd.Parameters.AddWithValue("id", characterId);
                cmd.Parameters.AddWithValue("coin", wrapper.character.coins);
                cmd.Parameters.AddWithValue("valor", wrapper.character.valor);
                cmd.Parameters.AddWithValue("precision", wrapper.character.precision);
                cmd.Parameters.AddWithValue("initiative", wrapper.character.initiative);
                cmd.Parameters.AddWithValue("combo", wrapper.character.combo);


                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                        characterId = reader.GetInt32(0);
                }

                if (wrapper.bodyPartLabels == null)
                {
                    Console.WriteLine("Body part labels are null.");
                    return -1;
                }

                using var bodyCmd = db.CreateCommand(@"
                INSERT INTO body_parts (hair, eyes, chest, legs, character)
                VALUES ($1, $2, $3, $4, $5)");

                bodyCmd.Parameters.AddWithValue(wrapper.bodyPartLabels.hair);
                bodyCmd.Parameters.AddWithValue(wrapper.bodyPartLabels.eyes);
                bodyCmd.Parameters.AddWithValue(wrapper.bodyPartLabels.chest);
                bodyCmd.Parameters.AddWithValue(wrapper.bodyPartLabels.legs);
                bodyCmd.Parameters.AddWithValue(characterId);

                await bodyCmd.ExecuteNonQueryAsync();
            }

            using var delSkillsCmd = db.CreateCommand("DELETE FROM characterxskills WHERE character = @id");
            delSkillsCmd.Parameters.AddWithValue("id", characterId);
            await delSkillsCmd.ExecuteNonQueryAsync();

            if (wrapper.skills?.skills == null)
            {
                Console.WriteLine("skills are null.");
                return -1;
            }

            if (wrapper.skills.skills.Count > 0)
            {
                foreach (var skillEntry in wrapper.skills.skills)
                {
                    string skillName = skillEntry.skillName;
                    int level = skillEntry.level;

                    Console.WriteLine($"Processing skill: {skillName} (Level {level})");

                    using var getSkillIdCmd = db.CreateCommand(@"
            SELECT id FROM skills WHERE name = @name");
                    getSkillIdCmd.Parameters.AddWithValue("name", skillName);

                    int skillId;
                    await using (var reader = await getSkillIdCmd.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                        {
                            Console.WriteLine($"Skill '{skillName}' not found in skills table.");
                            continue;
                        }
                        skillId = reader.GetInt32(0);
                    }

                    using var insertCmd = db.CreateCommand(@"
            INSERT INTO characterxskills (character, skill, level)
            VALUES (@charId, @skillId, @level)");
                    insertCmd.Parameters.AddWithValue("charId", characterId);
                    insertCmd.Parameters.AddWithValue("skillId", skillId);
                    insertCmd.Parameters.AddWithValue("level", level);

                    await insertCmd.ExecuteNonQueryAsync();
                }
            }
            else
            {
                Console.WriteLine("No skills provided.");
            }
            ////PETS
            /// 
            using var delPetsCmd = db.CreateCommand("DELETE FROM characterxpets WHERE character = @id");
            delPetsCmd.Parameters.AddWithValue("id", characterId);
            await delPetsCmd.ExecuteNonQueryAsync();

            if (wrapper.pets?.petNames is { Count: > 0 })
            {
                foreach (var petName in wrapper.pets.petNames)
                {
                    Console.WriteLine("Processing pet: " + petName);

                    using var getPetIdCmd = db.CreateCommand(@"
            SELECT id FROM pets WHERE name = @name");
                    getPetIdCmd.Parameters.AddWithValue("name", petName);

                    int petId;
                    await using (var reader = await getPetIdCmd.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                        {
                            Console.WriteLine($"Pet '{petName}' not found in pets table.");
                            continue;
                        }
                        petId = reader.GetInt32(0);
                    }

                    using var insertPetCmd = db.CreateCommand(@"
            INSERT INTO characterxpets (character, pet)
            VALUES (@charId, @petId)");
                    insertPetCmd.Parameters.AddWithValue("charId", characterId);
                    insertPetCmd.Parameters.AddWithValue("petId", petId);

                    await insertPetCmd.ExecuteNonQueryAsync();
                }

            }
            else
            {
                Console.WriteLine("No pets provided.");
            }


            ///weapons


            using var delWeaponsCmd = db.CreateCommand("DELETE FROM characterxweapons WHERE character = @id");
            delWeaponsCmd.Parameters.AddWithValue("id", characterId);
            await delWeaponsCmd.ExecuteNonQueryAsync();

            if (wrapper.weapons?.weaponNames is { Count: > 0 })
            {
                for (int i = 0; i < wrapper.weapons.weaponNames.Count; i++)
                {
                    string weaponName = wrapper.weapons.weaponNames[i];

                    using var getWeaponIdCmd = db.CreateCommand("SELECT id FROM weapons WHERE name = @name");
                    getWeaponIdCmd.Parameters.AddWithValue("name", weaponName);

                    int weaponId;
                    await using (var reader = await getWeaponIdCmd.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                        {
                            Console.WriteLine($"❌ Weapon '{weaponName}' not found.");
                            continue;
                        }
                        weaponId = reader.GetInt32(0);
                    }

                    // 🔍 Hitta om detta vapen finns i någon shortcut-slot
                    int shortcutSlot = -1;
                    if (wrapper.shortcuts?.shortcuts is { Count: > 0 })
                    {
                        foreach (var entry in wrapper.shortcuts.shortcuts)
                        {
                            if (entry.weaponName == weaponName) // matcha på namn, inte index
                            {
                                shortcutSlot = entry.slotIndex;
                                break;
                            }
                        }
                    }

                    using var insertWeaponCmd = db.CreateCommand(@"
            INSERT INTO characterxweapons (character, weapon, shortcut)
            VALUES (@charId, @weaponId, @shortcut)");
                    insertWeaponCmd.Parameters.AddWithValue("charId", characterId);
                    insertWeaponCmd.Parameters.AddWithValue("weaponId", weaponId);
                    insertWeaponCmd.Parameters.AddWithValue("shortcut", shortcutSlot);

                    await insertWeaponCmd.ExecuteNonQueryAsync();
                }
            }
            else
            {
                Console.WriteLine("No weapons provided.");
            }
            //Consumable

            using var delConsumablesCmd = db.CreateCommand("DELETE FROM characterxconsumables WHERE character = @id");
            delConsumablesCmd.Parameters.AddWithValue("id", characterId);
            await delConsumablesCmd.ExecuteNonQueryAsync();

            if (wrapper.consumables?.consumableNames is { Count: > 0 })
            {
                for (int i = 0; i < wrapper.consumables.consumableNames.Count; i++)
                {
                    string consumableName = wrapper.consumables.consumableNames[i];

                    Console.WriteLine("Processing consumable: " + consumableName);

                    using var getConsumableIdCmd = db.CreateCommand(@"
        SELECT id FROM consumables WHERE name = @name");
                    getConsumableIdCmd.Parameters.AddWithValue("name", consumableName);

                    int consumableId;
                    await using (var reader = await getConsumableIdCmd.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                        {
                            Console.WriteLine($"Consumable '{consumableName}' not found in consumables table.");
                            continue;
                        }
                        consumableId = reader.GetInt32(0);
                    }

                    using var insertConsumableCmd = db.CreateCommand(@"
        INSERT INTO characterxconsumables (character, consumable, index)
        VALUES (@charId, @consumableId, @index)");
                    insertConsumableCmd.Parameters.AddWithValue("charId", characterId);
                    insertConsumableCmd.Parameters.AddWithValue("consumableId", consumableId);
                    insertConsumableCmd.Parameters.AddWithValue("index", i); // 🔢 viktig!

                    await insertConsumableCmd.ExecuteNonQueryAsync();
                }
            }
            else
            {
                Console.WriteLine("No consumables provided.");
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine("Error saving character or body parts: " + ex.Message);
            return -1;
        }
        return characterId;
    }
    public record LinkCharacterRequest(int UserId, int CharacterId);
    public static async Task<IResult> LinkCharacterToUser(LinkCharacterRequest request, NpgsqlDataSource db)
    {
        // 2. Kolla om koppling redan finns
        await using var checkLinkCmd = db.CreateCommand();
        checkLinkCmd.CommandText = @"
        SELECT 1 FROM userxcharacter 
        WHERE ""user"" = @userId AND character = @characterId
        LIMIT 1";
        checkLinkCmd.Parameters.Add("userId", NpgsqlTypes.NpgsqlDbType.Integer).Value = request.UserId;
        checkLinkCmd.Parameters.Add("characterId", NpgsqlTypes.NpgsqlDbType.Integer).Value = request.CharacterId;

        var linkExists = await checkLinkCmd.ExecuteScalarAsync();
        if (linkExists != null && linkExists != DBNull.Value)
        {
            return Results.Ok("Link already exists.");
        }

        // 3. Skapa ny koppling
        await using var insertCmd = db.CreateCommand();
        insertCmd.CommandText = "INSERT INTO userxcharacter (\"user\", character) VALUES (@userId, @characterId)";
        insertCmd.Parameters.AddWithValue("userId", request.UserId);
        insertCmd.Parameters.AddWithValue("characterId", request.CharacterId);

        await insertCmd.ExecuteNonQueryAsync();

        return Results.Ok("Character linked successfully.");
    }

    public static async Task<IResult> GetAllCharacterNames(HttpContext context, NpgsqlDataSource db)
    {
        var characterNames = new List<string>();

        using var cmd = db.CreateCommand("SELECT name FROM public.characters ORDER BY name ASC"); // sorterat för enkelhets skull

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            characterNames.Add(reader.GetString(0));
        }

        return Results.Json(characterNames);
    }
    public record EnemyCharacterResponse
    {
        public int id { get; set; }
        public string name { get; set; }
    }
    public static async Task<IResult> GetRandomCharacterName(HttpContext context, NpgsqlDataSource db)
    {
        await using var cmd = db.CreateCommand("SELECT id, name FROM public.characters ORDER BY RANDOM() LIMIT 1");

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var id = reader.GetInt32(0);
            var name = reader.GetString(1);

            var response = new EnemyCharacterResponse
            {
                id = id,
                name = name
            };

            return Results.Json(response);
        }
        else
        {
            return Results.NotFound("No characters found.");
        }
    }

    public static async Task<IResult> GetCharacterByCharacterId(int characterId, HttpContext context, NpgsqlDataSource db)
    {


        // Hämta karaktärsinfo från characters-tabellen baserat på gladiatorId
        await using (var cmd = db.CreateCommand())
        {
            cmd.CommandText = @"
    SELECT id, name, level, xp, health, defense, life_steal,
           dodge_rate, crit_rate, stun_rate, hit_rate,
           strength, agility, intellect, fortune, coins, valor, precision, initiative, combo
    FROM public.characters
    WHERE id = @id";

            cmd.Parameters.AddWithValue("id", characterId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return Results.NotFound("Character not found.");

            var character = new CharacterDTO(
                charName: reader.GetString(1),
                level: reader.GetInt32(2),
                xp: reader.GetInt32(3),
                health: reader.GetInt32(4),
                defense: reader.GetInt32(5),
                lifeSteal: reader.GetInt32(6),
                dodgeRate: reader.GetInt32(7),
                critRate: reader.GetInt32(8),
                stunRate: reader.GetInt32(9),
                hitRate: reader.GetInt32(10),
                strength: reader.GetInt32(11),
                agility: reader.GetInt32(12),
                intellect: reader.GetInt32(13),
                fortune: reader.GetInt32(14),
                coins: reader.GetInt32(15),
                valor: reader.GetInt32(16),
                precision: reader.GetInt32(17),
                initiative: reader.GetInt32(18),
                combo: reader.GetInt32(19)
            );

            // Hämta övriga kopplade resurser
            var bodyParts = await GetBodyParts(db, characterId);
            var skills = await GetSkills(db, characterId);
            var pets = await GetPets(db, characterId);
            var weapons = await GetWeapons(db, characterId);
            var consumables = await GetConsumables(db, characterId);
            var shortcuts = await GetShortcuts(db, characterId);

            var wrapper = new CharacterWrapperDTO(character, bodyParts, skills, pets, weapons, consumables, shortcuts);


            return Results.Json(wrapper);
        }
    }

    private static async Task<CharacterBodyPartsDTO> GetBodyParts(NpgsqlDataSource db, int characterId)
    {
        using var cmd = db.CreateCommand(@"
        SELECT hair, eyes, chest, legs
        FROM public.body_parts
        WHERE character = @charId
        LIMIT 1");

        cmd.Parameters.AddWithValue("charId", characterId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new CharacterBodyPartsDTO(
                hair: reader.GetString(0),
                eyes: reader.GetString(1),
                chest: reader.GetString(2),
                legs: reader.GetString(3)
            );
        }

        return new CharacterBodyPartsDTO("", "", "", "");
    }
    private static async Task<ShortcutDTO> GetShortcuts(NpgsqlDataSource db, int characterId)
    {
        var list = new List<ShortcutEntry>();

        await using var cmd = db.CreateCommand(@"
        SELECT cw.shortcut, w.name
        FROM characterxweapons cw
        JOIN weapons w ON cw.weapon = w.id
        WHERE cw.character = @charId AND cw.shortcut >= 0
    ");
        cmd.Parameters.AddWithValue("charId", characterId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            int slotIndex = reader.GetInt32(0);         // shortcut position
            string weaponName = reader.GetString(1);    // weapon name

            list.Add(new ShortcutEntry(slotIndex, weaponName));
        }

        return new ShortcutDTO(list);
    }

    private static async Task<SkillsDTO> GetSkills(NpgsqlDataSource db, int characterId)
    {
        var skills = new List<SkillEntryDTO>();

        using var cmd = db.CreateCommand(@"
        SELECT s.name, cx.level
        FROM public.characterxskills cx
        JOIN public.skills s ON cx.skill = s.id
        WHERE cx.character = @charId");

        cmd.Parameters.AddWithValue("charId", characterId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            string name = reader.GetString(0);
            int level = reader.GetInt32(1);
            skills.Add(new SkillEntryDTO(name, level));
        }

        return new SkillsDTO(skills);
    }

    private static async Task<PetsDTO> GetPets(NpgsqlDataSource db, int characterId)
    {
        var pets = new List<string>();

        using var cmd = db.CreateCommand(@"
        SELECT p.name
        FROM public.characterxpets cp
        JOIN public.pets p ON cp.pet = p.id
        WHERE cp.character = @charId");

        cmd.Parameters.AddWithValue("charId", characterId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            pets.Add(reader.GetString(0));
        }

        return new PetsDTO(pets);
    }

    private static async Task<WeaponsDTO> GetWeapons(NpgsqlDataSource db, int characterId)
    {
        var weapons = new List<string>();

        using var cmd = db.CreateCommand(@"
        SELECT w.name
        FROM public.characterxweapons cw
        JOIN public.weapons w ON cw.weapon = w.id
        WHERE cw.character = @charId");

        cmd.Parameters.AddWithValue("charId", characterId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            weapons.Add(reader.GetString(0));
        }

        return new WeaponsDTO(weapons);
    }

    private static async Task<ConsumablesDTO> GetConsumables(NpgsqlDataSource db, int characterId)
    {
        var consumables = new List<string>();

        using var cmd = db.CreateCommand(@"
        SELECT c.name
        FROM public.characterxconsumables cc
        JOIN public.consumables c ON cc.consumable = c.id
        WHERE cc.character = @charId
        ORDER BY cc.index");

        cmd.Parameters.AddWithValue("charId", characterId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            consumables.Add(reader.GetString(0));
        }

        return new ConsumablesDTO(consumables);
    }

    public record EnergyCharacterDTO(int id, int energy);

    public static async Task<EnergyCharacterDTO> UpdateCharacterEnergy([FromQuery(Name = "characterid")] int characterId, NpgsqlDataSource db)
    {
        var now = DateTime.UtcNow;

        await using var loadCmd = db.CreateCommand();
        loadCmd.CommandText = @"SELECT energy, last_energy_update FROM characters WHERE id = @id";
        loadCmd.Parameters.AddWithValue("id", characterId);

        await using var reader = await loadCmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            throw new Exception("Character not found");

        int currentEnergy = reader.GetInt32(0);
        DateTime lastUpdate = reader.GetFieldValue<DateTime>(1);

        var timePassed = now - lastUpdate;
        int energyRecovered = (int)(timePassed.TotalMinutes / 5); // ändrat från 2 → 5

        currentEnergy = Math.Min(currentEnergy + energyRecovered, 10);

        await using var updateCmd = db.CreateCommand();
        updateCmd.CommandText = @"
    UPDATE characters 
    SET energy = @energy, last_energy_update = @updateTime
    WHERE id = @id";

        updateCmd.Parameters.AddWithValue("energy", currentEnergy);
        updateCmd.Parameters.AddWithValue("updateTime", lastUpdate.AddMinutes(energyRecovered * 5)); // ändrat från *2 → *5
        updateCmd.Parameters.AddWithValue("id", characterId);

        await updateCmd.ExecuteNonQueryAsync();

        return new EnergyCharacterDTO(characterId, currentEnergy);
    }

    public static async Task<IResult> UseEnergy([FromQuery(Name = "characterid")] int characterId, NpgsqlDataSource db)
    {
        await using var loadCmd = db.CreateCommand();
        loadCmd.CommandText = @"SELECT energy FROM characters WHERE id = @id";
        loadCmd.Parameters.AddWithValue("id", characterId);

        await using var reader = await loadCmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return Results.NotFound("Character not found");

        int currentEnergy = reader.GetInt32(0);

        if (currentEnergy <= 0)
        {
            // Returnera info, men markera att matchen inte får startas
            return Results.Json(new { success = false, energy = currentEnergy });
        }

        int newEnergy = currentEnergy - 1;

        await using var updateCmd = db.CreateCommand();
        updateCmd.CommandText = @"
        UPDATE characters
        SET energy = @newEnergy
        WHERE id = @id";
        updateCmd.Parameters.AddWithValue("newEnergy", newEnergy);
        updateCmd.Parameters.AddWithValue("id", characterId);

        await updateCmd.ExecuteNonQueryAsync();

        return Results.Json(new { success = true, energy = newEnergy });
    }
    public static async Task<IResult> AddCoinsToCharacter([FromQuery] int characterId, [FromQuery] int coinsToAdd, NpgsqlDataSource db)
    {
        // 1. Hämta nuvarande antal coins
        await using var getCmd = db.CreateCommand();
        getCmd.CommandText = "SELECT coins FROM characters WHERE id = @id";
        getCmd.Parameters.AddWithValue("id", characterId);

        await using var reader = await getCmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return Results.NotFound("Character not found");

        int currentCoins = reader.GetInt32(0);
        int newCoinAmount = currentCoins + coinsToAdd;

        // 2. Uppdatera coins
        await using var updateCmd = db.CreateCommand();
        updateCmd.CommandText = "UPDATE characters SET coins = @coins WHERE id = @id";
        updateCmd.Parameters.AddWithValue("coins", newCoinAmount);
        updateCmd.Parameters.AddWithValue("id", characterId);

        await updateCmd.ExecuteNonQueryAsync();

        return Results.Json(new { id = characterId, coins = newCoinAmount });
    }

    public static async Task<IResult> SpendCoinsFromCharacter([FromQuery] int characterId, [FromQuery] int coinsToSpend, NpgsqlDataSource db)
    {
        if (coinsToSpend <= 0)
            return Results.BadRequest("Amount must be greater than 0.");

        await using var getCmd = db.CreateCommand();
        getCmd.CommandText = "SELECT coins FROM characters WHERE id = @id";
        getCmd.Parameters.AddWithValue("id", characterId);

        await using var reader = await getCmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return Results.NotFound("Character not found");

        int currentCoins = reader.GetInt32(0);

        if (currentCoins < coinsToSpend)
            return Results.BadRequest("Not enough coins.");

        int newCoinAmount = currentCoins - coinsToSpend;

        await using var updateCmd = db.CreateCommand();
        updateCmd.CommandText = "UPDATE characters SET coins = @coins WHERE id = @id";
        updateCmd.Parameters.AddWithValue("coins", newCoinAmount);
        updateCmd.Parameters.AddWithValue("id", characterId);

        await updateCmd.ExecuteNonQueryAsync();

        return Results.Json(new { id = characterId, coins = newCoinAmount });
    }

    public record LeaderboardCharacterDTO(int CharacterId, string CharName, int Level, int Valor);

    public static async Task<IResult> GetLeaderboard(NpgsqlDataSource db)
    {
        var topCharacters = new List<LeaderboardCharacterDTO>();

        await using var cmd = db.CreateCommand();
        cmd.CommandText = @"
        SELECT id, name, level, valor FROM characters ORDER BY valor DESC LIMIT 50"; // Du kan ändra detta till så många du vill visa

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            int id = reader.GetInt32(0);
            string name = reader.GetString(1);
            int level = reader.GetInt32(2);
            int valor = reader.GetInt32(3);

            topCharacters.Add(new LeaderboardCharacterDTO(id, name, level, valor));
        }

        return Results.Json(topCharacters);
    }

    public static async Task<IResult> SearchCharactersByName([FromQuery] string query, [FromQuery] int excludeId, NpgsqlDataSource db)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return Results.BadRequest("Query must be at least 2 characters.");

        var list = new List<SimpleCharacterSearchDTO>();

        await using var cmd = db.CreateCommand();
        cmd.CommandText = @"
    SELECT c.id, c.name, c.level, bp.hair, bp.eyes, bp.chest
    FROM characters c
    JOIN body_parts bp ON bp.character = c.id
    WHERE LOWER(c.name) LIKE @query || '%' AND c.id != @excludeId
    ORDER BY c.name ASC
    LIMIT 9";

        cmd.Parameters.AddWithValue("query", query.ToLower());
        cmd.Parameters.AddWithValue("excludeId", excludeId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new SimpleCharacterSearchDTO(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5)
            ));
        }

        return Results.Json(new { results = list });
    }
    public static async Task<IResult> GetCharacterSkillsById(HttpContext context, NpgsqlDataSource db)
    {
        if (!context.Request.Query.ContainsKey("id"))
            return Results.BadRequest("Missing 'id' parameter.");

        if (!int.TryParse(context.Request.Query["id"], out int id))
            return Results.BadRequest("Invalid 'id' parameter.");

        var skillsDto = await GetSkills(db, id);
        return Results.Json(skillsDto);
    }

    public static async Task<IResult> SearchCharactersByLevel([FromQuery] int level, [FromQuery] int excludeId, NpgsqlDataSource db)
    {
        var list = new List<SimpleCharacterSearchDTO>();
        int currentLevel = level;

        while (list.Count < 9)
        {
            await using var cmd = db.CreateCommand();
            cmd.CommandText = @"
            SELECT c.id, c.name, c.level, bp.hair, bp.eyes, bp.chest
            FROM characters c
            JOIN body_parts bp ON bp.character = c.id
            WHERE c.level = @lvl AND c.id != @excludeId
            ORDER BY c.name ASC";

            cmd.Parameters.AddWithValue("lvl", currentLevel);
            cmd.Parameters.AddWithValue("excludeId", excludeId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int id = reader.GetInt32(0);

                // Kontrollera om karaktären redan är tillagd (undantag för dubbletter)
                if (list.Any(c => c.id == id))
                    continue;

                list.Add(new SimpleCharacterSearchDTO(
                    id,
                    reader.GetString(1),
                    reader.GetInt32(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5)
                ));

                if (list.Count == 9)
                    break;
            }

            currentLevel++; // Gå upp till nästa nivå
            if (currentLevel > 5) break; // skydd mot evighetsloop om något är fel
        }

        return Results.Json(new { results = list });
    }

}