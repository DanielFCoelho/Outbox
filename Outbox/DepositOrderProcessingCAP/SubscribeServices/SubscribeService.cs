using DepositOrderCreation.Database;
using DepositOrderProcessingCAP.Domain;
using DotNetCore.CAP;
using Microsoft.EntityFrameworkCore;

namespace DepositOrderProcessingCAP.SubscribeServices;

public class SubscribeService : ISubscribeService
{
    private readonly DOProcessingContext _context;

    public SubscribeService(DOProcessingContext context)
    {
        this._context = context;
    }

    [CapSubscribe("depositOrder.creation")]
    public async Task OnDepositOrderCreationSubscribe(DepositOrder depositOrder)
    {
        depositOrder = await _context.DepositOrders.FirstOrDefaultAsync(k => k.Id == depositOrder.Id);
        depositOrder.Process();
        await _context.SaveChangesAsync();
    }
}
