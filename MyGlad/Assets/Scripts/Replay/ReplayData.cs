using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text;
using System;
using System.Collections;

public class ReplayData : MonoBehaviour
{
    public static ReplayData Instance { get; private set; }

    public List<MatchEventDTO> ActionLog { get; private set; } = new();
    public CharacterWrapper PlayerSnapshot { get; private set; }
    public CharacterWrapper EnemySnapshot { get; private set; }

    public string MapName { get; set; }
    public string Winner { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddAction(MatchEventDTO action)
    {
        ActionLog.Add(action);
    }

    public void Clear()
    {
        ActionLog.Clear();
        PlayerSnapshot = null;
        EnemySnapshot = null;
    }

    public void SaveReplaySnapshotCharacters()
    {
        // Snapshot Player
        PlayerSnapshot = new CharacterWrapper
        {
            character = new CharacterDataSerializable
            {
                charName = CharacterData.Instance.CharName,
                level = CharacterData.Instance.Level,
                xp = CharacterData.Instance.Xp,
                health = CharacterData.Instance.Health,
                lifeSteal = CharacterData.Instance.LifeSteal,
                dodgeRate = CharacterData.Instance.DodgeRate,
                critRate = CharacterData.Instance.CritRate,
                stunRate = CharacterData.Instance.StunRate,
                hitRate = CharacterData.Instance.HitRate,
                fortune = CharacterData.Instance.Fortune,
                strength = CharacterData.Instance.Strength,
                agility = CharacterData.Instance.Agility,
                intellect = CharacterData.Instance.Intellect,
                defense = CharacterData.Instance.Defense
            },
            bodyPartLabels = new BodyPartsDataSerializable
            {
                hair = CharacterData.Instance.BodyPartLabels[0],
                eyes = CharacterData.Instance.BodyPartLabels[1],
                chest = CharacterData.Instance.BodyPartLabels[2],
                legs = CharacterData.Instance.BodyPartLabels[3]
            },
            skills = new SkillDataSerializable
            {
                skillNames = Inventory.Instance.GetSkills().ConvertAll(s => s.skillName)
            },
            pets = new PetDataSerializable
            {
                petNames = Inventory.Instance.GetPets().ConvertAll(p => p.GetComponent<MonsterStats>().MonsterName)
            },
            weapons = new WeaponDataSerializable
            {
                weaponNames = Inventory.Instance.GetWeapons().ConvertAll(w => w.itemName)
            },
            consumables = new ConsumableDataSerializable
            {
                consumableNames = Inventory.Instance.GetConsumables().ConvertAll(c => c.itemName)
            },
            shortcuts = new ShortcutDataSerializable
            {
                shortcuts = Inventory.Instance.shortcutWeaponIndexes
                    .Select((slotIndex, i) => slotIndex != -1 && Inventory.Instance.GetWeapons().Count > i
                        ? new ShortcutEntrySerializable
                        {
                            slotIndex = slotIndex,
                            weaponName = Inventory.Instance.GetWeapons()[i].itemName
                        }
                        : null)
                    .Where(s => s != null)
                    .ToList()
            }
        };

        // Snapshot Enemy
        EnemySnapshot = new CharacterWrapper
        {
            character = new CharacterDataSerializable
            {
                charName = EnemyGladiatorData.Instance.CharName,
                level = EnemyGladiatorData.Instance.Level,
                xp = EnemyGladiatorData.Instance.Xp,
                health = EnemyGladiatorData.Instance.Health,
                lifeSteal = EnemyGladiatorData.Instance.LifeSteal,
                dodgeRate = EnemyGladiatorData.Instance.DodgeRate,
                critRate = EnemyGladiatorData.Instance.CritRate,
                stunRate = EnemyGladiatorData.Instance.StunRate,
                hitRate = EnemyGladiatorData.Instance.HitRate,
                fortune = EnemyGladiatorData.Instance.Fortune,
                strength = EnemyGladiatorData.Instance.Strength,
                agility = EnemyGladiatorData.Instance.Agility,
                intellect = EnemyGladiatorData.Instance.Intellect,
                defense = EnemyGladiatorData.Instance.Defense
            },
            bodyPartLabels = new BodyPartsDataSerializable
            {
                hair = EnemyGladiatorData.Instance.BodyPartLabels[0],
                eyes = EnemyGladiatorData.Instance.BodyPartLabels[1],
                chest = EnemyGladiatorData.Instance.BodyPartLabels[2],
                legs = EnemyGladiatorData.Instance.BodyPartLabels[3]
            },
            skills = new SkillDataSerializable
            {
                skillNames = EnemyInventory.Instance.GetSkills().ConvertAll(s => s.skillName)
            },
            pets = new PetDataSerializable
            {
                petNames = EnemyInventory.Instance.GetPets().ConvertAll(p => p.GetComponent<MonsterStats>().MonsterName)
            },
            weapons = new WeaponDataSerializable
            {
                weaponNames = EnemyInventory.Instance.GetWeapons().ConvertAll(w => w.itemName)
            },
            consumables = new ConsumableDataSerializable
            {
                consumableNames = EnemyInventory.Instance.GetConsumables().ConvertAll(c => c.itemName)
            }
        };
    }

    public IEnumerator SendReplayToBackend()
    {
        if (PlayerSnapshot == null || EnemySnapshot == null || ActionLog.Count == 0)
        {
            Debug.LogWarning("ReplayData is incomplete. Cannot send to backend.");
            yield break;
        }

        ReplayPayload payload = new ReplayPayload
        {
            player = PlayerSnapshot,
            enemy = EnemySnapshot,
            actions = ActionLog,
            mapName = MapName,
            winner = Winner,
            timestamp = DateTime.UtcNow.ToString("o")
        };

        string json = JsonConvert.SerializeObject(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest("http://localhost:5000/api/replays", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // 🔐 Lägg till token från PlayerPrefs
        string jwt = PlayerPrefs.GetString("jwt");
        if (!string.IsNullOrEmpty(jwt))
        {
            request.SetRequestHeader("Authorization", $"Bearer {jwt}");
        }
        else
        {
            Debug.LogWarning("⚠️ No JWT token found. Replay will be rejected by the server if authentication is required.");
        }

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Replay sent to backend.");
        }
        else
        {
            Debug.LogError("❌ Failed to send replay: " + request.error);
        }
    }


    // === Internal class for final replay payload ===
    private class ReplayPayload
    {
        public CharacterWrapper player { get; set; }
        public CharacterWrapper enemy { get; set; }
        public List<MatchEventDTO> actions { get; set; }
        public string mapName { get; set; }
        public string winner { get; set; }
        public string timestamp { get; set; }
    }
}
