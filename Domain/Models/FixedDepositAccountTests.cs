using GDB.App.Domain.Models;
using GDB.App.Tests.Fixtures;
using GDB.App.Tests.TestData;

namespace GDB.App.Tests.Domain.Models;

[TestClass]
public sealed class FixedDepositAccountTests
{
    public static IEnumerable<object[]> MaturityCases =>
        ExcelTestData.LoadFor(nameof(CalculateMaturityAmount_WithConfiguredTerms_ReturnsExpectedAmount));

    [TestMethod]
    [DynamicData(nameof(MaturityCases))]
    public void CalculateMaturityAmount_WithConfiguredTerms_ReturnsExpectedAmount(ExcelTestCase testCase)
    {
        var account = (FixedDepositAccount)AccountFixture.Create(testCase);

        decimal actual = account.CalculateMaturityAmount();
        decimal expected = testCase.GetDecimal("ExpectedResult");

        Assert.IsLessThanOrEqualTo(
            0.0000001m,
            Math.Abs(expected - actual),
            $"{testCase.TestCaseId}: expected {expected}, actual {actual}.");
    }
}
