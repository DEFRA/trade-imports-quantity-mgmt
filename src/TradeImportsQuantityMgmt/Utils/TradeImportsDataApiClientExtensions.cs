using Defra.TradeImportsDataApi.Api.Client;

namespace TradeImportsQuantityMgmt.Utils;

public static class TradeImportsDataApiClientExtensions
{
    /// <summary>
    /// Returns the ETag of an existing CHED reservation record for optimistic-concurrency writes,
    /// or <c>null</c> when no record exists yet. The client already resolves "no record" (404) to a
    /// null result, so a genuine failure (outage, auth error) propagates rather than being treated
    /// as "no record" - swallowing it here would let a write proceed against an unreachable API.
    /// </summary>
    public static async Task<string?> GetChedReservationETag(
        this ITradeImportsDataApiClient client,
        string chedId,
        string mrn,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var existingReservation = await client.GetChedReservation(chedId, mrn, cancellationToken);
            return existingReservation?.ETag;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
