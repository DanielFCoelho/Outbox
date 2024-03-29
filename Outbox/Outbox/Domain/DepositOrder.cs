﻿using DepositOrderProcessing.Domain;

namespace Outbox.Domain;

public class DepositOrder
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public DepositOrderStatus Status { get; set; }

    private List<Transaction> _transactions = new List<Transaction>();

    public IReadOnlyList<Transaction> Transactions
    {
        get { return _transactions.AsReadOnly(); }
    }

    public void Process()
    {
        Status = DepositOrderStatus.Processed;
        _transactions.Add(new Transaction { Id = Guid.NewGuid(), Date = DateTimeOffset.UtcNow });
    }
}

public enum DepositOrderStatus
{
    Created, Processed
}