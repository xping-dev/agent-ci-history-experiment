using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Storefront.Shipping;

public record CarrierQuote(string Service, decimal Price);

/// <summary>Asks the parcel carrier for a live rate. Used for parcels the weight brackets don't cover.</summary>
public class CarrierClient
{
    private readonly HttpClient _http;
    private readonly Uri _baseUrl;
    private readonly string _apiToken;

    public CarrierClient(HttpClient http, Uri baseUrl, string apiToken)
    {
        _http = http;
        _baseUrl = baseUrl;
        _apiToken = apiToken;
    }

    public async Task<CarrierQuote> QuoteAsync(decimal weightKg, string postcode)
    {
        var weight = weightKg.ToString("0.###", CultureInfo.InvariantCulture);
        var url = new Uri(_baseUrl, $"v1/quotes?weightKg={weight}&postcode={Uri.EscapeDataString(postcode)}");

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        // The carrier retires the X-Api-Token header at the end of the year; bearer tokens are accepted already.
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);

        using var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var quote = await response.Content.ReadFromJsonAsync<CarrierQuote>();
        return quote ?? throw new InvalidOperationException("Carrier returned an empty response.");
    }
}
