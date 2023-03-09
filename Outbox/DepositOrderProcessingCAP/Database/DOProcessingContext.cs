using DepositOrderProcessingCAP.Domain;
using Microsoft.EntityFrameworkCore;

namespace DepositOrderCreation.Database
{
    public class DOProcessingContext : DbContext
    {
        public DOProcessingContext(DbContextOptions<DOProcessingContext> options) : base(options) { }

        public DbSet<DepositOrder> DepositOrders { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

    }
}
