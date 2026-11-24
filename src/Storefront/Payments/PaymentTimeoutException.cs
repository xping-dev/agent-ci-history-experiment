namespace Storefront.Payments;

public class PaymentTimeoutException : Exception
{
    public PaymentTimeoutException(TimeSpan timeout, Exception inner)
        : base($"Payment provider did not respond within {timeout.TotalSeconds:0.#} s.", inner)
    {
    }
}
