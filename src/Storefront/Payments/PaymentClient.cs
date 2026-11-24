using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

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

        // The provider drops a repeated authorization for the same order instead of charging twice.
        request.Headers.Add("Idempotency-Key", IdempotencyKey(orderId, amount));

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

    public static string IdempotencyKey(string orderId, decimal amount)
    {
        var input = Encoding.UTF8.GetBytes($"{orderId}:{amount.ToString("0.00", CultureInfo.InvariantCulture)}");
        return Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant();
    }
}
