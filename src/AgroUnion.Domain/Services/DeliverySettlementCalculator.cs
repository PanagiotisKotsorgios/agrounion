namespace AgroUnion.Domain.Services;

public sealed record DeliverySettlement(
    decimal NetWeight,
    decimal AcceptedWeight,
    decimal BaseAmount,
    decimal BonusAmount,
    decimal CommissionAmount,
    decimal WithholdingAmount,
    decimal VatAmount,
    decimal NetPayableAmount);

public static class DeliverySettlementCalculator
{
    public static DeliverySettlement Calculate(
        decimal grossWeight,
        decimal tareWeight,
        decimal rejectedWeight,
        decimal unitPrice,
        decimal bonusPercent,
        decimal commissionPercent,
        decimal withholdingPercent,
        decimal vatPercent,
        decimal transportCost,
        decimal otherDeductions)
    {
        if (grossWeight < 0 || tareWeight < 0 || rejectedWeight < 0 || unitPrice < 0 || transportCost < 0 || otherDeductions < 0)
            throw new ArgumentOutOfRangeException(nameof(grossWeight), "Weights, prices and deductions cannot be negative.");
        if (tareWeight > grossWeight) throw new ArgumentOutOfRangeException(nameof(tareWeight), "Tare cannot exceed gross weight.");
        if (bonusPercent is < 0 or > 100 || commissionPercent is < 0 or > 100 || withholdingPercent is < 0 or > 100 || vatPercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(bonusPercent), "Percentages must be between 0 and 100.");

        var netWeight = grossWeight - tareWeight;
        if (rejectedWeight > netWeight) throw new ArgumentOutOfRangeException(nameof(rejectedWeight), "Rejected weight cannot exceed net weight.");
        var acceptedWeight = netWeight - rejectedWeight;
        var baseAmount = decimal.Round(acceptedWeight * unitPrice, 2, MidpointRounding.AwayFromZero);
        var bonusAmount = Percent(baseAmount, bonusPercent);
        var commissionAmount = Percent(baseAmount, commissionPercent);
        var withholdingAmount = Percent(baseAmount, withholdingPercent);
        var vatAmount = Percent(baseAmount + bonusAmount, vatPercent);
        var payable = decimal.Round(baseAmount + bonusAmount + vatAmount - commissionAmount - withholdingAmount - transportCost - otherDeductions, 2, MidpointRounding.AwayFromZero);
        return new(netWeight, acceptedWeight, baseAmount, bonusAmount, commissionAmount, withholdingAmount, vatAmount, Math.Max(0, payable));
    }

    private static decimal Percent(decimal amount, decimal percent) =>
        decimal.Round(amount * percent / 100m, 2, MidpointRounding.AwayFromZero);
}
