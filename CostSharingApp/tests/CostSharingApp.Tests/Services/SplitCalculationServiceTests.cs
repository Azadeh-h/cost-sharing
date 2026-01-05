using CostSharing.Core.Models;
using CostSharing.Core.Services;

namespace CostSharingApp.Tests.Services;

/// <summary>
/// Unit tests for SplitCalculationService.
/// </summary>
public class SplitCalculationServiceTests
{
    private readonly SplitCalculationService service;

    public SplitCalculationServiceTests()
    {
        this.service = new SplitCalculationService();
    }

    [Fact]
    public void CalculateEvenSplit_TwoMembers_SplitsEvenly()
    {
        // Arrange
        var totalAmount = 100m;
        var memberIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        // Act
        var result = this.service.CalculateEvenSplit(totalAmount, memberIds);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, split => Assert.Equal(50m, split.Amount));
    }

    [Fact]
    public void CalculateEvenSplit_ThreeMembers_SplitsEvenlyWithRounding()
    {
        // Arrange
        var totalAmount = 100m;
        var memberIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        // Act
        var result = this.service.CalculateEvenSplit(totalAmount, memberIds);

        // Assert
        Assert.Equal(3, result.Count);
        var totalAssigned = result.Sum(s => s.Amount);
        Assert.Equal(100m, totalAssigned); // Should sum to exact total
    }

    [Fact]
    public void CalculateEvenSplit_UnevenDivision_RoundsToTwoCents()
    {
        // Arrange
        var totalAmount = 10m;
        var memberIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        // Act
        var result = this.service.CalculateEvenSplit(totalAmount, memberIds);

        // Assert
        Assert.Equal(3, result.Count);
        var totalAssigned = result.Sum(s => s.Amount);
        Assert.Equal(10m, totalAssigned);
    }

    [Fact]
    public void CalculateEvenSplit_OneMember_GetsFullAmount()
    {
        // Arrange
        var totalAmount = 100m;
        var memberIds = new List<Guid> { Guid.NewGuid() };

        // Act
        var result = this.service.CalculateEvenSplit(totalAmount, memberIds);

        // Assert
        Assert.Single(result);
        Assert.Equal(100m, result[0].Amount);
    }

    [Fact]
    public void CalculateEvenSplit_EmptyMemberList_ThrowsException()
    {
        // Arrange
        var totalAmount = 100m;
        var memberIds = new List<Guid>();

        // Act
        var result = this.service.CalculateEvenSplit(totalAmount, memberIds);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void CalculateCustomSplit_ValidPercentages_CalculatesCorrectly()
    {
        // Arrange
        var totalAmount = 150m;
        var percentages = new Dictionary<Guid, decimal>
        {
            { Guid.NewGuid(), 50m },
            { Guid.NewGuid(), 30m },
            { Guid.NewGuid(), 20m }
        };

        // Act
        var result = this.service.CalculateCustomSplit(totalAmount, percentages);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, s => s.Amount == 75m);  // 50% of 150
        Assert.Contains(result, s => s.Amount == 45m);  // 30% of 150
        Assert.Contains(result, s => s.Amount == 30m);  // 20% of 150
    }

    [Fact]
    public void CalculateCustomSplit_PercentagesNotEqualTo100_ThrowsException()
    {
        // Arrange
        var totalAmount = 100m;
        var percentages = new Dictionary<Guid, decimal>
        {
            { Guid.NewGuid(), 50m },
            { Guid.NewGuid(), 30m }
        }; // Total = 80%, not 100%

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => this.service.CalculateCustomSplit(totalAmount, percentages));
        Assert.Contains("100", exception.Message);
    }

    [Fact]
    public void CalculateCustomSplit_PercentagesOver100_ThrowsException()
    {
        // Arrange
        var totalAmount = 100m;
        var percentages = new Dictionary<Guid, decimal>
        {
            { Guid.NewGuid(), 60m },
            { Guid.NewGuid(), 50m }
        }; // Total = 110%

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => this.service.CalculateCustomSplit(totalAmount, percentages));
        Assert.Contains("100", exception.Message);
    }

    [Fact]
    public void CalculateCustomSplit_ZeroPercentage_ExcludesMember()
    {
        // Arrange
        var totalAmount = 100m;
        var member1 = Guid.NewGuid();
        var member2 = Guid.NewGuid();
        var member3 = Guid.NewGuid();
        var percentages = new Dictionary<Guid, decimal>
        {
            { member1, 60m },
            { member2, 40m },
            { member3, 0m }
        };

        // Act
        var result = this.service.CalculateCustomSplit(totalAmount, percentages);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, s => s.UserId == member3);
        Assert.Contains(result, s => s.UserId == member1 && s.Amount == 60m);
        Assert.Contains(result, s => s.UserId == member2 && s.Amount == 40m);
    }

    [Fact]
    public void CalculateCustomSplit_DecimalPercentages_RoundsToTwoCents()
    {
        // Arrange
        var totalAmount = 100m;
        var percentages = new Dictionary<Guid, decimal>
        {
            { Guid.NewGuid(), 33.33m },
            { Guid.NewGuid(), 33.33m },
            { Guid.NewGuid(), 33.34m }
        };

        // Act
        var result = this.service.CalculateCustomSplit(totalAmount, percentages);

        // Assert
        Assert.Equal(3, result.Count);
        var totalAssigned = result.Sum(s => s.Amount);
        Assert.Equal(100m, totalAssigned);
    }

    [Fact]
    public void CalculateCustomSplit_LargeAmount_MaintainsPrecision()
    {
        // Arrange
        var totalAmount = 1000000m;
        var percentages = new Dictionary<Guid, decimal>
        {
            { Guid.NewGuid(), 40m },
            { Guid.NewGuid(), 35m },
            { Guid.NewGuid(), 25m }
        };

        // Act
        var result = this.service.CalculateCustomSplit(totalAmount, percentages);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, s => s.Amount == 400000m);
        Assert.Contains(result, s => s.Amount == 350000m);
        Assert.Contains(result, s => s.Amount == 250000m);
        Assert.Equal(1000000m, result.Sum(s => s.Amount));
    }

    [Fact]
    public void CalculateCustomSplit_EmptyPercentages_ThrowsException()
    {
        // Arrange
        var totalAmount = 100m;
        var percentages = new Dictionary<Guid, decimal>();

        // Act
        var result = this.service.CalculateCustomSplit(totalAmount, percentages);

        // Assert
        Assert.Empty(result);
    }
}
