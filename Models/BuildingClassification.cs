namespace PropertyAssessmentApp.Models;

public class BuildingClassification
{
    public int Id { get; set; }
    public string ClassificationCode { get; set; } = string.Empty;
    public string ClassificationName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? StructuralTypeDefault { get; set; }
    public decimal DefaultUnitConstructionCost { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
