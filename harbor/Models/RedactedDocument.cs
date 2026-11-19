using System.Text.Json.Serialization;

namespace harbor.Models;

public class RedactedDocument
{
    [JsonPropertyName("input_file")]
    public string InputFile { get; set; } = "";
    [JsonPropertyName("output_file")]
    public string OutputFile { get; set; } = "";
    [JsonPropertyName("total_redactions")]
    public int TotalRedactions { get; set; }
    [JsonPropertyName("image_only_or_low_text_pages")]
    public List<int> LowTextPages { get; set; } = new();
}

public class RedactionResponse
{
    [JsonPropertyName("upload_held")]
    public bool UploadHeld { get; set; }
    [JsonPropertyName("s3_uri")]
    public string? S3Uri { get; set; }
    [JsonPropertyName("outputs")]
    public List<RedactedDocument> Outputs { get; set; } = new();
}
