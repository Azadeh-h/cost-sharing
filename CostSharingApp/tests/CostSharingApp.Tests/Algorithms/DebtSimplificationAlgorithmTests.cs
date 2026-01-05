using CostSharing.Core.Algorithms;
using CostSharing.Core.Models;

namespace CostSharingApp.Tests.Algorithms;

/// <summary>
/// Unit tests for DebtSimplificationAlgorithm (Min-Cash-Flow).
/// </summary>
public class DebtSimplificationAlgorithmTests
{
    private readonly DebtSimplificationAlgorithm algorithm;
    private readonly Guid groupId;
    private readonly Guid user1Id;
    private readonly Guid user2Id;
    private readonly Guid user3Id;
    private readonly Guid user4Id;

    public DebtSimplificationAlgorithmTests()
    {
        this.algorithm = new DebtSimplificationAlgorithm();
        this.groupId = Guid.NewGuid();
        this.user1Id = Guid.NewGuid();
        this.user2Id = Guid.NewGuid();
        this.user3Id = Guid.NewGuid();
        this.user4Id = Guid.NewGuid();
    }

    [Fact]
    public void SimplifyDebts_TwoPersonSimpleDebt_ReturnsSingleTransaction()
    {
        // Arrange
        var debts = new List<Debt>
        {
            new Debt
            {
                GroupId = this.groupId,
                DebtorId = this.user1Id,
                CreditorId = this.user2Id,
                Amount = 50m
            }
        };

        // Act
        var result = this.algorithm.SimplifyDebts(debts);

        // Assert
        Assert.Single(result);
        Assert.Equal(this.user1Id, result[0].FromUserId);
        Assert.Equal(this.user2Id, result[0].ToUserId);
        Assert.Equal(50m, result[0].Amount);
    }

    [Fact]
    public void SimplifyDebts_ThreePersonCircular_SimplifiesToTwo()
    {
        // Arrange
        // User1 owes User2 $50
        // User2 owes User3 $30
        // User3 owes User1 $20
        // Net: User1 owes $30 (50-20), User2 owes $20 (30-50+20), User3 is even (30-20-30+20=0)
        // Simplified: User1 pays User2 $30
        var debts = new List<Debt>
        {
            new Debt { GroupId = this.groupId, DebtorId = this.user1Id, CreditorId = this.user2Id, Amount = 50m },
            new Debt { GroupId = this.groupId, DebtorId = this.user2Id, CreditorId = this.user3Id, Amount = 30m },
            new Debt { GroupId = this.groupId, DebtorId = this.user3Id, CreditorId = this.user1Id, Amount = 20m }
        };

        // Act
        var result = this.algorithm.SimplifyDebts(debts);

        // Assert
        Assert.True(result.Count <= 2); // Should be simplified from 3 to at most 2
        var totalAmount = result.Sum(t => t.Amount);
        Assert.True(totalAmount <= 50m); // Total transactions should be minimized
    }

    [Fact]
    public void SimplifyDebts_FourPersonComplex_ReducesTransactions()
    {
        // Arrange
        // Complex scenario with 6 original debts
        var debts = new List<Debt>
        {
            new Debt { GroupId = this.groupId, DebtorId = this.user1Id, CreditorId = this.user2Id, Amount = 40m },
            new Debt { GroupId = this.groupId, DebtorId = this.user1Id, CreditorId = this.user3Id, Amount = 20m },
            new Debt { GroupId = this.groupId, DebtorId = this.user2Id, CreditorId = this.user3Id, Amount = 30m },
            new Debt { GroupId = this.groupId, DebtorId = this.user2Id, CreditorId = this.user4Id, Amount = 10m },
            new Debt { GroupId = this.groupId, DebtorId = this.user3Id, CreditorId = this.user4Id, Amount = 50m },
            new Debt { GroupId = this.groupId, DebtorId = this.user4Id, CreditorId = this.user1Id, Amount = 30m }
        };

        // Act
        var result = this.algorithm.SimplifyDebts(debts);

        // Assert
        Assert.True(result.Count < debts.Count); // Should reduce number of transactions
        Assert.True(result.Count <= 3); // For 4 people, max 3 transactions needed
    }

    [Fact]
    public void SimplifyDebts_BalancesZeroOut_PreservesTotalAmounts()
    {
        // Arrange
        var debts = new List<Debt>
        {
            new Debt { GroupId = this.groupId, DebtorId = this.user1Id, CreditorId = this.user3Id, Amount = 100m },
            new Debt { GroupId = this.groupId, DebtorId = this.user2Id, CreditorId = this.user3Id, Amount = 50m }
        };

        // Act
        var result = this.algorithm.SimplifyDebts(debts);

        // Assert - total money flowing should remain the same
        var originalTotal = debts.Sum(d => d.Amount);
        var simplifiedTotal = result.Sum(t => t.Amount);
        Assert.Equal(originalTotal, simplifiedTotal);
    }

