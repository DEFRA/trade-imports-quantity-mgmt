using System.Net;
using System.Net.Http.Json;
using System.Reflection.Metadata;
using System.Text.Json;
using Trade.Gateway.Api.Contract.Certificate;

namespace TradeImportsQuantityMgmt.IntegrationTests;

internal static class WireMockStubber
{
    private static readonly string[] s_mappingIds =
    [
        "ched-reservation-release",
        "ched-reservation-delete",
        "ched-reservation-put",
        "data-api-ched-reservation-put",
        "data-api-ched-reservation-delete",
        "data-api-ched-reservation-get",
        "data-api-traces-cheds-by-mrn",
    ];

    public static async Task StubChedReleaseAsync(
        string wireMockBaseUrl,
        string mrn,
        string ched,
        CancellationToken cancellationToken = default
    )
    {
        using var http = new HttpClient { BaseAddress = new Uri(wireMockBaseUrl) };

        await PostMappingAsync(
            http,
            "ched-reservation-release",
            new
            {
                priority = 1,
                request = new
                {
                    method = "PUT",
                    urlPath = $"/customs/cheds/{ched}/declarations/{mrn}/reservation/release",
                },
                response = new { status = (int)HttpStatusCode.OK, jsonBody = new { } },
            },
            cancellationToken
        );
    }

    public static async Task StubChedReservationDeleteAsync(
        string wireMockBaseUrl,
        string mrn,
        string ched,
        CancellationToken cancellationToken = default
    )
    {
        using var http = new HttpClient { BaseAddress = new Uri(wireMockBaseUrl) };

        await PostMappingAsync(
            http,
            "ched-reservation-delete",
            new
            {
                priority = 1,
                request = new { method = "DELETE", urlPath = $"/customs/cheds/{ched}/declarations/{mrn}/reservation" },
                response = new { status = (int)HttpStatusCode.NoContent },
            },
            cancellationToken
        );
    }

    public static async Task StubChedPutReservationAsync(
        string wireMockBaseUrl,
        string mrn,
        string ched,
        HttpStatusCode status,
        object? jsonBody = null,
        CancellationToken cancellationToken = default
    )
    {
        using var http = new HttpClient { BaseAddress = new Uri(wireMockBaseUrl) };

        var mapping = new
        {
            priority = 1,
            request = new { method = "PUT", urlPath = $"/customs/cheds/{ched}/declarations/{mrn}/reservation" },
            response = new { status = (int)status, jsonBody = jsonBody },
        };

        await PostMappingAsync(http, "ched-reservation-put", mapping, cancellationToken);
    }

    public static async Task StubDataApiChedReservationPutAsync(
        string wireMockBaseUrl,
        string ched,
        string mrn,
        HttpStatusCode status,
        CancellationToken cancellationToken = default
    )
    {
        using var http = new HttpClient { BaseAddress = new Uri(wireMockBaseUrl) };

        await PostMappingAsync(
            http,
            "data-api-ched-reservation-put",
            new
            {
                priority = 1,
                request = new { method = "PUT", urlPath = $"/traces-cheds/{ched}/reservation/{mrn}" },
                response = new { status = (int)status },
            },
            cancellationToken
        );
    }

    /// <summary>
    /// Stubs the Data API's existing-reservation lookup, returning it with the given ETag response
    /// header so callers threading it into a subsequent write can be asserted against.
    /// </summary>
    public static async Task StubDataApiChedReservationGetAsync(
        string wireMockBaseUrl,
        string ched,
        string mrn,
        string etag,
        CancellationToken cancellationToken = default
    )
    {
        using var http = new HttpClient { BaseAddress = new Uri(wireMockBaseUrl) };

        await PostMappingAsync(
            http,
            "data-api-ched-reservation-get",
            new
            {
                priority = 1,
                request = new { method = "GET", urlPath = $"/traces-cheds/{ched}/reservation/{mrn}" },
                response = new
                {
                    status = (int)HttpStatusCode.OK,
                    headers = new Dictionary<string, string> { ["ETag"] = etag },
                    jsonBody = new
                    {
                        reservation = new
                        {
                            chedId = ched,
                            mrn = mrn,
                            status = "Reserved",
                            timestamp = DateTime.UtcNow,
                            commodities = Array.Empty<object>(),
                        },
                        created = DateTime.UtcNow,
                        updated = DateTime.UtcNow,
                    },
                },
            },
            cancellationToken
        );
    }

