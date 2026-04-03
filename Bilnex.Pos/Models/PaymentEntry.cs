using System;

namespace Bilnex.Pos.Models;

public sealed class PaymentEntry
{
    public PaymentEntry(string method, string methodTitle, decimal amount)
    {
        Id = Guid.NewGuid().ToString();
        Method = method;
        MethodTitle = methodTitle;
        Amount = amount;
    }

    public string Id { get; }

    public string Method { get; }

    public string MethodTitle { get; }

    public decimal Amount { get; }
}
