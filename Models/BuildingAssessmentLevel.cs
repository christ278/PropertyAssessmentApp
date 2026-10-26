namespace PropertyAssessmentApp.Models;

public class BuildingAssessmentLevel
{
    public int Id { get; set; }
    public int ActualUseId { get; set; }
    public decimal LowerBound { get; set; }
    public decimal UpperBound { get; set; }
    public decimal AssessmentLevel { get; set; }
    public int RevisionYear { get; set; } = 2011;
}
