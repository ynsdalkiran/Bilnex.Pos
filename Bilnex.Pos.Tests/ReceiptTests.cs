using Bilnex.Pos.Models;

namespace Bilnex.Pos.Tests;

public class ReceiptTests
{
    private static Receipt MakeReceipt(params (string Name, decimal Price, int Qty)[] items)
    {
        var r = new Receipt();
        foreach (var (name, price, qty) in items)
        {
            var item = new BasketItem { ProductName = name, UnitPrice = price, Quantity = qty };
            r.Items.Add(item);
        }
        return r;
    }

    // ── SubtotalAmount ─────────────────────────────────────────────────────────

    [Fact]
    public void SubtotalAmount_IsSum_OfLineTotals()
    {
        var receipt = MakeReceipt(("Su", 10m, 2), ("Ekmek", 15m, 1));
        Assert.Equal(35m, receipt.SubtotalAmount);
    }

    [Fact]
    public void SubtotalAmount_IsZero_WhenNoItems()
    {
        var receipt = new Receipt();
        Assert.Equal(0m, receipt.SubtotalAmount);
    }

    // ── ApplyReceiptAdjustment — yüzde indirimi ────────────────────────────────

    [Fact]
    public void ApplyPercentageDiscount_ReducesTotalCorrectly()
    {
        var receipt = MakeReceipt(("Su", 100m, 1));
        // %10 indirim → 10 TL indirim → toplam 90 TL
        receipt.ApplyReceiptAdjustment(10m, 0m, "%10 İndirim", discountRate: 10m);
        Assert.Equal(10m, receipt.DiscountAmount);
        Assert.Equal(10m, receipt.DiscountRate);
        Assert.Equal(90m, receipt.TotalAmount);
    }

    [Fact]
    public void ApplyPercentageDiscount_StoresLabel()
    {
        var receipt = MakeReceipt(("Su", 100m, 1));
        receipt.ApplyReceiptAdjustment(5m, 0m, "Özel Müşteri %5", discountRate: 5m);
        Assert.Equal("Özel Müşteri %5", receipt.DiscountLabel);
    }

    // ── ApplyReceiptAdjustment — tutar indirimi ────────────────────────────────

    [Fact]
    public void ApplyAmountDiscount_ReducesTotalCorrectly()
    {
        var receipt = MakeReceipt(("Kola", 40m, 1), ("Su", 10m, 1));
        // 20 TL sabit indirim → toplam 30 TL
        receipt.ApplyReceiptAdjustment(20m, 0m, "Kampanya 20 TL", discountRate: 0m);
        Assert.Equal(20m, receipt.DiscountAmount);
        Assert.Equal(30m, receipt.TotalAmount);
    }

    [Fact]
    public void ApplyDiscount_ClampsToSubtotal_WhenExceedsIt()
    {
        var receipt = MakeReceipt(("Su", 10m, 1));
        // 50 TL indirim ama subtotal 10 TL → max 10 TL indirim
        receipt.ApplyReceiptAdjustment(50m, 0m, "Fazla İndirim", discountRate: 0m);
        Assert.Equal(10m, receipt.DiscountAmount);
        Assert.Equal(0m, receipt.TotalAmount);
    }

    // ── ApplyReceiptAdjustment — yuvarlama ────────────────────────────────────

    [Fact]
    public void ApplyRoundAdjustment_AddsPositiveDelta()
    {
        var receipt = MakeReceipt(("Su", 67m, 1));
        // Hedef 67,50 → +0,50 yuvarlama
        receipt.ApplyReceiptAdjustment(0m, 0.50m, string.Empty, 0m);
        Assert.Equal(67.50m, receipt.TotalAmount);
        Assert.Equal(0.50m, receipt.RoundAdjustment);
    }

