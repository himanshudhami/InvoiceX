namespace Application.DTOs.Assets;

public class CreateAssetDocumentDto
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public string? Notes { get; set; }
}
