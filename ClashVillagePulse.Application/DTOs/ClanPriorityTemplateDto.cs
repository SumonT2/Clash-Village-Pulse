using ClashVillagePulse.Domain.Enums;

namespace ClashVillagePulse.Application.DTOs;

public sealed class ClanPriorityTemplateDto
{
    public Guid VillageId { get; set; }
    public Guid ClanId { get; set; }
    public string ClanName { get; set; } = null!;
    public VillageSection Section { get; set; }
    public int HallLevel { get; set; }
    public List<VillageItemStateDto> Items { get; set; } = new();
}