﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoreBanking.Infrastructure.Data
{
    public class CoreBankingContextFactory : IDesignTimeDbContextFactory<CoreBankingDbContext>
    {
        public CoreBankingDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<CoreBankingDbContext>();
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Username=postgres;Password=u4+)Z927P}B(.2ArZn{cP+;Database=corebanking");
            return new CoreBankingDbContext(optionsBuilder.Options);
        }
    }
}
