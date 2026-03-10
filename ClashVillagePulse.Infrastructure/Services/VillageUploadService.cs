using System.Text.Json;
using ClashVillagePulse.Application.DTOs;
using ClashVillagePulse.Application.Interfaces;
using ClashVillagePulse.Domain.Entities;
using ClashVillagePulse.Domain.Enums;
using ClashVillagePulse.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace ClashVillagePulse.Infrastructure.Services;

public sealed class VillageUploadService : IVillageUploadService
{
    private readonly AppDbContext _db;

    public VillageUploadService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<VillageUploadResultDto> UploadAsync(
        string ownerUserId,
        string json,
        CancellationToken cancellationToken = default)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        string playerTag = GetRequiredString(root, "tag");
        long? gameTimestamp = root.TryGetProperty("timestamp", out var ts) && ts.ValueKind == JsonValueKind.Number
            ? ts.GetInt64()
            : null;

        string villageName = root.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
            ? nameEl.GetString()!.Trim()
            : playerTag;

        string? clanTag = TryGetString(root, "clanTag");
        string? clanName = TryGetString(root, "clanName");

        Clan? clan = null;
        bool clanLinked = false;

        if (!string.IsNullOrWhiteSpace(clanTag))
        {
            clan = await _db.Clans
                .FirstOrDefaultAsync(x => x.ClanTag == clanTag, cancellationToken);

            if (clan is null)
            {
                clan = new Clan
                {
                    Id = Guid.NewGuid(),
                    ClanTag = clanTag!,
                    Name = string.IsNullOrWhiteSpace(clanName) ? clanTag! : clanName!
                };

                _db.Clans.Add(clan);
            }
            else if (!string.IsNullOrWhiteSpace(clanName) && clan.Name != clanName)
            {
                clan.Name = clanName!;
                clan.UpdatedAtUtc = DateTime.UtcNow;
            }

            clanLinked = true;
        }

        var village = await _db.Villages
            .Include(x => x.ItemLevels)
            .FirstOrDefaultAsync(x => x.PlayerTag == playerTag, cancellationToken);

        bool villageCreated = false;

        if (village is null)
        {
            village = new Village
            {
                Id = Guid.NewGuid(),
                OwnerUserId = ownerUserId,
                PlayerTag = playerTag,
                Name = villageName,
                ClanId = clan?.Id,
                ClanTag = clanTag,
                ClanName = clanName,
                LastGameTimestamp = gameTimestamp,
                LastUploadedAtUtc = DateTime.UtcNow
            };

            _db.Villages.Add(village);
            villageCreated = true;
        }
        else
        {
            village.OwnerUserId = ownerUserId;
            village.Name = villageName;
            village.ClanId = clan?.Id;
            village.ClanTag = clanTag;
            village.ClanName = clanName;
            village.LastGameTimestamp = gameTimestamp;
            village.LastUploadedAtUtc = DateTime.UtcNow;
            village.IsArchived = false;
        }

        if (clan is not null)
        {
            bool memberExists = await _db.ClanMembers.AnyAsync(
                x => x.ClanId == clan.Id && x.UserId == ownerUserId,
                cancellationToken);

            if (!memberExists)
            {
                _db.ClanMembers.Add(new ClanMember
                {
                    Id = Guid.NewGuid(),
                    ClanId = clan.Id,
                    UserId = ownerUserId
                });
            }
        }

        // Remove old latest-state rows
        var oldItems = await _db.VillageItemLevels
            .Where(x => x.VillageId == village.Id)
            .ToListAsync(cancellationToken);

        if (oldItems.Count > 0)
            _db.VillageItemLevels.RemoveRange(oldItems);

        var items = ParseVillageItems(root, village.Id);

        if (items.Count > 0)
            _db.VillageItemLevels.AddRange(items);

        await _db.SaveChangesAsync(cancellationToken);

