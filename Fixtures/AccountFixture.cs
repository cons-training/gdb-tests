using GDB.App.Domain.Enums;
using GDB.App.Domain.Models;
using GDB.App.Tests.TestData;

namespace GDB.App.Tests.Fixtures;

public static class AccountFixture
{
    public static Account Create(ExcelTestCase testCase)
    {
        AccountType accountType = testCase.GetEnum<AccountType>("AccountType");
        decimal balance = testCase.GetDecimal("InitialBalance");
        AccountStatus status = testCase.GetEnum<AccountStatus>("Status");
        string pin = testCase.GetOptionalString("StoredPin") ?? "1234";
        string accountNumber = testCase.GetOptionalString("AccountNumber") ?? "1000000001";
        string name = testCase.GetOptionalString("Name") ?? "Test User";
        int age = testCase.GetOptionalInt32("Age") ?? 30;
        AccountPrivilege privilege =
            testCase.GetOptionalEnum<AccountPrivilege>("Privilege") ?? AccountPrivilege.Silver;

        return accountType switch
        {
            AccountType.Savings => new SavingsAccount(
                accountNumber, name, age, balance, accountType, status, pin, privilege,
                testCase.GetOptionalDecimal("MinimumBalance") ?? 1_000m,
                testCase.GetOptionalDouble("InterestRate") ?? 4.0),

            AccountType.Current => new CurrentAccount(
                accountNumber, name, age, balance, accountType, status, pin, privilege,
                testCase.GetOptionalDecimal("OverdraftLimit") ?? 25_000m),

            AccountType.Salary => new SalaryAccount(
                accountNumber, name, age, balance, accountType, status, pin, privilege,
                testCase.GetOptionalString("EmployerName") ?? "TechCorp"),

            AccountType.FixedDeposit => new FixedDepositAccount(
                accountNumber, name, age, balance, accountType, status, pin, privilege,
                testCase.GetOptionalInt32("TenureMonths") ?? 12,
                testCase.GetOptionalDouble("InterestRate") ?? 6.5),

            _ => throw new InvalidDataException(
                $"Unsupported fixture account type '{accountType}' in {testCase.TestCaseId}.")
        };
    }
}
