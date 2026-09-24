using GDB.App.Domain.Enums;
using GDB.App.Domain.Models;
using GDB.App.Tests.Fixtures;
using GDB.App.Tests.TestData;

namespace GDB.App.Tests.Domain.Models;

[TestClass]
public sealed class SalaryAccountTests
{
    public static IEnumerable<object[]> FundsCases =>
        ExcelTestData.LoadFor(nameof(CheckInsufficientFunds_WithConfiguredAmount_ReturnsExpectedResult));

    public static IEnumerable<object[]> InactivityCases =>
        ExcelTestData.LoadFor(nameof(IncrementInactiveMonths_WithConfiguredDuration_UpdatesStatus));

    public static IEnumerable<object[]> NegativeDebitCases =>
        ExcelTestData.LoadFor(nameof(ProcessDebit_WithNegativeAmount_IncreasesSalaryBalance));

    [TestMethod]
    [DynamicData(nameof(FundsCases))]
    public void CheckInsufficientFunds_WithConfiguredAmount_ReturnsExpectedResult(ExcelTestCase testCase)
    {
        var account = (SalaryAccount)AccountFixture.Create(testCase);

        bool actual = account.CheckInsufficientFunds(testCase.GetDecimal("Amount"));

        Assert.AreEqual(testCase.GetBoolean("ExpectedResult"), actual, testCase.TestCaseId);
    }

    [TestMethod]
    [DynamicData(nameof(InactivityCases))]
    public void IncrementInactiveMonths_WithConfiguredDuration_UpdatesStatus(ExcelTestCase testCase)
    {
        var account = (SalaryAccount)AccountFixture.Create(testCase);
        int inactiveMonths = testCase.GetInt32("InactiveMonths");

        for (int month = 0; month < inactiveMonths; month++)
        {
            account.IncrementInactiveMonths();
        }

        Assert.AreEqual(inactiveMonths, account.InactiveMonths, testCase.TestCaseId);
        Assert.AreEqual(
            testCase.GetEnum<AccountStatus>("ExpectedStatus"),
            account.Status,
            testCase.TestCaseId);
        Assert.AreEqual(
            testCase.GetBoolean("ExpectedResult"),
            account.CheckInactiveMonthsDuration(),
            testCase.TestCaseId);
    }

    [TestMethod]
    [DynamicData(nameof(NegativeDebitCases))]
    public void ProcessDebit_WithNegativeAmount_IncreasesSalaryBalance(ExcelTestCase testCase)
    {
        var account = (SalaryAccount)AccountFixture.Create(testCase);

        account.ProcessDebit(testCase.GetDecimal("Amount"));

        Assert.AreEqual(testCase.GetDecimal("ExpectedBalance"), account.Balance, testCase.TestCaseId);
    }
}
