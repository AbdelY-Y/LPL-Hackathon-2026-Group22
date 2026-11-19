using System.Text.Json;

namespace harbor.Models;

public static class HouseholdResults
{
    public static async Task<JsonElement> Load(string url)
    {
        // Only the team's S3 host is allowed for this temporary results connection.
        var address = new Uri(url);
        if (address.Scheme != "https" || address.Host != "lpl-financial-docs-team-10-2026-020467507399-us-east-1-an.s3.us-east-1.amazonaws.com")
            throw new InvalidDataException();
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(10) };
        using var response = await client.GetAsync(address);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.Clone();
    }

    public static HouseholdDemo Parse(JsonElement data)
    {
        var worth = data.GetProperty("net_worth");
        var result = new HouseholdDemo
        {
            HouseholdName = data.GetProperty("client_household").GetProperty("household_name").GetString()!,
            Fictional = data.GetProperty("_mock_metadata").GetProperty("fictional").GetBoolean(),
            TotalAssets = worth.GetProperty("total_assets").GetDecimal(),
            TotalLiabilities = worth.GetProperty("total_liabilities").GetDecimal(),
            EstimatedNetWorth = worth.GetProperty("estimated_net_worth").GetDecimal(),
            InvestableAssets = data.GetProperty("investable_assets").GetProperty("total").GetDecimal()
        };
        foreach (var account in data.GetProperty("asset_location").EnumerateObject())
            result.AssetLocation.Add(account.Name.Replace('_', ' '), account.Value.GetDecimal());
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        result.FinancialGoals = data.GetProperty("financial_goals").Deserialize<List<FinancialGoal>>(options)!;
        result.PlanningGaps = data.GetProperty("planning_gaps").Deserialize<List<PlanningGap>>(options)!;
        result.MissingDocuments = data.GetProperty("document_completeness").GetProperty("missing_categories").Deserialize<List<string>>()!;
        return result;
    }
}
