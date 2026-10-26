namespace PropertyAssessmentApp.Models;

public class BuildingAdditionalItem
{
    public int Id { get; set; }
    public string AdditionalItemDescription { get; set; } = string.Empty;
    public decimal PercentUnitValue { get; set; }
    public decimal CostPerSQM { get; set; }
    public decimal ExcessSQM { get; set; }
    public decimal ExcessPercentage { get; set; }
    public int RevisionYear { get; set; } = 2011;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