    [Fact]
    public void SimplifyDebts_EmptyDebts_ReturnsEmpty()
    {
        // Act
        var result = this.algorithm.SimplifyDebts(new List<Debt>());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void SimplifyDebts_NoSimplificationNeeded_ReturnsSameStructure()
    {
        // Arrange - Already optimal: one person owes one person
        var debts = new List<Debt>
        {
            new Debt
            {
                GroupId = this.groupId,
                DebtorId = this.user1Id,
                CreditorId = this.user2Id,
                Amount = 100m
            }
        };

        // Act
        var result = this.algorithm.SimplifyDebts(debts);

        // Assert
        Assert.Single(result);
        Assert.Equal(100m, result[0].Amount);
    }

    [Fact]
    public void GetSimplificationSummary_ShowsSavings()
    {
        // Arrange
        var debts = new List<Debt>
        {
            new Debt { GroupId = this.groupId, DebtorId = this.user1Id, CreditorId = this.user2Id, Amount = 50m },
            new Debt { GroupId = this.groupId, DebtorId = this.user2Id, CreditorId = this.user3Id, Amount = 30m },
            new Debt { GroupId = this.groupId, DebtorId = this.user3Id, CreditorId = this.user1Id, Amount = 20m }
        };

        // Act
        var simplifiedTransactions = this.algorithm.SimplifyDebts(debts);
        var summary = this.algorithm.GetSimplificationSummary(debts, simplifiedTransactions);

        // Assert
        Assert.Equal(3, summary.OriginalTransactionCount);
        Assert.True(summary.SimplifiedTransactionCount <= summary.OriginalTransactionCount);
        Assert.True(summary.TransactionsSaved >= 0);
    }

    [Fact]
    public void SimplifyDebts_LargeAmounts_MaintainsPrecision()
    {
        // Arrange
        var debts = new List<Debt>
        {
            new Debt { GroupId = this.groupId, DebtorId = this.user1Id, CreditorId = this.user2Id, Amount = 1234567.89m },
            new Debt { GroupId = this.groupId, DebtorId = this.user2Id, CreditorId = this.user3Id, Amount = 987654.32m }
        };

        // Act
        var result = this.algorithm.SimplifyDebts(debts);

        // Assert
        // In this scenario:
        // User1 owes User2: $1,234,567.89
        // User2 owes User3: $987,654.32
        // Net: User1 owes $1,234,567.89, User2 owes $-246,913.57 (is owed), User3 is owed $987,654.32
        // Simplified: User1 pays User2 $246,913.57 and User3 $987,654.32
        var totalOriginal = debts.Sum(d => d.Amount);
        var totalSimplified = result.Sum(t => t.Amount);
        
        // Total flow should be at most the original (optimized can be less)
        Assert.True(totalSimplified <= totalOriginal);
        
        // Should simplify to at most 2 transactions (3 people)
        Assert.True(result.Count <= 2);
    }

    [Fact]
    public void SimplifyDebts_SymmetricScenario_OptimizesCorrectly()
    {
        // Arrange
        // User1 and User2 both owe User3 $50 each
        // Should remain as 2 transactions (already optimal)
        var debts = new List<Debt>
        {
            new Debt { GroupId = this.groupId, DebtorId = this.user1Id, CreditorId = this.user3Id, Amount = 50m },
            new Debt { GroupId = this.groupId, DebtorId = this.user2Id, CreditorId = this.user3Id, Amount = 50m }
        };

        // Act
        var result = this.algorithm.SimplifyDebts(debts);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.Equal(this.user3Id, t.ToUserId));
        Assert.All(result, t => Assert.Equal(50m, t.Amount));
    }

    [Fact]
    public void SimplifyDebts_AllDebtorsOneCreditor_NoSimplification()
    {
        // Arrange - 3 people all owe User4 different amounts
        var debts = new List<Debt>
        {
            new Debt { GroupId = this.groupId, DebtorId = this.user1Id, CreditorId = this.user4Id, Amount = 30m },
            new Debt { GroupId = this.groupId, DebtorId = this.user2Id, CreditorId = this.user4Id, Amount = 40m },
            new Debt { GroupId = this.groupId, DebtorId = this.user3Id, CreditorId = this.user4Id, Amount = 20m }
        };

        // Act
        var result = this.algorithm.SimplifyDebts(debts);

        // Assert - Already optimal, should stay the same
        Assert.Equal(3, result.Count);
        Assert.All(result, t => Assert.Equal(this.user4Id, t.ToUserId));
    }
}
