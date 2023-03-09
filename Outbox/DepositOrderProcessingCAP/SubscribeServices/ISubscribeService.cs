using DepositOrderProcessingCAP.Domain;
using DotNetCore.CAP;

namespace DepositOrderProcessingCAP.SubscribeServices;

public interface ISubscribeService : ICapSubscribe
{
    [CapSubscribe("depositOrder.creation")]
    Task OnDepositOrderCreationSubscribe(DepositOrder depositOrder);
}
