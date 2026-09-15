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
}
