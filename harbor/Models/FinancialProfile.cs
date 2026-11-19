using System.ComponentModel.DataAnnotations;

namespace harbor.Models;

public class FinancialProfile
{
    public string AssetsText => Money(ServerResults != null ? ServerResults.TotalAssets : Demo?.TotalAssets);
    public string LiabilitiesText => Money(ServerResults != null ? ServerResults.TotalLiabilities : Demo?.TotalLiabilities);

    public static string Money(decimal? amount)
    {
        if (amount == null) return "—";
        return amount.Value.ToString("C0", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
    }

    public HouseholdDemo? Demo { get; set; }
    public ServerFinancialResults? ServerResults { get; set; }
    public bool UploadHeld { get; set; }
    public string? ResultsNotice { get; set; }
    public string? RedactionRun { get; set; }
    public List<RedactedDocument> RedactedDocuments { get; set; } = new();
    public int RedactionReviewCount => RedactedDocuments.Count(document => document.LowTextPages.Count > 0);
    public int Id { get; set; }
    [Required, StringLength(80), Display(Name = "Profile name")]
    public string Name { get; set; } = "";
    [Required, RegularExpression("Individual|Business"), Display(Name = "Profile type")]
    public string ProfileType { get; set; } = "Individual";
    public FinancialSummary Summary { get; set; } = new();
    public List<FinancialDocument> Documents { get; set; } = new();
    public List<ReviewItem> Reviews { get; set; } = new();
}
