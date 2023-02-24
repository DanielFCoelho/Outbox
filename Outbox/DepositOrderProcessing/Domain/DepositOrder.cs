namespace DepositOrderProcessing.Domain;

public class DepositOrder
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public DepositOrderStatus MyProperty { get; set; }
	private List<Transaction> _transactions = new List<Transaction>();

	public IReadOnlyList<Transaction> Transactions
	{
		get { return _transactions.AsReadOnly(); }		
	}

	public void AddTransaction(IReadOnlyList<Transaction> transaction)
	{
		_transactions.AddRange(transaction);
	}
}

public enum DepositOrderStatus
{
    Created, Processed
}
