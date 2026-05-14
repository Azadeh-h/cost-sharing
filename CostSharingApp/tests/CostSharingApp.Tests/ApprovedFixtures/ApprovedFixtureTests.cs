#nullable disable

using System.Text.Json;
using CostSharing.Core.Algorithms;
using CostSharing.Core.Models;
using CostSharing.Core.Services;

namespace CostSharingApp.Tests.ApprovedFixtures;

/// <summary>
/// Validates approved JSON-backed fixtures for rounding, simplification, and end-to-end debt flows.
/// </summary>
public class ApprovedFixtureTests
{
    private static readonly Guid[] ParticipantGuids =
    {
        Guid.Parse("00000000-0000-0000-0000-000000000001"),
        Guid.Parse("00000000-0000-0000-0000-000000000002"),
        Guid.Parse("00000000-0000-0000-0000-000000000003"),
        Guid.Parse("00000000-0000-0000-0000-000000000004"),
        Guid.Parse("00000000-0000-0000-0000-000000000005"),
        Guid.Parse("00000000-0000-0000-0000-000000000006"),
        Guid.Parse("00000000-0000-0000-0000-000000000007"),
    };

    private static readonly Guid GroupId = Guid.Parse("00000000-0000-0000-0000-100000000000");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void Rounding_EvenSplit_100_by_3()
    {
        var fixture = LoadRoundingFixture("EvenSplit_100_by_3");
        var service = new SplitCalculationService();
        var participants = CreateParticipantIds(fixture.ParticipantCount.Value);

        var result = service.CalculateEvenSplit(fixture.TotalAmount, participants);

        AssertRoundingAmounts(fixture, result);
    }

    [Fact]
    public void Rounding_EvenSplit_10_by_3()
    {
        var fixture = LoadRoundingFixture("EvenSplit_10_by_3");
        var service = new SplitCalculationService();
        var participants = CreateParticipantIds(fixture.ParticipantCount.Value);

        var result = service.CalculateEvenSplit(fixture.TotalAmount, participants);

        AssertRoundingAmounts(fixture, result);
    }

    [Fact]
    public void Rounding_EvenSplit_1_by_3()
    {
        var fixture = LoadRoundingFixture("EvenSplit_1_by_3");
        var service = new SplitCalculationService();
        var participants = CreateParticipantIds(fixture.ParticipantCount.Value);

        var result = service.CalculateEvenSplit(fixture.TotalAmount, participants);

        AssertRoundingAmounts(fixture, result);
    }

    [Fact]
    public void Rounding_EvenSplit_9999_by_7()
    {
        var fixture = LoadRoundingFixture("EvenSplit_9999_by_7");
        var service = new SplitCalculationService();
        var participants = CreateParticipantIds(fixture.ParticipantCount.Value);

        var result = service.CalculateEvenSplit(fixture.TotalAmount, participants);

        AssertRoundingAmounts(fixture, result);
    }

    [Fact]
    public void Rounding_EvenSplit_10001_by_2()
    {
        var fixture = LoadRoundingFixture("EvenSplit_10001_by_2");
        var service = new SplitCalculationService();
        var participants = CreateParticipantIds(fixture.ParticipantCount.Value);

        var result = service.CalculateEvenSplit(fixture.TotalAmount, participants);

        AssertRoundingAmounts(fixture, result);
    }

    [Fact]
    public void Rounding_CustomSplit_100_thirds()
    {
        var fixture = LoadRoundingFixture("CustomSplit_100_thirds");
        var service = new SplitCalculationService();
        var percentages = CreatePercentages(fixture.Percentages);

        var result = service.CalculateCustomSplit(fixture.TotalAmount, percentages);

        AssertRoundingAmounts(fixture, result);
    }

    [Fact]
    public void Rounding_AllScenarios_SumToTotal()
    {
        var fixtures = LoadAllRoundingFixtures();
        var service = new SplitCalculationService();

        foreach (var fixture in fixtures)
        {
            List<ExpenseSplit> result;
            if (fixture.Type == "even")
            {
                result = service.CalculateEvenSplit(fixture.TotalAmount, CreateParticipantIds(fixture.ParticipantCount.Value));
            }
            else
            {
                result = service.CalculateCustomSplit(fixture.TotalAmount, CreatePercentages(fixture.Percentages));
            }

            AssertRoundingAmounts(fixture, result);
            Assert.Equal(fixture.TotalAmount, result.Sum(s => s.Amount));
        }
    }

    [Fact]
    public void Simplification_CircularThreePerson()
    {
        var fixture = LoadSimplificationFixture("CircularThreePerson");
        var algorithm = new DebtSimplificationAlgorithm();
        var debts = CreateDebts(fixture.Debts);

        var result = algorithm.SimplifyDebts(debts);

        AssertSimplifiedTransactions(fixture.ExpectedTransactions, result);
    }

