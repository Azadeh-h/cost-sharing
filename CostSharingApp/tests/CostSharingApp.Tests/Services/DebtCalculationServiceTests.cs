using CostSharing.Core.Models;
using CostSharing.Core.Services;

namespace CostSharingApp.Tests.Services;

/// <summary>
/// Unit tests for DebtCalculationService.
/// </summary>
public class DebtCalculationServiceTests
{
    private readonly DebtCalculationService service;
    private readonly Guid groupId;
    private readonly Guid user1Id;
    private readonly Guid user2Id;
    private readonly Guid user3Id;

    public DebtCalculationServiceTests()
    {
        this.service = new DebtCalculationService();
        this.groupId = Guid.NewGuid();
        this.user1Id = Guid.NewGuid();
        this.user2Id = Guid.NewGuid();
        this.user3Id = Guid.NewGuid();
    }

    [Fact]
    public void CalculateDebts_SingleExpenseEvenSplit_CreatesCorrectDebts()
    {
        // Arrange
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            GroupId = this.groupId,
            PaidBy = this.user1Id,
            TotalAmount = 100m
        };

        var splits = new List<ExpenseSplit>
        {
            new ExpenseSplit { ExpenseId = expense.Id, UserId = this.user1Id, Amount = 50m },
            new ExpenseSplit { ExpenseId = expense.Id, UserId = this.user2Id, Amount = 50m }
        };

        // Act
        var debts = this.service.CalculateDebts(new List<Expense> { expense }, splits);

