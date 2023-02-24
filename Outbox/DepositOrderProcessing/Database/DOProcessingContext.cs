using DepositOrderProcessing.Domain;
using Microsoft.EntityFrameworkCore;

namespace DepositOrderProcessing.Database
{
    public class DOProcessingContext : DbContext
    {
        public DOProcessingContext(DbContextOptions<DOProcessingContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<DepositOrder>  DepositOrders { get; set; }
    }
}
