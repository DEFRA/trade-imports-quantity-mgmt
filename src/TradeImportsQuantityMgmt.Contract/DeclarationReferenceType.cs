using System.Text.Json.Serialization;

namespace TradeImportsQuantityMgmt.Contract;

/// <remarks>
/// Members are PascalCase for Sonar S2342 and carry the wire spelling explicitly; the acronym form
/// is what consumers see.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<DeclarationReferenceType>))]
public enum DeclarationReferenceType
{
    /// <summary>Local Reference Number — pre-lodgement, assigned by the declarant.</summary>
    [JsonStringEnumMemberName("LRN")]
    Lrn,

    /// <summary>Movement Reference Number — assigned by customs on acceptance.</summary>
    [JsonStringEnumMemberName("MRN")]
    Mrn,
}
