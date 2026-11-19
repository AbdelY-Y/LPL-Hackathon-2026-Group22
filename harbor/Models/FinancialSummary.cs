namespace harbor.Models;

public class FinancialSummary
{
    public decimal Assets { get; set; }
    public decimal Liabilities { get; set; }
    public decimal Investments { get; set; }
    public decimal Business { get; set; }
    public decimal Property { get; set; }
    public decimal Mortgage { get; set; }
    public decimal BusinessDebt { get; set; }
    public string AssetsDisplay => $"${Assets / 1000000:0.00}M";
}