    [Fact]
    public void ApplyRoundAdjustment_SubtractsNegativeDelta()
    {
        var receipt = MakeReceipt(("Su", 67m, 1));
        // Hedef 66,95 → -0,05 yuvarlama
        receipt.ApplyReceiptAdjustment(0m, -0.05m, string.Empty, 0m);
        Assert.Equal(66.95m, receipt.TotalAmount);
        Assert.Equal(-0.05m, receipt.RoundAdjustment);
    }

    [Fact]
    public void ApplyDiscountAndRound_CombineCorrectly()
    {
        var receipt = MakeReceipt(("Su", 100m, 1));
        // %10 indirim → 90 TL, sonra yuvarlama +0,10 → 90,10 TL
        receipt.ApplyReceiptAdjustment(10m, 0.10m, "%10 + Yuvarlama", discountRate: 10m);
        Assert.Equal(90.10m, receipt.TotalAmount);
    }

    // ── ClearReceiptAdjustment ─────────────────────────────────────────────────

    [Fact]
    public void ClearReceiptAdjustment_ResetsAllFields()
    {
        var receipt = MakeReceipt(("Su", 100m, 1));
        receipt.ApplyReceiptAdjustment(10m, 0.50m, "Test İndirim", 10m);
        receipt.ClearReceiptAdjustment();

        Assert.Equal(0m, receipt.DiscountAmount);
        Assert.Equal(0m, receipt.DiscountRate);
        Assert.Equal(0m, receipt.RoundAdjustment);
        Assert.Equal(string.Empty, receipt.DiscountLabel);
        Assert.Equal(100m, receipt.TotalAmount);
    }

    // ── Ödeme hesaplamaları ────────────────────────────────────────────────────

    [Fact]
    public void IsPaid_IsFalse_WhenNoPaidAmount()
    {
        var receipt = MakeReceipt(("Su", 10m, 1));
        Assert.False(receipt.IsPaid);
    }

    [Fact]
    public void IsPaid_IsTrue_WhenFullyCovered()
    {
        var receipt = MakeReceipt(("Su", 10m, 1));
        receipt.CashAmount = 10m;
        Assert.True(receipt.IsPaid);
    }

    [Fact]
    public void ChangeAmount_IsCorrect_WhenOverpaid()
    {
        var receipt = MakeReceipt(("Su", 10m, 1));
        receipt.CashAmount = 20m;
        Assert.Equal(10m, receipt.ChangeAmount);
    }

    // ── Preset indirim hesaplama (Receipt üzerinden) ───────────────────────────

    [Theory]
    [InlineData(100.0, 5.0,  5.0)]
    [InlineData(67.0,  10.0, 6.70)]
    [InlineData(231.0, 7.5,  17.32)]   // 231 * 7.5 / 100 = 17.325 → banker's rounding → 17.32
    public void PercentageDiscount_CalculatesCorrectly(double subtotalD, double rateD, double expectedDiscountD)
    {
        var subtotal         = (decimal)subtotalD;
        var rate             = (decimal)rateD;
        var expectedDiscount = (decimal)expectedDiscountD;

        var receipt = new Receipt();
        receipt.Items.Add(new BasketItem { ProductName = "Ürün", UnitPrice = subtotal, Quantity = 1 });

        var discountAmount = Math.Min(subtotal, decimal.Round(subtotal * rate / 100m, 2));
        receipt.ApplyReceiptAdjustment(discountAmount, 0m, $"%{rate} İndirim", rate);

        Assert.Equal(expectedDiscount, receipt.DiscountAmount);
        Assert.Equal(subtotal - expectedDiscount, receipt.TotalAmount);
    }

    // ── BasketItem hesaplamaları ───────────────────────────────────────────────

    [Fact]
    public void BasketItem_LineTotal_IsQuantityTimesUnitPrice()
    {
        var item = new BasketItem { ProductName = "Kola", UnitPrice = 40m, Quantity = 3 };
        Assert.Equal(120m, item.LineTotal);
    }

