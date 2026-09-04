using System.ComponentModel;
using System.Text.Json.Serialization;

namespace TradeImportsQuantityMgmt.Contract;

/// <summary>
/// The customs declaration an allocation was made against. Upstream this is a choice between an LRN
/// and an MRN, and both carry the same underlying string, so the discriminator is never inferable
/// from the value alone.
/// </summary>
public record DeclarationReference
{
    [JsonPropertyName("type")]
    [Description("Which kind of declaration reference this is: MRN or LRN.")]
    public required DeclarationReferenceType Type { get; init; }

    [JsonPropertyName("value")]
    [Description("The declaration reference itself.")]
    public required string Value { get; init; }
}
