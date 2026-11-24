using Storefront.Payments;

namespace Storefront.Specs;

public class PaymentClientTests : StorefrontTestBase
{
    [Fact]
    public async Task PaymentClient_AuthorizesCard_InSandbox()
    {
        using var http = new HttpClient();
        var client = new PaymentClient(http, new PaymentOptions { BaseUrl = SandboxUrl() });

        var authorization = await client.AuthorizeAsync("ORD-1001", 58.30m, "tok_visa");

        Assert.Equal("authorized", authorization.Status);
        Assert.False(string.IsNullOrEmpty(authorization.Id));
    }

    [Fact]
    public async Task PaymentClient_ThrowsTimeout_WhenProviderExceedsTimeout()
    {
        using var http = new HttpClient(new StalledHandler());
        var options = new PaymentOptions { BaseUrl = new Uri("https://payments.invalid/"), Timeout = TimeSpan.FromMilliseconds(50) };
        var client = new PaymentClient(http, options);

        await Assert.ThrowsAsync<PaymentTimeoutException>(() => client.AuthorizeAsync("ORD-1002", 10.00m, "tok_visa"));
    }

    // The payment provider's sandbox. CI gets the URL from the PAYMENTS_SANDBOX_URL CI/CD variable.
    private static Uri SandboxUrl()
    {
        var url = Environment.GetEnvironmentVariable("PAYMENTS_SANDBOX_URL");
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("PAYMENTS_SANDBOX_URL is not set.");
        }

        return new Uri(url.TrimEnd('/') + "/");
    }

    private sealed class StalledHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage();
        }
    }
}
