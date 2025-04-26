using Npgsql;
namespace server;

public class CharacterRoutes
{

    public static async Task GetCharacter()
    {

    }

    public record CharacterWrapperDTO(
        CharacterDTO character, CharacterBodyPartsDTO bodyPartLabels, SkillsDTO skills, PetsDTO pets, WeaponsDTO weapons, ConsumablesDTO consumables
    );

    public record CharacterDTO(
    string charName, int level, int xp, int health,
    int attackDamage, int lifeSteal, int dodgeRate, int critRate, int stunRate,
    int initiative, int strength, int agility, int intellect
);
    public record CharacterBodyPartsDTO(
        string hair, string eyes, string chest, string legs
    );

    public record SkillsDTO(
        List<string> skillNames
    );
    public record PetsDTO(
        List<string> petNames
    );
    public record WeaponsDTO(
        List<string> weaponNames
    );
    public record ConsumablesDTO(
        List<string> consumableNames
    );

    public static async Task<int> AddCharacter(CharacterWrapperDTO wrapper, NpgsqlDataSource db)
    {
        int characterId = -1;
        try
        {

            using var checkCmd = db.CreateCommand("SELECT id FROM characters WHERE name = @name");
            checkCmd.Parameters.AddWithValue("name", wrapper.character.charName);


            using (var reader = await checkCmd.ExecuteReaderAsync())
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
            attack_damage = @ad, life_steal = @ls,
            dodge_rate = @dr, crit_rate = @cr,
            stun_rate = @sr, initiative = @ini,
            strength = @str, agility = @agi, intellect = @int
        WHERE id = @id");

                updateCmd.Parameters.AddWithValue("level", wrapper.character.level);
                updateCmd.Parameters.AddWithValue("xp", wrapper.character.xp);
                updateCmd.Parameters.AddWithValue("health", wrapper.character.health);
                updateCmd.Parameters.AddWithValue("ad", wrapper.character.attackDamage);
                updateCmd.Parameters.AddWithValue("ls", wrapper.character.lifeSteal);
                updateCmd.Parameters.AddWithValue("dr", wrapper.character.dodgeRate);
                updateCmd.Parameters.AddWithValue("cr", wrapper.character.critRate);
                updateCmd.Parameters.AddWithValue("sr", wrapper.character.stunRate);
                updateCmd.Parameters.AddWithValue("ini", wrapper.character.initiative);
                updateCmd.Parameters.AddWithValue("str", wrapper.character.strength);
                updateCmd.Parameters.AddWithValue("agi", wrapper.character.agility);
                updateCmd.Parameters.AddWithValue("int", wrapper.character.intellect);
                updateCmd.Parameters.AddWithValue("id", characterId);

                await updateCmd.ExecuteNonQueryAsync();
            }
            else
            {
                // 1. Lägg till karaktären
                using var cmd = db.CreateCommand(@"
            INSERT INTO characters (
                name, level, xp, health, attack_damage, life_steal,
                dodge_rate, crit_rate, stun_rate, initiative, strength, agility, intellect
            ) VALUES (
                $1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13
            ) RETURNING id");

                cmd.Parameters.AddWithValue(wrapper.character.charName);
                cmd.Parameters.AddWithValue(wrapper.character.level);
                cmd.Parameters.AddWithValue(wrapper.character.xp);
                cmd.Parameters.AddWithValue(wrapper.character.health);
                cmd.Parameters.AddWithValue(wrapper.character.attackDamage);
                cmd.Parameters.AddWithValue(wrapper.character.lifeSteal);
                cmd.Parameters.AddWithValue(wrapper.character.dodgeRate);
                cmd.Parameters.AddWithValue(wrapper.character.critRate);
                cmd.Parameters.AddWithValue(wrapper.character.stunRate);
                cmd.Parameters.AddWithValue(wrapper.character.initiative);
                cmd.Parameters.AddWithValue(wrapper.character.strength);
                cmd.Parameters.AddWithValue(wrapper.character.agility);
                cmd.Parameters.AddWithValue(wrapper.character.intellect);


                using (var reader = await cmd.ExecuteReaderAsync())
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


            if (wrapper.skills?.skillNames == null)
            {
                Console.WriteLine("skills are null.");
                return -1;
            }

            if (wrapper.skills?.skillNames is { Count: > 0 })
            {
                foreach (var skillName in wrapper.skills.skillNames)
                {
                    Console.WriteLine("Processing skill: " + skillName);

                    using var getSkillIdCmd = db.CreateCommand(@"
            SELECT id FROM skills WHERE name = @name");
                    getSkillIdCmd.Parameters.AddWithValue("name", skillName);

                    int skillId;
                    using (var reader = await getSkillIdCmd.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                        {
                            Console.WriteLine($"Skill '{skillName}' not found in skills table.");
                            continue;
                        }
                        skillId = reader.GetInt32(0);
                    }

                    using var insertCmd = db.CreateCommand(@"
            INSERT INTO characterxskills (character, skill)
            VALUES (@charId, @skillId)");
                    insertCmd.Parameters.AddWithValue("charId", characterId);
                    insertCmd.Parameters.AddWithValue("skillId", skillId);

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
                    using (var reader = await getPetIdCmd.ExecuteReaderAsync())
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
                foreach (var weaponName in wrapper.weapons.weaponNames)
                {
                    Console.WriteLine("Processing weapon: " + weaponName);

                    using var getWeaponIdCmd = db.CreateCommand(@"
            SELECT id FROM weapons WHERE name = @name");
                    getWeaponIdCmd.Parameters.AddWithValue("name", weaponName);

                    int weaponId;
                    using (var reader = await getWeaponIdCmd.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                        {
                            Console.WriteLine($"Weapon '{weaponName}' not found in weapons table.");
                            continue; // hoppa över om den inte finns
                        }
                        weaponId = reader.GetInt32(0);
                    }

                    // 2. Lägg in i characterxskills
                    using var insertWeaponCmd = db.CreateCommand(@"
            INSERT INTO characterxweapons (character, weapon)
            VALUES (@charId, @weaponId)");
                    insertWeaponCmd.Parameters.AddWithValue("charId", characterId);
                    insertWeaponCmd.Parameters.AddWithValue("weaponId", weaponId);

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
                foreach (var consumableName in wrapper.consumables.consumableNames)
                {
                    Console.WriteLine("Processing consumable: " + consumableName);

                    using var getConsumableIdCmd = db.CreateCommand(@"
            SELECT id FROM consumables WHERE name = @name");
                    getConsumableIdCmd.Parameters.AddWithValue("name", consumableName);

                    int consumableId;
                    using (var reader = await getConsumableIdCmd.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                        {
                            Console.WriteLine($"Consumable '{consumableName}' not found in consumables table.");
                            continue; // hoppa över om den inte finns
                        }
                        consumableId = reader.GetInt32(0);
                    }

                    // 2. Lägg in i characterxskills
                    using var insertConsumableCmd = db.CreateCommand(@"
            INSERT INTO characterxconsumables (character, consumable)
            VALUES (@charId, @consumableId)");
                    insertConsumableCmd.Parameters.AddWithValue("charId", characterId);
                    insertConsumableCmd.Parameters.AddWithValue("consumableId", consumableId);

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

        using var reader = await cmd.ExecuteReaderAsync();
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
            cmd.CommandText = @"SELECT id, name, level, xp, health, attack_damage, life_steal, dodge_rate, crit_rate, stun_rate, initiative, strength, agility, intellect
                            FROM public.characters WHERE id = @id";
            cmd.Parameters.AddWithValue("id", characterId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return Results.NotFound("Character not found.");

            var character = new CharacterDTO(
                charName: reader.GetString(1),
                level: reader.GetInt32(2),
                xp: reader.GetInt32(3),
                health: reader.GetInt32(4),
                attackDamage: reader.GetInt32(5),
                lifeSteal: reader.GetInt32(6),
                dodgeRate: reader.GetInt32(7),
                critRate: reader.GetInt32(8),
                stunRate: reader.GetInt32(9),
                initiative: reader.GetInt32(10),
                strength: reader.GetInt32(11),
                agility: reader.GetInt32(12),
                intellect: reader.GetInt32(13)
            );

            // Hämta övriga kopplade resurser
            var bodyParts = await GetBodyParts(db, characterId);
            var skills = await GetSkills(db, characterId);
            var pets = await GetPets(db, characterId);
            var weapons = await GetWeapons(db, characterId);
            var consumables = await GetConsumables(db, characterId);

            var wrapper = new CharacterWrapperDTO(character, bodyParts, skills, pets, weapons, consumables);


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

        using var reader = await cmd.ExecuteReaderAsync();
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

    private static async Task<SkillsDTO> GetSkills(NpgsqlDataSource db, int characterId)
    {
        var skills = new List<string>();

        using var cmd = db.CreateCommand(@"
        SELECT s.name
        FROM public.characterxskills cx
        JOIN public.skills s ON cx.skill = s.id
        WHERE cx.character = @charId");

        cmd.Parameters.AddWithValue("charId", characterId);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            skills.Add(reader.GetString(0));
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

        using var reader = await cmd.ExecuteReaderAsync();
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

        using var reader = await cmd.ExecuteReaderAsync();
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
        WHERE cc.character = @charId");

        cmd.Parameters.AddWithValue("charId", characterId);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            consumables.Add(reader.GetString(0));
        }

        return new ConsumablesDTO(consumables);
    }



}