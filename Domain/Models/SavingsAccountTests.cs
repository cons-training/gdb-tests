using GDB.App.Domain.Models;
using GDB.App.Tests.Fixtures;
using GDB.App.Tests.TestData;

namespace GDB.App.Tests.Domain.Models;

[TestClass]
public sealed class SavingsAccountTests
{
    public static IEnumerable<object[]> MinimumBalanceCases =>
        ExcelTestData.LoadFor(nameof(CheckMinimumBalance_WithConfiguredAmount_ReturnsExpectedResult));

    public static IEnumerable<object[]> InterestCases =>
        ExcelTestData.LoadFor(nameof(ApplyInterest_WithConfiguredRate_UpdatesBalance));

    public static IEnumerable<object[]> NegativeDebitCases =>
        ExcelTestData.LoadFor(nameof(ProcessDebit_WithNegativeAmount_IncreasesSavingsBalance));

    [TestMethod]
    [DynamicData(nameof(MinimumBalanceCases))]
    public void CheckMinimumBalance_WithConfiguredAmount_ReturnsExpectedResult(ExcelTestCase testCase)
    {
        var account = (SavingsAccount)AccountFixture.Create(testCase);

        bool actual = account.CheckMinimumBalance(testCase.GetDecimal("Amount"));

        Assert.AreEqual(testCase.GetBoolean("ExpectedResult"), actual, testCase.TestCaseId);
    }

    [TestMethod]
    [DynamicData(nameof(InterestCases))]
    public void ApplyInterest_WithConfiguredRate_UpdatesBalance(ExcelTestCase testCase)
    {
        var account = (SavingsAccount)AccountFixture.Create(testCase);

        int iterations = testCase.GetOptionalInt32("Iterations") ?? 1;
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            account.ApplyInterest();
        }

        Assert.AreEqual(testCase.GetDecimal("ExpectedBalance"), account.Balance, testCase.TestCaseId);
    }

    [TestMethod]
    [DynamicData(nameof(NegativeDebitCases))]
    public void ProcessDebit_WithNegativeAmount_IncreasesSavingsBalance(ExcelTestCase testCase)
    {
        var account = (SavingsAccount)AccountFixture.Create(testCase);

        account.ProcessDebit(testCase.GetDecimal("Amount"));

        Assert.AreEqual(testCase.GetDecimal("ExpectedBalance"), account.Balance, testCase.TestCaseId);
    }
}
