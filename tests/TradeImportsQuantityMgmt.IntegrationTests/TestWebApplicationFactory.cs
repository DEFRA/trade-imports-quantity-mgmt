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

    /// <summary>
    /// The traces gateway client is mocked rather than routed through <see cref="WireMockServer"/> -
    /// it has no base URL configured for the test host, and its request/response shapes are owned by
    /// an upstream package rather than this API, so there's little value stubbing it over HTTP.
    /// </summary>
    public ITracesGatewayChedClient TracesGatewayChedClient { get; } = Substitute.For<ITracesGatewayChedClient>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Without this the underlying MetricsLogger used in the EmfExporter will try to probe for the environment
        // when AWS_EMF_ENABLED is set to true, which takes a long time
        Environment.SetEnvironmentVariable("AWS_EMF_ENVIRONMENT", "Local");

        builder.UseEnvironment("Development");

        var server = WireMockServer.Start();
        ////var tracesBaseUrl = $"http://localhost:{server.Port}";

        builder.ConfigureAppConfiguration(
            (_, config) =>
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ////["TracesNt:BaseUrl"] = tracesBaseUrl,
                        ////["TracesNt:CustomsOfficeReferenceNumber"] = "GBTEST01",
                        ////["TracesNt:Credentials:Default:Username"] = "test-user",
                        ////["TracesNt:Credentials:Default:AuthenticationKey"] = "test-auth-key",
                        ////["TracesNt:Credentials:Default:WebServiceClientId"] = "test-client-id",
                        ////// Deliberately different from the default set — TracesNtCredentialsTests
                        ////// asserts the customs port authenticates as this account, not the default one.
                        ////["TracesNt:Credentials:Customs:Username"] = "test-customs-user",
                        ////["TracesNt:Credentials:Customs:AuthenticationKey"] = "test-customs-auth-key",
                        ////["TracesNt:Credentials:Customs:WebServiceClientId"] = "test-customs-client-id",
                        ////// Authentication authorities come from appsettings.Development.json so that BindConfig
                        ////// (which reads config before WebApplicationFactory overrides apply) sees the same values
                        ////// as the token endpoints registered by LocalTokenServer at runtime.

                        // The tests run against a local Floci AWS emulator running via Docker
                        ["USE_FLOCI"] = "true",
                        ["AWS_ACCESS_KEY_ID"] = "test",
                        ["AWS_SECRET_ACCESS_KEY"] = "test",
                        ["AWS_REGION"] = "eu-west-2",
                        ["SNS_ENDPOINT"] = FlociEndpoint,
                        ["SQS_ENDPOINT"] = FlociEndpoint,

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
                services.AddSingleton(TracesGatewayChedClient);
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
