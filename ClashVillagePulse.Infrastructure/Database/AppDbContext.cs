using ClashVillagePulse.Domain.Entities;
using ClashVillagePulse.Domain.Enums;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClashVillagePulse.Infrastructure.Database;

public class AppDbContext : IdentityDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Clan> Clans => Set<Clan>();
    public DbSet<ClanMember> ClanMembers => Set<ClanMember>();
    public DbSet<Village> Villages => Set<Village>();
    public DbSet<VillageItemLevel> VillageItemLevels => Set<VillageItemLevel>();
    public DbSet<ClanPriorityItem> ClanPriorityItems => Set<ClanPriorityItem>();
    public DbSet<VillagePriorityItem> VillagePriorityItems => Set<VillagePriorityItem>();
    public DbSet<PrioritySuggestion> PrioritySuggestions => Set<PrioritySuggestion>();


    public DbSet<StaticDataRun> StaticDataRuns => Set<StaticDataRun>();
    public DbSet<StaticDataRunStep> StaticDataRunSteps => Set<StaticDataRunStep>();

    public DbSet<StaticItem> StaticItems => Set<StaticItem>();
    public DbSet<StaticItemLevel> StaticItemLevels => Set<StaticItemLevel>();
    public DbSet<StaticItemLevelUpgradeCost> StaticItemLevelUpgradeCosts => Set<StaticItemLevelUpgradeCost>();
    public DbSet<StaticItemRequirement> StaticItemRequirements => Set<StaticItemRequirement>();
    public DbSet<LocalizationText> LocalizationTexts => Set<LocalizationText>();
    public DbSet<StaticHallItemCap> StaticHallItemCaps => Set<StaticHallItemCap>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureClan(builder);
        ConfigureClanMember(builder);
        ConfigureVillage(builder);
        ConfigureVillageItem(builder);
        ConfigureClanPriority(builder);
        ConfigureVillagePriority(builder);
        ConfigureSuggestion(builder);

        ConfigureStaticDataRun(builder);
        ConfigureStaticDataRunStep(builder);
        ConfigureStaticItem(builder);
        ConfigureStaticItemLevel(builder);
        ConfigureStaticItemLevelUpgradeCost(builder);
        ConfigureStaticItemRequirement(builder);
        ConfigureStaticHallItemCap(builder);
        ConfigureLocalizationText(builder);
    }

    private static void ConfigureClan(ModelBuilder builder)
    {
        var e = builder.Entity<Clan>();

        e.ToTable("clans");

        e.HasKey(x => x.Id);

        e.Property(x => x.ClanTag)
            .HasMaxLength(20)
            .IsRequired();

        e.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        e.HasIndex(x => x.ClanTag)
            .IsUnique();

        e.HasMany(x => x.Members)
            .WithOne(x => x.Clan)
            .HasForeignKey(x => x.ClanId);

        e.HasMany(x => x.Villages)
            .WithOne(x => x.Clan)
            .HasForeignKey(x => x.ClanId);
    }

        private static void ConfigureClanMember(ModelBuilder builder)
    {
        var e = builder.Entity<ClanMember>();

        e.ToTable("clan_members");

        e.HasKey(x => x.Id);

        e.Property(x => x.UserId)
            .HasMaxLength(450)
            .IsRequired();

        e.HasIndex(x => new { x.ClanId, x.UserId })
            .IsUnique();
    }

        private static void ConfigureVillage(ModelBuilder builder)
    {
        var e = builder.Entity<Village>();

        e.ToTable("villages");

        e.HasKey(x => x.Id);

        e.Property(x => x.PlayerTag)
            .HasMaxLength(20)
            .IsRequired();

        e.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        e.Property(x => x.OwnerUserId)
            .HasMaxLength(450)
            .IsRequired();

        e.HasIndex(x => x.PlayerTag)
            .IsUnique();

        e.HasMany(x => x.ItemLevels)
            .WithOne(x => x.Village)
            .HasForeignKey(x => x.VillageId);

        e.HasMany(x => x.PriorityItems)
            .WithOne(x => x.Village)
            .HasForeignKey(x => x.VillageId);

        e.HasMany(x => x.Suggestions)
            .WithOne(x => x.Village)
            .HasForeignKey(x => x.VillageId);
    }

    private static void ConfigureVillageItem(ModelBuilder builder)
    {
        var e = builder.Entity<VillageItemLevel>();

        e.ToTable("village_item_levels");

        e.HasKey(x => x.Id);

        e.Property(x => x.ItemType)
            .HasConversion<int>();

        e.Property(x => x.Section)
            .HasConversion<int>();

        e.HasIndex(x => new
        {
            x.VillageId,
            x.Section,
            x.ItemType,
            x.ItemDataId,
            x.Level
        });
    }
    private static void ConfigureClanPriority(ModelBuilder builder)
    {
        var e = builder.Entity<ClanPriorityItem>();

        e.ToTable("clan_priority_items");

        e.HasKey(x => x.Id);

        e.Property(x => x.ItemType)
            .HasConversion<int>();

        e.Property(x => x.Section)
            .HasConversion<int>();

        e.HasIndex(x => new
        {
            x.ClanId,
            x.Section,
            x.TownHallLevel,
            x.BuilderHallLevel,
            x.ItemType,
            x.ItemDataId
        }).IsUnique();
    }

        private static void ConfigureVillagePriority(ModelBuilder builder)
    {
        var e = builder.Entity<VillagePriorityItem>();

        e.ToTable("village_priority_items");

        e.HasKey(x => x.Id);

        e.Property(x => x.ItemType)
            .HasConversion<int>();

        e.Property(x => x.Section)
            .HasConversion<int>();

        e.HasIndex(x => new
        {
            x.VillageId,
            x.Section,
            x.ItemType,
            x.ItemDataId
        }).IsUnique();
    }


    private static void ConfigureSuggestion(ModelBuilder builder)
    {
        var e = builder.Entity<PrioritySuggestion>();

        e.ToTable("priority_suggestions");

        e.HasKey(x => x.Id);

        e.Property(x => x.Status)
            .HasConversion<int>();

        e.Property(x => x.ItemType)
            .HasConversion<int>();

        e.Property(x => x.Section)
            .HasConversion<int>();

        e.HasIndex(x => new { x.VillageId, x.Status, x.CreatedAtUtc });
        e.HasIndex(x => new { x.VillageId, x.Section, x.ItemType, x.ItemDataId });
    }

    private static void ConfigureStaticDataRun(ModelBuilder builder)
    {
        var e = builder.Entity<StaticDataRun>();

        e.ToTable("static_data_runs");

        e.HasKey(x => x.Id);

        e.Property(x => x.Fingerprint)
            .HasMaxLength(100)
            .IsRequired();

        e.Property(x => x.RequestedByUserId)
            .HasMaxLength(450)
            .IsRequired();

        e.Property(x => x.Status)
            .HasConversion<int>();

        e.Property(x => x.Message)
            .HasMaxLength(2000);

        e.HasIndex(x => x.RequestedAtUtc);
        e.HasIndex(x => x.Status);

        e.HasMany(x => x.Steps)
            .WithOne(x => x.StaticDataRun)
            .HasForeignKey(x => x.StaticDataRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
    private static void ConfigureStaticDataRunStep(ModelBuilder builder)
    {
        var e = builder.Entity<StaticDataRunStep>();

        e.ToTable("static_data_run_steps");

        e.HasKey(x => x.Id);

        e.Property(x => x.TargetKey)
            .HasMaxLength(100)
            .IsRequired();

        e.Property(x => x.StepType)
            .HasConversion<int>();

        e.Property(x => x.Status)
            .HasConversion<int>();

        e.Property(x => x.Message)
            .HasMaxLength(2000);

        e.HasIndex(x => new { x.StaticDataRunId, x.TargetKey, x.StepType })
            .IsUnique();

        e.HasIndex(x => x.Status);
    }
    private static void ConfigureStaticItem(ModelBuilder builder)
    {
        var e = builder.Entity<StaticItem>();

        e.ToTable("static_items");

        e.HasKey(x => x.Id);

        e.Property(x => x.ItemType)
            .HasConversion<int>();

        e.Property(x => x.Section)
            .HasConversion<int>();

        e.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        e.HasIndex(x => new { x.ItemDataId, x.ItemType, x.Section })
            .IsUnique();

        e.HasMany(x => x.Levels)
            .WithOne(x => x.StaticItem)
            .HasForeignKey(x => x.StaticItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureStaticItemLevel(ModelBuilder builder)
    {
        var e = builder.Entity<StaticItemLevel>();

        e.ToTable("static_item_levels");

        e.HasKey(x => x.Id);

        e.HasIndex(x => new { x.StaticItemId, x.Level })
            .IsUnique();

        e.HasMany(x => x.UpgradeCosts)
            .WithOne(x => x.StaticItemLevel)
            .HasForeignKey(x => x.StaticItemLevelId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasMany(x => x.Requirements)
            .WithOne(x => x.StaticItemLevel)
            .HasForeignKey(x => x.StaticItemLevelId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureStaticItemLevelUpgradeCost(ModelBuilder builder)
    {
        var e = builder.Entity<StaticItemLevelUpgradeCost>();

        e.ToTable("static_item_level_upgrade_costs");

        e.HasKey(x => x.Id);

        e.Property(x => x.ResourceType)
            .HasConversion<int>();

        e.HasIndex(x => new { x.StaticItemLevelId, x.ResourceType })
            .IsUnique();
    }

    private static void ConfigureStaticItemRequirement(ModelBuilder builder)
    {
        var e = builder.Entity<StaticItemRequirement>();

        e.ToTable("static_item_requirements");

        e.HasKey(x => x.Id);

        e.Property(x => x.RequirementType)
            .HasConversion<int>();

        e.HasIndex(x => new
        {
            x.StaticItemLevelId,
            x.RequirementType,
            x.RequiredItemDataId,
            x.RequiredLevel
        }).IsUnique(false);
    }

    private static void ConfigureLocalizationText(ModelBuilder builder)
    {
        var e = builder.Entity<LocalizationText>();

        e.ToTable("localization_texts");

        e.HasKey(x => x.Id);

        e.Property(x => x.Tid)
            .HasMaxLength(200)
            .IsRequired();

        e.Property(x => x.LanguageCode)
            .HasMaxLength(10)
            .IsRequired();

        e.Property(x => x.Text)
            .HasMaxLength(4000)
            .IsRequired();

        e.HasIndex(x => new { x.Tid, x.LanguageCode })
            .IsUnique();
    }

    private static void ConfigureStaticHallItemCap(ModelBuilder builder)
    {
        var e = builder.Entity<StaticHallItemCap>();

        e.ToTable("static_hall_item_caps");

        e.HasKey(x => x.Id);

        e.Property(x => x.Section)
            .HasConversion<int>();

        e.Property(x => x.ItemType)
            .HasConversion<int>();

        e.HasIndex(x => new
        {
            x.Section,
            x.HallLevel,
            x.ItemType,
            x.ItemDataId
        }).IsUnique();
    }
}