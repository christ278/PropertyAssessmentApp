namespace PropertyAssessmentApp.Models;

public class BuildingStructureRate
{
    public int Id { get; set; }
    public int BuildingStructuralTypeId { get; set; }
    public int BuildingTypeId { get; set; }
    public decimal MinRate { get; set; }
    public decimal MaxRate { get; set; }
    public int RevisionYear { get; set; } = 2011;
}
