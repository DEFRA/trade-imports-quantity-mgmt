namespace TradeImportsQuantityMgmt.IntegrationTests.OpenApi;

[Collection(IntegrationTestCollection.Name)]
public class OpenApiTests(TradeGatewayWebApplicationFactory factory)
{
    [Fact]
    public async Task OpenApi_VerifyAsExpected()
    {
        var client = factory.CreateClient();
        var response = await client.GetStringAsync(
            "/.well-known/openapi/v1/openapi.json",
            TestContext.Current.CancellationToken
        );

        await VerifyJson(response);
    }
}
