using Storefront.Shipping;

namespace Storefront.Specs;

public class CarrierClientTests
{
    [Fact]
    public async Task CarrierClient_ReturnsQuote_FromSandbox()
    {
        using var http = new HttpClient();
        var client = new CarrierClient(http, SandboxUrl(), SandboxToken());

        var quote = await client.QuoteAsync(31m, "10115");

        Assert.Equal("freight", quote.Service);
        Assert.True(quote.Price > 0m);
    }

    // The carrier's sandbox. CI gets the URL and the token from CI/CD variables.
    private static Uri SandboxUrl()
    {
        return new Uri(Required("CARRIER_SANDBOX_URL").TrimEnd('/') + "/");
    }

    private static string SandboxToken()
    {
        return Required("CARRIER_SANDBOX_TOKEN");
    }

    private static string Required(string variable)
    {
        var value = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{variable} is not set.");
        }

        return value;
    }
}
