using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoreBanking.Infrastructure.Data
{
    public class CoreBankingContextFactory : IDesignTimeDbContextFactory<CoreBankingDbContext>
    {
        public CoreBankingDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<CoreBankingDbContext>();
            optionsBuilder.UseNpgsql("Host=localhost;Port=53245;Username=postgres;Password=u4+)Z927P}B(.2ArZn{cP+;Database=corebanking");
            return new CoreBankingDbContext(optionsBuilder.Options);
        }
    }
}
