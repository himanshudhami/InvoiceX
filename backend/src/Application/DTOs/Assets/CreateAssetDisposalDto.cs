using System;

namespace Application.DTOs.Assets;

public class CreateAssetDisposalDto
{
    public DateTime? DisposedOn { get; set; }
    public string Method { get; set; } = "retired";
    public decimal? Proceeds { get; set; }
    public decimal? DisposalCost { get; set; }
    public string? Currency { get; set; }
    public string? Buyer { get; set; }
    public string? Notes { get; set; }
}
