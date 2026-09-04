using System.ComponentModel.DataAnnotations;

namespace TradeImportsQuantityMgmt.Client;

public class QuantityManagementClientOptions
{
    public const string SectionName = "QuantityManagementClient";

    [Required]
    public required string BaseUrl { get; init; }

    [Required]
    public string Audience { get; init; } = "decision-deriver";

    // Platform policy caps sts:GetWebIdentityToken token lifetime at 900s
    public int DurationSeconds { get; init; } = 900;
}
