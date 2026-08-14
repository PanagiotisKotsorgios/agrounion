using AgroUnion.Domain.Entities;

namespace AgroUnion.Domain.Services;

public static class DealRules
{
    public static decimal CalculateMarginPerUnit(DealType type, decimal buyPrice, decimal sellPrice)
    {
        if (buyPrice < 0 || sellPrice < 0) throw new ArgumentOutOfRangeException(nameof(buyPrice), "Οι τιμές δεν μπορούν να είναι αρνητικές.");
        return type == DealType.Brokerage ? sellPrice - buyPrice : 0m;
    }

    public static decimal CalculateTotalMargin(DealType type, decimal buyPrice, decimal sellPrice, decimal buyQuantity, decimal sellQuantity)
    {
        if (buyQuantity <= 0 || sellQuantity <= 0) throw new ArgumentOutOfRangeException(nameof(buyQuantity), "Οι ποσότητες πρέπει να είναι θετικές.");
        return CalculateMarginPerUnit(type, buyPrice, sellPrice) * Math.Min(buyQuantity, sellQuantity);
    }

    public static DealStatus ConfirmBuySide(DealStatus status) => status switch
    {
        DealStatus.Proposed => DealStatus.BuySideConfirmed,
        DealStatus.SellSideConfirmed => DealStatus.Completed,
        _ => throw new InvalidOperationException("Το σκέλος αγοράς δεν μπορεί να επιβεβαιωθεί σε αυτή την κατάσταση.")
    };

    public static DealStatus ConfirmSellSide(DealStatus status) => status switch
    {
        DealStatus.Proposed => DealStatus.SellSideConfirmed,
        DealStatus.BuySideConfirmed => DealStatus.Completed,
        _ => throw new InvalidOperationException("Το σκέλος πώλησης δεν μπορεί να επιβεβαιωθεί σε αυτή την κατάσταση.")
    };
}
