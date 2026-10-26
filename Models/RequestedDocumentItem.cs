namespace PropertyAssessmentApp.Models;

public class RequestedDocumentItem
{
    public Guid FeeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Particular { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal LineTotal => UnitCost * Quantity;
}
