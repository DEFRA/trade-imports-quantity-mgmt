using System.ComponentModel;
using System.Text.Json.Serialization;

namespace TradeImportsQuantityMgmt.Contract;

/// <summary>
/// Quantity reserved or consumed against one customs declaration. Upstream this wraps the amount in
/// a rounding-aware complex type;
/// </summary>
public record AllocatedCommodityQuantity
{
    [JsonPropertyName("goodsItemNumber")]
    [Description("The item on the customs declaration this allocation was made for.")]
    public int? GoodsItemNumber { get; init; }

    [JsonPropertyName("commodityCode")]
    public CommodityCode? CommodityCode { get; init; }

    [JsonPropertyName("certificateLineNumber")]
    [Description("The CHED line this allocation draws from.")]
    public int? CertificateLineNumber { get; init; }

    [JsonPropertyName("unitOfMeasure")]
    [Description(
        "UN/ECE Recommendation 20 unit code, for example KGM or TNE. Absent when TracesNT did not "
            + "state one — never assume a default."
    )]
    public UniversalUnitOfMeasureType? UnitOfMeasure { get; init; }

    [JsonPropertyName("quantity")]
    [Description("The allocated amount, expressed in unitOfMeasure.")]
    public required decimal Quantity { get; init; }

    [JsonPropertyName("technicalRoundingQuantity")]
    [Description("Amount added or removed by TracesNT to reconcile unit conversion rounding.")]
    public decimal? TechnicalRoundingQuantity { get; init; }

    [JsonPropertyName("eventDateTime")]
    [Description("When the allocation was made.")]
    public DateTimeOffset? EventDateTime { get; init; }

    [JsonPropertyName("customsOffice")]
    [Description("Reference number of the customs office that made the allocation.")]
    public string? CustomsOffice { get; init; }

    [JsonPropertyName("declarationReference")]
    public DeclarationReference? DeclarationReference { get; init; }
}
