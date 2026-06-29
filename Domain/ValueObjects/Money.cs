namespace Domain.ValueObjects;

public class Money
{
    public decimal Amount { get; }
    public string CurrencyCode { get; }

    public Money(decimal amount, string currencyCode = "USD")
    {
        if (amount < 0) throw new ArgumentException("Amount cannot be negative.");
        Amount = Math.Round(amount, 2);
        CurrencyCode = currencyCode.ToUpperInvariant();
    }

    public Money Add(Money other)
    {
        if (CurrencyCode != other.CurrencyCode) 
            throw new InvalidOperationException("Cannot add different currencies.");
        return new Money(Amount + other.Amount, CurrencyCode);
    }
}