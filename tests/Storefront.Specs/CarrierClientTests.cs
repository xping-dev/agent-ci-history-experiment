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

    [Fact]
    public async Task CarrierClient_SendsToken_AsBearerAuthorization()
    {
        var handler = new RecordingHandler();
        using var http = new HttpClient(handler);
        var client = new CarrierClient(http, new Uri("https://carrier.invalid/"), "token-123");

        await client.QuoteAsync(2m, "10115");

        Assert.Equal("Bearer token-123", handler.Authorization);
        Assert.False(handler.HadLegacyHeader);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? Authorization { get; private set; }

        public bool HadLegacyHeader { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization?.ToString();
            HadLegacyHeader = request.Headers.Contains("X-Api-Token");
            return Task.FromResult(new HttpResponseMessage
            {
                Content = new StringContent("{\"service\":\"standard\",\"price\":11.20}", System.Text.Encoding.UTF8, "application/json"),
            });
        }
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