    [Fact]
    public void BasketItem_LineTotal_UpdatesWhenQuantityChanges()
    {
        var item = new BasketItem { ProductName = "Su", UnitPrice = 10m, Quantity = 1 };
        item.Quantity = 5;
        Assert.Equal(50m, item.LineTotal);
    }

    [Fact]
    public void BasketItem_LineTotal_UpdatesWhenUnitPriceChanges()
    {
        var item = new BasketItem { ProductName = "Simit", UnitPrice = 17m, Quantity = 2 };
        item.UnitPrice = 20m;
        Assert.Equal(40m, item.LineTotal);
    }

    // ── Receipt.ItemCount ─────────────────────────────────────────────────────

    [Fact]
    public void ItemCount_IsSumOfAllQuantities()
    {
        var receipt = MakeReceipt(("Su", 10m, 3), ("Ekmek", 15m, 2), ("Simit", 17m, 1));
        Assert.Equal(6, receipt.ItemCount);
    }

    [Fact]
    public void ItemCount_IsZero_WhenNoItems()
    {
        var receipt = new Receipt();
        Assert.Equal(0, receipt.ItemCount);
    }

    // ── Receipt TotalAmount sınır durumları ───────────────────────────────────

    [Fact]
    public void TotalAmount_IsNeverNegative()
    {
        var receipt = MakeReceipt(("Su", 10m, 1));
        // subtotal 10, 20 TL indirim → max 10 TL kırpılır → total = 0, negatif olmaz
        receipt.ApplyReceiptAdjustment(20m, 0m, "Fazla İndirim", 0m);
        Assert.True(receipt.TotalAmount >= 0m);
    }

    [Fact]
    public void TotalAmount_EqualsSubtotal_WhenNoAdjustment()
    {
        var receipt = MakeReceipt(("Peynir", 95m, 2));
        Assert.Equal(190m, receipt.TotalAmount);
    }

    // ── Kart ödemesi ──────────────────────────────────────────────────────────

    [Fact]
    public void IsPaid_IsTrue_WhenCoveredByCard()
    {
        var receipt = MakeReceipt(("Su", 50m, 1));
        receipt.CardAmount = 50m;
        Assert.True(receipt.IsPaid);
    }

    [Fact]
    public void IsPaid_IsTrue_WhenCoveredByCombined()
    {
        var receipt = MakeReceipt(("Su", 100m, 1));
        receipt.CashAmount = 60m;
        receipt.CardAmount = 40m;
        Assert.True(receipt.IsPaid);
    }

    [Fact]
    public void IsPaid_IsFalse_WhenUnderpaid()
    {
        var receipt = MakeReceipt(("Su", 100m, 1));
        receipt.CashAmount = 50m;
        Assert.False(receipt.IsPaid);
    }

    [Fact]
    public void ResetPayments_ClearsBothAmounts()
    {
        var receipt = MakeReceipt(("Su", 50m, 1));
        receipt.CashAmount = 30m;
        receipt.CardAmount = 20m;
        receipt.ResetPayments();
        Assert.Equal(0m, receipt.CashAmount);
        Assert.Equal(0m, receipt.CardAmount);
        Assert.Equal(0m, receipt.PaidAmount);
    }

    // ── Ürün ekleme / kaldırma ─────────────────────────────────────────────────

    [Fact]
    public void SubtotalAmount_UpdatesWhenItemAdded()
    {
        var receipt = MakeReceipt(("Ekmek", 15m, 1));
        receipt.Items.Add(new BasketItem { ProductName = "Su", UnitPrice = 10m, Quantity = 2 });
        Assert.Equal(35m, receipt.SubtotalAmount);
    }

    [Fact]
    public void SubtotalAmount_UpdatesWhenItemRemoved()
    {
        var receipt = MakeReceipt(("Ekmek", 15m, 1), ("Su", 10m, 1));
        receipt.Items.RemoveAt(1);
        Assert.Equal(15m, receipt.SubtotalAmount);
    }
}
