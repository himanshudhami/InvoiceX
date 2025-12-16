using System.ComponentModel.DataAnnotations;
using Application.DTOs.Assets;

namespace Application.DTOs.Assets;

/// <summary>
/// Request payload for bulk asset creation.
/// </summary>
public class BulkAssetsDto
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one asset is required")]
    public List<CreateAssetDto> Assets { get; set; } = new();

    /// <summary>
    /// Whether to keep processing valid rows when validation fails on a row.
    /// </summary>
    public bool SkipValidationErrors { get; set; } = false;

    /// <summary>
    /// Created by (optional audit field).
    /// </summary>
    [StringLength(255, ErrorMessage = "Created by cannot exceed 255 characters")]
    public string? CreatedBy { get; set; }
}

/// <summary>
/// Result payload for bulk asset creation.
/// </summary>
public class BulkAssetsResultDto
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int TotalCount { get; set; }
    public List<BulkAssetsErrorDto> Errors { get; set; } = new();
    public List<Guid> CreatedIds { get; set; } = new();
}

/// <summary>
/// Error details for a specific row.
/// </summary>
public class BulkAssetsErrorDto
{
    public int RowNumber { get; set; }
    public string? AssetReference { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string? FieldName { get; set; }
}
