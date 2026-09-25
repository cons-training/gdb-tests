using GDB.App.Application.Dtos;
using GDB.App.Application.Services.Implementations;
using GDB.App.Domain.Enums;
using GDB.App.Domain.Exceptions;
using GDB.App.Domain.Models;
using GDB.App.Infrastructure.Repositories.Contracts;

namespace GDB.App.Tests.Application.Services;

[TestClass]
public sealed class WithdrawTransactionCommandTests
{
    private static SavingsAccount Savings(
        string accountNumber,
        decimal balance,
        AccountStatus status = AccountStatus.Active,
        string pin = "1234",
        decimal minBalance = 1_000m)
    {
        return new SavingsAccount(
            accountNumber, "Test User", 30, balance, AccountType.Savings,
            status, pin, AccountPrivilege.Silver, minBalance);
    }

    private static TransactionDto Withdraw(string accountNumber, decimal amount, string pin)
    {
        return new TransactionDto
        {
            AccountNumber = accountNumber,
            Amount = amount,
            Pin = pin
        };
    }

    [TestMethod]
    public async Task ExecuteAsync_WithActiveAccountAndCorrectPin_UpdatesBalanceAndSavesOnce()
    {
        var account = Savings("ACC-WDT", 5_000m, pin: "1234");
        var accounts = new FakeAccountRepository(account);
        var tx = new FakeTransactionRepository();
        var command = new WithdrawTransactionCommand(accounts, tx);

        WithdrawResponseDto response = await command.ExecuteAsync(
            Withdraw("ACC-WDT", 1_000m, "1234"));

        Assert.AreEqual(4_000m, account.Balance);
        Assert.AreEqual(4_000m, response.Balance);
        Assert.AreEqual(TransactionStatus.Success, response.TransactionStat);

        Assert.AreEqual(1, accounts.UpdateBalanceCallCount);
        Assert.IsNotNull(accounts.LastUpdate);
        Assert.AreEqual("ACC-WDT", accounts.LastUpdate.Value.AccountNumber);
        Assert.AreEqual(4_000m, accounts.LastUpdate.Value.Balance);

        Assert.AreEqual(1, tx.SaveCallCount);
        Assert.IsNotNull(tx.Saved);
        (string? from, string? to, TransactionType type, decimal amount,
            TransactionStatus status, decimal balanceAfterFrom,
            decimal balanceAfterTo) = tx.Saved.Value;
        Assert.AreEqual("ACC-WDT", from);
        Assert.IsNull(to);
        Assert.AreEqual(TransactionType.Withdraw, type);
        Assert.AreEqual(1_000m, amount);
        Assert.AreEqual(TransactionStatus.Success, status);
        Assert.AreEqual(4_000m, balanceAfterFrom);
        Assert.AreEqual(0m, balanceAfterTo);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithMissingAccount_ThrowsAccountExceptionAndDoesNotWrite()
    {
        var accounts = new FakeAccountRepository();
        var tx = new FakeTransactionRepository();
        var command = new WithdrawTransactionCommand(accounts, tx);

        var ex = await Assert.ThrowsExactlyAsync<AccountException>(() =>
            command.ExecuteAsync(Withdraw("ACC-MISSING", 1_000m, "1234")));

        Assert.AreEqual("Account not found", ex.Message);
        Assert.AreEqual(0, accounts.UpdateBalanceCallCount);
        Assert.AreEqual(0, tx.SaveCallCount);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithInactiveAccount_ThrowsInactiveAccountExceptionAndDoesNotWrite()
    {
        var account = Savings("ACC-WDT", 5_000m, status: AccountStatus.Inactive);
        var accounts = new FakeAccountRepository(account);
        var tx = new FakeTransactionRepository();
        var command = new WithdrawTransactionCommand(accounts, tx);

        await Assert.ThrowsExactlyAsync<InactiveAccountException>(() =>
            command.ExecuteAsync(Withdraw("ACC-WDT", 1_000m, "1234")));

        Assert.AreEqual(5_000m, account.Balance);
        Assert.AreEqual(0, accounts.UpdateBalanceCallCount);
        Assert.AreEqual(0, tx.SaveCallCount);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithIncorrectPin_ThrowsInvalidPinExceptionAndDoesNotWrite()
    {
        var account = Savings("ACC-WDT", 5_000m, pin: "1234");
        var accounts = new FakeAccountRepository(account);
        var tx = new FakeTransactionRepository();
        var command = new WithdrawTransactionCommand(accounts, tx);

        await Assert.ThrowsExactlyAsync<InvalidPinException>(() =>
            command.ExecuteAsync(Withdraw("ACC-WDT", 1_000m, "9999")));

        Assert.AreEqual(5_000m, account.Balance);
        Assert.AreEqual(0, accounts.UpdateBalanceCallCount);
        Assert.AreEqual(0, tx.SaveCallCount);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithNonPositiveAmount_ThrowsInvalidAmountExceptionAndDoesNotWrite()
    {
        var account = Savings("ACC-WDT", 5_000m);
        var accounts = new FakeAccountRepository(account);
        var tx = new FakeTransactionRepository();
        var command = new WithdrawTransactionCommand(accounts, tx);

        await Assert.ThrowsExactlyAsync<InvalidAmountException>(() =>
            command.ExecuteAsync(Withdraw("ACC-WDT", 0m, "1234")));

        Assert.AreEqual(5_000m, account.Balance);
        Assert.AreEqual(0, accounts.UpdateBalanceCallCount);
        Assert.AreEqual(0, tx.SaveCallCount);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithAmountBreachingMinimumBalance_ThrowsMinimumBalanceViolationAndDoesNotWrite()
    {
        var account = Savings("ACC-WDT", 1_500m, pin: "1234", minBalance: 1_000m);
        var accounts = new FakeAccountRepository(account);
        var tx = new FakeTransactionRepository();
        var command = new WithdrawTransactionCommand(accounts, tx);

        await Assert.ThrowsExactlyAsync<MinimumBalanceViolationException>(() =>
            command.ExecuteAsync(Withdraw("ACC-WDT", 1_000m, "1234")));

        Assert.AreEqual(1_500m, account.Balance);
        Assert.AreEqual(0, accounts.UpdateBalanceCallCount);
        Assert.AreEqual(0, tx.SaveCallCount);
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenUpdateBalanceFails_DebitsBeforeError()
    {
        var account = Savings("ACC-WDT", 5_000m, pin: "1234");
        var accounts = new FakeAccountRepository(account)
        {
            UpdateException = new InvalidOperationException("persistence down")
        };
        var tx = new FakeTransactionRepository();
        var command = new WithdrawTransactionCommand(accounts, tx);

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            command.ExecuteAsync(Withdraw("ACC-WDT", 1_000m, "1234")));

        Assert.AreEqual("persistence down", ex.Message);
        Assert.AreEqual(4_000m, account.Balance, "account is debited before persistence");
        Assert.AreEqual(0, tx.SaveCallCount);
    }

    private sealed class FakeAccountRepository : IAccountRepository
    {
        private readonly Dictionary<string, IAccount> _accounts;

        public FakeAccountRepository(params IAccount[] accounts)
        {
            _accounts = accounts.ToDictionary(account => account.AccountNumber);
        }

        public int UpdateBalanceCallCount { get; private set; }
        public (string AccountNumber, decimal Balance)? LastUpdate { get; private set; }
        public Exception? UpdateException { get; set; }

        public Task<IAccount> GetAccountAsync(string accountNumber)
        {
            return Task.FromResult(
                _accounts.TryGetValue(accountNumber, out IAccount? account) ? account : null);
        }

        public void UpdateBalance(string accountNumber, decimal balance)
        {
            UpdateBalanceCallCount++;
            LastUpdate = (accountNumber, balance);
            if (UpdateException is not null)
            {
                throw UpdateException;
            }
        }

        public void CloseAccount(string accountNumber)
        {
        }

        public void SaveAccount(IAccount account, string pin)
        {
        }

        public List<IAccount> GetAllAccounts()
        {
            return _accounts.Values.ToList();
        }

        public void SaveAccounts(IAccount fromAccount, IAccount toAccount)
        {
        }
    }

    private sealed class FakeTransactionRepository : ITransactionRepository
    {
        public int SaveCallCount { get; private set; }

        public (
            string? From,
            string? To,
            TransactionType Type,
            decimal Amount,
            TransactionStatus Status,
            decimal BalanceAfterFrom,
            decimal BalanceAfterTo)? Saved { get; private set; }

        public List<ViewRecentTransactionsResponseDto> GetRecentTransactions(string accountNumber)
        {
            return new List<ViewRecentTransactionsResponseDto>();
        }

        public void SaveTransaction(
            string fromAccountNumber,
            string toAccountNumber,
            TransactionType transactionType,
            decimal amount,
            TransactionStatus transactionStatus,
            decimal balanceAfterFrom,
            decimal balanceAfterTo)
        {
            SaveCallCount++;
            Saved = (
                fromAccountNumber,
                toAccountNumber,
                transactionType,
                amount,
                transactionStatus,
                balanceAfterFrom,
                balanceAfterTo);
        }
    }
}