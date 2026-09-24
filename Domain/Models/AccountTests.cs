using GDB.App.Domain.Enums;
using GDB.App.Tests.Fixtures;
using GDB.App.Tests.TestData;

namespace GDB.App.Tests.Domain.Models;

[TestClass]
public sealed class AccountTests
{
    public static IEnumerable<object[]> ValidatePinCases =>
        ExcelTestData.LoadFor(nameof(ValidatePin_WithEnteredPin_ReturnsExpectedResult));

    public static IEnumerable<object[]> ChangePinCases =>
        ExcelTestData.LoadFor(nameof(ChangePin_WithEnteredPins_ReturnsExpectedResult));

    public static IEnumerable<object[]> ActiveStatusCases =>
        ExcelTestData.LoadFor(nameof(CheckIfAccountIsActive_WithConfiguredStatus_ReturnsExpectedResult));

    public static IEnumerable<object[]> AmountCases =>
        ExcelTestData.LoadFor(nameof(CheckIfAmountIsValid_WithConfiguredAmountAndLimit_ReturnsExpectedResult));

    public static IEnumerable<object[]> DepositCases =>
        ExcelTestData.LoadFor(nameof(Deposit_WithConfiguredAccountAndAmount_ReturnsExpectedBalanceOrThrows));

    public static IEnumerable<object[]> WithdrawalCases =>
        ExcelTestData.LoadFor(nameof(Withdraw_WithConfiguredAccountPinAndAmount_ReturnsExpectedBalanceOrThrows));

    [TestMethod]
    [DynamicData(nameof(ValidatePinCases))]
    public void ValidatePin_WithEnteredPin_ReturnsExpectedResult(ExcelTestCase testCase)
    {
        var account = AccountFixture.Create(testCase);

        bool actual = account.ValidatePin(testCase.GetOptionalString("EnteredPin")!);

        Assert.AreEqual(testCase.GetBoolean("ExpectedResult"), actual, testCase.TestCaseId);
    }

    [TestMethod]
    [DynamicData(nameof(ChangePinCases))]
    public void ChangePin_WithEnteredPins_ReturnsExpectedResult(ExcelTestCase testCase)
    {
        var account = AccountFixture.Create(testCase);

        bool actual = account.ChangePin(
            testCase.GetRequiredString("EnteredPin"),
            testCase.GetRequiredString("NewPin"));

        Assert.AreEqual(testCase.GetBoolean("ExpectedResult"), actual, testCase.TestCaseId);
        Assert.IsTrue(
            account.ValidatePin(testCase.GetRequiredString("ExpectedPin")),
            $"{testCase.TestCaseId} left an unexpected PIN configured.");
    }

    [TestMethod]
    [DynamicData(nameof(ActiveStatusCases))]
    public void CheckIfAccountIsActive_WithConfiguredStatus_ReturnsExpectedResult(ExcelTestCase testCase)
    {
        var account = AccountFixture.Create(testCase);

        bool actual = account.CheckIfAccountIsActive();

        Assert.AreEqual(testCase.GetBoolean("ExpectedResult"), actual, testCase.TestCaseId);
    }

    [TestMethod]
    [DynamicData(nameof(AmountCases))]
    public void CheckIfAmountIsValid_WithConfiguredAmountAndLimit_ReturnsExpectedResult(ExcelTestCase testCase)
    {
        var account = AccountFixture.Create(testCase);

        bool actual = account.CheckIfAmountIsValid(
            testCase.GetDecimal("Amount"),
            testCase.GetDecimal("Limit"));

        Assert.AreEqual(testCase.GetBoolean("ExpectedResult"), actual, testCase.TestCaseId);
    }

    [TestMethod]
    [DynamicData(nameof(DepositCases))]
    public void Deposit_WithConfiguredAccountAndAmount_ReturnsExpectedBalanceOrThrows(ExcelTestCase testCase)
    {
        var account = AccountFixture.Create(testCase);
        decimal amount = testCase.GetDecimal("Amount");

        if (testCase.GetOptionalString("ExpectedException") is not null)
        {
            ExcelCaseAssert.ThrowsExpected(testCase, () => account.Deposit(amount));
        }
        else
        {
            account.Deposit(amount);
        }

        Assert.AreEqual(
            testCase.GetDecimal("ExpectedBalance"),
            account.Balance,
            testCase.TestCaseId);
    }

    [TestMethod]
    [DynamicData(nameof(WithdrawalCases))]
    public void Withdraw_WithConfiguredAccountPinAndAmount_ReturnsExpectedBalanceOrThrows(ExcelTestCase testCase)
    {
        var account = AccountFixture.Create(testCase);
        decimal amount = testCase.GetDecimal("Amount");
        string pin = testCase.GetRequiredString("EnteredPin");

        if (testCase.GetOptionalString("ExpectedException") is not null)
        {
            ExcelCaseAssert.ThrowsExpected(testCase, () => account.Withdraw(amount, pin));
        }
        else
        {
            account.Withdraw(amount, pin);
        }

        Assert.AreEqual(
            testCase.GetDecimal("ExpectedBalance"),
            account.Balance,
            testCase.TestCaseId);
    }
}