    [Fact]
    public void Simplification_ComplexFourPerson()
    {
        var fixture = LoadSimplificationFixture("ComplexFourPerson");
        var algorithm = new DebtSimplificationAlgorithm();
        var debts = CreateDebts(fixture.Debts);

        var result = algorithm.SimplifyDebts(debts);

        AssertSimplifiedTransactions(fixture.ExpectedTransactions, result);
    }

    [Fact]
    public void Simplification_SymmetricTwoDebtors()
    {
        var fixture = LoadSimplificationFixture("SymmetricTwoDebtors");
        var algorithm = new DebtSimplificationAlgorithm();
        var debts = CreateDebts(fixture.Debts);

        var result = algorithm.SimplifyDebts(debts);

        AssertSimplifiedTransactions(fixture.ExpectedTransactions, result);
    }

    [Fact]
    public void Simplification_LargeAmounts()
    {
        var fixture = LoadSimplificationFixture("LargeAmounts");
        var algorithm = new DebtSimplificationAlgorithm();
        var debts = CreateDebts(fixture.Debts);

        var result = algorithm.SimplifyDebts(debts);

        AssertSimplifiedTransactions(fixture.ExpectedTransactions, result);
    }

    [Fact]
    public void Simplification_AllScenarios_PreserveNetBalances()
    {
        var fixtures = LoadAllSimplificationFixtures();
        var algorithm = new DebtSimplificationAlgorithm();

        foreach (var fixture in fixtures)
        {
            var debts = CreateDebts(fixture.Debts);
            var result = algorithm.SimplifyDebts(debts);
            var originalBalances = CalculateDebtNetBalances(debts);
            var simplifiedBalances = CalculateSimplifiedNetBalances(result);
            var allUsers = originalBalances.Keys.Union(simplifiedBalances.Keys).ToList();

            foreach (var userId in allUsers)
            {
                var original = originalBalances.ContainsKey(userId) ? originalBalances[userId] : 0m;
                var simplified = simplifiedBalances.ContainsKey(userId) ? simplifiedBalances[userId] : 0m;
                Assert.Equal(original, simplified);
            }
        }
    }

    [Fact]
    public void Pipeline_DinnerAndTaxi_FullFlow()
    {
        var fixture = LoadPipelineFixture("DinnerAndTaxi");
        var splitService = new SplitCalculationService();
        var debtService = new DebtCalculationService();
        var simplificationAlgorithm = new DebtSimplificationAlgorithm();

        var expense0Splits = splitService.CalculateEvenSplit(
            fixture.Expenses[0].TotalAmount,
            fixture.Expenses[0].Participants.Select(p => ParticipantGuids[p]).ToList());

        foreach (var expected in fixture.ExpectedSplits[0].Amounts)
        {
            var participantGuid = ParticipantGuids[int.Parse(expected.Key)];
            var actual = expense0Splits.First(s => s.UserId == participantGuid);
            Assert.Equal(expected.Value, actual.Amount);
        }

        var percentages = fixture.Expenses[1].CustomPercentages
            .ToDictionary(kv => ParticipantGuids[int.Parse(kv.Key)], kv => kv.Value);
        var expense1Splits = splitService.CalculateCustomSplit(
            fixture.Expenses[1].TotalAmount,
            percentages);

        foreach (var expected in fixture.ExpectedSplits[1].Amounts)
        {
            var participantGuid = ParticipantGuids[int.Parse(expected.Key)];
            var actual = expense1Splits.First(s => s.UserId == participantGuid);
            Assert.Equal(expected.Value, actual.Amount);
        }

        var expenses = new List<Expense>();
        var allSplits = new List<ExpenseSplit>();

        var exp0 = new Expense
        {
            Id = Guid.NewGuid(),
            GroupId = GroupId,
            PaidBy = ParticipantGuids[fixture.Expenses[0].PaidBy],
            TotalAmount = fixture.Expenses[0].TotalAmount
        };
        expenses.Add(exp0);
        foreach (var split in expense0Splits)
        {
            split.ExpenseId = exp0.Id;
        }

        allSplits.AddRange(expense0Splits);

        var exp1 = new Expense
        {
            Id = Guid.NewGuid(),
            GroupId = GroupId,
            PaidBy = ParticipantGuids[fixture.Expenses[1].PaidBy],
            TotalAmount = fixture.Expenses[1].TotalAmount
        };
        expenses.Add(exp1);
        foreach (var split in expense1Splits)
        {
            split.ExpenseId = exp1.Id;
        }

        allSplits.AddRange(expense1Splits);

        var debts = debtService.CalculateDebts(expenses, allSplits);

        Assert.Equal(fixture.ExpectedDebts.Length, debts.Count);
        for (int i = 0; i < debts.Count; i++)
        {
            Assert.Equal(ParticipantGuids[fixture.ExpectedDebts[i].From], debts[i].DebtorId);
            Assert.Equal(ParticipantGuids[fixture.ExpectedDebts[i].To], debts[i].CreditorId);
            Assert.Equal(fixture.ExpectedDebts[i].Amount, debts[i].Amount);
        }

        var simplified = simplificationAlgorithm.SimplifyDebts(debts);
        Assert.Equal(fixture.ExpectedSimplified.Length, simplified.Count);
        for (int i = 0; i < simplified.Count; i++)
        {
            Assert.Equal(ParticipantGuids[fixture.ExpectedSimplified[i].From], simplified[i].FromUserId);
            Assert.Equal(ParticipantGuids[fixture.ExpectedSimplified[i].To], simplified[i].ToUserId);
            Assert.Equal(fixture.ExpectedSimplified[i].Amount, simplified[i].Amount);
        }

        var settlement = new Settlement
        {
            Id = Guid.NewGuid(),
            GroupId = GroupId,
            PaidBy = ParticipantGuids[fixture.Settlement.PaidBy],
            PaidTo = ParticipantGuids[fixture.Settlement.PaidTo],
            Amount = fixture.Settlement.Amount,
            Status = SettlementStatus.Confirmed
        };

        var postSettlementDebts = debtService.CalculateDebts(expenses, allSplits, new List<Settlement> { settlement });
        Assert.Equal(fixture.ExpectedPostSettlementDebts.Length, postSettlementDebts.Count);
        for (int i = 0; i < postSettlementDebts.Count; i++)
        {
            Assert.Equal(ParticipantGuids[fixture.ExpectedPostSettlementDebts[i].From], postSettlementDebts[i].DebtorId);
            Assert.Equal(ParticipantGuids[fixture.ExpectedPostSettlementDebts[i].To], postSettlementDebts[i].CreditorId);
            Assert.Equal(fixture.ExpectedPostSettlementDebts[i].Amount, postSettlementDebts[i].Amount);
        }
    }

