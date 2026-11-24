namespace Storefront.Payments;

public class PaymentOptions
{
    /// <summary>Base URL of the payment provider, e.g. https://sandbox.payments.example.</summary>
    public required Uri BaseUrl { get; init; }

    /// <summary>How long to wait for the provider before giving up.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(2);
}
