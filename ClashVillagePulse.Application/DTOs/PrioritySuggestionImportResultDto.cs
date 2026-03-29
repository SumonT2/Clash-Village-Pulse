namespace ClashVillagePulse.Application.DTOs;

public sealed class PrioritySuggestionImportResultDto
{
    public string TargetVillageName { get; set; } = string.Empty;
    public int SourcePriorityCount { get; set; }
    public int AddedCount { get; set; }
    public int MissingInTargetCount { get; set; }
    public int AlreadyPrioritizedCount { get; set; }
    public int AlreadyPendingCount { get; set; }
}