    [Fact]
    public void Determinism_AllScenarios_ProduceIdenticalOutputAcrossRuns()
    {
        var fixtures = LoadAllSimplificationFixtures();
        var algorithm = new DebtSimplificationAlgorithm();

        foreach (var fixture in fixtures)
        {
            var debts = CreateDebts(fixture.Debts);
            var firstRun = algorithm.SimplifyDebts(debts);

            for (int run = 1; run < 10; run++)
            {
                var freshDebts = CreateDebts(fixture.Debts);
                var result = algorithm.SimplifyDebts(freshDebts);

                Assert.Equal(firstRun.Count, result.Count);
                for (int i = 0; i < firstRun.Count; i++)
                {
                    Assert.Equal(firstRun[i].FromUserId, result[i].FromUserId);
                    Assert.Equal(firstRun[i].ToUserId, result[i].ToUserId);
                    Assert.Equal(firstRun[i].Amount, result[i].Amount);
                }
            }
        }
    }

    private static List<Guid> CreateParticipantIds(int count)
    {
        return ParticipantGuids.Take(count).ToList();
    }

    private static Dictionary<Guid, decimal> CreatePercentages(decimal[] percentages)
    {
        return percentages
            .Select((percentage, index) => new KeyValuePair<Guid, decimal>(ParticipantGuids[index], percentage))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    private static void AssertRoundingAmounts(RoundingScenario fixture, List<ExpenseSplit> result)
    {
        Assert.Equal(fixture.ExpectedAmounts.Length, result.Count);
        for (int i = 0; i < result.Count; i++)
        {
            Assert.True(
                fixture.ExpectedAmounts[i] == result[i].Amount,
                $"Participant {i}: expected {fixture.ExpectedAmounts[i]} but got {result[i].Amount}");
        }
    }

    private static List<Debt> CreateDebts(TransactionEntry[] entries)
    {
        return entries.Select(d => new Debt
        {
            GroupId = GroupId,
            DebtorId = ParticipantGuids[d.From],
            CreditorId = ParticipantGuids[d.To],
            Amount = d.Amount
        }).ToList();
    }

    private static void AssertSimplifiedTransactions(TransactionEntry[] expectedTransactions, List<SimplifiedTransaction> result)
    {
        Assert.Equal(expectedTransactions.Length, result.Count);
        for (int i = 0; i < result.Count; i++)
        {
            Assert.Equal(ParticipantGuids[expectedTransactions[i].From], result[i].FromUserId);
            Assert.Equal(ParticipantGuids[expectedTransactions[i].To], result[i].ToUserId);
            Assert.Equal(expectedTransactions[i].Amount, result[i].Amount);
        }
    }

    private static Dictionary<Guid, decimal> CalculateDebtNetBalances(IEnumerable<Debt> debts)
    {
        var balances = new Dictionary<Guid, decimal>();
        foreach (var debt in debts)
        {
            if (!balances.ContainsKey(debt.CreditorId))
            {
                balances[debt.CreditorId] = 0m;
            }

            if (!balances.ContainsKey(debt.DebtorId))
            {
                balances[debt.DebtorId] = 0m;
            }

            balances[debt.CreditorId] += debt.Amount;
            balances[debt.DebtorId] -= debt.Amount;
        }

        return balances;
    }

    private static Dictionary<Guid, decimal> CalculateSimplifiedNetBalances(IEnumerable<SimplifiedTransaction> transactions)
    {
        var balances = new Dictionary<Guid, decimal>();
        foreach (var transaction in transactions)
        {
            if (!balances.ContainsKey(transaction.ToUserId))
            {
                balances[transaction.ToUserId] = 0m;
            }

            if (!balances.ContainsKey(transaction.FromUserId))
            {
                balances[transaction.FromUserId] = 0m;
            }

            balances[transaction.ToUserId] += transaction.Amount;
            balances[transaction.FromUserId] -= transaction.Amount;
        }

        return balances;
    }

    private static RoundingScenario LoadRoundingFixture(string name)
    {
        var file = JsonSerializer.Deserialize<RoundingFixtureFile>(
            File.ReadAllText(Path.Combine("ApprovedFixtures", "rounding-scenarios.json")),
            JsonOptions);
        return file.Scenarios.First(s => s.Name == name);
    }

    private static List<RoundingScenario> LoadAllRoundingFixtures()
    {
        var file = JsonSerializer.Deserialize<RoundingFixtureFile>(
            File.ReadAllText(Path.Combine("ApprovedFixtures", "rounding-scenarios.json")),
            JsonOptions);
        return file.Scenarios.ToList();
    }

    private static SimplificationScenario LoadSimplificationFixture(string name)
    {
        var file = JsonSerializer.Deserialize<SimplificationFixtureFile>(
            File.ReadAllText(Path.Combine("ApprovedFixtures", "simplification-scenarios.json")),
            JsonOptions);
        return file.Scenarios.First(s => s.Name == name);
    }

    private static List<SimplificationScenario> LoadAllSimplificationFixtures()
    {
        var file = JsonSerializer.Deserialize<SimplificationFixtureFile>(
            File.ReadAllText(Path.Combine("ApprovedFixtures", "simplification-scenarios.json")),
            JsonOptions);
        return file.Scenarios.ToList();
    }

    private static PipelineScenario LoadPipelineFixture(string name)
    {
        var file = JsonSerializer.Deserialize<PipelineFixtureFile>(
            File.ReadAllText(Path.Combine("ApprovedFixtures", "pipeline-scenarios.json")),
            JsonOptions);
        return file.Scenarios.First(s => s.Name == name);
    }

    private class RoundingFixtureFile
    {
        public string Description { get; set; }

        public RoundingScenario[] Scenarios { get; set; }
    }

    private class RoundingScenario
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public decimal TotalAmount { get; set; }

        public int? ParticipantCount { get; set; }

        public decimal[] Percentages { get; set; }

        public decimal[] ExpectedAmounts { get; set; }
    }

