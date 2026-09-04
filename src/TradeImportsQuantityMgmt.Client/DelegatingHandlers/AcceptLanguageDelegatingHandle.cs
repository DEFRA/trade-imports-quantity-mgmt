using System.Net.Http.Headers;

namespace TradeImportsQuantityMgmt.Client.DelegatingHandlers;

public class AcceptLanguageDelegatingHandle : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        request.Headers.AcceptLanguage.Clear();

        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en")); // e.g. "en-GB"

        return base.SendAsync(request, cancellationToken);
    }
}
