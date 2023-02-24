namespace Outbox.Domain;

public class DepositOrder
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public DepositOrderStatus Status { get; set; }
}

public enum DepositOrderStatus
{
    Created, Processed
}