        // Assert
        Assert.Single(debts);
        var debt = debts[0];
        Assert.Equal(this.user2Id, debt.DebtorId);
        Assert.Equal(this.user1Id, debt.CreditorId);
        Assert.Equal(50m, debt.Amount);
    }

    [Fact]
    public void CalculateDebts_ThreePeopleOneExpense_CreatesCorrectDebts()
    {
        // Arrange - User1 paid $300, split evenly among 3 people ($100 each)
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            GroupId = this.groupId,
            PaidBy = this.user1Id,
            TotalAmount = 300m
        };

        var splits = new List<ExpenseSplit>
        {
            new ExpenseSplit { ExpenseId = expense.Id, UserId = this.user1Id, Amount = 100m },
            new ExpenseSplit { ExpenseId = expense.Id, UserId = this.user2Id, Amount = 100m },
            new ExpenseSplit { ExpenseId = expense.Id, UserId = this.user3Id, Amount = 100m }
        };

        // Act
        var debts = this.service.CalculateDebts(new List<Expense> { expense }, splits);

        // Assert
        Assert.Equal(2, debts.Count);
        Assert.All(debts, d => Assert.Equal(this.user1Id, d.CreditorId));
        Assert.All(debts, d => Assert.Equal(100m, d.Amount));
        Assert.Contains(debts, d => d.DebtorId == this.user2Id);
        Assert.Contains(debts, d => d.DebtorId == this.user3Id);
    }

    [Fact]
    public void CalculateDebts_MultipleExpenses_AggregatesDebts()
    {
        // Arrange
        // Expense 1: User1 paid $100, split evenly with User2 ($50 each)
        var expense1 = new Expense
        {
            Id = Guid.NewGuid(),
            GroupId = this.groupId,
            PaidBy = this.user1Id,
            TotalAmount = 100m
        };

        var splits1 = new List<ExpenseSplit>
        {
            new ExpenseSplit { ExpenseId = expense1.Id, UserId = this.user1Id, Amount = 50m },
            new ExpenseSplit { ExpenseId = expense1.Id, UserId = this.user2Id, Amount = 50m }
        };

        // Expense 2: User2 paid $60, split evenly with User1 ($30 each)
        var expense2 = new Expense
        {
            Id = Guid.NewGuid(),
            GroupId = this.groupId,
            PaidBy = this.user2Id,
            TotalAmount = 60m
        };

        var splits2 = new List<ExpenseSplit>
        {
            new ExpenseSplit { ExpenseId = expense2.Id, UserId = this.user1Id, Amount = 30m },
            new ExpenseSplit { ExpenseId = expense2.Id, UserId = this.user2Id, Amount = 30m }
        };

        // Act
        var debts = this.service.CalculateDebts(
            new List<Expense> { expense1, expense2 },
            splits1.Concat(splits2).ToList());

        // Assert
        // User2 owes User1: $50 (from expense1) - $30 (User1 owes from expense2) = $20
        Assert.Single(debts);
        Assert.Equal(this.user2Id, debts[0].DebtorId);
        Assert.Equal(this.user1Id, debts[0].CreditorId);
        Assert.Equal(20m, debts[0].Amount);
    }

    [Fact]
    public void CalculateDebts_CustomSplit_CreatesCorrectDebts()
    {
        // Arrange - User1 paid $200, but User2 pays 70% ($140) and User3 pays 30% ($60)
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            GroupId = this.groupId,
            PaidBy = this.user1Id,
            TotalAmount = 200m
        };

        var splits = new List<ExpenseSplit>
        {
            new ExpenseSplit { ExpenseId = expense.Id, UserId = this.user2Id, Amount = 140m },
            new ExpenseSplit { ExpenseId = expense.Id, UserId = this.user3Id, Amount = 60m }
        };

        // Act
        var debts = this.service.CalculateDebts(new List<Expense> { expense }, splits);

        // Assert
        Assert.Equal(2, debts.Count);
        Assert.Contains(debts, d => d.DebtorId == this.user2Id && d.Amount == 140m);
        Assert.Contains(debts, d => d.DebtorId == this.user3Id && d.Amount == 60m);
    }

    [Fact]
    public void CalculateDebts_PayerNotInSplit_AllDebt()
    {
        // Arrange - User1 paid $100, but only User2 and User3 are in split
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            GroupId = this.groupId,
            PaidBy = this.user1Id,
            TotalAmount = 100m
        };

        var splits = new List<ExpenseSplit>
        {
            new ExpenseSplit { ExpenseId = expense.Id, UserId = this.user2Id, Amount = 60m },
            new ExpenseSplit { ExpenseId = expense.Id, UserId = this.user3Id, Amount = 40m }
        };

        // Act
        var debts = this.service.CalculateDebts(new List<Expense> { expense }, splits);

        // Assert
        Assert.Equal(2, debts.Count);
        Assert.All(debts, d => Assert.Equal(this.user1Id, d.CreditorId));
        Assert.Contains(debts, d => d.DebtorId == this.user2Id && d.Amount == 60m);
        Assert.Contains(debts, d => d.DebtorId == this.user3Id && d.Amount == 40m);
    }

    [Fact]
    public void CalculateDebts_EmptyExpenses_ReturnsEmptyList()
    {
        // Act
        var debts = this.service.CalculateDebts(new List<Expense>(), new List<ExpenseSplit>());

        // Assert
        Assert.Empty(debts);
    }

    [Fact]
    public void CalculateDebts_WithSettlements_ReducesDebts()
    {
        // Arrange
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            GroupId = this.groupId,
            PaidBy = this.user1Id,
            TotalAmount = 100m
        };

        var splits = new List<ExpenseSplit>
        {
            new ExpenseSplit { ExpenseId = expense.Id, UserId = this.user1Id, Amount = 50m },
            new ExpenseSplit { ExpenseId = expense.Id, UserId = this.user2Id, Amount = 50m }
        };

        // User2 paid User1 $30
        var settlement = new Settlement
        {
            Id = Guid.NewGuid(),
            GroupId = this.groupId,
            PaidBy = this.user2Id,
            PaidTo = this.user1Id,
            Amount = 30m,
            Status = SettlementStatus.Confirmed
        };

        // Act
        var debts = this.service.CalculateDebts(
            new List<Expense> { expense },
            splits,
            new List<Settlement> { settlement });

        // Assert
        // User2 owed User1 $50, paid $30, so $20 remaining
        Assert.Single(debts);
        Assert.Equal(this.user2Id, debts[0].DebtorId);
        Assert.Equal(this.user1Id, debts[0].CreditorId);
        Assert.Equal(20m, debts[0].Amount);
    }

    [Fact]
    public void CalculateDebts_SettlementFullyPaysDebt_NoRemainingDebt()
    {
        // Arrange
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            GroupId = this.groupId,
            PaidBy = this.user1Id,
            TotalAmount = 100m
        };

        var splits = new List<ExpenseSplit>
        {
            new ExpenseSplit { ExpenseId = expense.Id, UserId = this.user1Id, Amount = 50m },
            new ExpenseSplit { ExpenseId = expense.Id, UserId = this.user2Id, Amount = 50m }
        };

        var settlement = new Settlement
        {
            Id = Guid.NewGuid(),
            GroupId = this.groupId,
            PaidBy = this.user2Id,
            PaidTo = this.user1Id,
            Amount = 50m,
            Status = SettlementStatus.Confirmed
        };

        // Act
        var debts = this.service.CalculateDebts(
            new List<Expense> { expense },
            splits,
            new List<Settlement> { settlement });

        // Assert
        Assert.Empty(debts); // Debt fully settled
    }

    [Fact]
    public void CalculateDebts_PendingSettlement_NotIncluded()
    {
        // Arrange
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            GroupId = this.groupId,
            PaidBy = this.user1Id,
            TotalAmount = 100m
        };

        var splits = new List<ExpenseSplit>
        {
            new ExpenseSplit { ExpenseId = expense.Id, UserId = this.user1Id, Amount = 50m },
            new ExpenseSplit { ExpenseId = expense.Id, UserId = this.user2Id, Amount = 50m }
        };

        var pendingSettlement = new Settlement
        {
            Id = Guid.NewGuid(),
            GroupId = this.groupId,
            PaidBy = this.user2Id,
            PaidTo = this.user1Id,
            Amount = 30m,
            Status = SettlementStatus.Pending
        };

        // Act
        var debts = this.service.CalculateDebts(
            new List<Expense> { expense },
            splits,
            new List<Settlement> { pendingSettlement });

        // Assert
        // Pending settlement should not affect debt calculation
        Assert.Single(debts);
        Assert.Equal(50m, debts[0].Amount);
    }

    [Fact]
    public void CalculateDebts_CancelledSettlement_NotIncluded()
    {
        // Arrange
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            GroupId = this.groupId,
            PaidBy = this.user1Id,
            TotalAmount = 100m
        };

        var splits = new List<ExpenseSplit>
        {
            new ExpenseSplit { ExpenseId = expense.Id, UserId = this.user1Id, Amount = 50m },
            new ExpenseSplit { ExpenseId = expense.Id, UserId = this.user2Id, Amount = 50m }
        };

        var cancelledSettlement = new Settlement
        {
            Id = Guid.NewGuid(),
            GroupId = this.groupId,
            PaidBy = this.user2Id,
            PaidTo = this.user1Id,
            Amount = 30m,
            Status = SettlementStatus.Cancelled
        };

        // Act
        var debts = this.service.CalculateDebts(
            new List<Expense> { expense },
            splits,
            new List<Settlement> { cancelledSettlement });

        // Assert
        // Cancelled settlement should not affect debt calculation
        Assert.Single(debts);
        Assert.Equal(50m, debts[0].Amount);
    }
}
