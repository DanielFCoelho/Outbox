namespace DepositOrderProcessing.Domain
{
    public class Transaction
    {
        public Guid Id { get; set; }        
        public DateTimeOffset Date { get; set; }
        public DepositOrder DepositOrder { get; set; }
    }
}
