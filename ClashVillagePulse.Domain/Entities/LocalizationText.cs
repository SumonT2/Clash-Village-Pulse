namespace ClashVillagePulse.Domain.Entities;

public class LocalizationText
{
    public Guid Id { get; set; }

    public string Tid { get; set; } = null!;

    public string LanguageCode { get; set; } = "EN";

    public string Text { get; set; } = null!;
}