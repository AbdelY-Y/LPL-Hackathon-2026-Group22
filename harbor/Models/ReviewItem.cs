namespace harbor.Models;

public class ReviewItem
{
    public string Title { get; set; } = "";
    public string Evidence { get; set; } = "";
    public string SecondEvidence { get; set; } = "";
    public string Message { get; set; } = "";
    public bool IsConflict { get; set; }
}
