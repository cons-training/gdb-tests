using GDB.App.Application.Dtos;
using GDB.App.Application.Services.Implementations;
using GDB.App.Domain.Enums;
using GDB.App.Domain.Exceptions;
using GDB.App.Domain.Models;
using GDB.App.Infrastructure.Repositories.Contracts;

namespace GDB.App.Tests.Application.Services;

[TestClass]
public sealed class TransferTransactionCommandTests
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

    private static TransactionDto Transfer(string from, string to, decimal amount, string pin)
    {
        return new TransactionDto
        {
            FromAccount = from,
            ToAccount = to,
            Amount = amount,
            Pin = pin
        };
    }

    [TestMethod]
    public async Task ExecuteAsync_WithActiveAccountsAndCorrectPin_UpdatesBalancesAndSavesOnce()
    {
        var sender = Savings("ACC-SENDER", 5_000m, pin: "1234");
        var receiver = Savings("ACC-RECEIVER", 2_000m);
        var accounts = new FakeAccountRepository(sender, receiver);
        var tx = new FakeTransactionRepository();
        var command = new TransferTransactionCommand(accounts, tx);

        TranferFundsResponseDto response = await command.ExecuteAsync(
            Transfer("ACC-SENDER", "ACC-RECEIVER", 1_000m, "1234"));

        Assert.AreEqual(4_000m, sender.Balance);
        Assert.AreEqual(3_000m, receiver.Balance);
        Assert.AreEqual(4_000m, response.FromAccountBalance);
        Assert.AreEqual(3_000m, response.ToAccountBalance);
        Assert.AreEqual("ACC-SENDER", response.FromAccountNumber);
        Assert.AreEqual("ACC-RECEIVER", response.ToAccountNumber);
        Assert.AreEqual(1_000m, response.Amount);
        Assert.AreEqual(TransactionStatus.Success, response.TransactionStat);

        Assert.AreEqual(1, accounts.SaveAccountsCallCount);
        Assert.IsNotNull(accounts.Saved);
        Assert.AreSame(sender, accounts.Saved.Value.From);
        Assert.AreSame(receiver, accounts.Saved.Value.To);

        Assert.AreEqual(1, tx.SaveCallCount);
        Assert.IsNotNull(tx.Saved);
        (string from, string to, TransactionType type, decimal amount, TransactionStatus status,
            decimal balanceAfterFrom, decimal balanceAfterTo) = tx.Saved.Value;
        Assert.AreEqual("ACC-SENDER", from);
        Assert.AreEqual("ACC-RECEIVER", to);
        Assert.AreEqual(TransactionType.Transfer, type);
        Assert.AreEqual(1_000m, amount);
        Assert.AreEqual(TransactionStatus.Success, status);
        Assert.AreEqual(4_000m, balanceAfterFrom);
        Assert.AreEqual(3_000m, balanceAfterTo);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithMissingSender_ThrowsAccountExceptionAndDoesNotWrite()
    {
        var receiver = Savings("ACC-RECEIVER", 2_000m);
        var accounts = new FakeAccountRepository(receiver);
        var tx = new FakeTransactionRepository();
        var command = new TransferTransactionCommand(accounts, tx);

        var ex = await Assert.ThrowsExactlyAsync<AccountException>(() =>
            command.ExecuteAsync(Transfer("ACC-MISSING", "ACC-RECEIVER", 1_000m, "1234")));

        Assert.AreEqual("From account not found", ex.Message);
        Assert.AreEqual(0, accounts.SaveAccountsCallCount);
        Assert.AreEqual(0, tx.SaveCallCount);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithMissingReceiver_ThrowsAccountExceptionAndDoesNotWrite()
    {
        var sender = Savings("ACC-SENDER", 5_000m);
        var accounts = new FakeAccountRepository(sender);
        var tx = new FakeTransactionRepository();
        var command = new TransferTransactionCommand(accounts, tx);

        var ex = await Assert.ThrowsExactlyAsync<AccountException>(() =>
            command.ExecuteAsync(Transfer("ACC-SENDER", "ACC-MISSING", 1_000m, "1234")));

        Assert.AreEqual("To account not found", ex.Message);
        Assert.AreEqual(0, accounts.SaveAccountsCallCount);
        Assert.AreEqual(0, tx.SaveCallCount);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithInactiveSender_ThrowsInactiveAccountExceptionAndDoesNotWrite()
    {
        var sender = Savings("ACC-SENDER", 5_000m, status: AccountStatus.Inactive);
        var receiver = Savings("ACC-RECEIVER", 2_000m);
        var accounts = new FakeAccountRepository(sender, receiver);
        var tx = new FakeTransactionRepository();
        var command = new TransferTransactionCommand(accounts, tx);

        await Assert.ThrowsExactlyAsync<InactiveAccountException>(() =>
            command.ExecuteAsync(Transfer("ACC-SENDER", "ACC-RECEIVER", 1_000m, "1234")));

        Assert.AreEqual(0, accounts.SaveAccountsCallCount);
        Assert.AreEqual(0, tx.SaveCallCount);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithInactiveReceiver_ThrowsInactiveAccountExceptionAndDoesNotWrite()
    {
        var sender = Savings("ACC-SENDER", 5_000m);
        var receiver = Savings("ACC-RECEIVER", 2_000m, status: AccountStatus.Inactive);
        var accounts = new FakeAccountRepository(sender, receiver);
        var tx = new FakeTransactionRepository();
        var command = new TransferTransactionCommand(accounts, tx);

        await Assert.ThrowsExactlyAsync<InactiveAccountException>(() =>
            command.ExecuteAsync(Transfer("ACC-SENDER", "ACC-RECEIVER", 1_000m, "1234")));

        Assert.AreEqual(0, accounts.SaveAccountsCallCount);
        Assert.AreEqual(0, tx.SaveCallCount);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithIncorrectPin_ThrowsInvalidPinExceptionAndDoesNotWrite()
    {
        var sender = Savings("ACC-SENDER", 5_000m, pin: "1234");
        var receiver = Savings("ACC-RECEIVER", 2_000m);
        var accounts = new FakeAccountRepository(sender, receiver);
        var tx = new FakeTransactionRepository();
        var command = new TransferTransactionCommand(accounts, tx);

        await Assert.ThrowsExactlyAsync<InvalidPinException>(() =>
            command.ExecuteAsync(Transfer("ACC-SENDER", "ACC-RECEIVER", 1_000m, "9999")));

        Assert.AreEqual(5_000m, sender.Balance);
        Assert.AreEqual(2_000m, receiver.Balance);
        Assert.AreEqual(0, accounts.SaveAccountsCallCount);
        Assert.AreEqual(0, tx.SaveCallCount);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithNonPositiveAmount_ThrowsInvalidAmountExceptionAndDoesNotWrite()
    {
        var sender = Savings("ACC-SENDER", 5_000m);
        var receiver = Savings("ACC-RECEIVER", 2_000m);
        var accounts = new FakeAccountRepository(sender, receiver);
        var tx = new FakeTransactionRepository();
        var command = new TransferTransactionCommand(accounts, tx);

        await Assert.ThrowsExactlyAsync<InvalidAmountException>(() =>
            command.ExecuteAsync(Transfer("ACC-SENDER", "ACC-RECEIVER", 0m, "1234")));

        Assert.AreEqual(0, accounts.SaveAccountsCallCount);
        Assert.AreEqual(0, tx.SaveCallCount);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithAmountBreachingMinimumBalance_ThrowsMinimumBalanceViolationAndDoesNotWrite()
    {
        var sender = Savings("ACC-SENDER", 1_500m, pin: "1234", minBalance: 1_000m);
        var receiver = Savings("ACC-RECEIVER", 2_000m);
        var accounts = new FakeAccountRepository(sender, receiver);
        var tx = new FakeTransactionRepository();
        var command = new TransferTransactionCommand(accounts, tx);

        await Assert.ThrowsExactlyAsync<MinimumBalanceViolationException>(() =>
            command.ExecuteAsync(Transfer("ACC-SENDER", "ACC-RECEIVER", 1_000m, "1234")));

        Assert.AreEqual(0, accounts.SaveAccountsCallCount);
        Assert.AreEqual(0, tx.SaveCallCount);
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenSaveAccountsFails_DebitsSenderBeforeError()
    {
        var sender = Savings("ACC-SENDER", 5_000m, pin: "1234");
        var receiver = Savings("ACC-RECEIVER", 2_000m);
        var accounts = new FakeAccountRepository(sender, receiver)
        {
            SaveException = new InvalidOperationException("persistence down")
        };
        var tx = new FakeTransactionRepository();
        var command = new TransferTransactionCommand(accounts, tx);

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            command.ExecuteAsync(Transfer("ACC-SENDER", "ACC-RECEIVER", 1_000m, "1234")));

        Assert.AreEqual("persistence down", ex.Message);
        Assert.AreEqual(4_000m, sender.Balance, "sender is debited before persistence");
        Assert.AreEqual(3_000m, receiver.Balance);
        Assert.AreEqual(0, tx.SaveCallCount);
    }

    private sealed class FakeAccountRepository : IAccountRepository
    {
        private readonly Dictionary<string, IAccount> _accounts;

        public FakeAccountRepository(params IAccount[] accounts)
        {
            _accounts = accounts.ToDictionary(account => account.AccountNumber);
        }

        public int SaveAccountsCallCount { get; private set; }
        public (IAccount From, IAccount To)? Saved { get; private set; }
        public Exception? SaveException { get; set; }

        public Task<IAccount> GetAccountAsync(string accountNumber)
        {
            return Task.FromResult(
                _accounts.TryGetValue(accountNumber, out IAccount? account) ? account : null);
        }

        public void SaveAccounts(IAccount fromAccount, IAccount toAccount)
        {
            SaveAccountsCallCount++;
            Saved = (fromAccount, toAccount);
            if (SaveException is not null)
            {
                throw SaveException;
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

        public void UpdateBalance(string accountNumber, decimal balance)
        {
        }
    }

    private sealed class FakeTransactionRepository : ITransactionRepository
    {
        public int SaveCallCount { get; private set; }

        public (
            string From,
            string To,
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