namespace DistributedOrderSaga.Contracts.Models;

public record OrderItem(
    Guid ProductId, 
    int Quantity, 
    decimal UnitPrice)
{
    public decimal TotalPrice => Quantity * UnitPrice;
}