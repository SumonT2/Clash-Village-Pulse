using ClashVillagePulse.Application.DTOs;
using ClashVillagePulse.Application.Interfaces;
using ClashVillagePulse.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace ClashVillagePulse.Infrastructure.Services;

public sealed class VillageQueryService : IVillageQueryService
{
    private readonly AppDbContext _db;

    public VillageQueryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<VillageListItemDto>> GetMyVillagesAsync(
        string ownerUserId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Villages
            .AsNoTracking()
            .Where(x => x.OwnerUserId == ownerUserId && !x.IsArchived)
            .OrderBy(x => x.Name)
            .Select(x => new VillageListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                PlayerTag = x.PlayerTag,
                ClanName = x.ClanName,
                ClanTag = x.ClanTag,
                LastUploadedAtUtc = x.LastUploadedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<VillageDetailsDto?> GetVillageDetailsAsync(
        string ownerUserId,
        Guid villageId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Villages
            .AsNoTracking()
            .Where(x => x.Id == villageId && x.OwnerUserId == ownerUserId && !x.IsArchived)
            .Select(x => new VillageDetailsDto
            {
                Id = x.Id,
                Name = x.Name,
                PlayerTag = x.PlayerTag,
                ClanName = x.ClanName,
                ClanTag = x.ClanTag,
                TownHallLevel = x.TownHallLevel,
                BuilderHallLevel = x.BuilderHallLevel,
                LastUploadedAtUtc = x.LastUploadedAtUtc,
                Items = x.ItemLevels
                    .OrderBy(i => i.Section)
                    .ThenBy(i => i.ItemType)
                    .ThenBy(i => i.ItemDataId)
                    .ThenBy(i => i.Level)
                    .Select(i => new VillageItemLevelDto
                    {
                        Section = i.Section,
                        ItemType = i.ItemType,
                        ItemDataId = i.ItemDataId,
                        Level = i.Level,
                        Count = i.Count,
                        UpgradeTimerSeconds = i.UpgradeTimerSeconds
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}