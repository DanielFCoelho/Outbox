using Microsoft.EntityFrameworkCore;
using Outbox.Domain;
using System.Transactions;

namespace DepositOrderCreation.Database
{
    public class DOCreationContext : DbContext
    {     
        public DOCreationContext(DbContextOptions<DOCreationContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<DepositOrder> DepositOrders { get; set; }        
    }
}