        return new VillageUploadResultDto
        {
            VillageId = village.Id,
            PlayerTag = village.PlayerTag,
            VillageName = village.Name,
            TotalItemsImported = items.Count,
            VillageCreated = villageCreated,
            ClanLinked = clanLinked,
            ClanTag = clanTag,
            ClanName = clanName
        };
    }

    private static List<VillageItemLevel> ParseVillageItems(JsonElement root, Guid villageId)
    {
        var items = new List<VillageItemLevel>();

        // Home village
        AddObjectArray(root, "helpers", VillageSection.HomeVillage, ItemType.Helper, villageId, items);
        AddObjectArray(root, "guardians", VillageSection.HomeVillage, ItemType.Guardian, villageId, items);
        AddObjectArray(root, "buildings", VillageSection.HomeVillage, ItemType.Building, villageId, items);
        AddObjectArray(root, "traps", VillageSection.HomeVillage, ItemType.Trap, villageId, items);
        AddObjectArray(root, "decos", VillageSection.HomeVillage, ItemType.Decoration, villageId, items);
        AddObjectArray(root, "obstacles", VillageSection.HomeVillage, ItemType.Obstacle, villageId, items);
        AddObjectArray(root, "units", VillageSection.HomeVillage, ItemType.Troop, villageId, items);
        AddObjectArray(root, "siege_machines", VillageSection.HomeVillage, ItemType.SiegeMachine, villageId, items);
        AddObjectArray(root, "heroes", VillageSection.HomeVillage, ItemType.Hero, villageId, items);
        AddObjectArray(root, "spells", VillageSection.HomeVillage, ItemType.Spell, villageId, items);
        AddObjectArray(root, "pets", VillageSection.HomeVillage, ItemType.Pet, villageId, items);
        AddObjectArray(root, "equipment", VillageSection.HomeVillage, ItemType.Equipment, villageId, items);

        AddIntArray(root, "house_parts", VillageSection.HomeVillage, ItemType.HousePart, villageId, items);
        AddIntArray(root, "skins", VillageSection.HomeVillage, ItemType.Skin, villageId, items);
        AddIntArray(root, "sceneries", VillageSection.HomeVillage, ItemType.Scenery, villageId, items);

        // Builder base
        AddObjectArray(root, "buildings2", VillageSection.BuilderBase, ItemType.Building, villageId, items);
        AddObjectArray(root, "traps2", VillageSection.BuilderBase, ItemType.Trap, villageId, items);
        AddObjectArray(root, "decos2", VillageSection.BuilderBase, ItemType.Decoration, villageId, items);
        AddObjectArray(root, "obstacles2", VillageSection.BuilderBase, ItemType.Obstacle, villageId, items);
        AddObjectArray(root, "units2", VillageSection.BuilderBase, ItemType.Troop, villageId, items);
        AddObjectArray(root, "heroes2", VillageSection.BuilderBase, ItemType.Hero, villageId, items);

        AddIntArray(root, "skins2", VillageSection.BuilderBase, ItemType.Skin, villageId, items);
        AddIntArray(root, "sceneries2", VillageSection.BuilderBase, ItemType.Scenery, villageId, items);

        return items;
    }

    private static void AddObjectArray(
        JsonElement root,
        string propertyName,
        VillageSection section,
        ItemType itemType,
        Guid villageId,
        List<VillageItemLevel> items)
    {
        if (!root.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
            return;

        foreach (var entry in array.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
                continue;

            // Normal item
            if (entry.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Number)
            {
                int itemDataId = dataEl.GetInt32();
                int level = entry.TryGetProperty("lvl", out var lvlEl) && lvlEl.ValueKind == JsonValueKind.Number
                    ? lvlEl.GetInt32()
                    : 1;

                int count = entry.TryGetProperty("cnt", out var cntEl) && cntEl.ValueKind == JsonValueKind.Number
                    ? cntEl.GetInt32()
                    : 1;

                items.Add(new VillageItemLevel
                {
                    Id = Guid.NewGuid(),
                    VillageId = villageId,
                    Section = section,
                    ItemType = itemType,
                    ItemDataId = itemDataId,
                    Level = level,
                    Count = count,
                    UpgradeTimerSeconds = TryGetInt(entry, "timer"),
                    HelperCooldownSeconds = TryGetInt(entry, "helper_cooldown"),
                    HelperTimerSeconds = TryGetInt(entry, "helper_timer"),
                    IsExtra = TryGetBool(entry, "extra"),
                    IsGearUp = TryGetInt(entry, "gear_up") is int gearUp && gearUp > 0,
                    IsHelperRecurrent = TryGetBool(entry, "helper_recurrent"),
                    SuperchargeLevel = TryGetInt(entry, "supercharge"),
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }

            // Nested modules in buildings/types/modules
            if (entry.TryGetProperty("types", out var typesEl) && typesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var typeEntry in typesEl.EnumerateArray())
                {
                    if (!typeEntry.TryGetProperty("modules", out var modulesEl) || modulesEl.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (var module in modulesEl.EnumerateArray())
                    {
                        if (!module.TryGetProperty("data", out var modDataEl) || modDataEl.ValueKind != JsonValueKind.Number)
                            continue;

                        items.Add(new VillageItemLevel
                        {
                            Id = Guid.NewGuid(),
                            VillageId = villageId,
                            Section = section,
                            ItemType = itemType,
                            ItemDataId = modDataEl.GetInt32(),
                            Level = module.TryGetProperty("lvl", out var modLvlEl) && modLvlEl.ValueKind == JsonValueKind.Number
                                ? modLvlEl.GetInt32()
                                : 1,
                            Count = 1,
                            UpgradeTimerSeconds = TryGetInt(module, "timer"),
                            UpdatedAtUtc = DateTime.UtcNow
                        });
                    }
                }
            }
        }
    }

    private static void AddIntArray(
        JsonElement root,
        string propertyName,
        VillageSection section,
        ItemType itemType,
        Guid villageId,
        List<VillageItemLevel> items)
    {
        if (!root.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
            return;

        foreach (var entry in array.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Number)
                continue;

            items.Add(new VillageItemLevel
            {
                Id = Guid.NewGuid(),
                VillageId = villageId,
                Section = section,
                ItemType = itemType,
                ItemDataId = entry.GetInt32(),
                Level = 1,
                Count = 1,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
    }

    private static string GetRequiredString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var el) || el.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException($"Missing required property '{propertyName}'.");

        var value = el.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Property '{propertyName}' is empty.");

        return value;
    }

    private static string? TryGetString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var el) || el.ValueKind != JsonValueKind.String)
            return null;

        var value = el.GetString()?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static int? TryGetInt(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var el) || el.ValueKind != JsonValueKind.Number)
            return null;

        return el.GetInt32();
    }

    private static bool TryGetBool(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var el))
            return false;

        return el.ValueKind == JsonValueKind.True ||
               (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out int n) && n > 0);
    }
}