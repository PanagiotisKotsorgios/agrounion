using AgroUnion.Domain.Entities;
using AgroUnion.Domain.Services;

namespace AgroUnion.Tests;

public sealed class DealRulesTests
{
    [Fact]
    public void Brokerage_margin_is_sell_minus_buy() => Assert.Equal(0.75m, DealRules.CalculateMarginPerUnit(DealType.Brokerage, 6.10m, 6.85m));

    [Fact]
    public void Total_margin_uses_smallest_side_quantity() => Assert.Equal(750m, DealRules.CalculateTotalMargin(DealType.Brokerage, 6.10m, 6.85m, 1000m, 1200m));

    [Fact]
    public void Facilitation_has_no_trading_margin() => Assert.Equal(0m, DealRules.CalculateTotalMargin(DealType.Facilitation, 3m, 9m, 500m, 500m));

    [Fact]
    public void Negative_price_is_rejected() => Assert.Throws<ArgumentOutOfRangeException>(() => DealRules.CalculateMarginPerUnit(DealType.Brokerage, -1m, 2m));

    [Fact]
    public void Zero_quantity_is_rejected() => Assert.Throws<ArgumentOutOfRangeException>(() => DealRules.CalculateTotalMargin(DealType.Brokerage, 1m, 2m, 0m, 1m));

    [Fact]
    public void Buy_then_sell_completes_deal()
    {
        var afterBuy = DealRules.ConfirmBuySide(DealStatus.Proposed);
        Assert.Equal(DealStatus.BuySideConfirmed, afterBuy);
        Assert.Equal(DealStatus.Completed, DealRules.ConfirmSellSide(afterBuy));
    }

    [Fact]
    public void Sell_then_buy_completes_deal()
    {
        var afterSell = DealRules.ConfirmSellSide(DealStatus.Proposed);
        Assert.Equal(DealStatus.SellSideConfirmed, afterSell);
        Assert.Equal(DealStatus.Completed, DealRules.ConfirmBuySide(afterSell));
    }

    [Fact]
    public void Completed_deal_cannot_be_confirmed_again() => Assert.Throws<InvalidOperationException>(() => DealRules.ConfirmBuySide(DealStatus.Completed));

    [Theory]
    [InlineData(PartnerRole.Producer, RoleNames.Producer)]
    [InlineData(PartnerRole.Trader, RoleNames.Trader)]
    [InlineData(PartnerRole.Company, RoleNames.Company)]
    public void Partner_role_maps_to_identity_role(PartnerRole partnerRole, string expected) => Assert.Equal(expected, RoleNames.FromPartnerRole(partnerRole));
}
