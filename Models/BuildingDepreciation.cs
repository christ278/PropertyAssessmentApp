namespace PropertyAssessmentApp.Models;

public class BuildingDepreciation
{
    public int Id { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public int ActualUseId { get; set; }
    public string DegreeOfMaintenance { get; set; } = string.Empty;
    public int AgeLowerBound { get; set; }
    public int AgeUpperBound { get; set; }
    public decimal PercentDepreciated { get; set; }
}
