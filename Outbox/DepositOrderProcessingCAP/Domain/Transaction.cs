﻿namespace DepositOrderProcessingCAP.Domain;


public class Transaction
{
    public Guid Id { get; set; }
    public DateTimeOffset Date { get; set; }
    public Guid DepositOrderId { get; set; }
    public DepositOrder DepositOrder { get; set; }
}

