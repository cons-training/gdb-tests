using GDB.App.Domain;
using GDB.App.Domain.Enums;
using GDB.App.Domain.Models;
using GDB.App.Tests.TestData;

namespace GDB.App.Tests.Domain;

[TestClass]
public sealed class AccountFactoryTests
{
    public static IEnumerable<object[]> FactoryCases =>
        ExcelTestData.LoadFor(nameof(CreateAccount_WithConfiguredType_ReturnsExpectedAccountOrThrows));

    public static IEnumerable<object[]> FactoryDefaultCases =>
        ExcelTestData.LoadFor(nameof(CreateAccount_WithOptionalArgumentsOmitted_ReturnsDefaultSubtypeSettings));

    [TestMethod]
    [DynamicData(nameof(FactoryCases))]
    public void CreateAccount_WithConfiguredType_ReturnsExpectedAccountOrThrows(ExcelTestCase testCase)
    {
        AccountType accountType = testCase.GetEnum<AccountType>("AccountType");

        if (testCase.GetOptionalString("ExpectedException") is not null)
        {
            ExcelCaseAssert.ThrowsExpected(testCase, () => CreateAccount(testCase, accountType));
            return;
        }

        Account account = CreateAccount(testCase, accountType);

        Assert.AreEqual(
            testCase.GetRequiredString("ExpectedConcreteType"),
            account.GetType().Name,
            testCase.TestCaseId);
        Assert.AreEqual(testCase.GetDecimal("InitialBalance"), account.Balance, testCase.TestCaseId);
        Assert.AreEqual(accountType, account.AccountType, testCase.TestCaseId);
        AssertCommonProperties(testCase, account);

        AssertSubtypeProperties(testCase, account);
    }

    [TestMethod]
    [DynamicData(nameof(FactoryDefaultCases))]
    public void CreateAccount_WithOptionalArgumentsOmitted_ReturnsDefaultSubtypeSettings(
        ExcelTestCase testCase)
    {
        Account account = AccountFactory.CreateAccount(
            testCase.GetEnum<AccountType>("AccountType"),
            testCase.GetRequiredString("AccountNumber"),
            testCase.GetRequiredString("Name"),
            testCase.GetInt32("Age"),
            testCase.GetDecimal("InitialBalance"),
            testCase.GetEnum<AccountStatus>("Status"),
            testCase.GetRequiredString("StoredPin"),
            testCase.GetEnum<AccountPrivilege>("Privilege"));

        Assert.AreEqual(
            testCase.GetRequiredString("ExpectedConcreteType"),
            account.GetType().Name,
            testCase.TestCaseId);
        AssertCommonProperties(testCase, account);
        AssertSubtypeProperties(testCase, account);
    }

    private static void AssertCommonProperties(ExcelTestCase testCase, Account account)
    {
        Assert.AreEqual(testCase.GetRequiredString("AccountNumber"), account.AccountNumber);
        Assert.AreEqual(testCase.GetRequiredString("Name"), account.Name);
        Assert.AreEqual(testCase.GetInt32("Age"), account.Age);
        Assert.AreEqual(testCase.GetEnum<AccountStatus>("Status"), account.Status);
        Assert.AreEqual(testCase.GetEnum<AccountPrivilege>("Privilege"), account.Privilege);
    }

    private static void AssertSubtypeProperties(ExcelTestCase testCase, Account account)
    {
        switch (account)
        {
            case SavingsAccount savings:
                Assert.AreEqual(testCase.GetDecimal("MinimumBalance"), savings.MinBalance);
                Assert.AreEqual(testCase.GetOptionalDouble("InterestRate"), savings.InterestRate);
                break;
            case CurrentAccount current:
                Assert.AreEqual(testCase.GetDecimal("OverdraftLimit"), current.OverdraftLimit);
                break;
            case SalaryAccount salary:
                Assert.AreEqual(testCase.GetRequiredString("EmployerName"), salary.EmployerName);
                Assert.AreEqual(0, salary.InactiveMonths);
                break;
            case FixedDepositAccount fixedDeposit:
                Assert.AreEqual(testCase.GetInt32("TenureMonths"), fixedDeposit.TenureMonths);
                Assert.AreEqual(testCase.GetOptionalDouble("InterestRate"), fixedDeposit.InterestRate);
                break;
        }
    }

    private static Account CreateAccount(ExcelTestCase testCase, AccountType accountType)
    {
        return AccountFactory.CreateAccount(
            accountType,
            testCase.GetRequiredString("AccountNumber"),
            testCase.GetRequiredString("Name"),
            testCase.GetInt32("Age"),
            testCase.GetDecimal("InitialBalance"),
            testCase.GetEnum<AccountStatus>("Status"),
            testCase.GetRequiredString("StoredPin"),
            testCase.GetEnum<AccountPrivilege>("Privilege"),
            testCase.GetOptionalDecimal("OverdraftLimit") ?? 25_000m,
            testCase.GetOptionalInt32("TenureMonths") ?? 12,
            testCase.GetOptionalDouble("InterestRate") ?? 6.5,
            testCase.GetOptionalDecimal("MinimumBalance") ?? 1_000m,
            testCase.GetOptionalString("EmployerName") ?? "TechCorp");
    }
}
