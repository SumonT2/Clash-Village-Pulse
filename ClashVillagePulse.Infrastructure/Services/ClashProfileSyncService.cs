using ClashVillagePulse.Application.DTOs;
using ClashVillagePulse.Application.Interfaces;
using ClashVillagePulse.Domain.Entities;
using ClashVillagePulse.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace ClashVillagePulse.Infrastructure.Services;

public sealed class ClashProfileSyncService : IClashProfileSyncService
{
    private readonly AppDbContext _dbContext;
    private readonly IClashApiService _clashApiService;

    public ClashProfileSyncService(
        AppDbContext dbContext,
        IClashApiService clashApiService)
    {
        _dbContext = dbContext;
        _clashApiService = clashApiService;
    }

    public async Task<PlayerProfileDto> SyncVillageProfileAsync(
        Guid villageId,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        var village = await _dbContext.Villages
            .FirstOrDefaultAsync(x => x.Id == villageId && x.OwnerUserId == currentUserId, cancellationToken);

        if (village is null)
            throw new InvalidOperationException("Village not found or access denied.");

        var profile = await _clashApiService.GetPlayerProfileAsync(village.PlayerTag, cancellationToken);

        village.Name = !string.IsNullOrWhiteSpace(profile.PlayerName)
            ? profile.PlayerName!.Trim()
            : village.Name;

        village.TownHallLevel = profile.TownHallLevel ?? village.TownHallLevel;
        village.BuilderHallLevel = profile.BuilderHallLevel ?? village.BuilderHallLevel;

        Clan? clan = null;
        string? normalizedClanTag = NormalizeOptionalTag(profile.ClanTag);
        string? clanName = string.IsNullOrWhiteSpace(profile.ClanName)
            ? null
            : profile.ClanName.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedClanTag))
        {
            clan = await _dbContext.Clans
                .FirstOrDefaultAsync(x => x.ClanTag == normalizedClanTag, cancellationToken);

            if (clan is null)
            {
                clan = new Clan
                {
                    Id = Guid.NewGuid(),
                    ClanTag = normalizedClanTag,
                    Name = clanName ?? normalizedClanTag
                };

                _dbContext.Clans.Add(clan);
            }
            else if (!string.IsNullOrWhiteSpace(clanName) && clan.Name != clanName)
            {
                clan.Name = clanName;
                clan.UpdatedAtUtc = DateTime.UtcNow;
            }

            village.ClanId = clan.Id;
            village.ClanTag = normalizedClanTag;
            village.ClanName = clanName ?? clan.Name;

            bool membershipExists = await _dbContext.ClanMembers.AnyAsync(
                x => x.ClanId == clan.Id && x.UserId == currentUserId,
                cancellationToken);

            if (!membershipExists)
            {
                _dbContext.ClanMembers.Add(new ClanMember
                {
                    Id = Guid.NewGuid(),
                    ClanId = clan.Id,
                    UserId = currentUserId
                });
            }
        }
        else
        {
            village.ClanId = null;
            village.ClanTag = null;
            village.ClanName = null;
        }

        village.LastUploadedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return profile;
    }

    private static string? NormalizeOptionalTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return null;

        string value = tag.Trim().ToUpperInvariant();
        if (!value.StartsWith("#"))
            value = "#" + value;

        return value;
    }
}