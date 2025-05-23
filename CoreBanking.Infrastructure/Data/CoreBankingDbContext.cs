﻿using CoreBanking.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Infrastructure.Data
{
    public class CoreBankingDbContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; } = default!;
        public DbSet<Account> Accounts { get; set; } = default!;
        public DbSet<Transaction> Transactions { get; set; } = default!;
        public CoreBankingDbContext() { }
        public CoreBankingDbContext(DbContextOptions<CoreBankingDbContext> options) : base(options)
        {

        }
    }
}
