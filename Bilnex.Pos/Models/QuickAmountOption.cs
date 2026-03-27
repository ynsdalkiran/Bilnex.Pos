namespace Bilnex.Pos.Models;

public sealed class QuickAmountOption
{
    public QuickAmountOption(string title, string value)
    {
        Title = title;
        Value = value;
    }

    public string Title { get; }

    public string Value { get; }
}
