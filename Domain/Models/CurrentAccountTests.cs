using GDB.App.Domain.Models;
using GDB.App.Tests.Fixtures;
using GDB.App.Tests.TestData;

namespace GDB.App.Tests.Domain.Models;

[TestClass]
public sealed class CurrentAccountTests
{
    public static IEnumerable<object[]> OverdraftCases =>
        ExcelTestData.LoadFor(nameof(CheckIfOverdraftLimitIsExceeded_WithConfiguredAmount_ReturnsExpectedResult));

    public static IEnumerable<object[]> NegativeDebitCases =>
        ExcelTestData.LoadFor(nameof(ProcessDebit_WithNegativeAmount_IncreasesCurrentBalance));

    public static IEnumerable<object[]> OverdraftMutationCases =>
        ExcelTestData.LoadFor(nameof(SetOverdraftLimit_WithNegativeValue_ReducesAvailableCapacity));

    [TestMethod]
    [DynamicData(nameof(OverdraftCases))]
    public void CheckIfOverdraftLimitIsExceeded_WithConfiguredAmount_ReturnsExpectedResult(ExcelTestCase testCase)
    {
        var account = (CurrentAccount)AccountFixture.Create(testCase);

        bool actual = account.CheckIfOverdraftLimitIsExceeded(testCase.GetDecimal("Amount"));

        Assert.AreEqual(testCase.GetBoolean("ExpectedResult"), actual, testCase.TestCaseId);
    }

    [TestMethod]
    [DynamicData(nameof(NegativeDebitCases))]
    public void ProcessDebit_WithNegativeAmount_IncreasesCurrentBalance(ExcelTestCase testCase)
    {
        var account = (CurrentAccount)AccountFixture.Create(testCase);

        account.ProcessDebit(testCase.GetDecimal("Amount"));

        Assert.AreEqual(testCase.GetDecimal("ExpectedBalance"), account.Balance, testCase.TestCaseId);
    }

    [TestMethod]
    [DynamicData(nameof(OverdraftMutationCases))]
    public void SetOverdraftLimit_WithNegativeValue_ReducesAvailableCapacity(ExcelTestCase testCase)
    {
        var account = (CurrentAccount)AccountFixture.Create(testCase);
        account.OverdraftLimit = 25_000m;
        account.OverdraftLimit = testCase.GetDecimal("OverdraftLimit");

        bool actual = account.CheckIfOverdraftLimitIsExceeded(testCase.GetDecimal("Amount"));

        Assert.AreEqual(testCase.GetDecimal("OverdraftLimit"), account.OverdraftLimit);
        Assert.AreEqual(testCase.GetBoolean("ExpectedResult"), actual, testCase.TestCaseId);
    }
}
