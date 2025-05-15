namespace CoreBanking.Api.Services
{
    public class CoreBankingServices(ILogger<CoreBankingServices> logger, CoreBankingDbContext dbContext)
    {
        public CoreBankingDbContext DbContext { get; } = dbContext;
        public ILogger<CoreBankingServices> Logger => logger;
    }
}
