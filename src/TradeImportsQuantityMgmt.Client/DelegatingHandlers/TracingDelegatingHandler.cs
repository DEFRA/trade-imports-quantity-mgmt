namespace TradeImportsQuantityMgmt.Client.DelegatingHandlers;

public class TracingDelegatingHandler(Func<IServiceProvider, string> traceIdAccessor, IServiceProvider serviceProvider)
    : DelegatingHandler
{
    private const string TraceKey = "x-cdp-request-id";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        request.Headers.Add(TraceKey, traceIdAccessor(serviceProvider));
        return await base.SendAsync(request, cancellationToken);
    }
}
