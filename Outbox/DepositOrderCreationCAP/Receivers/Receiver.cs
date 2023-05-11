using DepositOrderCreationCAP.Database;
using DepositOrderCreationCAP.Domain;
using DepositOrderCreationCAP.ViewModels;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace DepositOrderCreationCAP.Receivers
{
    public class Receiver : ICapSubscribe
    {
        private readonly DOCreationContext _context;
        //private readonly IServiceScopeFactory _serviceScopeFactory;

        public Receiver(DOCreationContext context)
        {
            _context = context;
            //_serviceScopeFactory = serviceScopeFactory;
        }


        [CapSubscribe("receiveMessage")]
        public async Task ReceivingAndProcessingDepositOrder(DepositOrderCreateEventViewModel message)
        {
            try
            {
                //var serviceProvider = _serviceScopeFactory.CreateScope().ServiceProvider;
                //var ctx = serviceProvider.GetService<DOCreationContext>();


                var depositOrder = await _context.DepositOrders.Include(k => k.Transactions).FirstOrDefaultAsync(k => k.Id == message.DepositOrderId);
                if (depositOrder == null)
                {
                    return;
                }

                depositOrder.Process();

                _context.DepositOrders.Update(depositOrder);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            
        }

        //[CapSubscribe("receiveCallBack")]
        public async Task Callback()
        {
            await Console.Out.WriteLineAsync("CALLBACK RECEIVED !!");
        }
    }
}
