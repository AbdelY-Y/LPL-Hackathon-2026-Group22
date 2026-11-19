using System.Text.Json;

namespace harbor.Models;

public class ServerFinancialResults
{
    public string ClientId { get; set; } = "";
    public string StatementDate { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public int DocumentsProcessed { get; set; }
    public decimal? TotalAssets { get; set; }
    public decimal? TotalLiabilities { get; set; }
    public decimal? NetWorth { get; set; }
    public Dictionary<string, decimal> Values { get; set; } = new();
    public List<string> DataGaps { get; set; } = new();

    public bool IsChartValue(string key)
    {
        string[] chartFields = { "equities_pct", "fixed_income_pct", "cash_pct",
            "alternatives_pct", "real_estate_pct", "crypto_pct", "monthly_inflows", "monthly_outflows" };
        return chartFields.Contains(key);
    }

    public static string Label(string key)
    {
        var text = key.Replace('_', ' ').Replace(" pct", "").Replace("gain loss", "gain / loss");
        return char.ToUpper(text[0]) + text.Substring(1);
    }

    public static string DisplayValue(string key, decimal value)
    {
        if (key.EndsWith("_pct")) return value.ToString("0.0") + "%";
        return FinancialProfile.Money(value);
    }

    public static ServerFinancialResults Parse(JsonElement response)
    {
        var data = response.GetProperty("financial_profile");
        var summary = data.GetProperty("client_summary");
        var result = new ServerFinancialResults
        {
            ClientId = response.GetProperty("client_id").GetString()!,
            DocumentsProcessed = response.GetProperty("documents_processed").GetInt32(),
            StatementDate = summary.GetProperty("statement_date").GetString() ?? "",
            DocumentType = summary.GetProperty("document_type").GetString() ?? "",
            DataGaps = data.GetProperty("data_gaps").Deserialize<List<string>>()!
        };
        // Keep only supplied numeric values. Missing values stay missing.
        foreach (var section in data.EnumerateObject())
        {
            if (section.Value.ValueKind != JsonValueKind.Object) continue;
            foreach (var field in section.Value.EnumerateObject())
            {
                if (field.Value.ValueKind == JsonValueKind.Number)
                    result.Values.Add(field.Name, field.Value.GetDecimal());
            }
        }
        result.TotalAssets = result.Values.TryGetValue("total_assets", out var totalAssets) ? totalAssets : null;
        result.TotalLiabilities = result.Values.TryGetValue("total_liabilities", out var totalLiabilities) ? totalLiabilities : null;
        result.NetWorth = result.Values.TryGetValue("net_worth", out var netWorth) ? netWorth : null;
        return result;
    }
}
