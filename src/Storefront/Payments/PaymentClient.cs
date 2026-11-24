using System.Globalization;
using System.Net.Http.Json;

namespace Storefront.Payments;

public record PaymentAuthorization(string Id, string Status);

public class PaymentClient
{
    private readonly HttpClient _http;
    private readonly PaymentOptions _options;

    public PaymentClient(HttpClient http, PaymentOptions options)
    {
        _http = http;
        _options = options;
    }

    public async Task<PaymentAuthorization> AuthorizeAsync(string orderId, decimal amount, string cardToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_options.BaseUrl, "v1/authorizations"));
        request.Content = JsonContent.Create(new
        {
            orderId,
            amount = amount.ToString("0.00", CultureInfo.InvariantCulture),
            cardToken,
        });

        using var timeout = new CancellationTokenSource(_options.Timeout);

        try
        {
            using var response = await _http.SendAsync(request, timeout.Token);
            response.EnsureSuccessStatusCode();

            var authorization = await response.Content.ReadFromJsonAsync<PaymentAuthorization>(timeout.Token);
            return authorization ?? throw new InvalidOperationException("Payment provider returned an empty response.");
        }
        catch (OperationCanceledException ex)
        {
            throw new PaymentTimeoutException(_options.Timeout, ex);
        }
    }
}
