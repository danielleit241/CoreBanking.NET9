namespace CoreBanking.Api.Apis
{
    public static class CoreBankingApi
    {
        public static IEndpointRouteBuilder MapCoreBankingApi(this IEndpointRouteBuilder builder)
        {
            var vApi = builder.NewVersionedApi("CoreBanking");
            var v1 = vApi.MapGroup("api/v{version:apiVersion}/corebanking").HasApiVersion(1, 0);

            v1.MapGet("/customers", GetCustomers);
            v1.MapPost("/customers", CreateCustomer);

            v1.MapGet("/accounts", GetAccounts);
            v1.MapPost("/accounts", CreateAccount);
            v1.MapPut("/accounts/{id:guid}/deposit", Deposit);
            v1.MapPut("/accounts/{id:guid}/withdraw", Withdraw);
            v1.MapPut("/accounts/{id:guid}/transfer", Transfer);


            return builder;
        }

        private static async Task<Results<Ok, BadRequest>> Transfer
            ([AsParameters] CoreBankingServices services,
            Guid id,
            TransferRequest transfer)
        {
            if (id == Guid.Empty)
            {
                services.Logger.LogError("Account id is null or empty");
                return TypedResults.BadRequest();
            }

            if (string.IsNullOrEmpty(transfer.DestinationAccountNumber))
            {
                services.Logger.LogError("Destination account number cannot be empty");
                return TypedResults.BadRequest();
            }

            if (transfer.Amount <= 0)
            {
                services.Logger.LogError("Transfer amount is less than or equal to zero");
                return TypedResults.BadRequest();
            }

            var account = await services.DbContext.Accounts
                .Include(x => x.Transactions)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (account == null)
            {
                services.Logger.LogError("Account {Id} not found", id);
                return TypedResults.BadRequest();
            }

            if (account.Balance < transfer.Amount)
            {
                services.Logger.LogError("Insufficient funds");
                return TypedResults.BadRequest();
            }

            var destinationAccount = await services.DbContext.Accounts.FirstOrDefaultAsync(c => c.Number == transfer.DestinationAccountNumber);

            if (destinationAccount == null)
            {
                services.Logger.LogError("Destination account not found");
                return TypedResults.BadRequest();
            }

            account.Balance -= transfer.Amount;
            destinationAccount.Balance += transfer.Amount;

            try
            {
                var now = DateTime.UtcNow;

                services.DbContext.Transactions.Add(new Transaction
                {
                    Id = Guid.CreateVersion7(),
                    AccountId = account.Id,
                    Amount = transfer.Amount,
                    Type = TransactionTypes.Withdrawal,
                    DateUtc = now
                });

                services.DbContext.Transactions.Add(new Transaction
                {
                    Id = Guid.CreateVersion7(),
                    AccountId = destinationAccount.Id,
                    Amount = transfer.Amount,
                    Type = TransactionTypes.Deposit,
                    DateUtc = now
                });

                services.DbContext.Accounts.Update(account);
                services.DbContext.Accounts.Update(destinationAccount);

                await services.DbContext.SaveChangesAsync();

                services.Logger.LogInformation("Transferred {Amount} from account {SourceId} to {DestinationNumber}", transfer.Amount, account.Id, destinationAccount.Number);
                return TypedResults.Ok();
            }
            catch (Exception ex)
            {
                services.Logger.LogError(ex, "Error while saving transaction");
                return TypedResults.BadRequest();
            }
        }

        private static async Task<Results<Ok<Account>, BadRequest>> Withdraw
            ([AsParameters] CoreBankingServices services,
            Guid id,
            WithdrawalRequest withdrawal)
        {
            if (id == Guid.Empty)
            {
                services.Logger.LogError("Account id is null or empty");
                return TypedResults.BadRequest();
            }

            if (withdrawal.Amount <= 0)
            {
                services.Logger.LogError("Withdraw amount is less than or equal to zero");
                return TypedResults.BadRequest();
            }

            var account = await services.DbContext.Accounts
                .Include(x => x.Transactions)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (account == null)
            {
                services.Logger.LogError("Account {Id} not found", id);
                return TypedResults.BadRequest();
            }

            account.Balance -= withdrawal.Amount;
            if (account.Balance < 0)
            {
                services.Logger.LogError("Insufficient funds");
                return TypedResults.BadRequest();
            }

            try
            {
                services.DbContext.Transactions.Add(new Transaction
                {
                    Id = Guid.CreateVersion7(),
                    AccountId = account.Id,
                    Amount = withdrawal.Amount,
                    Type = TransactionTypes.Withdrawal,
                    DateUtc = DateTime.UtcNow
                });
                services.DbContext.Accounts.Update(account);
                await services.DbContext.SaveChangesAsync();

                services.Logger.LogInformation("Withdrawal {Amount} to account {Id}", withdrawal.Amount, account.Id);
                return TypedResults.Ok(account);
            }
            catch (Exception ex)
            {
                services.Logger.LogError(ex, "Error while saving transaction");
                return TypedResults.BadRequest();
            }
        }

        public static async Task<Results<Ok<Account>, BadRequest>> Deposit
            ([AsParameters] CoreBankingServices services,
            Guid id,
            DepositionRequest deposition)
        {
            if (id == Guid.Empty)
            {
                services.Logger.LogError("Account id is null or empty");
                return TypedResults.BadRequest();
            }

            if (deposition.Amount <= 0)
            {
                services.Logger.LogError("Deposit amount is less than or equal to zero");
                return TypedResults.BadRequest();
            }

            var account = await services.DbContext.Accounts
                .Include(x => x.Transactions)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (account == null)
            {
                services.Logger.LogError("Account {Id} not found", id);
                return TypedResults.BadRequest();
            }

            account.Balance += deposition.Amount;

            try
            {
                services.DbContext.Transactions.Add(new Transaction
                {
                    Id = Guid.CreateVersion7(),
                    AccountId = account.Id,
                    Amount = deposition.Amount,
                    Type = TransactionTypes.Deposit,
                    DateUtc = DateTime.UtcNow
                });
                services.DbContext.Accounts.Update(account);
                await services.DbContext.SaveChangesAsync();

                services.Logger.LogInformation("Deposit {Amount} to account {Id}", deposition.Amount, account.Id);
                return TypedResults.Ok(account);
            }
            catch (Exception ex)
            {
                services.Logger.LogError(ex, "Error while saving transaction");
                return TypedResults.BadRequest();
            }
        }

        #region Account
        public static async Task<Results<Ok<Account>, BadRequest>> CreateAccount
           ([AsParameters] CoreBankingServices services,
            Account account)
        {
            if (account.CustomerId == Guid.Empty)
            {
                services.Logger.LogError("Customer id is null or empty");
                return TypedResults.BadRequest();
            }

            account.Id = Guid.CreateVersion7();
            account.Balance = 0;
            account.Number = GenerateAccountNumber();

            services.DbContext.Accounts.Add(account);
            await services.DbContext.SaveChangesAsync();
            services.Logger.LogInformation("Account {Id} created", account.Id);
            return TypedResults.Ok(account);
        }

        private static string GenerateAccountNumber()
        {
            return DateTime.UtcNow.Ticks.ToString();
        }

        private static async Task<Ok<PaginationResponse<Account>>> GetAccounts
            ([AsParameters] PaginationRequest paginationRequest,
            [AsParameters] CoreBankingServices services,
            Guid? customerId = null)
        {
            IQueryable<Account> accounts = services.DbContext.Accounts;

            if (customerId.HasValue)
            {
                accounts = accounts.Where(x => x.CustomerId == customerId.Value);
            }

            return TypedResults.Ok(new PaginationResponse<Account>(
                paginationRequest.PageSize,
                paginationRequest.PageIndex,
                await accounts.CountAsync(),
                await accounts
                .OrderBy(x => x.Number)
                .Skip(paginationRequest.PageIndex * paginationRequest.PageSize)
                .Take(paginationRequest.PageSize)
                .ToListAsync()
                )
            );
        }
        #endregion

        #region Customer
        public static async Task<Results<Ok<Customer>, BadRequest>> CreateCustomer([AsParameters] CoreBankingServices services,
            Customer customer)
        {
            if (string.IsNullOrEmpty(customer.Name))
            {
                services.Logger.LogError("Customer name is null or empty");
                return TypedResults.BadRequest();
            }

            customer.Address ??= "";
            if (customer.Id == Guid.Empty)
                customer.Id = Guid.CreateVersion7();

            services.DbContext.Customers.Add(customer);
            await services.DbContext.SaveChangesAsync();
            services.Logger.LogInformation("Customer {Id} created", customer.Id);
            return TypedResults.Ok(customer);
        }

        private static async Task<Ok<PaginationResponse<Customer>>> GetCustomers
            ([AsParameters] PaginationRequest paginationRequest,
            [AsParameters] CoreBankingServices services)
        {
            return TypedResults.Ok(new PaginationResponse<Customer>(
                paginationRequest.PageSize,
                paginationRequest.PageIndex,
                await services.DbContext.Customers.CountAsync(),
                await services.DbContext.Customers
                .OrderBy(x => x.Name)
                .Skip(paginationRequest.PageIndex * paginationRequest.PageSize)
                .Take(paginationRequest.PageSize)
                .ToListAsync()
                )
            );
        }
        #endregion
    }
}

public class DepositionRequest
{
    public decimal Amount { get; set; }
}

public class WithdrawalRequest
{
    public decimal Amount { get; set; }
}

public class TransferRequest
{
    public decimal Amount { get; set; }
    public string DestinationAccountNumber { get; set; } = default!;
}