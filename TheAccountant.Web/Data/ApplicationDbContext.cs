using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TheAccountant.Models;
using TheAccountant.Web.Models;

namespace TheAccountant.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<RecurringPayment> RecurringPayments => Set<RecurringPayment>();

        public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();

        public DbSet<ImportRow> ImportRows => Set<ImportRow>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Account>()
                .HasOne(a => a.User)
                .WithMany(u => u.Accounts)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Account>()
                .Property(a => a.CurrentBalance)
                .HasPrecision(18, 2);

            builder.Entity<Account>()
                .Property(a => a.AvailableBalance)
                .HasPrecision(18, 2);

            builder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasPrecision(18, 2);

            builder.Entity<RecurringPayment>()
                .Property(r => r.Amount)
                .HasPrecision(18, 2);

            builder.Entity<ImportBatch>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ImportBatch>()
                .HasOne(b => b.Account)
                .WithMany()
                .HasForeignKey(b => b.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ImportRow>()
                .HasOne(r => r.ImportBatch)
                .WithMany(b => b.Rows)
                .HasForeignKey(r => r.ImportBatchId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ImportRow>()
                .Property(r => r.Amount)
                .HasPrecision(18, 2);

            builder.Entity<Transaction>()
                .HasOne(t => t.ImportBatch)
                .WithMany()
                .HasForeignKey(t => t.ImportBatchId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ImportRow>()
                .Property(r => r.Balance)
                .HasPrecision(18, 2);

        }
    }
}