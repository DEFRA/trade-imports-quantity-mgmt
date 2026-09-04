using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TradeImportsQuantityMgmt.Contract;

/// <summary>
/// One consignment item to reserve against a customs declaration. <c>goodsItemNumber</c>,
/// <c>certificateLineNumber</c> and <c>classCode</c> are required, and weight and volume are
/// independent: an item may be reserved by either or both, but at least one is required, and a
/// quantity without its unit of measure is rejected with a 400.
/// </summary>
public record ReservationCommodityItem
{
    [JsonPropertyName("goodsItemNumber")]
    [Description("The item on the customs declaration this reservation is for.")]
    [Required(ErrorMessage = "goodsItemNumber is required.")]
    public int? GoodsItemNumber { get; init; }

    [JsonPropertyName("certificateLineNumber")]
    [Description("The CHED line to draw the quantity from.")]
    [Required]
    public int? CertificateLineNumber { get; init; }

    [JsonPropertyName("classCode")]
    [Description("Commodity class code as stated on the CHED line.")]
    [Required(ErrorMessage = "classCode is required.")]
    public string? ClassCode { get; init; }

    [JsonPropertyName("netWeightQuantity")]
    [Description("Net weight to reserve, expressed in netWeightUnitOfMeasure.")]
    [Range(0, double.MaxValue)]
    public decimal? NetWeightQuantity { get; init; }

    [JsonPropertyName("netWeightUnitOfMeasure")]
    [Description("UN/ECE Recommendation 20 unit code for netWeightQuantity, for example KGM.")]
    public UniversalUnitOfMeasureType? NetWeightUnitOfMeasure { get; init; }

    [JsonPropertyName("netVolumeQuantity")]
    [Description("Net volume to reserve, expressed in netVolumeUnitOfMeasure.")]
    [Range(0, double.MaxValue)]
    public decimal? NetVolumeQuantity { get; init; }

    [JsonPropertyName("netVolumeUnitOfMeasure")]
    [Description("UN/ECE Recommendation 20 unit code for netVolumeQuantity, for example MTQ.")]
    public UniversalUnitOfMeasureType? NetVolumeUnitOfMeasure { get; init; }
}
