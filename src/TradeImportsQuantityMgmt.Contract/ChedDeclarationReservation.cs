using System.ComponentModel;
using System.Text.Json.Serialization;

namespace TradeImportsQuantityMgmt.Contract;

/// <summary>
/// What one customs declaration holds against one CHED, after a reservation. Narrowed to the
/// declaration in the request URL.
/// </summary>
[MediaType("application/vnd.defra.trade.ched-reservation.v1+json")]
public record ChedDeclarationReservation
{
    [JsonPropertyName("reserved")]
    [Description("Quantities this declaration holds against the CHED but has not yet consumed.")]
    public required AllocatedCommodityQuantity[] Reserved { get; init; }

    [JsonPropertyName("consumed")]
    [Description("Quantities this declaration has written off against the CHED.")]
    public required AllocatedCommodityQuantity[] Consumed { get; init; }
}
