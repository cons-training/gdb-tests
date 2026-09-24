using GDB.App.Tests.TestData;

namespace GDB.App.Tests.Validation;

[TestClass]
public sealed class ExcelWorkbookTests
{
    private static readonly HashSet<string> AllowedTypes =
        new(["Positive", "Negative", "Edge Case"], StringComparer.Ordinal);

    private static readonly HashSet<string> KnownMethods =
        new(
        [
            "ValidatePin_WithEnteredPin_ReturnsExpectedResult",
            "ChangePin_WithEnteredPins_ReturnsExpectedResult",
            "Deposit_WithConfiguredAccountAndAmount_ReturnsExpectedBalanceOrThrows",
            "Withdraw_WithConfiguredAccountPinAndAmount_ReturnsExpectedBalanceOrThrows",
            "CheckIfAccountIsActive_WithConfiguredStatus_ReturnsExpectedResult",
            "CheckIfAmountIsValid_WithConfiguredAmountAndLimit_ReturnsExpectedResult",
            "CheckMinimumBalance_WithConfiguredAmount_ReturnsExpectedResult",
            "ApplyInterest_WithConfiguredRate_UpdatesBalance",
            "CheckIfOverdraftLimitIsExceeded_WithConfiguredAmount_ReturnsExpectedResult",
            "CheckInsufficientFunds_WithConfiguredAmount_ReturnsExpectedResult",
            "IncrementInactiveMonths_WithConfiguredDuration_UpdatesStatus",
            "CalculateMaturityAmount_WithConfiguredTerms_ReturnsExpectedAmount",
            "CreateAccount_WithConfiguredType_ReturnsExpectedAccountOrThrows",
            "CreateAccount_WithOptionalArgumentsOmitted_ReturnsDefaultSubtypeSettings",
            "ProcessDebit_WithNegativeAmount_IncreasesSavingsBalance",
            "ProcessDebit_WithNegativeAmount_IncreasesCurrentBalance",
            "SetOverdraftLimit_WithNegativeValue_ReducesAvailableCapacity",
            "ProcessDebit_WithNegativeAmount_IncreasesSalaryBalance"
        ],
        StringComparer.Ordinal);

    [TestMethod]
    public void LoadWorkbook_WithConfiguredCases_ReturnsUniqueIdentifiersAndAllowedTypes()
    {
        IReadOnlyList<ExcelTestCase> cases = ExcelTestData.All;

        Assert.IsNotEmpty(cases);
        CollectionAssert.AllItemsAreUnique(cases.Select(c => c.TestCaseId).ToList());
        CollectionAssert.AllItemsAreUnique(
            cases.Select(c => c.GetInt32("serialNo")).ToList());

        foreach (ExcelTestCase testCase in cases)
        {
            testCase.GetRequiredString("MethodUnderTest");
            Assert.Contains(
                testCase.GetRequiredString("TestCaseType"),
                AllowedTypes,
                $"{testCase.TestCaseId} has unsupported TestCaseType.");
        }
    }

    [TestMethod]
    public void LoadWorkbook_WithConfiguredMethods_MapsEveryCaseToKnownMethod()
    {
        foreach (ExcelTestCase testCase in ExcelTestData.All)
        {
            Assert.Contains(
                testCase.TestMethod,
                KnownMethods,
                $"{testCase.TestCaseId} references unknown method '{testCase.TestMethod}'.");
        }

        foreach (string method in KnownMethods)
        {
            Assert.IsTrue(
                ExcelTestData.All.Any(testCase => testCase.TestMethod == method),
                $"No Excel cases target '{method}'.");
        }
    }
}
