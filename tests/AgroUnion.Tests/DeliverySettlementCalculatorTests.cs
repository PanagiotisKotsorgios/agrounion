using AgroUnion.Domain.Services;

namespace AgroUnion.Tests;

public sealed class DeliverySettlementCalculatorTests
{
    [Fact]
    public void Calculate_ReconcilesWeightAndEverySettlementComponent()
    {
        var result = DeliverySettlementCalculator.Calculate(
            grossWeight: 8230m,
            tareWeight: 3980m,
            rejectedWeight: 50m,
            unitPrice: 6.10m,
            bonusPercent: 1.50m,
            commissionPercent: 2.25m,
            withholdingPercent: 0m,
            vatPercent: 0m,
            transportCost: 240m,
            otherDeductions: 67.20m);

        Assert.Equal(4250m, result.NetWeight);
        Assert.Equal(4200m, result.AcceptedWeight);
        Assert.Equal(25620m, result.BaseAmount);
        Assert.Equal(384.30m, result.BonusAmount);
        Assert.Equal(576.45m, result.CommissionAmount);
        Assert.Equal(25120.65m, result.NetPayableAmount);
    }

    [Fact]
    public void Calculate_RejectsTareAboveGrossWeight()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DeliverySettlementCalculator.Calculate(100m, 101m, 0m, 1m, 0m, 0m, 0m, 0m, 0m, 0m));
    }

    [Fact]
    public void Calculate_RejectsRejectedWeightAboveNetWeight()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DeliverySettlementCalculator.Calculate(100m, 20m, 81m, 1m, 0m, 0m, 0m, 0m, 0m, 0m));
    }

    [Fact]
    public void Calculate_RejectsPercentageOutsideAllowedRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DeliverySettlementCalculator.Calculate(100m, 20m, 0m, 1m, 101m, 0m, 0m, 0m, 0m, 0m));
    }
}
