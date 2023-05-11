using Newtonsoft.Json;

namespace DepositOrderCreationCAP.Domain;

public class DepositOrder
{
    [JsonIgnore]
    public Guid Id { get; private set; }
    public decimal Amount { get; set; }
    [JsonIgnore]
    public DepositOrderStatus Status { get; set; }

    private List<Transaction> _transactions = new List<Transaction>();

    public IReadOnlyList<Transaction> Transactions
    {
        get { return _transactions.AsReadOnly(); }
    }

    public void GenerateNewId() => Id = Guid.NewGuid();

    public void Process()
    {
        Status = DepositOrderStatus.Processed;
        var transact = new Transaction(DateTimeOffset.UtcNow, Id);
        AddTransaction(transact);
    }

    private void AddTransaction(Transaction transaction)
    {
        _transactions.Add(transaction);
    }
}

public enum DepositOrderStatus
{
    Created, Processed
}