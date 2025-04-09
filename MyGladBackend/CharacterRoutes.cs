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
        try
        {

            using var checkCmd = db.CreateCommand("SELECT id FROM characters WHERE name = @name");
            checkCmd.Parameters.AddWithValue("name", wrapper.character.charName);

            int characterId = -1;
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
        return -1;
    }

    public static async Task<IResult> GetCharacterByName(HttpContext context, NpgsqlDataSource db)

    {
        var name = context.Request.RouteValues["name"]?.ToString();
        if (string.IsNullOrWhiteSpace(name))
        {
            return Results.BadRequest("Name is required.");
        }

        int characterId;

        // Hämta karaktärsinfo
        using (var cmd = db.CreateCommand("SELECT id, level, xp, health, attack_damage, life_steal, dodge_rate, crit_rate, stun_rate, initiative, strength, agility, intellect FROM characters WHERE name = @name"))
        {
            cmd.Parameters.AddWithValue("name", name);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return Results.NotFound("Character not found.");

            characterId = reader.GetInt32(0);
            var character = new CharacterDTO(
                charName: name,
                level: reader.GetInt32(1),
                xp: reader.GetInt32(2),
                health: reader.GetInt32(3),
                attackDamage: reader.GetInt32(4),
                lifeSteal: reader.GetInt32(5),
                dodgeRate: reader.GetInt32(6),
                critRate: reader.GetInt32(7),
                stunRate: reader.GetInt32(8),
                initiative: reader.GetInt32(9),
                strength: reader.GetInt32(10),
                agility: reader.GetInt32(11),
                intellect: reader.GetInt32(12)
            );

            // Hämtar nu övriga data baserat på characterId
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
        FROM body_parts
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
        FROM characterxskills cx
        JOIN skills s ON cx.skill = s.id
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
        FROM characterxpets cp
        JOIN pets p ON cp.pet = p.id
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
        FROM characterxweapons cw
        JOIN weapons w ON cw.weapon = w.id
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
        FROM characterxconsumables cc
        JOIN consumables c ON cc.consumable = c.id
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