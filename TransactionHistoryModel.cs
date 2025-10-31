public class TransactionHistoryItem
{
    public long Id { get; set; }              
    public string? Date { get; set; }
    public string? ContractName { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
}

public class TransactionHistoryResponse
{
    public List<TransactionHistoryItem>? Items { get; set; }
}
