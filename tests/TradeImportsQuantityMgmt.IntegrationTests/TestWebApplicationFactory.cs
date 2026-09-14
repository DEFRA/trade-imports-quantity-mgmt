using System.Text.Json;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Refit;
using Serilog;
using Serilog.Extensions.Logging;
using Trade.Gateway.Api.Client.Clients;
using TradeImportsQuantityMgmt.Client.Clients;
using WireMock.Server;

namespace TradeImportsQuantityMgmt.IntegrationTests;

public class TradeGatewayWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string FlociEndpoint = "http://localhost:4566";

    public WireMockServer WireMockServer => Services.GetRequiredService<WireMockServer>();
    public string WireMockBaseUrl { get; } = "http://localhost:8088";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Without this the underlying MetricsLogger used in the EmfExporter will try to probe for the environment
        // when AWS_EMF_ENABLED is set to true, which takes a long time
        Environment.SetEnvironmentVariable("AWS_EMF_ENVIRONMENT", "Local");
        var server = WireMockServer.Start();

        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration(
            (_, config) =>
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        // The tests run against a local Floci AWS emulator running via Docker
                        ["USE_FLOCI"] = "true",
                        ["AWS_ACCESS_KEY_ID"] = "test",
                        ["AWS_SECRET_ACCESS_KEY"] = "test",
                        ["AWS_REGION"] = "eu-west-2",
                        ["SNS_ENDPOINT"] = FlociEndpoint,
                        ["SQS_ENDPOINT"] = FlociEndpoint,

                        // The tests run against a local WireMock container emulating the Traces Gateway
                        ["TracesGateway:BaseUrl"] = WireMockBaseUrl,

                        ["ResourceEventsConsumer:ResourceEventsQueueUrl"] =
                            "http://floci:4566/000000000000/trade_imports_data_upserted_quantity_mgmt",
                    }
                )
        );

        builder.ConfigureServices(
            (_, services) =>
            {
                // Floci does not implement the STS GetWebIdentityToken operation that the real
                // StsAuthDelegatingHandler relies on, so stub the STS client to return a fake token.
                // Without this, every Traces Gateway request throws before it is sent.
                var sts = Substitute.For<IAmazonSecurityTokenService>();
                sts.GetWebIdentityTokenAsync(Arg.Any<GetWebIdentityTokenRequest>(), Arg.Any<CancellationToken>())
                    .Returns(
                        new GetWebIdentityTokenResponse
                        {
                            WebIdentityToken = "integration-test-token",
                            Expiration = DateTime.UtcNow.AddHours(1),
                        }
                    );

                services.RemoveAll<IAmazonSecurityTokenService>();
                services.AddSingleton(sts);

                services.AddSingleton(server);
            }
        );
    }

    /// <summary>Routes the AWS SDK's requests into the in-memory test server.</summary>
    ////private sealed class TestServerHttpClientFactory(TradeGatewayWebApplicationFactory factory) : HttpClientFactory
    ////{
    ////    public override HttpClient CreateHttpClient(IClientConfig clientConfig) =>
    ////        factory.CreateDefaultClient();

    ////    // The test server's handler is per-factory; caching clients across configs would outlive it.
    ////    public override bool UseSDKHttpClientCaching(IClientConfig clientConfig) => false;
    ////}

    public IQuantityManagementClient CreateQuantityManagementClient()
    {
        var client = CreateClient();
        return RestService.For<IQuantityManagementClient>(
            client,
            new RefitSettings(
                new SystemTextJsonContentSerializer(
                    new JsonSerializerOptions(JsonSerializerDefaults.Web)
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
                    }
                )
            )
        );
    }
}
