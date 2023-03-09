using DepositOrderCreation.Domain;
using Microsoft.EntityFrameworkCore;
using Outbox.Domain;

namespace DepositOrderCreation.Database
{
    public class DOCreationContext : DbContext
    {     
        public DOCreationContext(DbContextOptions<DOCreationContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<DepositOrder> DepositOrders { get; set; }
        public DbSet<OutboxEvent> IntegrationEventOutbox { get; set; }
    }
}