    private class SimplificationFixtureFile
    {
        public string Description { get; set; }

        public SimplificationScenario[] Scenarios { get; set; }
    }

    private class SimplificationScenario
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public TransactionEntry[] Debts { get; set; }

        public TransactionEntry[] ExpectedTransactions { get; set; }
    }

    private class TransactionEntry
    {
        public int From { get; set; }

        public int To { get; set; }

        public decimal Amount { get; set; }
    }

    private class PipelineFixtureFile
    {
        public string Description { get; set; }

        public PipelineScenario[] Scenarios { get; set; }
    }

    private class PipelineScenario
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public PipelineExpense[] Expenses { get; set; }

        public PipelineSplitExpectation[] ExpectedSplits { get; set; }

        public TransactionEntry[] ExpectedDebts { get; set; }

        public TransactionEntry[] ExpectedSimplified { get; set; }

        public PipelineSettlement Settlement { get; set; }

        public TransactionEntry[] ExpectedPostSettlementDebts { get; set; }
    }

    private class PipelineExpense
    {
        public int PaidBy { get; set; }

        public decimal TotalAmount { get; set; }

        public string SplitType { get; set; }

        public int[] Participants { get; set; }

        public Dictionary<string, decimal> CustomPercentages { get; set; }
    }

    private class PipelineSplitExpectation
    {
        public int ExpenseIndex { get; set; }

        public Dictionary<string, decimal> Amounts { get; set; }
    }

    private class PipelineSettlement
    {
        public int PaidBy { get; set; }

        public int PaidTo { get; set; }

        public decimal Amount { get; set; }
    }
}
