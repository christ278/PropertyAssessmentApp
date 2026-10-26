namespace PropertyAssessmentApp.Models;

public class BuildingPropertyTypeAccordUse
{
    public int Id { get; set; }
    public string PropertyTypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
