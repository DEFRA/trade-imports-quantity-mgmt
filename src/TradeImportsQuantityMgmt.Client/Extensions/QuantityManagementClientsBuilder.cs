using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;

namespace TradeImportsQuantityMgmt.Client.Extensions;

[ExcludeFromCodeCoverage]
public sealed class QuantityManagementClientsBuilder
{
    private readonly List<IHttpClientBuilder> _clients = [];

    internal QuantityManagementClientsBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; set; }

    internal void AddClient<TClient>()
        where TClient : class
    {
        var client = Services
            .AddRefitClient<TClient>(_ => new RefitSettings())
            .ConfigureHttpClient(
                (sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<QuantityManagementClientOptions>>().Value;

                    client.BaseAddress = new Uri(options.BaseUrl);
                }
            );

        _clients.Add(client);
    }

    internal void AddHandler<THandler>()
        where THandler : DelegatingHandler
    {
        Services.AddTransient<THandler>();

        foreach (var client in _clients)
        {
            client.AddHttpMessageHandler<THandler>();
        }
    }

    internal void AddHandler<THandler>(Func<IServiceProvider, THandler> factory)
        where THandler : DelegatingHandler
    {
        Services.AddTransient(factory);

        foreach (var client in _clients)
        {
            client.AddHttpMessageHandler<THandler>();
        }
    }
}
