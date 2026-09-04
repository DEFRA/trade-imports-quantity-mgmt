using System.ComponentModel;
using System.Text.Json.Serialization;

namespace TradeImportsQuantityMgmt.Contract;

public record CommodityCode
{
    [JsonPropertyName("harmonizedSystemSubheadingCode")]
    [Description("Harmonized System subheading, 6 digits.")]
    public string? HarmonizedSystemSubheadingCode { get; init; }

    [JsonPropertyName("combinedNomenclatureCode")]
    [Description("Combined Nomenclature code, extending the HS subheading to 8 digits.")]
    public string? CombinedNomenclatureCode { get; init; }

    [JsonPropertyName("taricCode")]
    [Description("TARIC code, extending the Combined Nomenclature code to 10 digits.")]
    public string? TaricCode { get; init; }
}
