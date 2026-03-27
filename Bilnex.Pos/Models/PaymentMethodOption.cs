namespace Bilnex.Pos.Models;

public sealed class PaymentMethodOption
{
    public PaymentMethodOption(string key, string title)
    {
        Key = key;
        Title = title;
    }

    public string Key { get; }

    public string Title { get; }
}
