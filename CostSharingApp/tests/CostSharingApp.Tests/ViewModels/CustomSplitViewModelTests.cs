using CostSharing.Core.Models;
using CostSharing.Core.Services;
using Xunit;

namespace CostSharingApp.Tests.Services;

/// <summary>
/// Unit tests for custom percentage split functionality (Phase 6).
/// </summary>
public class CustomSplitTests
{
    private readonly SplitCalculationService splitService;

    public CustomSplitTests()
    {
        this.splitService = new SplitCalculationService();
    }

    [Fact]
    public void CustomSplit_ThreeMembersUnequalPercentages_CalculatesCorrectly()
    {
        // Arrange
        var amount = 150.00m;
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var user3 = Guid.NewGuid();
        var percentages = new Dictionary<Guid, decimal>
        {
            [user1] = 50m,  // Should get $75
            [user2] = 30m,  // Should get $45
            [user3] = 20m   // Should get $30
        };

        // Act
        var splits = this.splitService.CalculateCustomSplit(amount, percentages);

        // Assert
        Assert.Equal(3, splits.Count);
        Assert.Equal(75.00m, splits.First(s => s.UserId == user1).Amount);
        Assert.Equal(45.00m, splits.First(s => s.UserId == user2).Amount);
        Assert.Equal(30.00m, splits.First(s => s.UserId == user3).Amount);
        Assert.Equal(50m, splits.First(s => s.UserId == user1).Percentage);
        Assert.Equal(30m, splits.First(s => s.UserId == user2).Percentage);
        Assert.Equal(20m, splits.First(s => s.UserId == user3).Percentage);
    }

    [Fact]
    public void CustomSplit_TwoMembers50_50_CalculatesCorrectly()
    {
        // Arrange
        var amount = 100.00m;
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var percentages = new Dictionary<Guid, decimal>
        {
            [user1] = 50m,
            [user2] = 50m
        };

        // Act
        var splits = this.splitService.CalculateCustomSplit(amount, percentages);

        // Assert
        Assert.Equal(2, splits.Count);
        Assert.Equal(50.00m, splits.First(s => s.UserId == user1).Amount);
        Assert.Equal(50.00m, splits.First(s => s.UserId == user2).Amount);
    }

    [Fact]
    public void CustomSplit_InvalidPercentageSum_ThrowsException()
    {
        // Arrange
        var amount = 100.00m;
        var percentages = new Dictionary<Guid, decimal>
        {
            [Guid.NewGuid()] = 60m,
            [Guid.NewGuid()] = 30m  // Total = 90%
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            this.splitService.CalculateCustomSplit(amount, percentages));
        Assert.Contains("100", exception.Message);
    }

    [Fact]
    public void CustomSplit_PercentagesOver100_ThrowsException()
    {
        // Arrange
        var amount = 100.00m;
        var percentages = new Dictionary<Guid, decimal>
        {
            [Guid.NewGuid()] = 60m,
            [Guid.NewGuid()] = 50m  // Total = 110%
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            this.splitService.CalculateCustomSplit(amount, percentages));
        Assert.Contains("100", exception.Message);
    }

    [Fact]
    public void CustomSplit_ZeroPercentageMemberExcluded()
    {
        // Arrange
        var amount = 150.00m;
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var user3 = Guid.NewGuid();
        var percentages = new Dictionary<Guid, decimal>
        {
            [user1] = 100m,  // Gets everything
            [user2] = 0m,    // Should be excluded
            [user3] = 0m     // Should be excluded
        };

        // Act
        var splits = this.splitService.CalculateCustomSplit(amount, percentages);

        // Assert
        Assert.Single(splits);  // Only one split (0% members excluded)
        Assert.Equal(150.00m, splits.First().Amount);
        Assert.Equal(user1, splits.First().UserId);
    }

    [Fact]
    public void CustomSplit_FourMembersComplexPercentages()
    {
        // Arrange
        var amount = 200.00m;
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var user3 = Guid.NewGuid();
        var user4 = Guid.NewGuid();
        var percentages = new Dictionary<Guid, decimal>
        {
            [user1] = 40m,  // $80
            [user2] = 30m,  // $60
            [user3] = 20m,  // $40
            [user4] = 10m   // $20
        };

        // Act
        var splits = this.splitService.CalculateCustomSplit(amount, percentages);

        // Assert
        Assert.Equal(4, splits.Count);
        Assert.Equal(80.00m, splits.First(s => s.UserId == user1).Amount);
        Assert.Equal(60.00m, splits.First(s => s.UserId == user2).Amount);
        Assert.Equal(40.00m, splits.First(s => s.UserId == user3).Amount);
        Assert.Equal(20.00m, splits.First(s => s.UserId == user4).Amount);
        // Verify total adds up
        Assert.Equal(200.00m, splits.Sum(s => s.Amount));
    }

    [Fact]
    public void CustomSplit_DecimalPercentages_RoundsCorrectly()
    {
        // Arrange
        var amount = 100.00m;
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var user3 = Guid.NewGuid();
        var percentages = new Dictionary<Guid, decimal>
        {
            [user1] = 33.33m,
            [user2] = 33.33m,
            [user3] = 33.34m  // Remainder to last
        };

        // Act
        var splits = this.splitService.CalculateCustomSplit(amount, percentages);

        // Assert
        Assert.Equal(3, splits.Count);
        // Each should be properly rounded to 2 decimal places
        Assert.True(splits.All(s => s.Amount == Math.Round(s.Amount, 2)));
        // Total should equal original amount (accounting for rounding)
        var total = splits.Sum(s => s.Amount);
        Assert.True(Math.Abs(total - amount) < 0.01m);
    }

    [Fact]
    public void CustomSplit_LargeAmount_MaintainsPrecision()
    {
        // Arrange
        var amount = 999.99m;
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var percentages = new Dictionary<Guid, decimal>
        {
            [user1] = 75m,  // Should get $749.99
            [user2] = 25m   // Should get $250.00
        };

        // Act
        var splits = this.splitService.CalculateCustomSplit(amount, percentages);

        // Assert
        Assert.Equal(2, splits.Count);
        var split1 = splits.First(s => s.UserId == user1).Amount;
        var split2 = splits.First(s => s.UserId == user2).Amount;
        Assert.True(Math.Abs(split1 - 749.99m) < 0.02m);
        Assert.True(Math.Abs(split2 - 250.00m) < 0.02m);
        // Verify total (with rounding tolerance)
        Assert.True(Math.Abs(split1 + split2 - amount) < 0.01m);
    }
}

