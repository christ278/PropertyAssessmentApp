namespace PropertyAssessmentApp.Models;

public class LandReferenceSettingsViewModel
{
    public string ActiveTab { get; set; } = "classification";
    public string StatusFilter { get; set; } = "all";
    public string Search { get; set; } = string.Empty;

    public List<LandClassification> Classifications { get; set; } = new();
    public List<LandSubClassification> SubClassifications { get; set; } = new();
    public List<LandActualUse> ActualUses { get; set; } = new();
    public List<LandAssessmentLevel> AssessmentLevels { get; set; } = new();
    public List<LandAdjustmentFactor> AdjustmentFactors { get; set; } = new();
    public List<LandImprovementType> ImprovementTypes { get; set; } = new();

    public Dictionary<int, string> ClassificationNames { get; set; } = new();
    public Dictionary<int, string> ActualUseNames { get; set; } = new();
}