    /// <summary>
    /// Stubs the Data API's lookup of the Traces CHEDs held against a declaration, which the
    /// consumer now uses instead of the CHED references embedded in the resource event body.
    /// </summary>
    public static async Task StubDataApiTracesChedsByMrnAsync(
        string wireMockBaseUrl,
        string mrn,
        params string[] chedReferences
    )
    {
        using var http = new HttpClient { BaseAddress = new Uri(wireMockBaseUrl) };

        var mapping = new
        {
            priority = 1,
            request = new { method = "GET", urlPath = $"/customs-declarations/{mrn}/traces-cheds" },
            response = new
            {
                status = (int)HttpStatusCode.OK,
                jsonBody = new
                {
                    cheds = chedReferences.Select(ched => new
                    {
                        ched = new { exchangedDocument = new { identifier = ched }, specifiedConsignment = new { } },
                        created = DateTime.UtcNow,
                        updated = DateTime.UtcNow,
                    }),
                },
            },
        };

        await PostMappingAsync(http, "data-api-traces-cheds-by-mrn", mapping, CancellationToken.None);
    }

    public static async Task StubDataApiChedReservationDeleteAsync(
        string wireMockBaseUrl,
        string ched,
        string mrn,
        HttpStatusCode status,
        CancellationToken cancellationToken = default
    )
    {
        using var http = new HttpClient { BaseAddress = new Uri(wireMockBaseUrl) };

        await PostMappingAsync(
            http,
            "data-api-ched-reservation-delete",
            new
            {
                priority = 1,
                request = new { method = "DELETE", urlPath = $"/traces-cheds/{ched}/reservation/{mrn}" },
                response = new { status = (int)status },
            },
            cancellationToken
        );
    }

    public static async Task ResetAsync(string wireMockBaseUrl, CancellationToken cancellationToken = default)
    {
        using var http = new HttpClient { BaseAddress = new Uri(wireMockBaseUrl) };
        foreach (var id in s_mappingIds)
        {
            await http.DeleteAsync($"/__admin/mappings/{id}", cancellationToken);
        }

        // Also clear recorded requests so VerifyRequest returns fresh results.
        await http.DeleteAsync("/__admin/requests", cancellationToken);
    }

    private static async Task PostMappingAsync(
        HttpClient http,
        string id,
        object mapping,
        CancellationToken cancellationToken
    )
    {
        var response = await http.PostAsJsonAsync($"/__admin/mappings?id={id}", mapping, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public static async Task<bool> VerifyRequest(
        string wireMockBaseUrl,
        string pathFragment,
        CancellationToken cancellationToken = default
    )
    {
        using var http = new HttpClient { BaseAddress = new Uri(wireMockBaseUrl) };

        var resp = await http.GetAsync("/__admin/requests", cancellationToken);
        resp.EnsureSuccessStatusCode();

        var content = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (content.Contains(pathFragment, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the value of <paramref name="headerName"/> on the recorded <paramref name="method"/>
    /// request whose URL contains <paramref name="pathFragment"/>, or <c>null</c> if no such
    /// request/header exists.
    /// </summary>
    public static async Task<string?> GetRequestHeader(
        string wireMockBaseUrl,
        string method,
        string pathFragment,
        string headerName,
        CancellationToken cancellationToken = default
    )
    {
        using var http = new HttpClient { BaseAddress = new Uri(wireMockBaseUrl) };

        var resp = await http.GetAsync("/__admin/requests", cancellationToken);
        resp.EnsureSuccessStatusCode();

        using var document = await JsonDocument.ParseAsync(
            await resp.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken
        );

        return document
            .RootElement.GetProperty("requests")
            .EnumerateArray()
            .Select(x => x.GetProperty("request"))
            .Where(x =>
                string.Equals(x.GetProperty("method").GetString(), method, StringComparison.OrdinalIgnoreCase)
                && x.GetProperty("url").GetString()?.Contains(pathFragment, StringComparison.OrdinalIgnoreCase) is true
            )
            .Select(x =>
                x.GetProperty("headers").TryGetProperty(headerName, out var header) ? header.GetString() : null
            )
            .FirstOrDefault();
    }
}
