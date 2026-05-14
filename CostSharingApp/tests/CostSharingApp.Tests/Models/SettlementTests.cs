using CostSharing.Core.Models;

namespace CostSharingApp.Tests.Models;

public class SettlementTests
{
    [Fact]
    public void SettledAt_Get_ReturnsSettlementDate()
    {
        var date = new DateTime(2026, 5, 14, 10, 0, 0);
        var settlement = new Settlement { SettlementDate = date };

        Assert.Equal(date, settlement.SettledAt);
    }

    [Fact]
    public void SettledAt_Set_WritesSettlementDate()
    {
        var date = new DateTime(2026, 5, 14, 10, 0, 0);
        var settlement = new Settlement { SettledAt = date };

        Assert.Equal(date, settlement.SettlementDate);
    }
}
