namespace harbor.Models;

// Selected fields from the team's fictional household JSON.
public class HouseholdDemo
{
    public bool Fictional { get; set; }
    public List<FinancialGoal> FinancialGoals { get; set; } = new();
    public string HouseholdName { get; set; } = "";
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal EstimatedNetWorth { get; set; }
    public decimal InvestableAssets { get; set; }
    public Dictionary<string, decimal> AssetLocation { get; set; } = new();
    public List<PlanningGap> PlanningGaps { get; set; } = new();
    public List<string> MissingDocuments { get; set; } = new();
}

public class PlanningGap
{
    public string Category { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Finding { get; set; } = "";
}

public class FinancialGoal
{
    public string Name { get; set; } = "";
    public int FundedPercent { get; set; }
}
