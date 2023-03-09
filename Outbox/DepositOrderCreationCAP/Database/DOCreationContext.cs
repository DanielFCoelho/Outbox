using DepositOrderCreationCAP.Domain;
using Microsoft.EntityFrameworkCore;

namespace DepositOrderCreationCAP.Database
{
    public class DOCreationContext : DbContext
    {

        public DOCreationContext(DbContextOptions dbContextOptions) : base(dbContextOptions) { }

        public DbSet<DepositOrder> DepositOrders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DepositOrder>().ToTable("DepositOrders", "dbo").HasKey(k => k.Id);
            modelBuilder.Entity<DepositOrder>().Property(k => k.Id).HasColumnName("Id");
            modelBuilder.Entity<DepositOrder>().Property(k => k.Amount).HasColumnName("Amount");
            modelBuilder.Entity<DepositOrder>().Property(k => k.Status).HasColumnName("Status");
            modelBuilder.Entity<DepositOrder>().HasMany(d => d.Transactions).WithOne(k => k.DepositOrder);

            modelBuilder.Entity<Transaction>().ToTable("Transactions", "dbo").HasKey(k => k.Id);
            modelBuilder.Entity<Transaction>().Property(k => k.Id).HasColumnName("Id");
            modelBuilder.Entity<Transaction>().Property(k => k.Date).HasColumnName("Date");
            modelBuilder.Entity<Transaction>().Property(k => k.DepositOrderId).HasColumnName("DepositOrderId");
            modelBuilder.Entity<Transaction>().HasOne(d => d.DepositOrder).WithMany(k => k.Transactions);

        }
    }
}
